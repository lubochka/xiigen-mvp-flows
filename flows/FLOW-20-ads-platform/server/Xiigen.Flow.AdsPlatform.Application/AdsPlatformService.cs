using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.AdsPlatform.Application;

public sealed class AdsPlatformService : MicroserviceBase<AdsPlatformService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] ForbiddenPayloadKeys =
    [
        "tenantId",
        "tenant_id",
        "flowTenantId",
        "flow_tenant_id",
        "ownerTenantId",
        "owner_tenant_id",
        "masterTenantId",
        "master_tenant_id"
    ];

    private static readonly string[] SensitivePaymentKeys =
    [
        "pan",
        "cardNumber",
        "card_number",
        "paymentCardNumber",
        "cvv",
        "cvc",
        "bankAccountNumber",
        "bank_account_number"
    ];

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new(StringComparer.Ordinal);

    public AdsPlatformService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<AdsPlatformService> logger)
        : base(
            new ServiceDescriptor("ads-platform-service", "Ads Platform", AdsPlatformConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<AdsPlatformOutcome>> ProcessAdRequestAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "requestId", "userId", "advertiserId", "auctionId", "bidId", "adId", "paymentMethodToken");
        ValidatePositiveLong(request, "bidAmountCents", validation);
        ValidatePositiveLong(request, "budgetRemainingCents", validation);
        if (ContainsSensitivePaymentData(request, out var sensitiveField))
        {
            validation.Add(FieldError(sensitiveField, "Raw payment account data is forbidden. Use a tokenized payment reference."));
        }
        if (validation.Count > 0)
        {
            return Success(new AdsPlatformOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var requestId = ReadString(request, "requestId")!.Trim();
        var userId = ReadString(request, "userId")!.Trim();
        var advertiserId = ReadString(request, "advertiserId")!.Trim();
        var auctionId = ReadString(request, "auctionId")!.Trim();
        var bidId = ReadString(request, "bidId")!.Trim();
        var adId = ReadString(request, "adId")!.Trim();
        var operationKey = $"{AdsPlatformConstants.FlowSlug}:request:{CurrentTenant.TenantId}:{requestId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = ErrorBody("TIMEOUT", "Ads platform fabric timed out before the request could commit.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["requestId"] = requestId
            });
            await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.RequestTimeoutRetainedEvent, requestId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Ads platform fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new AdsPlatformOutcome(504, timeoutBody, false, true));
        }

        var gate = GetResourceLock($"auction:{CurrentTenant.TenantId}:{auctionId}:{bidId}");
        if (!gate.Wait(0))
        {
            return Failure<AdsPlatformOutcome>("CONCURRENT_UPDATE", "Concurrent auction write detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var existingBid = await _documents.GetAsync(AdsPlatformConstants.AuctionBidLocks, bidId, cancellationToken).ConfigureAwait(false);
            if (existingBid.Data is not null && !string.Equals(ReadString(existingBid.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BOLA_VIOLATION", "Tenant does not own this auction bid.");
                await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.BidRejectedEvent, bidId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(403, rejected, true, true));
            }

            if (existingBid.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, existingBid.Data.Version, StringComparison.Ordinal))
            {
                return Failure<AdsPlatformOutcome>("CONCURRENT_UPDATE", "Auction bid version does not match.", new Dictionary<string, object?> { ["currentVersion"] = existingBid.Data.Version });
            }

            if (existingBid.Data is not null)
            {
                var duplicate = new Dictionary<string, object?>
                {
                    ["id"] = bidId,
                    ["bidId"] = bidId,
                    ["status"] = "BID_ALREADY_LOCKED",
                    ["version"] = existingBid.Data.Version
                };
                await _idempotency.MarkSucceededAsync(operationKey, duplicate, cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(200, duplicate, true, false));
            }

            var consent = await EvaluateConsentAsync(request, userId, cancellationToken).ConfigureAwait(false);
            if (!consent.Passed)
            {
                var failed = ErrorBody("CONSENT_GATE_FAILED", consent.Reason, consent.Evidence);
                await StoreAuditAsync(AdsPlatformConstants.AuctionAudits, $"consent:{requestId}", "CONSENT_GATE_FAILED", failed, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.ConsentGateFailedEvent, requestId, failed), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, "CONSENT_GATE_FAILED", consent.Reason, cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(403, failed, true, true));
            }

            var bidAmount = ReadLong(request, "bidAmountCents");
            var lockedAt = Now();
            var lockBody = WithTenant(new Dictionary<string, object?>
            {
                ["bid_id"] = bidId,
                ["auction_id"] = auctionId,
                ["request_id"] = requestId,
                ["ad_id"] = adId,
                ["advertiser_id"] = advertiserId,
                ["bid_amount_cents"] = bidAmount,
                ["status"] = "LOCKED",
                ["locked_at"] = lockedAt,
                ["setnx_equivalent"] = true
            });
            var lockWrite = await _documents.UpsertAsync(AdsPlatformConstants.AuctionBidLocks, bidId, lockBody, null, cancellationToken).ConfigureAwait(false);
            if (lockWrite.IsFailure)
            {
                return Failure<AdsPlatformOutcome>("CONCURRENT_UPDATE", "Auction bid lock could not be acquired.", lockWrite.Error?.Details);
            }

            var budget = await ReserveBudgetAsync(request, advertiserId, bidAmount, cancellationToken).ConfigureAwait(false);
            if (!budget.Accepted)
            {
                var rejected = ErrorBody("BUDGET_INSUFFICIENT", "Advertiser budget cannot cover this bid.", budget.Evidence);
                await StoreAuditAsync(AdsPlatformConstants.AuctionAudits, $"budget:{requestId}", "BID_REJECTED", rejected, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.BidRejectedEvent, bidId, rejected), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, "BUDGET_INSUFFICIENT", "Advertiser budget cannot cover this bid", cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(402, rejected, true, true));
            }

            var fraud = await EvaluateFraudAsync(request, bidAmount, cancellationToken).ConfigureAwait(false);
            if (!fraud.Passed)
            {
                await RestoreBudgetAsync(advertiserId, bidAmount, cancellationToken).ConfigureAwait(false);
                var fraudBody = ErrorBody("FRAUD_DETECTED", "Fraud score exceeded the pre-billing threshold.", fraud.Evidence);
                await StoreAuditAsync(AdsPlatformConstants.FraudAudits, $"fraud:{requestId}", "FRAUD_REJECTED_PRE_BILLING", fraudBody, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.FraudDetectedEvent, requestId, fraudBody), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, "FRAUD_DETECTED", "Fraud score exceeded the pre-billing threshold", cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(409, fraudBody, true, true));
            }

            await StoreAuditAsync(AdsPlatformConstants.FraudAudits, $"fraud-pass:{requestId}", "FRAUD_CHECK_PASSED", fraud.Evidence, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.FraudCheckPassedEvent, requestId, fraud.Evidence), cancellationToken).ConfigureAwait(false);

            var political = await EvaluatePoliticalContentAsync(request, cancellationToken).ConfigureAwait(false);
            if (string.Equals(political.Status, "PENDING_HUMAN_REVIEW", StringComparison.Ordinal))
            {
                var pending = new Dictionary<string, object?>(political.Evidence)
                {
                    ["id"] = requestId,
                    ["requestId"] = requestId,
                    ["bidId"] = bidId,
                    ["adId"] = adId,
                    ["status"] = political.Status
                };
                await _documents.UpsertAsync(AdsPlatformConstants.HumanReviewQueue, requestId, WithTenant(pending), null, cancellationToken).ConfigureAwait(false);
                await StoreAuditAsync(AdsPlatformConstants.PoliticalAudits, $"political:{requestId}", "HUMAN_REVIEW_REQUIRED", pending, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.PoliticalReviewPendingEvent, requestId, pending), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, pending, cancellationToken).ConfigureAwait(false);
                return Success(new AdsPlatformOutcome(202, pending, true, true));
            }

            var delivery = WithTenant(new Dictionary<string, object?>
            {
                ["id"] = requestId,
                ["request_id"] = requestId,
                ["bid_id"] = bidId,
                ["auction_id"] = auctionId,
                ["ad_id"] = adId,
                ["advertiser_id"] = advertiserId,
                ["user_id"] = userId,
                ["status"] = "DELIVERY_AUTHORIZED",
                ["bid_amount_cents"] = bidAmount,
                ["remaining_budget_cents"] = budget.RemainingBudgetCents,
                ["fraud_score"] = fraud.Score,
                ["political_status"] = political.Status,
                ["authorized_at"] = Now()
            });
            var stored = await _documents.UpsertAsync(AdsPlatformConstants.CampaignDeliveries, requestId, delivery, null, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["id"] = requestId,
                ["requestId"] = requestId,
                ["status"] = "DELIVERY_AUTHORIZED",
                ["version"] = stored.Data!.Version,
                ["remainingBudgetCents"] = budget.RemainingBudgetCents,
                ["fraudScore"] = fraud.Score,
                ["politicalStatus"] = political.Status
            };
            await StoreAuditAsync(AdsPlatformConstants.AuctionAudits, $"auction:{requestId}", "BID_ACCEPTED", response, cancellationToken).ConfigureAwait(false);
            await StoreAuditAsync(AdsPlatformConstants.PoliticalAudits, $"political-pass:{requestId}", "POLITICAL_REVIEW_APPROVED", political.Evidence, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.BidAcceptedEvent, bidId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.AdDeliveryAuthorizedEvent, requestId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.PoliticalContentApprovedEvent, requestId, political.Evidence), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new AdsPlatformOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<AdsPlatformOutcome>> RecordConsentAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "userId", "consentScope");
        if (validation.Count > 0)
        {
            return Success(new AdsPlatformOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var userId = ReadString(request, "userId")!.Trim();
        var operationKey = $"{AdsPlatformConstants.FlowSlug}:consent:{CurrentTenant.TenantId}:{userId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var expiresAt = ReadString(request, "consentExpiresAt") ??
            DateTimeOffset.UtcNow.AddDays(90).ToString("O", CultureInfo.InvariantCulture);
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["consent_scope"] = ReadString(request, "consentScope"),
            ["status"] = ReadBool(request, "revoked") ? "REVOKED" : "ACTIVE",
            ["expires_at"] = expiresAt,
            ["updated_at"] = Now()
        });
        var stored = await _documents.UpsertAsync(AdsPlatformConstants.ConsentRecords, userId, body, null, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["id"] = userId,
            ["userId"] = userId,
            ["status"] = body["status"],
            ["expiresAt"] = expiresAt,
            ["version"] = stored.Data!.Version
        };
        await StoreAuditAsync(AdsPlatformConstants.AuctionAudits, $"consent-update:{userId}:{Guid.NewGuid():N}", "CONSENT_UPDATED", response, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.ConsentUpdatedEvent, userId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new AdsPlatformOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<AdsPlatformOutcome>> ReviewPoliticalContentAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "requestId", "decision");
        if (validation.Count > 0)
        {
            return Success(new AdsPlatformOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var requestId = ReadString(request, "requestId")!.Trim();
        var decision = ReadString(request, "decision")!.Trim().ToUpperInvariant();
        var operationKey = $"{AdsPlatformConstants.FlowSlug}:political-review:{CurrentTenant.TenantId}:{requestId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var existing = await _documents.GetAsync(AdsPlatformConstants.HumanReviewQueue, requestId, cancellationToken).ConfigureAwait(false);
        if (existing.Data is null)
        {
            return Success(new AdsPlatformOutcome(404, ErrorBody("REVIEW_NOT_FOUND", "Political content review was not found."), false, false));
        }
        if (!string.Equals(ReadString(existing.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
        {
            return Success(new AdsPlatformOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this political review."), false, false));
        }

        var response = new Dictionary<string, object?>
        {
            ["id"] = requestId,
            ["requestId"] = requestId,
            ["status"] = decision is "APPROVE" or "APPROVED" ? "HUMAN_APPROVED" : "HUMAN_REJECTED",
            ["reviewedBy"] = CurrentTenant.UserId ?? "platform-reviewer",
            ["reviewedAt"] = Now()
        };
        await _documents.UpsertAsync(AdsPlatformConstants.HumanReviewQueue, requestId, WithTenant(response), existing.Data.Version, cancellationToken).ConfigureAwait(false);
        await StoreAuditAsync(AdsPlatformConstants.PoliticalAudits, $"political-decision:{requestId}", "HUMAN_DECISION_IMMUTABLE", response, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(AdsPlatformConstants.PoliticalContentApprovedEvent, requestId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new AdsPlatformOutcome(200, response, true, true));
    }

    public DataProcessResult<AdsPlatformOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var errors = new List<Dictionary<string, object?>>();
        if (steps.Count == 0)
        {
            errors.Add(FieldError("planSteps", "At least one plan step is required."));
        }

        foreach (var taskType in AdsPlatformConstants.RequiredPlanTaskTypes)
        {
            if (!steps.Any(step => string.Equals(step.TaskTypeId, taskType, StringComparison.Ordinal)))
            {
                errors.Add(FieldError("planSteps", $"Every FLOW-20 task type must have a plan step. Missing {taskType}."));
            }
        }

        foreach (var taskType in AdsPlatformConstants.EnforcementTaskTypes)
        {
            if (!steps.Any(step => string.Equals(step.TaskTypeId, taskType, StringComparison.Ordinal)))
            {
                errors.Add(FieldError("planSteps", $"Every enforcement task type must have a plan step. Missing {taskType}."));
            }
        }

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Responsibility))
            {
                errors.Add(FieldError("step.responsibility", "Each plan step must have one responsibility."));
            }
            if (step.Config.TryGetValue("providerSdkImport", out var providerImport) && IsTruthy(providerImport))
            {
                errors.Add(FieldError("step.config.providerSdkImport", "Plan steps must use fabric interfaces and cannot import provider SDKs."));
            }
            if (step.Config.TryGetValue("entitySpecificController", out var controller) && IsTruthy(controller))
            {
                errors.Add(FieldError("step.config.entitySpecificController", "Entity-specific controllers are forbidden."));
            }
            if (!string.Equals(ReadString(step.Config, "returns"), "DataProcessResult", StringComparison.Ordinal))
            {
                errors.Add(FieldError("step.config.returns", "Every step must return DataProcessResult."));
            }
        }

        return errors.Count > 0
            ? Success(new AdsPlatformOutcome(400, ValidationBody(errors), false, false))
            : Success(new AdsPlatformOutcome(200, new Dictionary<string, object?> { ["status"] = "PLAN_COVERED", ["stepCount"] = steps.Count }, false, false));
    }

    public async Task<DataProcessResult<AdsPlatformDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var deliveries = await SearchTenantAsync(AdsPlatformConstants.CampaignDeliveries, cancellationToken).ConfigureAwait(false);
        var consents = await SearchTenantAsync(AdsPlatformConstants.ConsentRecords, cancellationToken).ConfigureAwait(false);
        var fraud = await SearchTenantAsync(AdsPlatformConstants.FraudAudits, cancellationToken).ConfigureAwait(false);
        var political = await SearchTenantAsync(AdsPlatformConstants.PoliticalAudits, cancellationToken).ConfigureAwait(false);
        var deliveryBodies = (deliveries.Data ?? []).Select(record => record.Body).ToArray();
        var consentBodies = (consents.Data ?? []).Select(record => record.Body).ToArray();
        var fraudBodies = (fraud.Data ?? []).Select(record => record.Body).ToArray();
        var politicalBodies = (political.Data ?? []).Select(record => record.Body).ToArray();
        return Success(new AdsPlatformDashboard(
            deliveryBodies,
            consentBodies,
            fraudBodies,
            politicalBodies,
            new Dictionary<string, object?>
            {
                ["authorizedDeliveries"] = deliveryBodies.Length,
                ["activeConsents"] = consentBodies.Count(body => string.Equals(ReadString(body, "status"), "ACTIVE", StringComparison.Ordinal)),
                ["fraudAudits"] = fraudBodies.Length,
                ["politicalReviews"] = politicalBodies.Length
            }));
    }

    private async Task<ConsentGateResult> EvaluateConsentAsync(IReadOnlyDictionary<string, object?> request, string userId, CancellationToken cancellationToken)
    {
        var evidence = new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["order"] = 1,
            ["gate"] = "consent"
        };

        if (request.ContainsKey("adsConsent") || request.ContainsKey("consentGranted"))
        {
            var granted = ReadBool(request, "adsConsent") || ReadBool(request, "consentGranted");
            if (!granted)
            {
                return new ConsentGateResult(false, "Advertising consent is missing or revoked.", evidence);
            }

            if (ReadString(request, "consentExpiresAt") is { } expiresText &&
                DateTimeOffset.TryParse(expiresText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires) &&
                expires <= DateTimeOffset.UtcNow)
            {
                evidence["expiresAt"] = expiresText;
                return new ConsentGateResult(false, "Advertising consent is expired.", evidence);
            }

            evidence["source"] = "request";
            return new ConsentGateResult(true, "Advertising consent passed.", evidence);
        }

        var stored = await _documents.GetAsync(AdsPlatformConstants.ConsentRecords, userId, cancellationToken).ConfigureAwait(false);
        if (stored.Data is null ||
            !string.Equals(ReadString(stored.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal) ||
            !string.Equals(ReadString(stored.Data.Body, "status"), "ACTIVE", StringComparison.Ordinal))
        {
            return new ConsentGateResult(false, "Advertising consent is missing or revoked.", evidence);
        }

        if (DateTimeOffset.TryParse(ReadString(stored.Data.Body, "expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var storedExpires) &&
            storedExpires <= DateTimeOffset.UtcNow)
        {
            evidence["expiresAt"] = storedExpires.ToString("O", CultureInfo.InvariantCulture);
            return new ConsentGateResult(false, "Advertising consent is expired.", evidence);
        }

        evidence["source"] = "document";
        return new ConsentGateResult(true, "Advertising consent passed.", evidence);
    }

    private async Task<BudgetReservationResult> ReserveBudgetAsync(
        IReadOnlyDictionary<string, object?> request,
        string advertiserId,
        long bidAmount,
        CancellationToken cancellationToken)
    {
        var current = await _documents.GetAsync(AdsPlatformConstants.AdvertiserBudgets, advertiserId, cancellationToken).ConfigureAwait(false);
        var available = current.Data is null
            ? ReadLong(request, "budgetRemainingCents")
            : ReadLong(current.Data.Body, "remaining_budget_cents");
        var evidence = new Dictionary<string, object?>
        {
            ["advertiserId"] = advertiserId,
            ["bidAmountCents"] = bidAmount,
            ["budgetBeforeCents"] = available,
            ["budgetMutation"] = "DECRBY"
        };

        if (available < bidAmount)
        {
            evidence["budgetAfterCents"] = available;
            return new BudgetReservationResult(false, available, evidence);
        }

        var remaining = available - bidAmount;
        evidence["budgetAfterCents"] = remaining;
        await _documents.UpsertAsync(AdsPlatformConstants.AdvertiserBudgets, advertiserId, WithTenant(new Dictionary<string, object?>
        {
            ["advertiser_id"] = advertiserId,
            ["remaining_budget_cents"] = remaining,
            ["last_bid_cents"] = bidAmount,
            ["updated_at"] = Now()
        }), current.Data?.Version, cancellationToken).ConfigureAwait(false);
        return new BudgetReservationResult(true, remaining, evidence);
    }

    private async Task RestoreBudgetAsync(string advertiserId, long bidAmount, CancellationToken cancellationToken)
    {
        var current = await _documents.GetAsync(AdsPlatformConstants.AdvertiserBudgets, advertiserId, cancellationToken).ConfigureAwait(false);
        var existing = current.Data is null ? 0 : ReadLong(current.Data.Body, "remaining_budget_cents");
        await _documents.UpsertAsync(AdsPlatformConstants.AdvertiserBudgets, advertiserId, WithTenant(new Dictionary<string, object?>
        {
            ["advertiser_id"] = advertiserId,
            ["remaining_budget_cents"] = existing + bidAmount,
            ["last_restore_cents"] = bidAmount,
            ["restore_operation"] = "INCRBY",
            ["updated_at"] = Now()
        }), current.Data?.Version, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FraudReviewResult> EvaluateFraudAsync(IReadOnlyDictionary<string, object?> request, long bidAmount, CancellationToken cancellationToken)
    {
        var threshold = await ReadNumberFreedomAsync("flow20_fraud_score_threshold", 0.7, cancellationToken).ConfigureAwait(false);
        var score = ReadDouble(request, "fraudScore");
        var evidence = new Dictionary<string, object?>
        {
            ["score"] = score,
            ["threshold"] = threshold,
            ["order"] = 1,
            ["beforeBilling"] = true,
            ["bidAmountCents"] = bidAmount,
            ["rawPaymentDataPersisted"] = false
        };
        return new FraudReviewResult(score <= threshold, score, threshold, evidence);
    }

    private async Task<PoliticalReviewResult> EvaluatePoliticalContentAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken)
    {
        var threshold = await ReadNumberFreedomAsync("flow20_political_score_threshold", 0.65, cancellationToken).ConfigureAwait(false);
        var divergenceThreshold = await ReadNumberFreedomAsync("flow20_political_divergence_threshold", 0.25, cancellationToken).ConfigureAwait(false);
        var isPolitical = ReadBool(request, "isPoliticalContent");
        var modelA = ReadDouble(request, "politicalModelAScore");
        var modelB = ReadDouble(request, "politicalModelBScore");
        var minScore = Math.Min(modelA, modelB);
        var divergence = Math.Abs(modelA - modelB);
        var pending = isPolitical && (minScore >= threshold || divergence >= divergenceThreshold || ReadBool(request, "requiresHumanReview"));
        var evidence = new Dictionary<string, object?>
        {
            ["isPoliticalContent"] = isPolitical,
            ["modelA"] = modelA,
            ["modelB"] = modelB,
            ["minScore"] = minScore,
            ["divergence"] = divergence,
            ["threshold"] = threshold,
            ["divergenceThreshold"] = divergenceThreshold,
            ["usesMathMinConvergence"] = true,
            ["humanReviewMandatory"] = pending,
            ["ambiguousAutoBlocked"] = false
        };
        return new PoliticalReviewResult(pending ? "PENDING_HUMAN_REVIEW" : "APPROVED", minScore, divergence, evidence);
    }

    private SemaphoreSlim GetResourceLock(string key) => _resourceLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private async Task<AdsPlatformOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new AdsPlatformOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new AdsPlatformOutcome(200, reservation.Data.CachedResponse, true, false)
            : null;
    }

    private async Task StoreAuditAsync(string collection, string id, string auditType, IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(collection, id, WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = Hash(JsonSerializer.Serialize(body)),
            ["created_at"] = Now(),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<double> ReadNumberFreedomAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return value.Data?.Value switch
        {
            int i => i,
            long l => l,
            double d => d,
            decimal m => (double)m,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => fallback
        };
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
            AdsPlatformConstants.FlowId,
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
                ["xiigenflowid"] = AdsPlatformConstants.FlowId
            });

    private static List<AdsPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "planSteps").Select(item => new AdsPlanStep(
            ReadString(item, "id") ?? string.Empty,
            ReadString(item, "taskTypeId") ?? ReadString(item, "task_type_id") ?? string.Empty,
            ReadString(item, "name") ?? string.Empty,
            ReadString(item, "responsibility") ?? string.Empty,
            ReadDictionary(item, "config"))).ToList();

    private static List<IReadOnlyDictionary<string, object?>> ReadArray(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> direct)
        {
            return direct.ToList();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(ReadJsonObject).ToList<IReadOnlyDictionary<string, object?>>();
        }
        if (value is object[] objects)
        {
            return objects.OfType<IReadOnlyDictionary<string, object?>>().ToList();
        }
        return [];
    }

    private static IReadOnlyDictionary<string, object?> ReadJsonObject(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ReadJsonValue(prop.Value))
            : new Dictionary<string, object?>();

    private static IReadOnlyDictionary<string, object?> ReadDictionary(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>();
        }
        if (value is IReadOnlyDictionary<string, object?> direct)
        {
            return direct;
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return ReadJsonObject(element);
        }
        return new Dictionary<string, object?>();
    }

    private static object? ReadJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
        JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
        JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => ReadJsonObject(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ReadJsonValue).ToArray(),
        _ => value.ToString()
    };

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

    private static void ValidatePositiveLong(IReadOnlyDictionary<string, object?> request, string field, List<Dictionary<string, object?>> errors)
    {
        if (ReadLong(request, field) <= 0)
        {
            errors.Add(FieldError(field, "Value must be greater than zero."));
        }
    }

    private static AdsPlatformOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new AdsPlatformOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static AdsPlatformOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key => ForbiddenPayloadKeys.Any(forbiddenKey => string.Equals(key, forbiddenKey, StringComparison.OrdinalIgnoreCase)));
        return forbidden is null
            ? null
            : new AdsPlatformOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
    }

    private static bool ContainsSensitivePaymentData(IReadOnlyDictionary<string, object?> body, out string sensitiveField)
    {
        foreach (var pair in body)
        {
            if (SensitivePaymentKeys.Any(key => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)))
            {
                sensitiveField = pair.Key;
                return true;
            }

            if (pair.Value is IReadOnlyDictionary<string, object?> nested && ContainsSensitivePaymentData(nested, out sensitiveField))
            {
                return true;
            }

            if (pair.Value is JsonElement element && element.ValueKind == JsonValueKind.Object &&
                ContainsSensitivePaymentData(ReadJsonObject(element), out sensitiveField))
            {
                return true;
            }
        }

        sensitiveField = string.Empty;
        return false;
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

    private static long ReadLong(IReadOnlyDictionary<string, object?> body, string key)
    {
        var text = ReadString(body, key);
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> body, string key)
    {
        var text = ReadString(body, key);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && (value is bool boolValue && boolValue || value is JsonElement { ValueKind: JsonValueKind.True });

    private static bool IsTruthy(object? value) => value switch
    {
        true => true,
        string text => string.Equals(text, "true", StringComparison.OrdinalIgnoreCase),
        JsonElement { ValueKind: JsonValueKind.True } => true,
        _ => false
    };
}
