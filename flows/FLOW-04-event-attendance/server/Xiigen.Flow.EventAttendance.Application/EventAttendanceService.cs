using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.EventAttendance.Application;

public sealed class EventAttendanceService : MicroserviceBase<EventAttendanceService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightRsvps = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public EventAttendanceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<EventAttendanceService> logger)
        : base(
            new ServiceDescriptor("T63-T67", nameof(EventAttendanceService), EventAttendanceConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<EventAttendanceOutcome>> SubmitRsvpAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventAttendanceOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var validation = ValidateRsvpRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var eventId = ReadString(request, "eventId")!.Trim();
        var attendeeId = ReadString(request, "attendeeId")!.Trim();
        var operationKey = $"{EventAttendanceConstants.FlowSlug}:rsvp:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<EventAttendanceOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventAttendanceOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["eventId"] = eventId,
                ["attendeeId"] = attendeeId,
                ["retryAfterMs"] = 5000
            };
            var retained = await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.TimeoutRetainedEvent, eventId, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<EventAttendanceOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<EventAttendanceOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        if (ReadBool(request, "isPaidEvent") && !ReadBool(request, "paymentConfirmed"))
        {
            await _idempotency.MarkFailedAsync(operationKey, "PAYMENT_REQUIRED", "Paid RSVP requires payment.confirmed", cancellationToken).ConfigureAwait(false);
            return Failure<EventAttendanceOutcome>(
                "PAYMENT_REQUIRED",
                "Paid RSVP requires payment.confirmed before attendance can be confirmed");
        }

        var eventRecord = await EnsureEventSourceAsync(request, eventId, cancellationToken).ConfigureAwait(false);
        if (eventRecord.IsFailure || eventRecord.Data is null)
        {
            await _idempotency.MarkFailedAsync(operationKey, eventRecord.Error?.Code ?? "EVENT_NOT_FOUND", eventRecord.Error?.Message ?? "Event not found", cancellationToken).ConfigureAwait(false);
            return Failure<EventAttendanceOutcome>(
                eventRecord.Error?.Code ?? "EVENT_NOT_FOUND",
                eventRecord.Error?.Message ?? "Event not found",
                eventRecord.Error?.Details);
        }

        var resourceKey = $"{CurrentTenant.TenantId}:{eventId}:{attendeeId}";
        if (!InFlightRsvps.TryAdd(resourceKey, 1))
        {
            await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Concurrent RSVP detected", cancellationToken).ConfigureAwait(false);
            return Failure<EventAttendanceOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent RSVP detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var existing = await FindRsvpAsync(eventId, attendeeId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                var replayBody = RsvpResponse(existing);
                await _idempotency.MarkSucceededAsync(operationKey, replayBody, cancellationToken).ConfigureAwait(false);
                return Success(new EventAttendanceOutcome(200, replayBody, true, true));
            }

            var capacity = ReadNullableInt(eventRecord.Data.Body, "capacity");
            var confirmedCount = await CountRsvpsByStatusAsync(eventId, "CONFIRMED", cancellationToken).ConfigureAwait(false);
            var routed = capacity is null
                ? "CONFIRMED"
                : capacity == 0 || confirmedCount >= capacity
                    ? "WAITLIST"
                    : "CONFIRMED";
            var status = routed == "WAITLIST" ? "WAITLISTED" : "CONFIRMED";
            var now = DateTimeOffset.UtcNow;
            var cancellationWindow = (int)await GetFreedomNumberAsync("flow04_rsvp_cancellation_window_hours", 24, cancellationToken).ConfigureAwait(false);
            var rsvpId = $"rsvp-{CurrentTenant.TenantId}-{eventId}-{attendeeId}";
            var body = new Dictionary<string, object?>
            {
                ["id"] = rsvpId,
                ["rsvp_id"] = rsvpId,
                ["event_id"] = eventId,
                ["attendee_id"] = attendeeId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["status"] = status,
                ["routed"] = routed,
                ["payment_confirmed"] = ReadBool(request, "paymentConfirmed"),
                ["join_timestamp"] = now.ToString("O", CultureInfo.InvariantCulture),
                ["cancellable_until"] = now.AddHours(cancellationWindow).ToString("O", CultureInfo.InvariantCulture),
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now.ToString("O", CultureInfo.InvariantCulture)
            };

            var stored = await _documents.UpsertAsync(EventAttendanceConstants.Rsvps, rsvpId, body, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                await _idempotency.MarkFailedAsync(operationKey, stored.Error?.Code ?? "RSVP_STORE_FAILED", stored.Error?.Message ?? "Failed to store RSVP", cancellationToken).ConfigureAwait(false);
                return Failure<EventAttendanceOutcome>(
                    stored.Error?.Code ?? "RSVP_STORE_FAILED",
                    stored.Error?.Message ?? "Failed to store RSVP",
                    stored.Error?.Details);
            }

            var response = RsvpResponse(stored.Data!);
            await _outbox.AppendAsync(
                CreateEvent(status == "CONFIRMED" ? EventAttendanceConstants.RsvpConfirmedEvent : EventAttendanceConstants.RsvpWaitlistedEvent, eventId, response),
                cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(201, response, true, true));
        }
        finally
        {
            InFlightRsvps.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<EventAttendanceOutcome>> CancelRsvpAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventAttendanceOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var attendeeId = ReadString(request, "attendeeId");
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(attendeeId))
        {
            return Failure<EventAttendanceOutcome>("VALIDATION_FAILED", "Cancellation requires eventId and attendeeId");
        }

        var operationKey = $"{EventAttendanceConstants.FlowSlug}:cancel:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventAttendanceOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var rsvp = await FindRsvpAsync(eventId, attendeeId.Trim(), cancellationToken).ConfigureAwait(false);
        if (rsvp is null)
        {
            return Failure<EventAttendanceOutcome>("RSVP_NOT_FOUND", "RSVP not found");
        }

        if (string.Equals(ReadString(rsvp.Body, "status"), "CANCELLED", StringComparison.Ordinal))
        {
            var replay = RsvpResponse(rsvp);
            await _idempotency.MarkSucceededAsync(operationKey, replay, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(200, replay, true, true));
        }

        if (!CanCancel(rsvp))
        {
            var windowClosed = new Dictionary<string, object?>
            {
                ["eventId"] = eventId,
                ["attendeeId"] = attendeeId,
                ["cancelled"] = false,
                ["reason"] = "WINDOW_CLOSED"
            };
            await _idempotency.MarkSucceededAsync(operationKey, windowClosed, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(200, windowClosed, false, false));
        }

        var wasConfirmed = string.Equals(ReadString(rsvp.Body, "status"), "CONFIRMED", StringComparison.Ordinal);
        var updated = new Dictionary<string, object?>(rsvp.Body)
        {
            ["status"] = "CANCELLED",
            ["routed"] = "CANCELLED",
            ["cancelled_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.Rsvps, rsvp.Id, updated, rsvp.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EventAttendanceOutcome>(stored.Error?.Code ?? "CANCEL_FAILED", stored.Error?.Message ?? "Failed to cancel RSVP");
        }

        var response = new Dictionary<string, object?>(RsvpResponse(stored.Data!));
        await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.RsvpCancelledEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        if (wasConfirmed)
        {
            var promoted = await PromoteNextWaitlistedAsync(eventId, cancellationToken).ConfigureAwait(false);
            response["promotedAttendeeId"] = promoted;
        }

        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new EventAttendanceOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<EventAttendanceOutcome>> CheckInAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventAttendanceOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var attendeeId = ReadString(request, "attendeeId");
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(attendeeId))
        {
            return Failure<EventAttendanceOutcome>("VALIDATION_FAILED", "Check-in requires eventId and attendeeId");
        }

        var operationKey = $"{EventAttendanceConstants.FlowSlug}:checkin:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventAttendanceOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var existingCheckIn = await FindCheckInAsync(eventId, attendeeId.Trim(), cancellationToken).ConfigureAwait(false);
        if (existingCheckIn is not null)
        {
            var replay = CheckInResponse(existingCheckIn, true);
            await _idempotency.MarkSucceededAsync(operationKey, replay, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(200, replay, true, true));
        }

        var rsvp = await FindRsvpAsync(eventId, attendeeId.Trim(), cancellationToken).ConfigureAwait(false);
        if (rsvp is null)
        {
            var noRsvp = new Dictionary<string, object?>
            {
                ["eventId"] = eventId,
                ["attendeeId"] = attendeeId,
                ["checkedIn"] = false,
                ["reason"] = "NO_RSVP"
            };
            await _idempotency.MarkSucceededAsync(operationKey, noRsvp, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(200, noRsvp, false, false));
        }

        if (!string.Equals(ReadString(rsvp.Body, "status"), "CONFIRMED", StringComparison.Ordinal))
        {
            var notConfirmed = new Dictionary<string, object?>
            {
                ["eventId"] = eventId,
                ["attendeeId"] = attendeeId,
                ["checkedIn"] = false,
                ["reason"] = "NOT_CONFIRMED",
                ["rsvpStatus"] = ReadString(rsvp.Body, "status")
            };
            await _idempotency.MarkSucceededAsync(operationKey, notConfirmed, cancellationToken).ConfigureAwait(false);
            return Success(new EventAttendanceOutcome(200, notConfirmed, false, false));
        }

        var checkInId = $"checkin-{CurrentTenant.TenantId}-{eventId}-{attendeeId}";
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["id"] = checkInId,
            ["checkin_id"] = checkInId,
            ["event_id"] = eventId,
            ["attendee_id"] = attendeeId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["status"] = "CHECKED_IN",
            ["checked_in_at"] = now,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED"
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.CheckIns, checkInId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EventAttendanceOutcome>(stored.Error?.Code ?? "CHECKIN_FAILED", stored.Error?.Message ?? "Failed to check in attendee");
        }

        var response = CheckInResponse(stored.Data!, false);
        await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.AttendeeCheckedInEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new EventAttendanceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<EventAttendanceOutcome>> OpenFeedbackWindowAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(ReadString(request, "triggerSource"), "event.ended", StringComparison.Ordinal))
        {
            return Failure<EventAttendanceOutcome>(
                "FEEDBACK_TRIGGER_REJECTED",
                "Feedback window opens only from event.ended CloudEvent");
        }

        var existing = await _documents.SearchAsync(
            EventAttendanceConstants.FeedbackWindows,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        if (existing.IsSuccess && (existing.Data?.Count ?? 0) > 0)
        {
            return Success(new EventAttendanceOutcome(200, FeedbackWindowResponse(existing.Data![0]), true, false));
        }

        var hours = (int)await GetFreedomNumberAsync("flow04_feedback_window_hours", 72, cancellationToken).ConfigureAwait(false);
        var openedAt = DateTimeOffset.UtcNow;
        var windowId = $"feedback-window-{CurrentTenant.TenantId}-{eventId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = windowId,
            ["feedback_window_id"] = windowId,
            ["event_id"] = eventId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["opened_at"] = openedAt.ToString("O", CultureInfo.InvariantCulture),
            ["closes_at"] = openedAt.AddHours(hours).ToString("O", CultureInfo.InvariantCulture),
            ["trigger_source"] = "event.ended",
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED"
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.FeedbackWindows, windowId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EventAttendanceOutcome>(stored.Error?.Code ?? "FEEDBACK_WINDOW_FAILED", stored.Error?.Message ?? "Failed to open feedback window");
        }

        var response = FeedbackWindowResponse(stored.Data!);
        await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.FeedbackWindowOpenedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        return Success(new EventAttendanceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<EventAttendanceOutcome>> SubmitFeedbackAsync(
        string eventId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<EventAttendanceOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var attendeeId = ReadString(request, "attendeeId");
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(attendeeId))
        {
            return Failure<EventAttendanceOutcome>("VALIDATION_FAILED", "Feedback requires eventId and attendeeId");
        }

        var operationKey = $"{EventAttendanceConstants.FlowSlug}:feedback:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new EventAttendanceOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var window = await FindFeedbackWindowAsync(eventId, cancellationToken).ConfigureAwait(false);
        if (window is null)
        {
            return Failure<EventAttendanceOutcome>("FEEDBACK_WINDOW_NOT_FOUND", "Feedback window is not open");
        }

        if (DateTimeOffset.TryParse(ReadString(window.Body, "closes_at"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var closesAt) &&
            closesAt < DateTimeOffset.UtcNow)
        {
            return Failure<EventAttendanceOutcome>("FEEDBACK_WINDOW_CLOSED", "Feedback window is closed");
        }

        var feedbackId = $"feedback-{CurrentTenant.TenantId}-{eventId}-{attendeeId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = feedbackId,
            ["feedback_id"] = feedbackId,
            ["event_id"] = eventId,
            ["attendee_id"] = attendeeId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["rating"] = ReadInt(request, "rating"),
            ["comment"] = ReadString(request, "comment") ?? string.Empty,
            ["submitted_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED"
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.Feedback, feedbackId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<EventAttendanceOutcome>(stored.Error?.Code ?? "FEEDBACK_FAILED", stored.Error?.Message ?? "Failed to submit feedback");
        }

        var response = FeedbackResponse(stored.Data!);
        await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.FeedbackSubmittedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new EventAttendanceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<EventAttendanceDashboard>> GetDashboardAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var eventRecord = await _documents.GetAsync(EventAttendanceConstants.EventSource, eventId, cancellationToken).ConfigureAwait(false);
        if (eventRecord.IsFailure || eventRecord.Data is null)
        {
            return Failure<EventAttendanceDashboard>("EVENT_NOT_FOUND", "Event not found");
        }

        var rsvps = await SearchRsvpsAsync(eventId, cancellationToken).ConfigureAwait(false);
        var checkIns = await SearchCheckInsAsync(eventId, cancellationToken).ConfigureAwait(false);
        var feedback = await SearchFeedbackAsync(eventId, cancellationToken).ConfigureAwait(false);
        var confirmed = rsvps.Count(item => string.Equals(ReadString(item.Body, "status"), "CONFIRMED", StringComparison.Ordinal));
        var waitlisted = rsvps.Count(item => string.Equals(ReadString(item.Body, "status"), "WAITLISTED", StringComparison.Ordinal));
        var attendees = rsvps
            .OrderBy(item => ReadString(item.Body, "join_timestamp"), StringComparer.Ordinal)
            .Select(item => new Dictionary<string, object?>
            {
                ["attendeeId"] = ReadString(item.Body, "attendee_id"),
                ["status"] = ReadString(item.Body, "status"),
                ["routed"] = ReadString(item.Body, "routed"),
                ["checkedIn"] = checkIns.Any(check => string.Equals(ReadString(check.Body, "attendee_id"), ReadString(item.Body, "attendee_id"), StringComparison.Ordinal))
            })
            .Cast<IReadOnlyDictionary<string, object?>>()
            .ToArray();
        var capacity = ReadNullableInt(eventRecord.Data.Body, "capacity");

        return Success(new EventAttendanceDashboard(
            eventId,
            ReadString(eventRecord.Data.Body, "title") ?? eventId,
            capacity,
            confirmed,
            waitlisted,
            checkIns.Count,
            feedback.Count,
            CapacityLabel(capacity, confirmed, waitlisted),
            attendees));
    }

    public async Task<DataProcessResult<EventAttendanceStatus>> GetAttendanceStatusAsync(
        string eventId,
        string attendeeId,
        CancellationToken cancellationToken = default)
    {
        var rsvp = await FindRsvpAsync(eventId, attendeeId, cancellationToken).ConfigureAwait(false);
        var checkIn = await FindCheckInAsync(eventId, attendeeId, cancellationToken).ConfigureAwait(false);
        var window = await FindFeedbackWindowAsync(eventId, cancellationToken).ConfigureAwait(false);
        return Success(new EventAttendanceStatus(
            eventId,
            attendeeId,
            rsvp is null ? "NONE" : ReadString(rsvp.Body, "status") ?? "UNKNOWN",
            rsvp is null ? "NONE" : ReadString(rsvp.Body, "routed") ?? "UNKNOWN",
            rsvp?.Id,
            checkIn?.Id,
            window?.Id,
            rsvp is not null && CanCancel(rsvp)));
    }

    private async Task<string?> PromoteNextWaitlistedAsync(string eventId, CancellationToken cancellationToken)
    {
        var waitlisted = await SearchRsvpsAsync(eventId, cancellationToken).ConfigureAwait(false);
        var next = waitlisted
            .Where(item => string.Equals(ReadString(item.Body, "status"), "WAITLISTED", StringComparison.Ordinal))
            .OrderBy(item => ReadString(item.Body, "join_timestamp"), StringComparer.Ordinal)
            .FirstOrDefault();
        if (next is null)
        {
            return null;
        }

        var updated = new Dictionary<string, object?>(next.Body)
        {
            ["status"] = "CONFIRMED",
            ["routed"] = "CONFIRMED",
            ["promoted_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.Rsvps, next.Id, updated, next.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return null;
        }

        var storedRecord = stored.Data!;
        var response = RsvpResponse(storedRecord);
        await _outbox.AppendAsync(CreateEvent(EventAttendanceConstants.WaitlistPromotionCompletedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        return ReadString(storedRecord.Body, "attendee_id");
    }

    private async Task<DataProcessResult<DocumentRecord?>> EnsureEventSourceAsync(
        IReadOnlyDictionary<string, object?> request,
        string eventId,
        CancellationToken cancellationToken)
    {
        var existing = await _documents.GetAsync(EventAttendanceConstants.EventSource, eventId, cancellationToken).ConfigureAwait(false);
        if (existing.IsSuccess && existing.Data is not null)
        {
            return existing;
        }

        if (!request.ContainsKey("eventCapacity") && !ReadBool(request, "seedEvent"))
        {
            return DataProcessResult<DocumentRecord?>.Failure("EVENT_NOT_FOUND", "FLOW-03 event source record is required");
        }

        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var eventBody = new Dictionary<string, object?>
        {
            ["id"] = eventId,
            ["event_id"] = eventId,
            ["title"] = ReadString(request, "eventTitle") ?? "Attendance event",
            ["tenant_id"] = CurrentTenant.TenantId,
            ["capacity"] = ReadNullableInt(request, "eventCapacity"),
            ["starts_at"] = ReadString(request, "eventStartsAtUtc") ?? now,
            ["ends_at"] = ReadString(request, "eventEndsAtUtc") ?? DateTimeOffset.UtcNow.AddHours(2).ToString("O", CultureInfo.InvariantCulture),
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = now
        };
        var stored = await _documents.UpsertAsync(EventAttendanceConstants.EventSource, eventId, eventBody, null, cancellationToken).ConfigureAwait(false);
        return stored.IsSuccess
            ? DataProcessResult<DocumentRecord?>.Success(stored.Data)
            : DataProcessResult<DocumentRecord?>.Failure(stored.Error?.Code ?? "EVENT_SOURCE_STORE_FAILED", stored.Error?.Message ?? "Failed to store event source", stored.Error?.Details);
    }

    private async Task<DocumentRecord?> FindRsvpAsync(string eventId, string attendeeId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.Rsvps,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId, ["attendee_id"] = attendeeId },
            cancellationToken).ConfigureAwait(false);
        return records.Data?.FirstOrDefault();
    }

    private async Task<DocumentRecord?> FindCheckInAsync(string eventId, string attendeeId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.CheckIns,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId, ["attendee_id"] = attendeeId },
            cancellationToken).ConfigureAwait(false);
        return records.Data?.FirstOrDefault();
    }

    private async Task<DocumentRecord?> FindFeedbackWindowAsync(string eventId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.FeedbackWindows,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        return records.Data?.FirstOrDefault();
    }

    private async Task<IReadOnlyList<DocumentRecord>> SearchRsvpsAsync(string eventId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.Rsvps,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        return records.Data ?? [];
    }

    private async Task<IReadOnlyList<DocumentRecord>> SearchCheckInsAsync(string eventId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.CheckIns,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        return records.Data ?? [];
    }

    private async Task<IReadOnlyList<DocumentRecord>> SearchFeedbackAsync(string eventId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.Feedback,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId },
            cancellationToken).ConfigureAwait(false);
        return records.Data ?? [];
    }

    private async Task<int> CountRsvpsByStatusAsync(string eventId, string status, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(
            EventAttendanceConstants.Rsvps,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["event_id"] = eventId, ["status"] = status },
            cancellationToken).ConfigureAwait(false);
        return records.Data?.Count ?? 0;
    }

    private DataProcessResult<EventAttendanceOutcome>? ValidateRsvpRequest(IReadOnlyDictionary<string, object?> request)
    {
        var fieldErrors = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "eventId")))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "eventId", ["reason"] = "required" });
        }

        if (string.IsNullOrWhiteSpace(ReadString(request, "attendeeId")))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "attendeeId", ["reason"] = "required" });
        }

        if (request.TryGetValue("eventCapacity", out var capacityValue) &&
            capacityValue is not null &&
            (!int.TryParse(Convert.ToString(capacityValue, CultureInfo.InvariantCulture), out var capacity) || capacity < 0))
        {
            fieldErrors.Add(new Dictionary<string, object?> { ["field"] = "eventCapacity", ["reason"] = "must be null, zero, or positive" });
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return Failure<EventAttendanceOutcome>(
            "VALIDATION_FAILED",
            "RSVP input validation failed",
            new Dictionary<string, object?> { ["details"] = fieldErrors });
    }

    private static IReadOnlyDictionary<string, object?> RsvpResponse(DocumentRecord record) =>
        new Dictionary<string, object?>
        {
            ["rsvpId"] = record.Id,
            ["eventId"] = ReadString(record.Body, "event_id"),
            ["attendeeId"] = ReadString(record.Body, "attendee_id"),
            ["status"] = ReadString(record.Body, "status"),
            ["routed"] = ReadString(record.Body, "routed"),
            ["cancellableUntil"] = ReadString(record.Body, "cancellable_until")
        };

    private static IReadOnlyDictionary<string, object?> CheckInResponse(DocumentRecord record, bool replayed) =>
        new Dictionary<string, object?>
        {
            ["checkInId"] = record.Id,
            ["eventId"] = ReadString(record.Body, "event_id"),
            ["attendeeId"] = ReadString(record.Body, "attendee_id"),
            ["checkedIn"] = true,
            ["replayed"] = replayed,
            ["checkedInAt"] = ReadString(record.Body, "checked_in_at")
        };

    private static IReadOnlyDictionary<string, object?> FeedbackWindowResponse(DocumentRecord record) =>
        new Dictionary<string, object?>
        {
            ["feedbackWindowId"] = record.Id,
            ["eventId"] = ReadString(record.Body, "event_id"),
            ["openedAt"] = ReadString(record.Body, "opened_at"),
            ["closesAt"] = ReadString(record.Body, "closes_at")
        };

    private static IReadOnlyDictionary<string, object?> FeedbackResponse(DocumentRecord record) =>
        new Dictionary<string, object?>
        {
            ["feedbackId"] = record.Id,
            ["eventId"] = ReadString(record.Body, "event_id"),
            ["attendeeId"] = ReadString(record.Body, "attendee_id"),
            ["rating"] = ReadInt(record.Body, "rating"),
            ["submittedAt"] = ReadString(record.Body, "submitted_at")
        };

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            "xiigen://flow/event-attendance",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = EventAttendanceConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private async Task<double> GetFreedomNumberAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value.IsSuccess && value.Data?.Value is not null && double.TryParse(Convert.ToString(value.Data.Value, CultureInfo.InvariantCulture), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static bool CanCancel(DocumentRecord rsvp)
    {
        if (!DateTimeOffset.TryParse(ReadString(rsvp.Body, "cancellable_until"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var until))
        {
            return true;
        }

        return until >= DateTimeOffset.UtcNow;
    }

    private static string CapacityLabel(int? capacity, int confirmed, int waitlisted) =>
        capacity is null
            ? $"{confirmed} confirmed (unlimited)"
            : capacity == 0
                ? $"{waitlisted} waitlisted"
                : $"{confirmed} / {capacity} confirmed";

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
}
