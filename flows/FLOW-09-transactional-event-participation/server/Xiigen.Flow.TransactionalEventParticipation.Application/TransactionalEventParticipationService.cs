using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.TransactionalEventParticipation.Application;

public sealed class TransactionalEventParticipationService : MicroserviceBase<TransactionalEventParticipationService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightSeatReservations = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public TransactionalEventParticipationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<TransactionalEventParticipationService> logger)
        : base(
            new ServiceDescriptor("FLOW-09", nameof(TransactionalEventParticipationService), TransactionalEventParticipationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<TransactionalEventParticipationOutcome>> StartParticipationAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<TransactionalEventParticipationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required", FieldDetails("X-Idempotency-Key", "required"));
        }

        var eventId = (ReadString(request, "eventId") ?? string.Empty).Trim();
        var userId = (ReadString(request, "userId") ?? string.Empty).Trim();
        var participationId = (ReadString(request, "participationId") ?? $"participation-{Guid.NewGuid():N}").Trim();
        var operationKey = $"{TransactionalEventParticipationConstants.FlowSlug}:participate:{CurrentTenant.TenantId}:{eventId}:{userId}:{idempotencyKey}";

        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<TransactionalEventParticipationOutcome>(reservation.Error?.Code ?? "IDEMPOTENCY_FAILED", reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new TransactionalEventParticipationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["eventId"] = eventId,
                ["retryAfterMs"] = 5000
            };
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.TimeoutRetainedEvent, participationId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new TransactionalEventParticipationOutcome(504, timeoutBody, false, true));
        }

        var seatKey = $"{CurrentTenant.TenantId}:{eventId}:{ReadString(request, "seatId") ?? "general"}";
        if (!InFlightSeatReservations.TryAdd(seatKey, 0))
        {
            return Failure<TransactionalEventParticipationOutcome>("CONCURRENT_UPDATE", "Concurrent seat reservation mutation detected", new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(90, cancellationToken).ConfigureAwait(false);
            }

            var validation = ValidateParticipation(request);
            if (validation is not null)
            {
                return Success(validation);
            }

            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.ParticipationRequestedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);

            if (ReadBool(request, "eligible") == false && request.ContainsKey("eligible"))
            {
                await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.EligibilityFailedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);
                var rejected = new Dictionary<string, object?> { ["code"] = "ELIGIBILITY_FAILED", ["eventId"] = eventId, ["participationId"] = participationId };
                await _idempotency.MarkSucceededAsync(operationKey, rejected, cancellationToken).ConfigureAwait(false);
                return Success(new TransactionalEventParticipationOutcome(400, rejected, false, true));
            }

            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.EligibilityPassedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);

            var capacityAvailable = ReadInt(request, "capacityAvailable") ?? 10;
            if (capacityAvailable <= 0)
            {
                await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.InventoryExhaustedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);
                var soldOut = new Dictionary<string, object?> { ["code"] = "SOLD_OUT", ["eventId"] = eventId, ["waitlistAvailable"] = true };
                await _idempotency.MarkSucceededAsync(operationKey, soldOut, cancellationToken).ConfigureAwait(false);
                return Success(new TransactionalEventParticipationOutcome(409, soldOut, false, true));
            }

            var groupMembers = ReadStringArray(request, "groupMembers");
            if (ReadBool(request, "simulateGroupMemberFailure") && groupMembers.Count > 0)
            {
                await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.PaymentFailedEvent, participationId, new Dictionary<string, object?> { ["eventId"] = eventId, ["reason"] = "group_member_failure", ["memberCount"] = groupMembers.Count }), cancellationToken).ConfigureAwait(false);
                var groupFailure = new Dictionary<string, object?> { ["code"] = "GROUP_BOOKING_ROLLED_BACK", ["eventId"] = eventId, ["memberCount"] = groupMembers.Count };
                await _idempotency.MarkSucceededAsync(operationKey, groupFailure, cancellationToken).ConfigureAwait(false);
                return Success(new TransactionalEventParticipationOutcome(409, groupFailure, false, true));
            }

            var amount = ReadDecimal(request, "amount") ?? 0m;
            var currency = ReadString(request, "currency") ?? "USD";
            var fee = await CalculateFeeBreakdownAsync(amount, currency, cancellationToken).ConfigureAwait(false);
            var reservationBody = CreateReservationBody(participationId, eventId, userId, request, fee);
            var storedReservation = await _documents.UpsertAsync(TransactionalEventParticipationConstants.SeatReservations, participationId, reservationBody, ifMatch, cancellationToken).ConfigureAwait(false);
            if (storedReservation.IsFailure)
            {
                return Failure<TransactionalEventParticipationOutcome>(storedReservation.Error?.Code ?? "RESERVATION_FAILED", storedReservation.Error?.Message ?? "Seat reservation failed");
            }

            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.InventoryReservedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);

            if (ReadBool(request, "simulateFraudServiceDown"))
            {
                await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.FraudCheckFailedEvent, participationId, new Dictionary<string, object?> { ["eventId"] = eventId, ["reason"] = "service_unavailable", ["proceed"] = true }), cancellationToken).ConfigureAwait(false);
            }

            if (ReadBool(request, "paymentDeclined"))
            {
                await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.PaymentFailedEvent, participationId, MinimalPayload(participationId, eventId, userId)), cancellationToken).ConfigureAwait(false);
                var declined = new Dictionary<string, object?> { ["code"] = "PAYMENT_DECLINED", ["eventId"] = eventId, ["reservationId"] = participationId };
                await _idempotency.MarkSucceededAsync(operationKey, declined, cancellationToken).ConfigureAwait(false);
                return Success(new TransactionalEventParticipationOutcome(402, declined, true, true));
            }

            var paymentId = $"pay-{participationId}";
            var paymentBody = CreatePaymentBody(paymentId, participationId, eventId, userId, request, fee);
            await _documents.UpsertAsync(TransactionalEventParticipationConstants.Payments, paymentId, paymentBody, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.PaymentClearedEvent, paymentId, CountPayload(eventId, "PAYMENT_CLEARED", 1)), cancellationToken).ConfigureAwait(false);

            if (groupMembers.Count > 0)
            {
                await _documents.UpsertAsync(TransactionalEventParticipationConstants.GroupBookings, $"group-{participationId}", CreateGroupBody(participationId, eventId, groupMembers), null, cancellationToken).ConfigureAwait(false);
            }

            var ticketId = $"ticket-{participationId}";
            var qrToken = CreatePlatformQrToken(ticketId, eventId, userId);
            var ticketBody = CreateTicketBody(ticketId, participationId, eventId, userId, qrToken, request, storedReservation.Data?.Version ?? "v1");
            var storedTicket = await _documents.UpsertAsync(TransactionalEventParticipationConstants.Tickets, ticketId, ticketBody, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.TicketIssuedEvent, ticketId, CountPayload(eventId, "TICKET_ISSUED", 1)), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.TokenIssuedEvent, ticketId, new Dictionary<string, object?> { ["ticketId"] = ticketId, ["ttlSeconds"] = 60, ["platformOnly"] = true }), cancellationToken).ConfigureAwait(false);

            await RecordAnalyticsAsync(eventId, participationId, request, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.AnalyticsRecordedEvent, participationId, CountPayload(eventId, "ANALYTICS_RECORDED", 1)), cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["id"] = participationId,
                ["eventId"] = eventId,
                ["ticketId"] = ticketId,
                ["paymentId"] = paymentId,
                ["status"] = "CONFIRMED",
                ["version"] = storedTicket.Data?.Version ?? "v1",
                ["qrToken"] = qrToken,
                ["fee"] = FeeToDictionary(fee),
                ["seatReservedBeforePayment"] = true,
                ["platformQrBoundary"] = true
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new TransactionalEventParticipationOutcome(201, response, true, true));
        }
        finally
        {
            InFlightSeatReservations.TryRemove(seatKey, out _);
        }
    }

    public async Task<DataProcessResult<TicketSnapshot>> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _documents.GetAsync(TransactionalEventParticipationConstants.Tickets, ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket.IsFailure || ticket.Data is null)
        {
            return Failure<TicketSnapshot>("NOT_FOUND", "Ticket was not found");
        }

        var body = ticket.Data.Body;
        return Success(new TicketSnapshot(
            ticket.Data.Id,
            ReadString(body, "event_id") ?? string.Empty,
            ReadString(body, "user_id") ?? string.Empty,
            ReadString(body, "status") ?? "UNKNOWN",
            ReadString(body, "qr_token") ?? string.Empty,
            ticket.Data.Version));
    }

    public async Task<DataProcessResult<TransactionalEventParticipationOutcome>> RequestRefundAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<TransactionalEventParticipationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required", FieldDetails("X-Idempotency-Key", "required"));
        }

        var ticketId = (ReadString(request, "ticketId") ?? string.Empty).Trim();
        var operationKey = $"{TransactionalEventParticipationConstants.FlowSlug}:refund:{CurrentTenant.TenantId}:{ticketId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new TransactionalEventParticipationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (string.IsNullOrWhiteSpace(ticketId))
        {
            return Success(new TransactionalEventParticipationOutcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = new[] { Field("ticketId", "required") } }, false, false));
        }

        var attempt = ReadInt(request, "attempt") ?? 1;
        var maxAttempts = ReadInt(request, "maxAttempts") ?? 3;
        var refundId = $"refund-{ticketId}-{attempt}";
        var refundBody = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["ticket_id"] = ticketId,
            ["refund_id"] = refundId,
            ["reason"] = ReadString(request, "reason") ?? "requested_by_user",
            ["attempt"] = attempt,
            ["status"] = attempt >= maxAttempts ? "ESCALATED" : "PROCESSING",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(TransactionalEventParticipationConstants.Refunds, refundId, refundBody, null, cancellationToken).ConfigureAwait(false);

        if (attempt >= maxAttempts)
        {
            var complianceBody = new Dictionary<string, object?>(refundBody) { ["manual_review"] = true };
            await _documents.UpsertAsync(TransactionalEventParticipationConstants.ComplianceQueue, $"compliance-{refundId}", complianceBody, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.RefundFailedEvent, refundId, refundBody), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.ComplianceReviewRequestedEvent, refundId, complianceBody), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.RefundProcessedEvent, refundId, refundBody), cancellationToken).ConfigureAwait(false);
        }

        var response = new Dictionary<string, object?> { ["refundId"] = refundId, ["status"] = refundBody["status"], ["attempt"] = attempt };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new TransactionalEventParticipationOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<TransactionalEventParticipationOutcome>> JoinWaitlistAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var eventId = (ReadString(request, "eventId") ?? string.Empty).Trim();
        var userId = (ReadString(request, "userId") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(userId))
        {
            return Success(new TransactionalEventParticipationOutcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED" }, false, false));
        }

        var id = $"waitlist-{eventId}-{userId}";
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["event_id"] = eventId,
            ["user_id"] = userId,
            ["status"] = "PENDING",
            ["joined_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(TransactionalEventParticipationConstants.Waitlist, id, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.WaitlistJoinedEvent, id, body), cancellationToken).ConfigureAwait(false);
        return Success(new TransactionalEventParticipationOutcome(201, new Dictionary<string, object?> { ["id"] = id, ["status"] = "PENDING" }, true, true));
    }

    public async Task<DataProcessResult<TransactionalEventParticipationOutcome>> RedeemTokenAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var qrToken = ReadString(request, "qrToken") ?? string.Empty;
        if (!qrToken.StartsWith("platform-qr:", StringComparison.Ordinal))
        {
            return Success(new TransactionalEventParticipationOutcome(400, new Dictionary<string, object?> { ["code"] = "INVALID_TOKEN", ["platformOnly"] = true }, false, false));
        }

        var redemptionId = $"redemption-{Guid.NewGuid():N}";
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["redemption_id"] = redemptionId,
            ["qr_token_hash"] = qrToken.GetHashCode(StringComparison.Ordinal).ToString(CultureInfo.InvariantCulture),
            ["status"] = "REDEEMED",
            ["redeemed_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(TransactionalEventParticipationConstants.TokenRedemptions, redemptionId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(TransactionalEventParticipationConstants.TokenRedeemedEvent, redemptionId, new Dictionary<string, object?> { ["redemptionId"] = redemptionId }), cancellationToken).ConfigureAwait(false);
        return Success(new TransactionalEventParticipationOutcome(201, new Dictionary<string, object?> { ["redemptionId"] = redemptionId, ["status"] = "REDEEMED" }, true, true));
    }

    public async Task<DataProcessResult<ParticipationSummary>> GetTicketsAsync(CancellationToken cancellationToken = default)
    {
        var tickets = await _documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        if (tickets.IsFailure)
        {
            return Failure<ParticipationSummary>(tickets.Error?.Code ?? "TICKET_READ_FAILED", tickets.Error?.Message ?? "Could not read tickets");
        }

        var rows = (tickets.Data ?? []).Select(row => (IReadOnlyDictionary<string, object?>)row.Body).ToList();
        return Success(new ParticipationSummary(rows, rows.Count, CountByStatus(rows)));
    }

    public async Task<DataProcessResult<ParticipationAnalytics>> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var tickets = await _documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var payments = await _documents.SearchAsync(TransactionalEventParticipationConstants.Payments, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var refunds = await _documents.SearchAsync(TransactionalEventParticipationConstants.Refunds, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var waitlist = await _documents.SearchAsync(TransactionalEventParticipationConstants.Waitlist, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var redemptions = await _documents.SearchAsync(TransactionalEventParticipationConstants.TokenRedemptions, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var analytics = await _documents.SearchAsync(TransactionalEventParticipationConstants.Analytics, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);

        var ticketCount = tickets.Data?.Count ?? 0;
        var paymentCount = payments.Data?.Count ?? 0;
        var aggregateLeaksParticipantIds = (analytics.Data ?? []).Any(row => row.Body.ContainsKey("participantIds") || row.Body.ContainsKey("participant_ids") || row.Body.ContainsKey("userIds"));
        return Success(new ParticipationAnalytics(
            ticketCount,
            paymentCount,
            refunds.Data?.Count ?? 0,
            waitlist.Data?.Count ?? 0,
            redemptions.Data?.Count ?? 0,
            ticketCount / (double)(paymentCount == 0 ? 1 : paymentCount),
            aggregateLeaksParticipantIds));
    }

    private TransactionalEventParticipationOutcome? ValidateParticipation(IReadOnlyDictionary<string, object?> request)
    {
        var details = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "eventId"))) details.Add(Field("eventId", "event is required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "userId"))) details.Add(Field("userId", "user is required"));
        var amount = ReadDecimal(request, "amount");
        if (!amount.HasValue) details.Add(Field("amount", "amount is required"));
        if (amount.HasValue && amount.Value < 0) details.Add(Field("amount", "negative amount is not allowed"));
        var seats = ReadInt(request, "seats") ?? 1;
        if (seats <= 0) details.Add(Field("seats", "seat count must be positive"));

        return details.Count == 0
            ? null
            : new TransactionalEventParticipationOutcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false, false);
    }

    private async Task<FeeBreakdown> CalculateFeeBreakdownAsync(decimal amount, string currency, CancellationToken cancellationToken)
    {
        var platformFeeRate = await ResolveDecimalAsync("flow09_platform_fee_rate", 0.05m, cancellationToken).ConfigureAwait(false);
        var processingFeeRate = await ResolveDecimalAsync("flow09_processing_fee_rate", 0.029m, cancellationToken).ConfigureAwait(false);
        var taxRate = await ResolveDecimalAsync("flow09_tax_rate", 0m, cancellationToken).ConfigureAwait(false);
        var serviceFee = decimal.Round(amount * platformFeeRate, 2, MidpointRounding.AwayFromZero);
        var processingFee = decimal.Round(amount * processingFeeRate, 2, MidpointRounding.AwayFromZero);
        var tax = decimal.Round(amount * taxRate, 2, MidpointRounding.AwayFromZero);
        return new FeeBreakdown(amount, serviceFee, processingFee, tax, amount + serviceFee + processingFee + tax, currency);
    }

    private async Task<decimal> ResolveDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken)
    {
        var result = await _freedom.GetAsync($"{key}.{CurrentTenant.TenantId}", cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Data?.Value is not null && decimal.TryParse(Convert.ToString(result.Data.Value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private async Task RecordAnalyticsAsync(string eventId, string participationId, IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["event_id"] = eventId,
            ["participation_count"] = 1,
            ["payment_count"] = 1,
            ["connection_views"] = ReadInt(request, "connectionsViewed") ?? 0,
            ["connection_requests"] = ReadInt(request, "connectionsRequested") ?? 0,
            ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(TransactionalEventParticipationConstants.Analytics, $"analytics-{participationId}", body, null, cancellationToken).ConfigureAwait(false);
    }

    private Dictionary<string, object?> CreateReservationBody(string participationId, string eventId, string userId, IReadOnlyDictionary<string, object?> request, FeeBreakdown fee) => new()
    {
        ["tenant_id"] = CurrentTenant.TenantId,
        ["participation_id"] = participationId,
        ["event_id"] = eventId,
        ["user_id"] = userId,
        ["seat_id"] = ReadString(request, "seatId") ?? "general",
        ["status"] = "RESERVED",
        ["hold_ttl_seconds"] = ReadInt(request, "holdTtlSeconds") ?? 300,
        ["fee_total"] = fee.Total,
        ["reserved_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private Dictionary<string, object?> CreatePaymentBody(string paymentId, string participationId, string eventId, string userId, IReadOnlyDictionary<string, object?> request, FeeBreakdown fee) => new()
    {
        ["tenant_id"] = CurrentTenant.TenantId,
        ["payment_id"] = paymentId,
        ["participation_id"] = participationId,
        ["event_id"] = eventId,
        ["user_id"] = userId,
        ["amount"] = fee.Total,
        ["currency"] = fee.Currency,
        ["method"] = ReadString(request, "paymentMethod") ?? "card",
        ["status"] = "CLEARED",
        ["completed_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private Dictionary<string, object?> CreateTicketBody(string ticketId, string participationId, string eventId, string userId, string qrToken, IReadOnlyDictionary<string, object?> request, string reservationVersion) => new()
    {
        ["tenant_id"] = CurrentTenant.TenantId,
        ["ticket_id"] = ticketId,
        ["participation_id"] = participationId,
        ["event_id"] = eventId,
        ["user_id"] = userId,
        ["ticket_type"] = ReadString(request, "ticketType") ?? "general",
        ["status"] = "ISSUED",
        ["qr_token"] = qrToken,
        ["reservation_version"] = reservationVersion,
        ["issued_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
    };

    private Dictionary<string, object?> CreateGroupBody(string participationId, string eventId, IReadOnlyList<string> members) => new()
    {
        ["tenant_id"] = CurrentTenant.TenantId,
        ["participation_id"] = participationId,
        ["event_id"] = eventId,
        ["member_count"] = members.Count,
        ["status"] = "CONFIRMED"
    };

    private string CreatePlatformQrToken(string ticketId, string eventId, string userId)
    {
        var payload = $"{CurrentTenant.TenantId}|{ticketId}|{eventId}|{userId}|{CurrentCorrelationId}";
        return $"platform-qr:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload))}";
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{TransactionalEventParticipationConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = TransactionalEventParticipationConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static Dictionary<string, object?> MinimalPayload(string participationId, string eventId, string userId) => new()
    {
        ["participationId"] = participationId,
        ["eventId"] = eventId,
        ["userId"] = userId
    };

    private static Dictionary<string, object?> CountPayload(string eventId, string status, int count) => new()
    {
        ["eventId"] = eventId,
        ["status"] = status,
        ["count"] = count
    };

    private static Dictionary<string, object?> FeeToDictionary(FeeBreakdown fee) => new()
    {
        ["ticketPrice"] = fee.TicketPrice,
        ["serviceFee"] = fee.ServiceFee,
        ["processingFee"] = fee.ProcessingFee,
        ["tax"] = fee.Tax,
        ["total"] = fee.Total,
        ["currency"] = fee.Currency
    };

    private static IReadOnlyDictionary<string, int> CountByStatus(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var status = Convert.ToString(row.GetValueOrDefault("status"), CultureInfo.InvariantCulture) ?? "UNKNOWN";
            counts[status] = counts.GetValueOrDefault(status) + 1;
        }

        return counts;
    }

    private static Dictionary<string, object?> FieldDetails(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };
    private static Dictionary<string, object?> Field(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };

    private static string? ReadString(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) && result;

    private static decimal? ReadDecimal(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static int? ReadInt(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is string[] typed)
        {
            return typed;
        }

        if (value is object?[] objects)
        {
            return objects.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToList();
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
