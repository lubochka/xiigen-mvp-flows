using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SubscriptionBilling.Application;

public sealed class SubscriptionBillingService : MicroserviceBase<SubscriptionBillingService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly IPaymentFabricService _payment;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _planLocks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _billingLocks = new(StringComparer.Ordinal);

    public SubscriptionBillingService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        IPaymentFabricService payment,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<SubscriptionBillingService> logger)
        : base(
            new ServiceDescriptor("subscription-billing-service", "Subscription Billing", SubscriptionBillingConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
        _payment = payment;
    }

    public async Task<DataProcessResult<SubscriptionBillingOutcome>> PublishPlanAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidatePlanRequest(request);
        if (validation.Count > 0)
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var planId = ReadString(request, "planId")!.Trim();
        var operationKey = $"{SubscriptionBillingConstants.FlowSlug}:publish-plan:{CurrentTenant.TenantId}:{planId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }

        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, reservation.Data.CachedResponse, true, false));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var body = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["message"] = "Plan publication timed out before document commit.",
                ["retryAfterMs"] = 1500,
                ["planId"] = planId
            };
            await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.TimeoutRetainedEvent, planId, body), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Plan publication timed out", cancellationToken).ConfigureAwait(false);
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status504GatewayTimeout, body, false, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:plan:{planId}";
        var gate = _planLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<SubscriptionBillingOutcome>("CONCURRENT_UPDATE", "Concurrent plan publication detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var current = await _documents.GetAsync(SubscriptionBillingConstants.Plans, PlanDocumentId(planId), cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return Failure<SubscriptionBillingOutcome>(current.Error!.Code, current.Error.Message, current.Error.Details);
            }

            if (current.Data is not null && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<SubscriptionBillingOutcome>(
                    "CONCURRENT_UPDATE",
                    "Plan version does not match",
                    new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            var planBody = CreatePlanBody(request, planId);
            var stored = await _documents.UpsertAsync(SubscriptionBillingConstants.Plans, PlanDocumentId(planId), planBody, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<SubscriptionBillingOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
            }

            await StoreAuditAsync($"plan-{planId}-{Guid.NewGuid():N}", "PLAN_PUBLISHED", planBody, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["status"] = "ACTIVE",
                ["versionPin"] = stored.Data!.Version,
                ["priceCents"] = planBody["price_cents"],
                ["billingInterval"] = planBody["billing_interval"]
            };

            await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.PlanPublishedEvent, planId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status201Created, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<SubscriptionBillingOutcome>> SubscribeAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateSubscriptionRequest(request);
        if (validation.Count > 0)
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var planId = ReadString(request, "planId")!.Trim();
        var subscriberId = ReadString(request, "subscriberId")!.Trim();
        var operationKey = $"{SubscriptionBillingConstants.FlowSlug}:subscribe:{CurrentTenant.TenantId}:{subscriberId}:{planId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, reservation.Data.CachedResponse, true, false));
        }

        var plan = await _documents.GetAsync(SubscriptionBillingConstants.Plans, PlanDocumentId(planId), cancellationToken).ConfigureAwait(false);
        if (plan.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(plan.Error!.Code, plan.Error.Message, plan.Error.Details);
        }

        if (plan.Data is null || !IsTenantRecord(plan.Data.Body) || !string.Equals(ReadString(plan.Data.Body, "status"), "ACTIVE", StringComparison.Ordinal))
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status404NotFound, ErrorBody("PLAN_NOT_FOUND", "Active plan was not found."), false, false));
        }

        var validationResult = await _payment.ValidateAsync(new Dictionary<string, object?>
        {
            ["paymentMethodId"] = ReadString(request, "paymentMethodId"),
            ["paymentDeclined"] = ReadBool(request, "paymentDeclined")
        }, cancellationToken).ConfigureAwait(false);

        if (validationResult.IsFailure || validationResult.Data is null || !validationResult.Data.Valid)
        {
            var body = ErrorBody("PAYMENT_METHOD_REJECTED", "Payment method validation failed.");
            await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.SubscriptionRejectedEvent, subscriberId, body), cancellationToken).ConfigureAwait(false);
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status402PaymentRequired, body, false, true));
        }

        var subscriptionId = ReadString(request, "subscriptionId") ?? $"sub-{CurrentTenant.TenantId}-{subscriberId}-{planId}";
        var status = ReadInt(request, "trialDays") > 0 ? "TRIALING" : "ACTIVE";
        var bodyToStore = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["subscription_id"] = subscriptionId,
            ["subscriber_id"] = subscriberId,
            ["plan_id"] = planId,
            ["status"] = status,
            ["payment_method_ref"] = validationResult.Data.PaymentMethodRef,
            ["price_cents"] = plan.Data.Body["price_cents"],
            ["billing_interval"] = plan.Data.Body["billing_interval"],
            ["currency"] = plan.Data.Body["currency"],
            ["trial_ends_at"] = status == "TRIALING" ? DateTimeOffset.UtcNow.AddDays(ReadInt(request, "trialDays")).ToString("O", CultureInfo.InvariantCulture) : null,
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var stored = await _documents.UpsertAsync(SubscriptionBillingConstants.Subscriptions, SubscriptionDocumentId(subscriptionId), bodyToStore, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync($"subscription-{subscriptionId}-{Guid.NewGuid():N}", "SUBSCRIPTION_ACTIVATED", bodyToStore, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscriptionId,
            ["planId"] = planId,
            ["status"] = status,
            ["paymentMethodRef"] = validationResult.Data.PaymentMethodRef,
            ["versionPin"] = stored.Data!.Version
        };

        await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.SubscriptionActivatedEvent, subscriptionId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new SubscriptionBillingOutcome(StatusCodes.Status201Created, response, true, true));
    }

    public async Task<DataProcessResult<SubscriptionBillingOutcome>> ProcessBillingCycleAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var subscriptionId = ReadString(request, "subscriptionId");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ValidationBody(Field("subscriptionId", "required")), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var subscription = await _documents.GetAsync(SubscriptionBillingConstants.Subscriptions, SubscriptionDocumentId(subscriptionId), cancellationToken).ConfigureAwait(false);
        if (subscription.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(subscription.Error!.Code, subscription.Error.Message, subscription.Error.Details);
        }

        if (subscription.Data is null || !IsTenantRecord(subscription.Data.Body))
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status404NotFound, ErrorBody("SUBSCRIPTION_NOT_FOUND", "Subscription was not found."), false, false));
        }

        var period = ReadString(request, "period") ?? DateTimeOffset.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var operationKey = $"{SubscriptionBillingConstants.FlowSlug}:bill:{CurrentTenant.TenantId}:{subscriptionId}:{period}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, reservation.Data.CachedResponse, true, false));
        }

        var status = ReadString(subscription.Data.Body, "status") ?? "UNKNOWN";
        if (string.Equals(status, "CANCELLED", StringComparison.Ordinal) ||
            string.Equals(status, "PAUSED", StringComparison.Ordinal))
        {
            var voided = new Dictionary<string, object?>
            {
                ["subscriptionId"] = subscriptionId,
                ["period"] = period,
                ["status"] = "VOIDED",
                ["reason"] = status
            };
            await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.InvoiceVoidedEvent, subscriptionId, voided), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, voided, cancellationToken).ConfigureAwait(false);
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, voided, false, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:invoice:{subscriptionId}:{period}";
        var gate = _billingLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<SubscriptionBillingOutcome>("CONCURRENT_UPDATE", "Concurrent invoice charge detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var invoiceId = $"inv-{CurrentTenant.TenantId}-{subscriptionId}-{period}".Replace(":", "-", StringComparison.Ordinal);
            if (ReadBool(request, "simulateFabricTimeout"))
            {
                var timeout = new Dictionary<string, object?>
                {
                    ["code"] = "TIMEOUT",
                    ["message"] = "Billing cycle timed out before invoice commit.",
                    ["retryAfterMs"] = 1500,
                    ["invoiceId"] = invoiceId
                };
                await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.TimeoutRetainedEvent, invoiceId, timeout), cancellationToken).ConfigureAwait(false);
                return Success(new SubscriptionBillingOutcome(StatusCodes.Status504GatewayTimeout, timeout, false, true));
            }

            var charge = await _payment.ChargeAsync(new Dictionary<string, object?>
            {
                ["paymentMethodRef"] = ReadString(subscription.Data.Body, "payment_method_ref"),
                ["priceCents"] = ReadInt(subscription.Data.Body, "price_cents"),
                ["currency"] = ReadString(subscription.Data.Body, "currency") ?? "USD",
                ["paymentDeclined"] = ReadBool(request, "paymentDeclined"),
                ["invoiceId"] = invoiceId
            }, cancellationToken).ConfigureAwait(false);

            if (charge.IsFailure || charge.Data is null || !charge.Data.Charged)
            {
                var dunning = await ResolveDunningScheduleAsync(cancellationToken).ConfigureAwait(false);
                var failed = new Dictionary<string, object?>
                {
                    ["invoiceId"] = invoiceId,
                    ["subscriptionId"] = subscriptionId,
                    ["status"] = "FAILED",
                    ["maxAttempts"] = dunning.Count,
                    ["dunningScheduleDays"] = dunning
                };
                await StoreAuditAsync($"invoice-failed-{invoiceId}", "INVOICE_CHARGE_FAILED", failed, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.InvoiceChargeFailedEvent, invoiceId, failed), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, failed, cancellationToken).ConfigureAwait(false);
                return Success(new SubscriptionBillingOutcome(StatusCodes.Status402PaymentRequired, failed, true, true));
            }

            var invoice = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["invoice_id"] = invoiceId,
                ["subscription_id"] = subscriptionId,
                ["subscriber_id"] = subscription.Data.Body["subscriber_id"],
                ["plan_id"] = subscription.Data.Body["plan_id"],
                ["period"] = period,
                ["status"] = "CHARGED",
                ["price_cents"] = subscription.Data.Body["price_cents"],
                ["currency"] = subscription.Data.Body["currency"],
                ["payment_reference"] = charge.Data.PaymentReference,
                ["charged_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(SubscriptionBillingConstants.Invoices, invoiceId, invoice, null, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<SubscriptionBillingOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
            }

            await StoreAuditAsync($"invoice-{invoiceId}", "INVOICE_CHARGED", invoice, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["invoiceId"] = invoiceId,
                ["subscriptionId"] = subscriptionId,
                ["status"] = "CHARGED",
                ["paymentReference"] = charge.Data.PaymentReference,
                ["versionPin"] = stored.Data!.Version
            };

            await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.InvoiceChargedEvent, invoiceId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<SubscriptionBillingOutcome>> CancelSubscriptionAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = ReadString(request, "subscriptionId");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ValidationBody(Field("subscriptionId", "required")), false, false));
        }

        var subscription = await _documents.GetAsync(SubscriptionBillingConstants.Subscriptions, SubscriptionDocumentId(subscriptionId), cancellationToken).ConfigureAwait(false);
        if (subscription.Data is null || !IsTenantRecord(subscription.Data.Body))
        {
            return Success(new SubscriptionBillingOutcome(StatusCodes.Status404NotFound, ErrorBody("SUBSCRIPTION_NOT_FOUND", "Subscription was not found."), false, false));
        }

        var updated = new Dictionary<string, object?>(subscription.Data.Body)
        {
            ["status"] = "CANCELLED",
            ["cancelled_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["cancellation_reason"] = ReadString(request, "reason") ?? "requested"
        };
        var stored = await _documents.UpsertAsync(SubscriptionBillingConstants.Subscriptions, SubscriptionDocumentId(subscriptionId), updated, subscription.Data.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        var response = new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscriptionId,
            ["status"] = "CANCELLED",
            ["versionPin"] = stored.Data!.Version
        };
        await StoreAuditAsync($"subscription-cancel-{subscriptionId}", "SUBSCRIPTION_CANCELLED", updated, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.SubscriptionCancelledEvent, subscriptionId, response), cancellationToken).ConfigureAwait(false);
        return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, response, true, true));
    }

    public async Task<DataProcessResult<SubscriptionBillingOutcome>> RecalculateMrrAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _documents.SearchAsync(SubscriptionBillingConstants.Subscriptions, TenantFilter(), cancellationToken).ConfigureAwait(false);
        if (subscriptions.IsFailure)
        {
            return Failure<SubscriptionBillingOutcome>(subscriptions.Error!.Code, subscriptions.Error.Message, subscriptions.Error.Details);
        }

        var active = (subscriptions.Data ?? [])
            .Where(record => string.Equals(ReadString(record.Body, "status"), "ACTIVE", StringComparison.Ordinal) ||
                string.Equals(ReadString(record.Body, "status"), "TRIALING", StringComparison.Ordinal))
            .ToArray();
        var totalCents = active.Sum(record => NormalizeMonthlyCents(
            ReadInt(record.Body, "price_cents"),
            ReadString(record.Body, "billing_interval") ?? "month"));
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["active_subscriptions"] = active.Length,
            ["monthly_recurring_revenue_cents"] = totalCents,
            ["calculated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(SubscriptionBillingConstants.MrrMetrics, "active", body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SubscriptionBillingConstants.MrrRecordedEvent, "active", body), cancellationToken).ConfigureAwait(false);
        return Success(new SubscriptionBillingOutcome(StatusCodes.Status200OK, body, true, true));
    }

    public async Task<DataProcessResult<SubscriptionBillingDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _documents.SearchAsync(SubscriptionBillingConstants.Plans, TenantFilter(), cancellationToken).ConfigureAwait(false);
        var subscriptions = await _documents.SearchAsync(SubscriptionBillingConstants.Subscriptions, TenantFilter(), cancellationToken).ConfigureAwait(false);
        var invoices = await _documents.SearchAsync(SubscriptionBillingConstants.Invoices, TenantFilter(), cancellationToken).ConfigureAwait(false);
        var mrr = await _documents.GetAsync(SubscriptionBillingConstants.MrrMetrics, "active", cancellationToken).ConfigureAwait(false);

        if (plans.IsFailure)
        {
            return Failure<SubscriptionBillingDashboard>(plans.Error!.Code, plans.Error.Message, plans.Error.Details);
        }

        return Success(new SubscriptionBillingDashboard(
            (plans.Data ?? []).Select(record => record.Body).ToArray(),
            (subscriptions.Data ?? []).Select(record => record.Body).ToArray(),
            (invoices.Data ?? []).Select(record => record.Body).ToArray(),
            mrr.Data?.Body is not null && IsTenantRecord(mrr.Data.Body) ? mrr.Data.Body : new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["monthly_recurring_revenue_cents"] = 0,
                ["active_subscriptions"] = 0
            }));
    }

    private IReadOnlyDictionary<string, object?> CreatePlanBody(IReadOnlyDictionary<string, object?> request, string planId) =>
        new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["plan_id"] = planId,
            ["plan_name"] = ReadString(request, "planName") ?? ReadString(request, "name"),
            ["price_cents"] = ReadInt(request, "priceCents"),
            ["currency"] = (ReadString(request, "currency") ?? "USD").ToUpperInvariant(),
            ["billing_interval"] = ReadString(request, "billingInterval") ?? "month",
            ["status"] = "ACTIVE",
            ["created_by"] = CurrentTenant.UserId ?? "system",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

    private async Task StoreAuditAsync(
        string id,
        string auditType,
        IReadOnlyDictionary<string, object?> body,
        CancellationToken cancellationToken)
    {
        var audit = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["audit_type"] = auditType,
            ["body"] = new Dictionary<string, object?>(body),
            ["correlation_id"] = CurrentCorrelationId,
            ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SubscriptionBillingConstants.BillingAudits, id, audit, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope CreateEvent(
        string eventType,
        string subject,
        IReadOnlyDictionary<string, object?> body) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{SubscriptionBillingConstants.FlowId}",
            eventType,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(body),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = SubscriptionBillingConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private IReadOnlyDictionary<string, object?> TenantFilter() => new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId };

    private bool IsTenantRecord(IReadOnlyDictionary<string, object?> body) =>
        string.Equals(ReadString(body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal);

    private static IReadOnlyList<Dictionary<string, object?>> ValidatePlanRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "planId")))
        {
            errors.Add(Field("planId", "required"));
        }
        if (string.IsNullOrWhiteSpace(ReadString(request, "planName")) && string.IsNullOrWhiteSpace(ReadString(request, "name")))
        {
            errors.Add(Field("planName", "required"));
        }
        if (!TryReadPositiveIntegerCents(request, "priceCents", out _))
        {
            errors.Add(Field("priceCents", "positive-integer-cents-required"));
        }
        var interval = ReadString(request, "billingInterval");
        if (!string.Equals(interval, "month", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(interval, "year", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Field("billingInterval", "month-or-year-required"));
        }
        return errors;
    }

    private static IReadOnlyList<Dictionary<string, object?>> ValidateSubscriptionRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "planId")))
        {
            errors.Add(Field("planId", "required"));
        }
        if (string.IsNullOrWhiteSpace(ReadString(request, "subscriberId")))
        {
            errors.Add(Field("subscriberId", "required"));
        }
        if (request.ContainsKey("paymentMethodToken"))
        {
            errors.Add(Field("paymentMethodToken", "raw-token-forbidden"));
        }
        if (string.IsNullOrWhiteSpace(ReadString(request, "paymentMethodId")))
        {
            errors.Add(Field("paymentMethodId", "required"));
        }
        return errors;
    }

    private static IReadOnlyDictionary<string, object?> ValidationBody(params Dictionary<string, object?>[] fields) =>
        ValidationBody((IEnumerable<Dictionary<string, object?>>)fields);

    private static IReadOnlyDictionary<string, object?> ValidationBody(IEnumerable<Dictionary<string, object?>> fields) =>
        new Dictionary<string, object?>
        {
            ["code"] = "VALIDATION_FAILED",
            ["details"] = fields.ToArray()
        };

    private static Dictionary<string, object?> Field(string name, string reason) =>
        new() { ["field"] = name, ["reason"] = reason };

    private SubscriptionBillingOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request) =>
        request.ContainsKey("tenantId") || request.ContainsKey("tenant_id")
            ? new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ErrorBody("TENANT_SCOPE_FORBIDDEN", "Tenant scope is resolved by platform context."), false, false)
            : null;

    private static SubscriptionBillingOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new SubscriptionBillingOutcome(StatusCodes.Status400BadRequest, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message) =>
        new Dictionary<string, object?> { ["code"] = code, ["message"] = message };

    private async Task<IReadOnlyList<int>> ResolveDunningScheduleAsync(CancellationToken cancellationToken)
    {
        var configured = await _freedom.GetAsync("subscription_billing_dunning_schedule", cancellationToken).ConfigureAwait(false);
        if (configured.Data?.Value is string text)
        {
            var values = text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0)
                .Where(value => value > 0)
                .ToArray();
            if (values.Length > 0)
            {
                return values;
            }
        }

        return [1, 3, 7];
    }

    private static int NormalizeMonthlyCents(int priceCents, string interval) =>
        string.Equals(interval, "year", StringComparison.OrdinalIgnoreCase)
            ? (int)Math.Round(priceCents / 12.0, MidpointRounding.AwayFromZero)
            : priceCents;

    private static class StatusCodes
    {
        public const int Status200OK = 200;
        public const int Status201Created = 201;
        public const int Status400BadRequest = 400;
        public const int Status402PaymentRequired = 402;
        public const int Status404NotFound = 404;
        public const int Status504GatewayTimeout = 504;
    }

    private static string PlanDocumentId(string planId) => $"plan-{planId}";

    private static string SubscriptionDocumentId(string subscriptionId) => $"subscription-{subscriptionId}";

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement { ValueKind: JsonValueKind.Number } json => json.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool boolValue => boolValue,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            string text => bool.TryParse(text, out var parsed) && parsed,
            _ => false
        };
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
            long longValue => checked((int)longValue),
            decimal decimalValue => decimal.ToInt32(decimalValue),
            JsonElement { ValueKind: JsonValueKind.Number } json when json.TryGetInt32(out var parsed) => parsed,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool TryReadPositiveIntegerCents(IReadOnlyDictionary<string, object?> body, string key, out int cents)
    {
        cents = 0;
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        switch (value)
        {
            case int intValue:
                cents = intValue;
                return cents > 0;
            case long longValue when longValue <= int.MaxValue:
                cents = (int)longValue;
                return cents > 0;
            case JsonElement { ValueKind: JsonValueKind.Number } json when json.TryGetInt32(out var parsed):
                cents = parsed;
                return cents > 0;
            case string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                cents = parsed;
                return cents > 0;
            default:
                return false;
        }
    }
}
