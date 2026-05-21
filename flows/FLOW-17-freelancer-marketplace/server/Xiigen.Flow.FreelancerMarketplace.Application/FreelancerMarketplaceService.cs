using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.FreelancerMarketplace.Application;

public sealed class FreelancerMarketplaceService : MicroserviceBase<FreelancerMarketplaceService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GigLockTtl = TimeSpan.FromMinutes(5);
    private static readonly string[] ImmutableContractFields = ["clientId", "gigId", "freelancerId"];
    private static readonly string[] ValidReviewDirections = ["CLIENT_TO_FREELANCER", "FREELANCER_TO_CLIENT"];

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gigLocks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _contractLocks = new(StringComparer.Ordinal);

    public FreelancerMarketplaceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<FreelancerMarketplaceService> logger)
        : base(
            new ServiceDescriptor("freelancer-marketplace-service", "Freelancer Marketplace", FreelancerMarketplaceConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<FreelancerMarketplaceOutcome>> AcceptGigAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "gigId", "bidId", "freelancerId");
        if (ReadInt(request, "budgetCents") <= 0)
        {
            validation.Add(FieldError("budgetCents", "Budget must be greater than zero."));
        }
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var gigId = ReadString(request, "gigId")!.Trim();
        var bidId = ReadString(request, "bidId")!.Trim();
        var freelancerId = ReadString(request, "freelancerId")!.Trim();
        var operationKey = $"{FreelancerMarketplaceConstants.FlowSlug}:accept:{CurrentTenant.TenantId}:{gigId}:{bidId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<FreelancerMarketplaceOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }
        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new FreelancerMarketplaceOutcome(200, reservation.Data.CachedResponse, true, false));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = ErrorBody("TIMEOUT", "Gig acceptance timed out before completion.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["gigId"] = gigId
            });
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.TimeoutRetainedEvent, gigId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Gig acceptance timed out", cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(504, timeoutBody, false, true));
        }

        var gate = _gigLocks.GetOrAdd($"{CurrentTenant.TenantId}:{gigId}", _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Concurrent gig acceptance detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var currentGig = await _documents.GetAsync(FreelancerMarketplaceConstants.GigPostings, gigId, cancellationToken).ConfigureAwait(false);
            if (currentGig.IsFailure)
            {
                return Failure<FreelancerMarketplaceOutcome>(currentGig.Error!.Code, currentGig.Error.Message, currentGig.Error.Details);
            }

            var gigBody = currentGig.Data?.Body ?? WithTenant(new Dictionary<string, object?>
            {
                ["gig_id"] = gigId,
                ["status"] = "OPEN",
                ["client_tenant_id"] = CurrentTenant.TenantId,
                ["budget_cents"] = ReadInt(request, "budgetCents")
            });

            if (!string.Equals(ReadString(gigBody, "client_tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BOLA_VIOLATION", "This gig belongs to a different tenant.");
                await StoreAuditAsync(FreelancerMarketplaceConstants.GigAudits, $"gig-forbidden:{gigId}:{Guid.NewGuid():N}", "GIG_ACCEPTANCE_FORBIDDEN", rejected, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.GigAcceptanceFailedEvent, gigId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new FreelancerMarketplaceOutcome(403, rejected, true, true));
            }

            if (!string.Equals(ReadString(gigBody, "status"), "OPEN", StringComparison.Ordinal))
            {
                var rejected = ErrorBody("GIG_NOT_OPEN", "Only open gigs can accept bids.");
                await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.GigAcceptanceFailedEvent, gigId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new FreelancerMarketplaceOutcome(409, rejected, false, true));
            }

            if (string.Equals(freelancerId, CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("SELF_ACCEPTANCE", "A client tenant cannot accept its own bid.");
                await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.GigAcceptanceFailedEvent, gigId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new FreelancerMarketplaceOutcome(409, rejected, false, true));
            }

            var lockId = $"gig-accept-lock:{gigId}";
            var existingLock = await _documents.GetAsync(FreelancerMarketplaceConstants.GigAcceptLocks, lockId, cancellationToken).ConfigureAwait(false);
            if (existingLock.Data is not null)
            {
                return Success(new FreelancerMarketplaceOutcome(409, ErrorBody("GIG_ALREADY_LOCKED", "This gig acceptance is already locked."), false, false));
            }

            await _documents.UpsertAsync(FreelancerMarketplaceConstants.GigAcceptLocks, lockId, WithTenant(new Dictionary<string, object?>
            {
                ["lock_key"] = lockId,
                ["gig_id"] = gigId,
                ["bid_id"] = bidId,
                ["locked_at"] = Now(),
                ["expires_at"] = DateTimeOffset.UtcNow.Add(GigLockTtl).ToString("O", CultureInfo.InvariantCulture),
                ["ttl_ms"] = GigLockTtl.TotalMilliseconds
            }), null, cancellationToken).ConfigureAwait(false);

            var bidRead = await _documents.GetAsync(FreelancerMarketplaceConstants.Bids, bidId, cancellationToken).ConfigureAwait(false);
            if (bidRead.IsFailure)
            {
                return Failure<FreelancerMarketplaceOutcome>(bidRead.Error!.Code, bidRead.Error.Message, bidRead.Error.Details);
            }
            var bidBody = bidRead.Data?.Body ?? WithTenant(new Dictionary<string, object?>
            {
                ["bid_id"] = bidId,
                ["gig_id"] = gigId,
                ["freelancer_id"] = freelancerId,
                ["status"] = "OPEN"
            });
            if (!string.Equals(ReadString(bidBody, "status"), "OPEN", StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BID_NOT_OPEN", "Only open bids can be accepted.");
                await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.GigAcceptanceFailedEvent, gigId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new FreelancerMarketplaceOutcome(409, rejected, false, true));
            }

            if (currentGig.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, currentGig.Data.Version, StringComparison.Ordinal))
            {
                return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Gig version does not match.", new Dictionary<string, object?> { ["currentVersion"] = currentGig.Data.Version });
            }

            var acceptedAt = Now();
            var storedGig = await _documents.UpsertAsync(FreelancerMarketplaceConstants.GigPostings, gigId, WithTenant(new Dictionary<string, object?>(gigBody)
            {
                ["status"] = "ACCEPTANCE_PENDING",
                ["accepted_bid_id"] = bidId,
                ["freelancer_id"] = freelancerId,
                ["updated_at"] = acceptedAt
            }), currentGig.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (storedGig.IsFailure)
            {
                return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Concurrent gig acceptance detected.", new Dictionary<string, object?> { ["currentVersion"] = currentGig.Data?.Version ?? "missing" });
            }

            var response = WithTenant(new Dictionary<string, object?>
            {
                ["id"] = gigId,
                ["gigId"] = gigId,
                ["bidId"] = bidId,
                ["freelancerId"] = freelancerId,
                ["status"] = "ACCEPTED",
                ["version"] = storedGig.Data!.Version,
                ["acceptedAt"] = acceptedAt
            });
            await StoreAuditAsync(FreelancerMarketplaceConstants.GigAudits, $"gig-accepted:{gigId}:{Guid.NewGuid():N}", "GIG_ACCEPTED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.GigAcceptedEvent, gigId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<FreelancerMarketplaceOutcome>> UpdateContractAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "contractId");
        var milestones = ReadMilestones(request);
        if (milestones.Count == 0)
        {
            validation.Add(FieldError("milestones", "At least one milestone is required."));
        }
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var contractId = ReadString(request, "contractId")!.Trim();
        var operationKey = $"{FreelancerMarketplaceConstants.FlowSlug}:contract:{CurrentTenant.TenantId}:{contractId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var fields = ReadStringArray(request, "fieldsToUpdate");
        var immutable = fields.FirstOrDefault(field => ImmutableContractFields.Contains(field, StringComparer.Ordinal));
        if (immutable is not null)
        {
            var body = ErrorBody("FIELD_IMMUTABLE", $"{immutable} cannot be changed after contract creation.", new Dictionary<string, object?> { ["field"] = immutable });
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.ContractFieldImmutableEvent, contractId, body), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "FIELD_IMMUTABLE", "Immutable contract field", cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(409, body, false, true));
        }

        var total = ReadInt(request, "contractTotalCents");
        var sum = milestones.Sum(m => ReadInt(m, "amountCents"));
        if (total <= 0)
        {
            validation.Add(FieldError("contractTotalCents", "Contract total must be greater than zero."));
        }
        if (sum != total)
        {
            var body = ErrorBody("MILESTONE_SUM_MISMATCH", "Milestone amounts must equal contract total.", new Dictionary<string, object?> { ["expected"] = total, ["actual"] = sum });
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.MilestoneSumMismatchEvent, contractId, body), cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(400, body, false, true));
        }
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        var gate = _contractLocks.GetOrAdd($"{CurrentTenant.TenantId}:{contractId}", _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Concurrent contract update detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var current = await _documents.GetAsync(FreelancerMarketplaceConstants.Contracts, contractId, cancellationToken).ConfigureAwait(false);
            if (current.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Contract version does not match.", new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            var body = WithTenant(new Dictionary<string, object?>
            {
                ["contract_id"] = contractId,
                ["client_id"] = ReadString(request, "clientId") ?? CurrentTenant.TenantId,
                ["client_tenant_id"] = CurrentTenant.TenantId,
                ["gig_id"] = ReadString(request, "gigId") ?? $"gig-{contractId}",
                ["freelancer_id"] = ReadString(request, "freelancerId") ?? "freelancer-1",
                ["freelancer_tenant_id"] = ReadString(request, "freelancerTenantId") ?? "freelancer-1",
                ["contract_total_cents"] = total,
                ["milestones"] = milestones,
                ["status"] = "ACTIVE",
                ["updated_at"] = Now()
            });
            var stored = await _documents.UpsertAsync(FreelancerMarketplaceConstants.Contracts, contractId, body, current.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<FreelancerMarketplaceOutcome>("CONCURRENT_UPDATE", "Concurrent contract update detected.", new Dictionary<string, object?> { ["currentVersion"] = current.Data?.Version ?? "missing" });
            }

            await StoreAuditAsync(FreelancerMarketplaceConstants.ContractAudits, $"contract:{contractId}:{Guid.NewGuid():N}", "MILESTONES_UPDATED", body, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["id"] = contractId,
                ["contractId"] = contractId,
                ["status"] = "ACTIVE",
                ["milestoneCount"] = milestones.Count,
                ["contractTotalCents"] = total,
                ["milestonesSumCents"] = sum,
                ["version"] = stored.Data!.Version
            };
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.FreelancerContractActivatedEvent, contractId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<FreelancerMarketplaceOutcome>> ReleaseMilestoneAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "milestoneId", "contractId", "deliveryId");
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        var milestoneId = ReadString(request, "milestoneId")!;
        var operationKey = $"{FreelancerMarketplaceConstants.FlowSlug}:release:{CurrentTenant.TenantId}:{milestoneId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var deliveryId = ReadString(request, "deliveryId")!;
        var delivery = await _documents.GetAsync(FreelancerMarketplaceConstants.Deliveries, deliveryId, cancellationToken).ConfigureAwait(false);
        var deliveryStatus = delivery.Data is null ? ReadString(request, "deliveryStatus") ?? "SUBMITTED" : ReadString(delivery.Data.Body, "status");
        if (!string.Equals(deliveryStatus, "SUBMITTED", StringComparison.Ordinal))
        {
            return Success(new FreelancerMarketplaceOutcome(409, ErrorBody("DELIVERY_NOT_SUBMITTED", "Delivery must be submitted before funds are released."), false, false));
        }

        if (ReadBool(request, "simulateReleaseWriteFailure"))
        {
            await StartLifoCompensationAsync(milestoneId, ReadString(request, "contractId")!, "RELEASE_FAILED", cancellationToken).ConfigureAwait(false);
            return Failure<FreelancerMarketplaceOutcome>("RELEASE_FAILED", "Milestone release write failed.");
        }

        var body = WithTenant(new Dictionary<string, object?>
        {
            ["milestone_id"] = milestoneId,
            ["contract_id"] = ReadString(request, "contractId"),
            ["delivery_id"] = deliveryId,
            ["status"] = "RELEASED",
            ["released_at"] = Now(),
            ["auto_release_days"] = await ResolveIntConfigAsync("milestone_auto_release_days", 14, cancellationToken).ConfigureAwait(false)
        });
        var stored = await _documents.UpsertAsync(FreelancerMarketplaceConstants.Milestones, milestoneId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            await StartLifoCompensationAsync(milestoneId, ReadString(request, "contractId")!, "RELEASE_FAILED", cancellationToken).ConfigureAwait(false);
            return Failure<FreelancerMarketplaceOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync(FreelancerMarketplaceConstants.EscrowAudits, $"release:{milestoneId}:{Guid.NewGuid():N}", "MILESTONE_RELEASED", body, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.MilestoneReleasedEvent, milestoneId, body), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["id"] = milestoneId, ["milestoneId"] = milestoneId, ["status"] = "RELEASED", ["version"] = stored.Data!.Version };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new FreelancerMarketplaceOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<FreelancerMarketplaceOutcome>> DisputeMilestoneAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "milestoneId", "contractId");
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        var milestoneId = ReadString(request, "milestoneId")!;
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["milestone_id"] = milestoneId,
            ["contract_id"] = ReadString(request, "contractId"),
            ["status"] = "DISPUTED",
            ["dispute_reason"] = ReadString(request, "reason") ?? "client_requested",
            ["disputed_at"] = Now()
        });
        var stored = await _documents.UpsertAsync(FreelancerMarketplaceConstants.Milestones, milestoneId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<FreelancerMarketplaceOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync(FreelancerMarketplaceConstants.EscrowAudits, $"dispute:{milestoneId}:{Guid.NewGuid():N}", "MILESTONE_DISPUTED", body, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.MilestoneDisputedEvent, milestoneId, body), cancellationToken).ConfigureAwait(false);
        return Success(new FreelancerMarketplaceOutcome(200, new Dictionary<string, object?> { ["id"] = milestoneId, ["milestoneId"] = milestoneId, ["status"] = "DISPUTED" }, true, true));
    }

    public async Task<DataProcessResult<FreelancerMarketplaceOutcome>> SubmitReviewAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "engagementId", "direction", "reviewerId", "targetId");
        var rating = ReadInt(request, "rating");
        if (rating is < 1 or > 5)
        {
            validation.Add(FieldError("rating", "Rating must be between 1 and 5."));
        }
        if (validation.Count > 0)
        {
            return Success(new FreelancerMarketplaceOutcome(400, ValidationBody(validation), false, false));
        }

        var engagementId = ReadString(request, "engagementId")!;
        var direction = ReadString(request, "direction")!;
        if (!ValidReviewDirections.Contains(direction, StringComparer.Ordinal))
        {
            return Success(new FreelancerMarketplaceOutcome(400, ErrorBody("INVALID_DIRECTION", "Review direction is not allowed."), false, false));
        }

        var operationKey = $"{FreelancerMarketplaceConstants.FlowSlug}:review:{CurrentTenant.TenantId}:{engagementId}:{direction}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var contract = await _documents.GetAsync(FreelancerMarketplaceConstants.Contracts, engagementId, cancellationToken).ConfigureAwait(false);
        if (contract.Data is not null)
        {
            var clientTenant = ReadString(contract.Data.Body, "client_tenant_id") ?? ReadString(contract.Data.Body, "client_id");
            var freelancerTenant = ReadString(contract.Data.Body, "freelancer_tenant_id") ?? ReadString(contract.Data.Body, "freelancer_id");
            if (!string.Equals(CurrentTenant.TenantId, clientTenant, StringComparison.Ordinal) && !string.Equals(CurrentTenant.TenantId, freelancerTenant, StringComparison.Ordinal))
            {
                return Success(new FreelancerMarketplaceOutcome(403, ErrorBody("NOT_CONTRACT_PARTICIPANT", "Reviewer is not a participant in this contract."), false, false));
            }
        }

        var duplicate = await _documents.SearchAsync(FreelancerMarketplaceConstants.Reviews, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["engagement_id"] = engagementId,
            ["direction"] = direction
        }, cancellationToken).ConfigureAwait(false);
        if ((duplicate.Data ?? []).Count > 0)
        {
            var rejected = ErrorBody("DUPLICATE_REVIEW", "Only one review per direction is allowed for an engagement.");
            await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.DuplicateReviewRejectedEvent, engagementId, rejected), cancellationToken).ConfigureAwait(false);
            return Success(new FreelancerMarketplaceOutcome(409, rejected, false, true));
        }

        var reviewSeed = $"{CurrentTenant.TenantId}:{engagementId}:{direction}:{ReadString(request, "reviewerId")}";
        var reviewId = $"review-{Hash(reviewSeed)[..16]}";
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["review_id"] = reviewId,
            ["engagement_id"] = engagementId,
            ["direction"] = direction,
            ["rating"] = rating,
            ["comment"] = ReadString(request, "comment") ?? string.Empty,
            ["reviewer_id"] = ReadString(request, "reviewerId"),
            ["target_id"] = ReadString(request, "targetId"),
            ["submitted_at"] = Now(),
            ["knowledge_scope"] = "PRIVATE"
        });
        var stored = await _documents.UpsertAsync(FreelancerMarketplaceConstants.Reviews, reviewId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<FreelancerMarketplaceOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreReviewAuditAsync(reviewId, body, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["id"] = reviewId, ["reviewId"] = reviewId, ["engagementId"] = engagementId, ["direction"] = direction, ["rating"] = rating, ["version"] = stored.Data!.Version };
        await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.FreelancerReviewSubmittedEvent, reviewId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new FreelancerMarketplaceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<FreelancerMarketplaceDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var gigs = await SearchTenantAsync(FreelancerMarketplaceConstants.GigPostings, cancellationToken).ConfigureAwait(false);
        var contracts = await SearchTenantAsync(FreelancerMarketplaceConstants.Contracts, cancellationToken).ConfigureAwait(false);
        var milestones = await SearchTenantAsync(FreelancerMarketplaceConstants.Milestones, cancellationToken).ConfigureAwait(false);
        var reviews = await SearchTenantAsync(FreelancerMarketplaceConstants.Reviews, cancellationToken).ConfigureAwait(false);
        var gigBodies = (gigs.Data ?? []).Select(record => record.Body).ToArray();
        var contractBodies = (contracts.Data ?? []).Select(record => record.Body).ToArray();
        var milestoneBodies = (milestones.Data ?? []).Select(record => record.Body).ToArray();
        var reviewBodies = (reviews.Data ?? []).Select(record => record.Body).ToArray();
        return Success(new FreelancerMarketplaceDashboard(
            gigBodies,
            contractBodies,
            milestoneBodies,
            reviewBodies,
            new Dictionary<string, object?>
            {
                ["acceptedGigs"] = gigBodies.Count(body => string.Equals(ReadString(body, "status"), "ACCEPTANCE_PENDING", StringComparison.Ordinal)),
                ["activeContracts"] = contractBodies.Length,
                ["releasedMilestones"] = milestoneBodies.Count(body => string.Equals(ReadString(body, "status"), "RELEASED", StringComparison.Ordinal)),
                ["submittedReviews"] = reviewBodies.Length
            }));
    }

    private async Task StartLifoCompensationAsync(string milestoneId, string contractId, string reason, CancellationToken cancellationToken)
    {
        foreach (var step in FreelancerMarketplaceConstants.EscrowCompensationStepNames)
        {
            await _documents.UpsertAsync(FreelancerMarketplaceConstants.CompensationSteps, $"comp:{milestoneId}:{step}", WithTenant(new Dictionary<string, object?>
            {
                ["milestone_id"] = milestoneId,
                ["contract_id"] = contractId,
                ["step"] = step,
                ["reason"] = reason,
                ["executed_at"] = Now()
            }), null, cancellationToken).ConfigureAwait(false);
        }
        await _outbox.AppendAsync(CreateEvent(FreelancerMarketplaceConstants.EscrowCompensationStartedEvent, milestoneId, new Dictionary<string, object?>
        {
            ["milestoneId"] = milestoneId,
            ["contractId"] = contractId,
            ["reason"] = reason,
            ["compensationSteps"] = FreelancerMarketplaceConstants.EscrowCompensationStepNames
        }), cancellationToken).ConfigureAwait(false);
    }

    private async Task<FreelancerMarketplaceOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new FreelancerMarketplaceOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new FreelancerMarketplaceOutcome(200, reservation.Data.CachedResponse, true, false)
            : null;
    }

    private async Task<int> ResolveIntConfigAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var doc = await _documents.GetAsync(FreelancerMarketplaceConstants.FreedomConfig, $"config:{CurrentTenant.TenantId}:{key}", cancellationToken).ConfigureAwait(false);
        _ = _freedom;
        return doc.IsSuccess && doc.Data is not null && ReadInt(doc.Data.Body, "value") > 0
            ? ReadInt(doc.Data.Body, "value")
            : fallback;
    }

    private async Task StoreAuditAsync(string collection, string id, string auditType, IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(collection, id, WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = Hash(JsonSerializer.Serialize(body)),
            ["created_at"] = Now()
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task StoreReviewAuditAsync(string reviewId, IReadOnlyDictionary<string, object?> review, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(FreelancerMarketplaceConstants.ReviewAudits, $"review-audit:{reviewId}:{Guid.NewGuid():N}", WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = "REVIEW_SUBMITTED",
            ["review_id"] = reviewId,
            ["engagement_id"] = ReadString(review, "engagement_id"),
            ["direction"] = ReadString(review, "direction"),
            ["rating"] = ReadInt(review, "rating"),
            ["reviewer_id"] = ReadString(review, "reviewer_id"),
            ["target_id"] = ReadString(review, "target_id"),
            ["comment_hash"] = Hash(ReadString(review, "comment") ?? string.Empty),
            ["created_at"] = Now(),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataProcessResult<IReadOnlyList<DocumentRecord>>> SearchTenantAsync(string collection, CancellationToken cancellationToken) =>
        await _documents.SearchAsync(collection, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);

    private IReadOnlyDictionary<string, object?> WithTenant(IReadOnlyDictionary<string, object?> body)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = CurrentTenant.TenantId
        };
        return copy;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            FreelancerMarketplaceConstants.FlowId,
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["tenantid"] = CurrentTenant.TenantId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId,
                ["xiigenflowid"] = FreelancerMarketplaceConstants.FlowId
            });

    private static List<IReadOnlyDictionary<string, object?>> ReadMilestones(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("milestones", out var value) || value is null)
        {
            return [];
        }
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> direct)
        {
            return direct.ToList();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => item.EnumerateObject().ToDictionary(prop => prop.Name, prop => (object?)ReadJsonValue(prop.Value))).ToList<IReadOnlyDictionary<string, object?>>();
        }
        return [];
    }

    private static object? ReadJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.ToString()
    };

    private static string[] ReadStringArray(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }
        if (value is string[] array)
        {
            return array;
        }
        if (value is IEnumerable<string> strings)
        {
            return strings.ToArray();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(text => text.Length > 0).ToArray();
        }
        return [];
    }

    private static List<Dictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields)
    {
        var errors = new List<Dictionary<string, object?>>();
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(ReadString(request, field)))
            {
                errors.Add(FieldError(field, "Value is required."));
            }
        }
        return errors;
    }

    private static FreelancerMarketplaceOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new FreelancerMarketplaceOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static FreelancerMarketplaceOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key =>
            string.Equals(key, "tenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "tenant_id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "clientTenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "client_tenant_id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "masterTenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "master_tenant_id", StringComparison.OrdinalIgnoreCase));
        return forbidden is null
            ? null
            : new FreelancerMarketplaceOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
    }

    private static IReadOnlyDictionary<string, object?> ValidationBody(IEnumerable<Dictionary<string, object?>> details) =>
        new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details.ToArray() };

    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message, IReadOnlyDictionary<string, object?>? extra = null)
    {
        var body = new Dictionary<string, object?> { ["code"] = code, ["message"] = message };
        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                body[pair.Key] = pair.Value;
            }
        }
        return body;
    }

    private static Dictionary<string, object?> FieldError(string field, string reason) =>
        new() { ["field"] = field, ["reason"] = reason };

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }
        return value is JsonElement element ? element.ToString() : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => Convert.ToInt32(longValue, CultureInfo.InvariantCulture),
            decimal decimalValue => Convert.ToInt32(decimalValue, CultureInfo.InvariantCulture),
            double doubleValue => Convert.ToInt32(doubleValue, CultureInfo.InvariantCulture),
            JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var parsed) => parsed,
            JsonElement element when element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && (value is bool boolValue && boolValue || value is JsonElement { ValueKind: JsonValueKind.True });
}
