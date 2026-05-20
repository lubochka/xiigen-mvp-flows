using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.ReviewsReputation.Application;

public sealed class ReviewsReputationService : MicroserviceBase<ReviewsReputationService>
{
    private static readonly ConcurrentDictionary<string, byte> SubmissionLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> SecondaryMutationLocks = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public ReviewsReputationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<ReviewsReputationService> logger)
        : base(
            new ServiceDescriptor("FLOW-10", nameof(ReviewsReputationService), ReviewsReputationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<ReviewsReputationOutcome>> SubmitReviewAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = (ReadString(request, "reviewerId") ?? string.Empty).Trim();
        var targetEntityId = (ReadString(request, "targetEntityId") ?? string.Empty).Trim();
        var targetEntityType = (ReadString(request, "targetEntityType") ?? "profile").Trim();

        var eligible = IsEligibleForReview(request);
        if (!eligible)
        {
            return Success(new ReviewsReputationOutcome(
                StatusCodes.Forbidden,
                new Dictionary<string, object?>
                {
                    ["code"] = "ELIGIBILITY_FAILED",
                    ["reason"] = "A confirmed check-in or purchase is required before a review can be submitted."
                },
                false,
                false));
        }

        var validation = ValidateSubmission(request);
        if (validation is not null)
        {
            return Success(validation);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<ReviewsReputationOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                FieldDetails("X-Idempotency-Key", "required"));
        }

        var reviewId = DeriveReviewId(reviewerId, targetEntityId, targetEntityType);
        var operationKey = $"{ReviewsReputationConstants.FlowSlug}:submit:{CurrentTenant.TenantId}:{reviewId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<ReviewsReputationOutcome>(reservation.Error?.Code ?? "IDEMPOTENCY_FAILED", reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["reviewId"] = reviewId,
                ["retryAfterMs"] = 5000
            };
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.TimeoutRetainedEvent, reviewId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Review write fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new ReviewsReputationOutcome(StatusCodes.GatewayTimeout, timeoutBody, false, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:{reviewId}";
        var lockTaken = SubmissionLocks.TryAdd(lockKey, 0);
        if (!lockTaken)
        {
            return Failure<ReviewsReputationOutcome>("CONCURRENT_UPDATE", "Concurrent review submission detected", new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var auditBody = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["review_id"] = reviewId,
                ["reviewer_id"] = reviewerId,
                ["target_entity_id"] = targetEntityId,
                ["target_entity_type"] = targetEntityType,
                ["rating"] = ReadInt(request, "rating"),
                ["action"] = "SUBMITTED",
                ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            await _documents.UpsertAsync(ReviewsReputationConstants.ReviewAudit, $"audit-{reviewId}", auditBody, null, cancellationToken).ConfigureAwait(false);

            var reviewBody = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["review_id"] = reviewId,
                ["reviewer_id"] = reviewerId,
                ["target_entity_id"] = targetEntityId,
                ["target_entity_type"] = targetEntityType,
                ["rating"] = ReadInt(request, "rating")!.Value,
                ["review_text"] = ReadString(request, "reviewText") ?? string.Empty,
                ["status"] = "ACCEPTED",
                ["eligibility_source"] = ReadBool(request, "checkInConfirmed") ? "FLOW-04" : "FLOW-09",
                ["submitted_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(ReviewsReputationConstants.Reviews, reviewId, reviewBody, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<ReviewsReputationOutcome>(stored.Error?.Code ?? "REVIEW_WRITE_FAILED", stored.Error?.Message ?? "Review write failed", stored.Error?.Details);
            }

            var eventData = ReviewEventPayload(reviewId, reviewerId, targetEntityId, targetEntityType, ReadInt(request, "rating")!.Value);
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewAcceptedEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewSubmissionCompletedEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["id"] = reviewId,
                ["reviewId"] = reviewId,
                ["status"] = "ACCEPTED",
                ["targetEntityId"] = targetEntityId,
                ["targetEntityType"] = targetEntityType,
                ["version"] = stored.Data?.Version ?? "1",
                ["serverDerivedReviewId"] = true
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new ReviewsReputationOutcome(StatusCodes.Created, response, true, true));
        }
        finally
        {
            SubmissionLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<ReviewsReputationOutcome>> ModerateReviewAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var reviewId = (ReadString(request, "reviewId") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reviewId))
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.BadRequest, ValidationBody(Field("reviewId", "required")), false, false));
        }

        var operationKey = $"{ReviewsReputationConstants.FlowSlug}:moderate:{CurrentTenant.TenantId}:{reviewId}:{idempotencyKey ?? "no-key"}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var review = await _documents.GetAsync(ReviewsReputationConstants.Reviews, reviewId, cancellationToken).ConfigureAwait(false);
        if (review.IsFailure || review.Data is null || !IsTenantRecord(review.Data.Body))
        {
            return Failure<ReviewsReputationOutcome>("NOT_FOUND", "Review was not found");
        }

        var confidence = ReadDouble(request, "confidence") ?? 0.5d;
        var passThreshold = await ResolveDoubleAsync("flow10_moderation_pass_threshold", 0.85d, cancellationToken).ConfigureAwait(false);
        var rejectThreshold = await ResolveDoubleAsync("flow10_moderation_reject_threshold", 0.30d, cancellationToken).ConfigureAwait(false);
        var path = confidence >= passThreshold ? "PASS" : confidence < rejectThreshold ? "REJECT" : "UNCERTAIN";
        var status = path switch
        {
            "PASS" => "PUBLISHED",
            "REJECT" => "REJECTED",
            _ => "PENDING"
        };

        var updated = new Dictionary<string, object?>(review.Data.Body)
        {
            ["status"] = status,
            ["moderation_path"] = path,
            ["moderation_confidence"] = confidence,
            ["moderated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(ReviewsReputationConstants.Reviews, reviewId, updated, review.Data.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<ReviewsReputationOutcome>(stored.Error?.Code ?? "CONCURRENT_UPDATE", stored.Error?.Message ?? "Review changed during moderation", stored.Error?.Details);
        }

        var targetEntityId = ReadString(updated, "target_entity_id") ?? string.Empty;
        var targetEntityType = ReadString(updated, "target_entity_type") ?? "profile";
        var eventData = new Dictionary<string, object?>
        {
            ["reviewId"] = reviewId,
            ["targetEntityId"] = targetEntityId,
            ["targetEntityType"] = targetEntityType,
            ["moderationPath"] = path,
            ["confidence"] = confidence
        };

        if (path == "PASS")
        {
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewPublishedEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);
            await RecalculateReputationInternalAsync(targetEntityId, targetEntityType, cancellationToken).ConfigureAwait(false);
        }
        else if (path == "REJECT")
        {
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewRejectedEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _documents.UpsertAsync(ReviewsReputationConstants.ModerationQueue, $"moderation-{reviewId}", new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["review_id"] = reviewId,
                ["target_entity_id"] = targetEntityId,
                ["status"] = "PENDING",
                ["confidence"] = confidence,
                ["queued_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            }, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewFlaggedForHumanEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);
        }

        var response = new Dictionary<string, object?>
        {
            ["reviewId"] = reviewId,
            ["status"] = status,
            ["moderationPath"] = path,
            ["confidence"] = confidence
        };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new ReviewsReputationOutcome(StatusCodes.Ok, response, true, true));
    }

    public async Task<DataProcessResult<ReviewsReputationOutcome>> RecalculateReputationAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var targetEntityId = (ReadString(request, "targetEntityId") ?? string.Empty).Trim();
        var targetEntityType = (ReadString(request, "targetEntityType") ?? "profile").Trim();
        if (string.IsNullOrWhiteSpace(targetEntityId))
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.BadRequest, ValidationBody(Field("targetEntityId", "required")), false, false));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<ReviewsReputationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required", FieldDetails("X-Idempotency-Key", "required"));
        }

        var operationKey = $"{ReviewsReputationConstants.FlowSlug}:score:{CurrentTenant.TenantId}:{targetEntityId}:{targetEntityType}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var summary = await RecalculateReputationInternalAsync(targetEntityId, targetEntityType, cancellationToken).ConfigureAwait(false);
        if (summary.IsFailure || summary.Data is null)
        {
            return Failure<ReviewsReputationOutcome>(summary.Error?.Code ?? "REPUTATION_UPDATE_FAILED", summary.Error?.Message ?? "Reputation update failed");
        }

        var response = ReputationBody(summary.Data);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new ReviewsReputationOutcome(StatusCodes.Ok, response, true, true));
    }

    public async Task<DataProcessResult<ReviewsReputationOutcome>> RetractReviewAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var reviewId = (ReadString(request, "reviewId") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reviewId))
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.BadRequest, ValidationBody(Field("reviewId", "required")), false, false));
        }

        var operationKey = $"{ReviewsReputationConstants.FlowSlug}:retract:{CurrentTenant.TenantId}:{reviewId}:{idempotencyKey ?? "no-key"}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:retract:{reviewId}";
        if (!SecondaryMutationLocks.TryAdd(lockKey, 0))
        {
            return Failure<ReviewsReputationOutcome>("CONCURRENT_UPDATE", "Concurrent review retraction detected", new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var review = await _documents.GetAsync(ReviewsReputationConstants.Reviews, reviewId, cancellationToken).ConfigureAwait(false);
            if (review.IsFailure || review.Data is null || !IsTenantRecord(review.Data.Body))
            {
                return Failure<ReviewsReputationOutcome>("NOT_FOUND", "Review was not found");
            }

            var updated = new Dictionary<string, object?>(review.Data.Body)
            {
                ["status"] = "RETRACTED",
                ["retracted_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(ReviewsReputationConstants.Reviews, reviewId, updated, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<ReviewsReputationOutcome>(stored.Error?.Code ?? "CONCURRENT_UPDATE", stored.Error?.Message ?? "Review changed during retraction", stored.Error?.Details);
            }

            var targetEntityId = ReadString(updated, "target_entity_id") ?? string.Empty;
            var targetEntityType = ReadString(updated, "target_entity_type") ?? "profile";
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewRetractedEvent, reviewId, new Dictionary<string, object?>
            {
                ["reviewId"] = reviewId,
                ["targetEntityId"] = targetEntityId,
                ["targetEntityType"] = targetEntityType
            }), cancellationToken).ConfigureAwait(false);
            await RecalculateReputationInternalAsync(targetEntityId, targetEntityType, cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?> { ["reviewId"] = reviewId, ["status"] = "RETRACTED", ["version"] = stored.Data?.Version ?? "1" };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, response, true, true));
        }
        finally
        {
            SecondaryMutationLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<ReviewsReputationOutcome>> SubmitResponseAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var reviewId = (ReadString(request, "reviewId") ?? string.Empty).Trim();
        var ownerId = (ReadString(request, "ownerId") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reviewId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.BadRequest, ValidationBody(Field("reviewId", "required"), Field("ownerId", "required")), false, false));
        }

        if (ReadBool(request, "ownerConfirmed") == false)
        {
            return Success(new ReviewsReputationOutcome(
                StatusCodes.Forbidden,
                new Dictionary<string, object?> { ["code"] = "NOT_OWNER", ["reviewId"] = reviewId, ["revisionAllowed"] = false },
                false,
                false));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<ReviewsReputationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required", FieldDetails("X-Idempotency-Key", "required"));
        }

        var operationKey = $"{ReviewsReputationConstants.FlowSlug}:response:{CurrentTenant.TenantId}:{reviewId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ReviewsReputationOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:response:{reviewId}";
        if (!SecondaryMutationLocks.TryAdd(lockKey, 0))
        {
            return Failure<ReviewsReputationOutcome>("CONCURRENT_UPDATE", "Concurrent owner response mutation detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var existing = await _documents.GetAsync(ReviewsReputationConstants.ReviewResponses, $"response-{reviewId}", cancellationToken).ConfigureAwait(false);
            var allowRevision = ReadBool(request, "allowRevision");
            if (existing.Data is not null && IsTenantRecord(existing.Data.Body) && !allowRevision)
            {
                var already = new Dictionary<string, object?> { ["code"] = "ALREADY_RESPONDED", ["reviewId"] = reviewId, ["revisionAllowed"] = false };
                await _idempotency.MarkSucceededAsync(operationKey, already, cancellationToken).ConfigureAwait(false);
                return Success(new ReviewsReputationOutcome(StatusCodes.Conflict, already, false, false));
            }

            if (request.ContainsKey("contentPolicyPassed") && !ReadBool(request, "contentPolicyPassed"))
            {
                var rejected = new Dictionary<string, object?>
                {
                    ["code"] = "CONTENT_POLICY_REJECTED",
                    ["reviewId"] = reviewId,
                    ["revisionAllowed"] = true
                };
                await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewResponseRejectedEvent, reviewId, rejected), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, rejected, cancellationToken).ConfigureAwait(false);
                return Success(new ReviewsReputationOutcome(StatusCodes.UnprocessableEntity, rejected, false, true));
            }

            var responseText = ReadString(request, "responseText") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return Success(new ReviewsReputationOutcome(StatusCodes.BadRequest, ValidationBody(Field("responseText", "required")), false, false));
            }

            var audit = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["review_id"] = reviewId,
                ["owner_id"] = ownerId,
                ["action"] = allowRevision ? "REVISED" : "PUBLISHED",
                ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            await _documents.UpsertAsync(ReviewsReputationConstants.ResponseAudit, $"response-audit-{reviewId}", audit, null, cancellationToken).ConfigureAwait(false);

            var body = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["review_id"] = reviewId,
                ["owner_id"] = ownerId,
                ["response_text"] = responseText,
                ["status"] = "PUBLISHED",
                ["revision"] = allowRevision ? 2 : 1,
                ["published_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(ReviewsReputationConstants.ReviewResponses, $"response-{reviewId}", body, existing.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<ReviewsReputationOutcome>(stored.Error?.Code ?? "CONCURRENT_UPDATE", stored.Error?.Message ?? "Response changed during update", stored.Error?.Details);
            }

            var eventData = new Dictionary<string, object?>
            {
                ["reviewId"] = reviewId,
                ["ownerId"] = ownerId,
                ["status"] = "PUBLISHED"
            };
            await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReviewResponsePublishedEvent, reviewId, eventData), cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["reviewId"] = reviewId,
                ["status"] = "PUBLISHED",
                ["revision"] = body["revision"],
                ["version"] = stored.Data?.Version ?? "1"
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new ReviewsReputationOutcome(StatusCodes.Created, response, true, true));
        }
        finally
        {
            SecondaryMutationLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<ReputationSummary>> GetReputationAsync(
        string targetEntityId,
        string targetEntityType = "profile",
        CancellationToken cancellationToken = default)
    {
        var id = ReputationId(targetEntityId, targetEntityType);
        var score = await _documents.GetAsync(ReviewsReputationConstants.ReputationScores, id, cancellationToken).ConfigureAwait(false);
        if (score.IsFailure || score.Data is null || !IsTenantRecord(score.Data.Body))
        {
            return Success(new ReputationSummary(targetEntityId, targetEntityType, 1.0d, 0, "0"));
        }

        return Success(new ReputationSummary(
            targetEntityId,
            targetEntityType,
            ReadDouble(score.Data.Body, "score") ?? 1.0d,
            ReadInt(score.Data.Body, "published_review_count") ?? 0,
            score.Data.Version));
    }

    public async Task<DataProcessResult<ReviewCollection>> GetReviewsAsync(CancellationToken cancellationToken = default)
    {
        var reviews = await _documents.SearchAsync(ReviewsReputationConstants.Reviews, TenantFilter(), cancellationToken).ConfigureAwait(false);
        if (reviews.IsFailure)
        {
            return Failure<ReviewCollection>(reviews.Error?.Code ?? "REVIEW_READ_FAILED", reviews.Error?.Message ?? "Review read failed");
        }

        var rows = (reviews.Data ?? []).Select(row => WithoutPrivateReviewText(row.Body)).ToList();
        return Success(new ReviewCollection(rows, rows.Count));
    }

    public async Task<DataProcessResult<ModerationQueueSnapshot>> GetModerationQueueAsync(CancellationToken cancellationToken = default)
    {
        var queue = await _documents.SearchAsync(ReviewsReputationConstants.ModerationQueue, TenantFilter(), cancellationToken).ConfigureAwait(false);
        if (queue.IsFailure)
        {
            return Failure<ModerationQueueSnapshot>(queue.Error?.Code ?? "QUEUE_READ_FAILED", queue.Error?.Message ?? "Moderation queue read failed");
        }

        var rows = (queue.Data ?? []).Select(row => (IReadOnlyDictionary<string, object?>)row.Body).ToList();
        return Success(new ModerationQueueSnapshot(rows, rows.Count));
    }

    private async Task<DataProcessResult<ReputationSummary>> RecalculateReputationInternalAsync(
        string targetEntityId,
        string targetEntityType,
        CancellationToken cancellationToken)
    {
        var reviews = await _documents.SearchAsync(ReviewsReputationConstants.Reviews, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["target_entity_id"] = targetEntityId,
            ["target_entity_type"] = targetEntityType,
            ["status"] = "PUBLISHED"
        }, cancellationToken).ConfigureAwait(false);
        if (reviews.IsFailure)
        {
            return Failure<ReputationSummary>(reviews.Error?.Code ?? "REVIEW_READ_FAILED", reviews.Error?.Message ?? "Review read failed");
        }

        var ordered = (reviews.Data ?? [])
            .OrderByDescending(row => ReadDate(row.Body, "submitted_at"))
            .ToArray();
        var weights = new[] { 1.0d, 0.9d, 0.8d, 0.7d, 0.6d };
        var weightedTotal = 0d;
        var totalWeight = 0d;
        for (var i = 0; i < ordered.Length; i++)
        {
            var weight = i < weights.Length ? weights[i] : weights[^1];
            weightedTotal += (ReadInt(ordered[i].Body, "rating") ?? 1) * weight;
            totalWeight += weight;
        }

        var score = ordered.Length == 0 ? 1.0d : Math.Clamp(weightedTotal / totalWeight, 1.0d, 5.0d);
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["target_entity_id"] = targetEntityId,
            ["target_entity_type"] = targetEntityType,
            ["score"] = Math.Round(score, 2),
            ["published_review_count"] = ordered.Length,
            ["calculated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(ReviewsReputationConstants.ReputationScores, ReputationId(targetEntityId, targetEntityType), body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<ReputationSummary>(stored.Error?.Code ?? "REPUTATION_WRITE_FAILED", stored.Error?.Message ?? "Reputation write failed", stored.Error?.Details);
        }

        await _outbox.AppendAsync(CreateEvent(ReviewsReputationConstants.ReputationUpdatedEvent, ReputationId(targetEntityId, targetEntityType), new Dictionary<string, object?>
        {
            ["targetEntityId"] = targetEntityId,
            ["targetEntityType"] = targetEntityType,
            ["score"] = Math.Round(score, 2),
            ["publishedReviewCount"] = ordered.Length
        }), cancellationToken).ConfigureAwait(false);

        return Success(new ReputationSummary(targetEntityId, targetEntityType, Math.Round(score, 2), ordered.Length, stored.Data?.Version ?? "1"));
    }

    private async Task<double> ResolveDoubleAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var result = await _freedom.GetAsync($"{key}.{CurrentTenant.TenantId}", cancellationToken).ConfigureAwait(false);
        return result.IsSuccess &&
            result.Data?.Value is not null &&
            double.TryParse(Convert.ToString(result.Data.Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
    }

    private ReviewsReputationOutcome? ValidateSubmission(IReadOnlyDictionary<string, object?> request)
    {
        var details = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "reviewerId"))) details.Add(Field("reviewerId", "required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "targetEntityId"))) details.Add(Field("targetEntityId", "required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "targetEntityType"))) details.Add(Field("targetEntityType", "required"));
        var rating = ReadInt(request, "rating");
        if (!rating.HasValue) details.Add(Field("rating", "integer from 1 to 5 is required"));
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5)) details.Add(Field("rating", "must be between 1 and 5"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "reviewText"))) details.Add(Field("reviewText", "required"));

        return details.Count == 0
            ? null
            : new ReviewsReputationOutcome(StatusCodes.BadRequest, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false, false);
    }

    private static bool IsEligibleForReview(IReadOnlyDictionary<string, object?> request)
    {
        if (request.ContainsKey("eligibilityConfirmed") && ReadBool(request, "eligibilityConfirmed"))
        {
            return true;
        }

        if (request.ContainsKey("checkInConfirmed") && ReadBool(request, "checkInConfirmed"))
        {
            return true;
        }

        if (request.ContainsKey("purchaseConfirmed") && ReadBool(request, "purchaseConfirmed"))
        {
            return true;
        }

        return false;
    }

    private string DeriveReviewId(string reviewerId, string targetEntityId, string targetEntityType)
    {
        var input = $"{CurrentTenant.TenantId}:{reviewerId}:{targetEntityId}:{targetEntityType}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"review-{Convert.ToHexString(hash).ToLowerInvariant()[..24]}";
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{ReviewsReputationConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = ReviewsReputationConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private Dictionary<string, object?> ReviewEventPayload(
        string reviewId,
        string reviewerId,
        string targetEntityId,
        string targetEntityType,
        int rating) => new()
    {
        ["reviewId"] = reviewId,
        ["reviewerId"] = reviewerId,
        ["targetEntityId"] = targetEntityId,
        ["targetEntityType"] = targetEntityType,
        ["rating"] = rating
    };

    private Dictionary<string, object?> ReputationBody(ReputationSummary summary) => new()
    {
        ["targetEntityId"] = summary.TargetEntityId,
        ["targetEntityType"] = summary.TargetEntityType,
        ["score"] = summary.Score,
        ["publishedReviewCount"] = summary.PublishedReviewCount,
        ["version"] = summary.Version
    };

    private Dictionary<string, object?> TenantFilter() => new() { ["tenant_id"] = CurrentTenant.TenantId };

    private bool IsTenantRecord(IReadOnlyDictionary<string, object?> body) =>
        string.Equals(ReadString(body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal);

    private static string ReputationId(string targetEntityId, string targetEntityType) => $"reputation-{targetEntityType}-{targetEntityId}";

    private static IReadOnlyDictionary<string, object?> WithoutPrivateReviewText(IReadOnlyDictionary<string, object?> body)
    {
        var output = new Dictionary<string, object?>(body);
        output.Remove("review_text");
        return output;
    }

    private static Dictionary<string, object?> ValidationBody(params Dictionary<string, object?>[] fields) => new()
    {
        ["code"] = "VALIDATION_FAILED",
        ["details"] = fields
    };

    private static Dictionary<string, object?> FieldDetails(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };
    private static Dictionary<string, object?> Field(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };

    private static string? ReadString(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) && result;

    private static int? ReadInt(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static double? ReadDouble(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static DateTimeOffset ReadDate(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) &&
           value is not null &&
           DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;

    private static class StatusCodes
    {
        public const int Ok = 200;
        public const int Created = 201;
        public const int BadRequest = 400;
        public const int Forbidden = 403;
        public const int Conflict = 409;
        public const int UnprocessableEntity = 422;
        public const int GatewayTimeout = 504;
    }
}
