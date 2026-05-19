using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.EventManagement.Application;

public sealed class EventManagementService : MicroserviceBase<EventManagementService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightEvents = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> InFlightRegistrations = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public EventManagementService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<EventManagementService> logger)
        : base(
            new ServiceDescriptor("T59-T62", nameof(EventManagementService), EventManagementConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<EventManagementOutcome>> CreateEventAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventManagementOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var validation = ValidateCreateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var title = ReadString(request, "title")!.Trim();
        var organizerId = ReadString(request, "organizerId")!.Trim();
        var startDate = ReadString(request, "startDate")!.Trim();
        var capacity = ReadNullableInt(request, "capacity");
        var visibility = ReadBool(request, "isPrivate") ? "private" : "public";
        var isPaidEvent = ReadBool(request, "isPaidEvent");
        var promotionEnabled = ReadBool(request, "promotionEnabled");
        var resourceKey = $"{CurrentTenant.TenantId}:{organizerId}:{title.ToUpperInvariant()}";
        var operationKey = $"{EventManagementConstants.FlowSlug}:create:{CurrentTenant.TenantId}:{idempotencyKey}";

        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<EventManagementOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventManagementOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["title"] = title,
                ["retryAfterMs"] = 5000
            };
            var retained = await _outbox.AppendAsync(CreateEvent(EventManagementConstants.TimeoutRetainedEvent, title, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<EventManagementOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<EventManagementOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        if (!InFlightEvents.TryAdd(resourceKey, 1))
        {
            return Failure<EventManagementOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent event creation detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            if (isPaidEvent && !ReadBool(request, "hasPaymentConfig"))
            {
                await _idempotency.MarkFailedAsync(operationKey, "PAYMENT_CONFIG_MISSING", "Payment configuration required", cancellationToken).ConfigureAwait(false);
                return Failure<EventManagementOutcome>(
                    "PAYMENT_CONFIG_MISSING",
                    "Paid events require a payment configuration before publishing");
            }

            var maxEvents = (int)await GetFreedomNumberAsync("flow03_max_events_per_day", 5, cancellationToken).ConfigureAwait(false);
            var existingEvents = await _documents.SearchAsync(
                EventManagementConstants.Events,
                new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["organizer_id"] = organizerId },
                cancellationToken).ConfigureAwait(false);
            if (existingEvents.IsSuccess && (existingEvents.Data?.Count ?? 0) >= maxEvents)
            {
                await _idempotency.MarkFailedAsync(operationKey, "RATE_LIMIT_EXCEEDED", "Event creation limit reached", cancellationToken).ConfigureAwait(false);
                return Failure<EventManagementOutcome>("RATE_LIMIT_EXCEEDED", "Event creation limit reached for organizer");
            }

            var eventId = $"evt-{CurrentTenant.TenantId}-{Slugify(title)}";
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var criteria = ReadObject(request, "matchingCriteria");
            var audience = CalculateAudienceSegments(criteria);
            var notificationChannels = ResolveNotificationChannels(audience.TopScore, ReadBool(request, "pushEnabled"), ReadBool(request, "urgent"), ReadDouble(criteria, "distanceKm"));

            var eventBody = new Dictionary<string, object?>
            {
                ["id"] = eventId,
                ["event_id"] = eventId,
                ["title"] = title,
                ["organizer_id"] = organizerId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["start_date"] = startDate,
                ["capacity"] = capacity,
                ["capacity_state"] = capacity is null ? "unlimited" : capacity == 0 ? "closed" : "limited",
                ["is_private"] = visibility == "private",
                ["is_paid_event"] = isPaidEvent,
                ["matching_criteria"] = criteria,
                ["audience_segments"] = audience.Segments,
                ["top_audience_score"] = audience.TopScore,
                ["feed_placements"] = ResolveFeedPlacements(audience.TopScore),
                ["notification_channels"] = notificationChannels,
                ["knowledge_scope"] = visibility == "private" || isPaidEvent ? "PRIVATE" : "GLOBAL",
                ["connection_type"] = "FLOW_SCOPED",
                ["status"] = promotionEnabled ? "PUBLISHED" : "DRAFT",
                ["created_at"] = now
            };

            var stored = await _documents.UpsertAsync(EventManagementConstants.Events, eventId, eventBody, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                await _idempotency.MarkFailedAsync(operationKey, stored.Error?.Code ?? "EVENT_STORE_FAILED", stored.Error?.Message ?? "Failed to store event", cancellationToken).ConfigureAwait(false);
                return Failure<EventManagementOutcome>(
                    stored.Error?.Code ?? "EVENT_STORE_FAILED",
                    stored.Error?.Message ?? "Failed to store event",
                    stored.Error?.Details);
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = eventId,
                ["eventId"] = eventId,
                ["version"] = stored.Data!.Version,
                ["title"] = title,
                ["capacity"] = capacity,
                ["capacityLabel"] = CapacityLabel(capacity, 0),
                ["status"] = eventBody["status"],
                ["audienceSegments"] = audience.Segments,
                ["feedPlacements"] = eventBody["feed_placements"],
                ["notificationChannels"] = notificationChannels
            };

            await _outbox.AppendAsync(CreateEvent(EventManagementConstants.CreationRequestedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(EventManagementConstants.EventCreatedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(EventManagementConstants.EventPublishedEvent, eventId, response), cancellationToken).ConfigureAwait(false);

            await StoreAudienceMatchAsync(eventId, audience, now, cancellationToken).ConfigureAwait(false);
            await TrackBestEffortAsync(eventId, "creation", cancellationToken).ConfigureAwait(false);

            if (promotionEnabled)
            {
                var promotion = await PromoteEventAsync(
                    eventId,
                    new Dictionary<string, object?>
                    {
                        ["organizerId"] = organizerId,
                        ["contentSafetyStatus"] = ReadString(request, "contentSafetyStatus") ?? "pass",
                        ["audienceScore"] = audience.TopScore
                    },
                    cancellationToken).ConfigureAwait(false);
                response["promotion"] = promotion.Data?.Body;
            }

            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new EventManagementOutcome(201, response, true, true));
        }
        finally
        {
            InFlightEvents.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<EventManagementOutcome>> RegisterAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventManagementOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var attendeeId = ReadString(request, "attendeeId");
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(attendeeId))
        {
            return Failure<EventManagementOutcome>(
                "VALIDATION_FAILED",
                "Registration input validation failed",
                new Dictionary<string, object?>
                {
                    ["details"] = new[] { new Dictionary<string, object?> { ["field"] = string.IsNullOrWhiteSpace(eventId) ? "eventId" : "attendeeId", ["reason"] = "required" } }
                });
        }

        var operationKey = $"{EventManagementConstants.FlowSlug}:register:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventManagementOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var resourceKey = $"{CurrentTenant.TenantId}:{eventId}:capacity";
        if (!InFlightRegistrations.TryAdd(resourceKey, 1))
        {
            return Failure<EventManagementOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent capacity update detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var existing = await _documents.SearchAsync(
                EventManagementConstants.Registrations,
                new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId, ["attendee_id"] = attendeeId },
                cancellationToken).ConfigureAwait(false);
            if (existing.IsSuccess && (existing.Data?.Count ?? 0) > 0)
            {
                var current = existing.Data![0];
                var replayBody = new Dictionary<string, object?>
                {
                    ["registrationId"] = current.Id,
                    ["eventId"] = eventId,
                    ["attendeeId"] = attendeeId,
                    ["status"] = ReadString(current.Body, "status") ?? "CONFIRMED",
                    ["routed"] = ReadString(current.Body, "routed") ?? "CONFIRMED"
                };
                await _idempotency.MarkSucceededAsync(operationKey, replayBody, cancellationToken).ConfigureAwait(false);
                return Success(new EventManagementOutcome(200, replayBody, true, true));
            }

            var eventRecord = await _documents.GetAsync(EventManagementConstants.Events, eventId, cancellationToken).ConfigureAwait(false);
            if (eventRecord.IsFailure || eventRecord.Data is null)
            {
                return Failure<EventManagementOutcome>("EVENT_NOT_FOUND", "Event not found");
            }

            var capacity = ReadNullableInt(eventRecord.Data.Body, "capacity");
            var confirmed = await _documents.SearchAsync(
                EventManagementConstants.Registrations,
                new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId, ["status"] = "CONFIRMED" },
                cancellationToken).ConfigureAwait(false);
            var confirmedCount = confirmed.Data?.Count ?? 0;
            var routed = capacity is null
                ? "CONFIRMED"
                : capacity == 0 || confirmedCount >= capacity
                    ? "WAITLIST"
                    : "CONFIRMED";
            var status = routed == "WAITLIST" ? "WAITLISTED" : "CONFIRMED";
            var registrationId = $"reg-{CurrentTenant.TenantId}-{eventId}-{attendeeId}";
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var body = new Dictionary<string, object?>
            {
                ["id"] = registrationId,
                ["registration_id"] = registrationId,
                ["event_id"] = eventId,
                ["attendee_id"] = attendeeId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["status"] = status,
                ["routed"] = routed,
                ["join_timestamp"] = now,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now
            };

            var stored = await _documents.UpsertAsync(EventManagementConstants.Registrations, registrationId, body, expectedVersion: null, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<EventManagementOutcome>(stored.Error?.Code ?? "REGISTRATION_STORE_FAILED", stored.Error?.Message ?? "Failed to store registration", stored.Error?.Details);
            }

            var response = new Dictionary<string, object?>
            {
                ["registrationId"] = registrationId,
                ["eventId"] = eventId,
                ["attendeeId"] = attendeeId,
                ["status"] = status,
                ["routed"] = routed
            };
            await _outbox.AppendAsync(CreateEvent(EventManagementConstants.RegistrationRecordedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
            await TrackBestEffortAsync(eventId, "registration", cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new EventManagementOutcome(201, response, true, true));
        }
        finally
        {
            InFlightRegistrations.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<EventManagementOutcome>> PromoteEventAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var organizerId = ReadString(request, "organizerId") ?? "organizer-session";
        var safetyStatus = (ReadString(request, "contentSafetyStatus") ?? "pass").Trim().ToLowerInvariant();
        var audienceScore = ReadDouble(request, "audienceScore");
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return Failure<EventManagementOutcome>("VALIDATION_FAILED", "Promotion input validation failed");
        }

        await _outbox.AppendAsync(CreateEvent(EventManagementConstants.PromotionRequestedEvent, eventId, new Dictionary<string, object?> { ["eventId"] = eventId }), cancellationToken).ConfigureAwait(false);

        if (safetyStatus is "fail" or "blocked" or "rejected")
        {
            var rejected = new Dictionary<string, object?>
            {
                ["eventId"] = eventId,
                ["organizerId"] = organizerId,
                ["promoted"] = false,
                ["reason"] = "CONTENT_REJECTED"
            };
            await _outbox.AppendAsync(CreateEvent(EventManagementConstants.PromotionRejectedEvent, eventId, rejected), cancellationToken).ConfigureAwait(false);
            return Success(new EventManagementOutcome(200, rejected, false, true));
        }

        var channels = await GetFreedomStringListAsync("flow03_promotion_channels", ["in-app", "push"], cancellationToken).ConfigureAwait(false);
        var promotionId = $"promo-{CurrentTenant.TenantId}-{eventId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = promotionId,
            ["promotion_id"] = promotionId,
            ["event_id"] = eventId,
            ["organizer_id"] = organizerId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["channels"] = channels,
            ["feed_placements"] = ResolveFeedPlacements(audienceScore),
            ["safety_passed"] = true,
            ["knowledge_scope"] = "GLOBAL",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var stored = await _documents.UpsertAsync(EventManagementConstants.Promotions, promotionId, body, expectedVersion: null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EventManagementOutcome>(stored.Error?.Code ?? "PROMOTION_STORE_FAILED", stored.Error?.Message ?? "Failed to store promotion");
        }

        await _outbox.AppendAsync(CreateEvent(EventManagementConstants.PromotionPublishedEvent, eventId, body), cancellationToken).ConfigureAwait(false);
        await TrackBestEffortAsync(eventId, "promotion", cancellationToken).ConfigureAwait(false);
        return Success(new EventManagementOutcome(201, body, true, true));
    }

    public async Task<DataProcessResult<EventManagementReadModel>> GetEventAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var record = await _documents.GetAsync(EventManagementConstants.Events, eventId, cancellationToken).ConfigureAwait(false);
        if (record.IsFailure || record.Data is null)
        {
            return Failure<EventManagementReadModel>("EVENT_NOT_FOUND", "Event not found");
        }

        var registrations = await _documents.SearchAsync(
            EventManagementConstants.Registrations,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        var confirmed = registrations.Data?.Count(item => string.Equals(ReadString(item.Body, "status"), "CONFIRMED", StringComparison.Ordinal)) ?? 0;
        var waitlisted = registrations.Data?.Count(item => string.Equals(ReadString(item.Body, "status"), "WAITLISTED", StringComparison.Ordinal)) ?? 0;
        var capacity = ReadNullableInt(record.Data.Body, "capacity");

        return Success(new EventManagementReadModel(
            eventId,
            ReadString(record.Data.Body, "title") ?? string.Empty,
            ReadString(record.Data.Body, "organizer_id") ?? string.Empty,
            ReadString(record.Data.Body, "start_date") ?? string.Empty,
            capacity,
            CapacityLabel(capacity, confirmed),
            ReadString(record.Data.Body, "status") ?? "DRAFT",
            ReadStringArray(record.Data.Body, "audience_segments"),
            ReadStringArray(record.Data.Body, "notification_channels"),
            confirmed,
            waitlisted));
    }

    public async Task<DataProcessResult<IReadOnlyList<EventManagementReadModel>>> ListEventsAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId },
            cancellationToken).ConfigureAwait(false);
        if (records.IsFailure)
        {
            return Failure<IReadOnlyList<EventManagementReadModel>>("EVENT_LIST_FAILED", "Failed to list events");
        }

        var output = new List<EventManagementReadModel>();
        foreach (var record in records.Data ?? [])
        {
            var eventId = ReadString(record.Body, "event_id") ?? record.Id;
            var item = await GetEventAsync(eventId, cancellationToken).ConfigureAwait(false);
            if (item.IsSuccess && item.Data is not null)
            {
                output.Add(item.Data);
            }
        }

        return Success<IReadOnlyList<EventManagementReadModel>>(output);
    }

    public async Task<DataProcessResult<EventAnalyticsReadModel>> TrackAnalyticsAsync(
        string eventId,
        string analyticsType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await TrackBestEffortAsync(eventId, analyticsType, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return Success(new EventAnalyticsReadModel(eventId, string.Empty, analyticsType, 0, string.Empty, false));
        }
    }

    private async Task<DataProcessResult> StoreAudienceMatchAsync(
        string eventId,
        AudienceResolution audience,
        string now,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["id"] = $"audience-{CurrentTenant.TenantId}-{eventId}",
            ["event_id"] = eventId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["audience_segments"] = audience.Segments,
            ["top_score"] = audience.TopScore,
            ["fallback_used"] = audience.FallbackUsed,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = now
        };
        var stored = await _documents.UpsertAsync(EventManagementConstants.AudienceMatches, (string)body["id"]!, body, null, cancellationToken).ConfigureAwait(false);
        return stored.IsSuccess ? DataProcessResult.Success() : DataProcessResult.Failure(stored.Error!.Code, stored.Error.Message);
    }

    private async Task<DataProcessResult<EventAnalyticsReadModel>> TrackBestEffortAsync(
        string eventId,
        string analyticsType,
        CancellationToken cancellationToken)
    {
        try
        {
            var ttlSeconds = (int)await GetFreedomNumberAsync("flow03_analytics_counter_ttl_seconds", 86400, cancellationToken).ConfigureAwait(false);
            var threshold = (int)await GetFreedomNumberAsync("flow03_campaign_engagement_threshold", 1000, cancellationToken).ConfigureAwait(false);
            var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowStart = (nowEpoch / ttlSeconds) * ttlSeconds;
            var counterKey = $"{eventId}:{analyticsType}:{windowStart}";
            var existing = await _documents.GetAsync(EventManagementConstants.AnalyticsCounters, counterKey, cancellationToken).ConfigureAwait(false);
            var count = existing.Data is null ? 1 : ReadInt(existing.Data.Body, "count") + 1;
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(windowStart + ttlSeconds).ToString("O", CultureInfo.InvariantCulture);
            var body = new Dictionary<string, object?>
            {
                ["counter_key"] = counterKey,
                ["event_id"] = eventId,
                ["analytics_type"] = analyticsType,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["count"] = count,
                ["expires_at"] = expiresAt,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };

            await _documents.UpsertAsync(EventManagementConstants.AnalyticsCounters, counterKey, body, null, cancellationToken).ConfigureAwait(false);
            if (analyticsType == "campaign_engagement" && count >= threshold)
            {
                await _outbox.AppendAsync(CreateEvent(EventManagementConstants.PromotionCampaignCompletedEvent, eventId, body), cancellationToken).ConfigureAwait(false);
            }

            return Success(new EventAnalyticsReadModel(eventId, counterKey, analyticsType, count, expiresAt, true));
        }
        catch
        {
            return Success(new EventAnalyticsReadModel(eventId, string.Empty, analyticsType, 0, string.Empty, false));
        }
    }

    private DataProcessResult<EventManagementOutcome>? ValidateCreateRequest(IReadOnlyDictionary<string, object?> request)
    {
        var fieldErrors = new List<Dictionary<string, object?>>();
        var title = ReadString(request, "title");
        var organizerId = ReadString(request, "organizerId");
        var startDate = ReadString(request, "startDate");
        var capacity = ReadNullableInt(request, "capacity");

        if (string.IsNullOrWhiteSpace(title))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "title", ["reason"] = "required" });
        }

        if (string.IsNullOrWhiteSpace(organizerId))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "organizerId", ["reason"] = "required" });
        }

        if (string.IsNullOrWhiteSpace(startDate) || !DateTimeOffset.TryParse(startDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedStart))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "startDate", ["reason"] = "valid future date required" });
        }
        else if (parsedStart <= DateTimeOffset.UtcNow)
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "startDate", ["reason"] = "must be in the future" });
        }

        if (capacity < 0)
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "capacity", ["reason"] = "must be null, zero, or positive" });
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return Failure<EventManagementOutcome>(
            "VALIDATION_FAILED",
            "Event creation input validation failed",
            new Dictionary<string, object?> { ["details"] = fieldErrors });
    }

    private AudienceResolution CalculateAudienceSegments(IReadOnlyDictionary<string, object?> criteria)
    {
        var score =
            Weighted(criteria, "interestRelevance", 0.35) +
            Weighted(criteria, "locationProximity", 0.25) +
            Weighted(criteria, "timeAvailability", 0.20) +
            Weighted(criteria, "priceSensitivity", 0.10) +
            Weighted(criteria, "socialConnections", 0.10);

        if (score <= 0)
        {
            var fallback = new[]
            {
                $"region:{ReadString(criteria, "region") ?? "local"}",
                $"industry:{ReadString(criteria, "industry") ?? "general"}"
            };
            return new AudienceResolution(fallback, 0.31, true);
        }

        var strength = score >= 0.75 ? "strong" : score >= 0.50 ? "medium" : score >= 0.25 ? "weak" : "fallback";
        return new AudienceResolution([$"audience:{strength}", $"industry:{ReadString(criteria, "industry") ?? "general"}"], Math.Round(score, 2), false);
    }

    private static IReadOnlyList<string> ResolveFeedPlacements(double score)
    {
        if (score > 0.8)
        {
            return ["top-3", "top-10", "within-view"];
        }

        if (score > 0.6)
        {
            return ["top-10", "within-view"];
        }

        if (score > 0.4)
        {
            return ["within-view"];
        }

        return ["below-fold"];
    }

    private static IReadOnlyList<string> ResolveNotificationChannels(double score, bool pushEnabled, bool urgent, double distanceKm)
    {
        var channels = new List<string> { "in-app" };
        if (pushEnabled && score > 0.6)
        {
            channels.Add("push");
        }

        if (score > 0.4)
        {
            channels.Add("email-digest");
        }

        if (urgent && score > 0.8 && distanceKm <= 50)
        {
            channels.Add("sms");
        }

        return channels;
    }

    private async Task<double> GetFreedomNumberAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value.IsSuccess && value.Data?.Value is not null && double.TryParse(Convert.ToString(value.Data.Value, CultureInfo.InvariantCulture), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private async Task<IReadOnlyList<string>> GetFreedomStringListAsync(string key, IReadOnlyList<string> fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value.IsSuccess && value.Data?.Value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
        {
            return stringValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        return fallback;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            "xiigen://flow/event-management",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = EventManagementConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static string CapacityLabel(int? capacity, int registrations) =>
        capacity is null
            ? $"{registrations} registered (unlimited)"
            : capacity == 0
                ? "Closed"
                : $"{registrations} / {capacity}";

    private static string Slugify(string value) =>
        string.Join("-", value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace("/", "-", StringComparison.Ordinal)
            .Replace("\\", "-", StringComparison.Ordinal);

    private static double Weighted(IReadOnlyDictionary<string, object?> criteria, string key, double weight) =>
        ReadDouble(criteria, key) * weight;

    private static IReadOnlyDictionary<string, object?> ReadObject(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) && value is IReadOnlyDictionary<string, object?> dict
            ? dict
            : source.TryGetValue(key, out value) && value is Dictionary<string, object?> mutable
                ? mutable
                : new Dictionary<string, object?>();

    private static string? ReadString(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static int ReadInt(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : 0;

    private static int? ReadNullableInt(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?>? source, string key)
    {
        if (source is null || !source.TryGetValue(key, out var value))
        {
            return [];
        }

        return value switch
        {
            string[] strings => strings,
            IEnumerable<string> strings => strings.ToArray(),
            object?[] objects => objects.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToArray(),
            _ => []
        };
    }

    private sealed record AudienceResolution(
        IReadOnlyList<string> Segments,
        double TopScore,
        bool FallbackUsed);
}
