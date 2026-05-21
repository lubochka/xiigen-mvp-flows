using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.MarketplacePayments.Application;

public sealed class MarketplacePaymentsService : MicroserviceBase<MarketplacePaymentsService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CartLockTtl = TimeSpan.FromMinutes(5);
    private const int DefaultPlatformFeeBps = 290;
    private const int DefaultEscrowAutoReleaseDays = 14;

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _checkoutLocks = new(StringComparer.Ordinal);

    public MarketplacePaymentsService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MarketplacePaymentsService> logger)
        : base(
            new ServiceDescriptor("marketplace-payments-service", "Marketplace Payments", MarketplacePaymentsConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<MarketplacePaymentsOutcome>> CheckoutAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateCheckout(request);
        if (validation.Count > 0)
        {
            return Success(new MarketplacePaymentsOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var cartId = ReadString(request, "cartId")!.Trim();
        var totalAmountCents = ReadInt(request, "totalAmountCents");
        var operationKey = $"{MarketplacePaymentsConstants.FlowSlug}:checkout:{CurrentTenant.TenantId}:{cartId}:{totalAmountCents}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<MarketplacePaymentsOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }

        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new MarketplacePaymentsOutcome(200, reservation.Data.CachedResponse, true, false));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = ErrorBody("TIMEOUT", "Payment processing timed out before confirmation.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["cartId"] = cartId
            });
            await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.TimeoutRetainedEvent, cartId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Payment processing timed out", cancellationToken).ConfigureAwait(false);
            return Success(new MarketplacePaymentsOutcome(504, timeoutBody, false, true));
        }

        var gate = _checkoutLocks.GetOrAdd($"{CurrentTenant.TenantId}:{cartId}", _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<MarketplacePaymentsOutcome>("CONCURRENT_UPDATE", "Concurrent checkout detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var buyerTenantId = ReadString(request, "buyerTenantId") ?? CurrentTenant.TenantId;
            if (!string.Equals(buyerTenantId, CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BOLA_VIOLATION", "This cart belongs to a different storefront.");
                await StoreAuditAsync(MarketplacePaymentsConstants.CheckoutAudits, $"checkout-forbidden:{cartId}", "CHECKOUT_FORBIDDEN", rejected, cancellationToken).ConfigureAwait(false);
                await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.CheckoutRejectedEvent, cartId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new MarketplacePaymentsOutcome(403, rejected, true, true));
            }

            var lockRead = await _documents.GetAsync(MarketplacePaymentsConstants.CartLocks, $"cart-lock:{cartId}", cancellationToken).ConfigureAwait(false);
            if (lockRead.IsFailure)
            {
                return Failure<MarketplacePaymentsOutcome>(lockRead.Error!.Code, lockRead.Error.Message, lockRead.Error.Details);
            }

            if (lockRead.Data is not null)
            {
                return Success(new MarketplacePaymentsOutcome(409, ErrorBody("CHECKOUT_ALREADY_IN_PROGRESS", "This checkout is already being processed."), false, false));
            }

            await _documents.UpsertAsync(
                MarketplacePaymentsConstants.CartLocks,
                $"cart-lock:{cartId}",
                WithTenant(new Dictionary<string, object?>
                {
                    ["cart_id"] = cartId,
                    ["locked_at"] = Now(),
                    ["expires_at"] = DateTimeOffset.UtcNow.Add(CartLockTtl).ToString("O", CultureInfo.InvariantCulture),
                    ["ttl_ms"] = CartLockTtl.TotalMilliseconds
                }),
                null,
                cancellationToken).ConfigureAwait(false);

            var inventoryId = $"inventory:{ReadString(request, "itemId") ?? "primary"}";
            var currentInventory = await _documents.GetAsync(MarketplacePaymentsConstants.Inventory, inventoryId, cancellationToken).ConfigureAwait(false);
            if (currentInventory.IsFailure)
            {
                return Failure<MarketplacePaymentsOutcome>(currentInventory.Error!.Code, currentInventory.Error.Message, currentInventory.Error.Details);
            }

            if (currentInventory.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, currentInventory.Data.Version, StringComparison.Ordinal))
            {
                return Failure<MarketplacePaymentsOutcome>("CONCURRENT_UPDATE", "Inventory version does not match.", new Dictionary<string, object?> { ["currentVersion"] = currentInventory.Data.Version });
            }

            var quantity = Math.Max(1, ReadInt(request, "quantity"));
            var available = currentInventory.Data is null ? 100 : ReadInt(currentInventory.Data.Body, "available");
            if (available < quantity)
            {
                return Success(new MarketplacePaymentsOutcome(409, ErrorBody("INVENTORY_UNAVAILABLE", "Not enough inventory remains for this purchase."), false, false));
            }

            var inventory = await _documents.UpsertAsync(
                MarketplacePaymentsConstants.Inventory,
                inventoryId,
                WithTenant(new Dictionary<string, object?>
                {
                    ["item_id"] = ReadString(request, "itemId") ?? "primary",
                    ["cart_id"] = cartId,
                    ["available"] = available - quantity,
                    ["reserved"] = quantity,
                    ["reserved_at"] = Now(),
                    ["reservation_status"] = "RESERVED"
                }),
                currentInventory.Data?.Version,
                cancellationToken).ConfigureAwait(false);
            if (inventory.IsFailure)
            {
                return Failure<MarketplacePaymentsOutcome>(inventory.Error!.Code, inventory.Error.Message, inventory.Error.Details);
            }

            var split = await CaptureSplitAndHoldEscrowAsync(request, cartId, totalAmountCents, cancellationToken).ConfigureAwait(false);
            if (split.IsFailure)
            {
                return Failure<MarketplacePaymentsOutcome>(split.Error!.Code, split.Error.Message, split.Error.Details);
            }

            var payment = split.Data!;
            var checkoutAudit = WithTenant(new Dictionary<string, object?>
            {
                ["cart_id"] = cartId,
                ["item_id"] = ReadString(request, "itemId") ?? "primary",
                ["quantity"] = quantity,
                ["total_amount_cents"] = totalAmountCents,
                ["action"] = "CHECKOUT_RESERVED",
                ["reserved_at"] = Now()
            });
            await StoreAuditAsync(MarketplacePaymentsConstants.CheckoutAudits, $"checkout:{cartId}", "CHECKOUT_RESERVED", checkoutAudit, cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["id"] = payment["orderId"],
                ["cartId"] = cartId,
                ["escrowId"] = payment["escrowId"],
                ["status"] = "CONFIRMED",
                ["version"] = inventory.Data!.Version,
                ["totalAmountCents"] = totalAmountCents,
                ["platformFeeCents"] = payment["platformFeeCents"],
                ["sellerAmountCents"] = payment["sellerAmountCents"],
                ["feeSplitStatus"] = "BALANCED",
                ["escrowStatus"] = "HELD"
            };
            await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.CheckoutReservedEvent, cartId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.MarketplaceOrderConfirmedEvent, Convert.ToString(payment["orderId"], CultureInfo.InvariantCulture)!, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new MarketplacePaymentsOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<MarketplacePaymentsOutcome>> ReleaseEscrowAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequired(request, "escrowId", "orderId");
        if (validation.Count > 0)
        {
            return Success(new MarketplacePaymentsOutcome(400, ValidationBody(validation), false, false));
        }

        var escrowId = ReadString(request, "escrowId")!;
        var operationKey = $"{MarketplacePaymentsConstants.FlowSlug}:release:{CurrentTenant.TenantId}:{escrowId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var escrow = await _documents.GetAsync(MarketplacePaymentsConstants.EscrowHolds, escrowId, cancellationToken).ConfigureAwait(false);
        if (escrow.IsFailure)
        {
            return Failure<MarketplacePaymentsOutcome>(escrow.Error!.Code, escrow.Error.Message, escrow.Error.Details);
        }

        if (escrow.Data is null || !string.Equals(ReadString(escrow.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
        {
            return Failure<MarketplacePaymentsOutcome>("NOT_FOUND", "Escrow record was not found for this storefront.");
        }

        var status = ReadString(escrow.Data.Body, "status") ?? "HELD";
        if (status is not ("HELD" or "DISPUTED"))
        {
            return Success(new MarketplacePaymentsOutcome(409, ErrorBody("INVALID_ESCROW_TRANSITION", "Only held or disputed escrow can be released."), false, false));
        }

        var body = WithTenant(new Dictionary<string, object?>(escrow.Data.Body)
        {
            ["status"] = "RELEASED",
            ["released_at"] = Now(),
            ["auto_release_days"] = await ResolveIntConfigAsync("escrow_auto_release_days", DefaultEscrowAutoReleaseDays, cancellationToken).ConfigureAwait(false)
        });
        var stored = await _documents.UpsertAsync(MarketplacePaymentsConstants.EscrowHolds, escrowId, body, escrow.Data.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<MarketplacePaymentsOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync(MarketplacePaymentsConstants.EscrowAudits, $"release:{escrowId}:{Guid.NewGuid():N}", "ESCROW_RELEASED", body, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["escrowId"] = escrowId,
            ["orderId"] = ReadString(request, "orderId"),
            ["status"] = "RELEASED",
            ["version"] = stored.Data!.Version
        };
        await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.EscrowReleasedEvent, escrowId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new MarketplacePaymentsOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<MarketplacePaymentsOutcome>> DisputeEscrowAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequired(request, "escrowId", "orderId", "sellerId");
        if (validation.Count > 0)
        {
            return Success(new MarketplacePaymentsOutcome(400, ValidationBody(validation), false, false));
        }

        var escrowId = ReadString(request, "escrowId")!;
        var orderId = ReadString(request, "orderId")!;
        var sellerId = ReadString(request, "sellerId")!;
        var disputedAt = Now();
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["escrow_id"] = escrowId,
            ["order_id"] = orderId,
            ["seller_id"] = sellerId,
            ["status"] = "DISPUTED",
            ["access_blocked"] = true,
            ["reason"] = ReadString(request, "reason") ?? "buyer_dispute",
            ["disputed_at"] = disputedAt
        });
        await _documents.UpsertAsync(MarketplacePaymentsConstants.EscrowHolds, escrowId, body, null, cancellationToken).ConfigureAwait(false);

        var hold = WithTenant(new Dictionary<string, object?>
        {
            ["seller_id"] = sellerId,
            ["order_id"] = orderId,
            ["escrow_id"] = escrowId,
            ["status"] = "FROZEN",
            ["notified"] = true,
            ["notified_at"] = disputedAt
        });
        await _documents.UpsertAsync(MarketplacePaymentsConstants.SellerPayoutHolds, $"hold:{sellerId}:{orderId}", hold, null, cancellationToken).ConfigureAwait(false);
        await StoreAuditAsync(MarketplacePaymentsConstants.EscrowAudits, $"dispute:{escrowId}:{Guid.NewGuid():N}", "ESCROW_DISPUTED", body, cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["escrowId"] = escrowId,
            ["orderId"] = orderId,
            ["status"] = "DISPUTED",
            ["sellerPayoutFrozen"] = true,
            ["sellerNotified"] = true
        };
        await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.PayoutHoldNotifiedEvent, sellerId, response), cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.EscrowDisputedEvent, escrowId, response), cancellationToken).ConfigureAwait(false);
        return Success(new MarketplacePaymentsOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<MarketplacePaymentsOutcome>> ReversePaymentAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequired(request, "orderId", "escrowId");
        if (validation.Count > 0)
        {
            return Success(new MarketplacePaymentsOutcome(400, ValidationBody(validation), false, false));
        }

        var orderId = ReadString(request, "orderId")!;
        var escrowId = ReadString(request, "escrowId")!;
        var executed = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var step in MarketplacePaymentsConstants.CompensationStepNames)
        {
            var row = WithTenant(new Dictionary<string, object?>
            {
                ["order_id"] = orderId,
                ["escrow_id"] = escrowId,
                ["step"] = step,
                ["executed_at"] = Now()
            });
            await _documents.UpsertAsync(MarketplacePaymentsConstants.CompensationStepRecords, $"compensation:{orderId}:{step}", row, null, cancellationToken).ConfigureAwait(false);
            executed.Add(row);
        }

        await StoreAuditAsync(MarketplacePaymentsConstants.EscrowAudits, $"reverse:{orderId}:{Guid.NewGuid():N}", "PAYMENT_REVERSED", new Dictionary<string, object?> { ["steps"] = MarketplacePaymentsConstants.CompensationStepNames }, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["orderId"] = orderId,
            ["escrowId"] = escrowId,
            ["status"] = "REVERSED",
            ["compensationSteps"] = MarketplacePaymentsConstants.CompensationStepNames,
            ["executed"] = executed
        };
        await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.PaymentReversedEvent, orderId, response), cancellationToken).ConfigureAwait(false);
        return Success(new MarketplacePaymentsOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<MarketplacePaymentsOutcome>> WritePayoutAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequired(request, "orderId", "escrowId", "sellerId");
        if (ReadInt(request, "sellerAmountCents") <= 0)
        {
            validation.Add(FieldError("sellerAmountCents", "Amount must be greater than zero."));
        }
        if (validation.Count > 0)
        {
            return Success(new MarketplacePaymentsOutcome(400, ValidationBody(validation), false, false));
        }

        var orderId = ReadString(request, "orderId")!;
        var operationKey = $"{MarketplacePaymentsConstants.FlowSlug}:payout:{CurrentTenant.TenantId}:{orderId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var sellerId = ReadString(request, "sellerId")!;
        var vault = await _documents.GetAsync(MarketplacePaymentsConstants.SellerVaultRefs, $"vault:{sellerId}", cancellationToken).ConfigureAwait(false);
        if (vault.IsFailure)
        {
            return Failure<MarketplacePaymentsOutcome>(vault.Error!.Code, vault.Error.Message, vault.Error.Details);
        }

        var payoutVaultRef = vault.Data is null
            ? $"vault-ref:{CurrentTenant.TenantId}:{sellerId}"
            : ReadString(vault.Data.Body, "payoutVaultRef") ?? $"vault-ref:{CurrentTenant.TenantId}:{sellerId}";
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["order_id"] = orderId,
            ["escrow_id"] = ReadString(request, "escrowId"),
            ["seller_id"] = sellerId,
            ["amount_cents"] = ReadInt(request, "sellerAmountCents"),
            ["payout_vault_ref"] = payoutVaultRef,
            ["status"] = "PAYOUT_WRITTEN",
            ["written_at"] = Now()
        });
        await _documents.UpsertAsync(MarketplacePaymentsConstants.PayoutLocks, $"payout-lock:{orderId}", WithTenant(new Dictionary<string, object?> { ["order_id"] = orderId, ["locked_at"] = Now() }), null, cancellationToken).ConfigureAwait(false);
        var stored = await _documents.UpsertAsync(MarketplacePaymentsConstants.PayoutRecords, $"payout:{orderId}", body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<MarketplacePaymentsOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
        }

        await StoreAuditAsync(MarketplacePaymentsConstants.PayoutAudits, $"payout:{orderId}:{Guid.NewGuid():N}", "PAYOUT_WRITTEN", body, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["orderId"] = orderId,
            ["status"] = "PAYOUT_WRITTEN",
            ["payoutVaultRef"] = payoutVaultRef,
            ["version"] = stored.Data!.Version
        };
        await _outbox.AppendAsync(CreateEvent(MarketplacePaymentsConstants.PayoutCompletedEvent, orderId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new MarketplacePaymentsOutcome(200, response, true, true));
    }

    public async Task<DataProcessResult<MarketplacePaymentsDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var payments = await SearchTenantAsync(MarketplacePaymentsConstants.PaymentRecords, cancellationToken).ConfigureAwait(false);
        var escrows = await SearchTenantAsync(MarketplacePaymentsConstants.EscrowHolds, cancellationToken).ConfigureAwait(false);
        var payouts = await SearchTenantAsync(MarketplacePaymentsConstants.PayoutRecords, cancellationToken).ConfigureAwait(false);
        var refunds = await SearchTenantAsync(MarketplacePaymentsConstants.RefundQueue, cancellationToken).ConfigureAwait(false);
        if (payments.IsFailure || escrows.IsFailure || payouts.IsFailure || refunds.IsFailure)
        {
            var error = payments.Error ?? escrows.Error ?? payouts.Error ?? refunds.Error!;
            return Failure<MarketplacePaymentsDashboard>(error.Code, error.Message, error.Details);
        }

        var paymentBodies = (payments.Data ?? []).Select(record => record.Body).ToArray();
        var escrowBodies = (escrows.Data ?? []).Select(record => record.Body).ToArray();
        var payoutBodies = (payouts.Data ?? []).Select(record => record.Body).ToArray();
        var refundBodies = (refunds.Data ?? []).Select(record => record.Body).ToArray();
        var gross = paymentBodies.Sum(body => ReadInt(body, "total_amount_cents"));
        var platform = paymentBodies.Sum(body => ReadInt(body, "platform_fee_cents"));
        return Success(new MarketplacePaymentsDashboard(
            paymentBodies,
            escrowBodies,
            payoutBodies,
            refundBodies,
            new Dictionary<string, object?>
            {
                ["grossVolumeCents"] = gross,
                ["platformRevenueCents"] = platform,
                ["heldEscrows"] = escrowBodies.Count(body => string.Equals(ReadString(body, "status"), "HELD", StringComparison.Ordinal)),
                ["disputedEscrows"] = escrowBodies.Count(body => string.Equals(ReadString(body, "status"), "DISPUTED", StringComparison.Ordinal)),
                ["payoutsWritten"] = payoutBodies.Length
            }));
    }

    private async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> CaptureSplitAndHoldEscrowAsync(
        IReadOnlyDictionary<string, object?> request,
        string cartId,
        int totalAmountCents,
        CancellationToken cancellationToken)
    {
        var feeBps = await ResolveIntConfigAsync("marketplace_platform_fee_bps", DefaultPlatformFeeBps, cancellationToken).ConfigureAwait(false);
        var platformFeeCents = (int)Math.Round(totalAmountCents * (feeBps / 10000m), MidpointRounding.AwayFromZero);
        var sellerAmountCents = totalAmountCents - platformFeeCents;
        if (platformFeeCents + sellerAmountCents != totalAmountCents)
        {
            return Failure<IReadOnlyDictionary<string, object?>>("SPLIT_IMBALANCE", "Payment split does not sum to total.");
        }

        var orderId = $"order-{Hash($"{CurrentTenant.TenantId}:{cartId}:{totalAmountCents}")[..16]}";
        var escrowId = $"escrow-{Hash($"{orderId}:{ReadString(request, "sellerId") ?? "seller"}")[..16]}";
        var paymentBody = WithTenant(new Dictionary<string, object?>
        {
            ["order_id"] = orderId,
            ["cart_id"] = cartId,
            ["buyer_role"] = CurrentTenant.Roles.FirstOrDefault() ?? "anonymous",
            ["seller_id"] = ReadString(request, "sellerId") ?? "seller-1",
            ["total_amount_cents"] = totalAmountCents,
            ["platform_fee_cents"] = platformFeeCents,
            ["seller_amount_cents"] = sellerAmountCents,
            ["card_last4"] = Last4(ReadString(request, "cardNumber") ?? "4242424242424242"),
            ["payment_status"] = ReadBool(request, "fraudHold") ? "FRAUD_REVIEW" : "CAPTURED",
            ["captured_at"] = Now()
        });
        var payment = await _documents.UpsertAsync(MarketplacePaymentsConstants.PaymentRecords, $"payment:{orderId}", paymentBody, null, cancellationToken).ConfigureAwait(false);
        if (payment.IsFailure)
        {
            return Failure<IReadOnlyDictionary<string, object?>>(payment.Error!.Code, payment.Error.Message, payment.Error.Details);
        }

        await AppendNonRepudiationAuditAsync(orderId, paymentBody, cancellationToken).ConfigureAwait(false);
        var escrowBody = WithTenant(new Dictionary<string, object?>
        {
            ["escrow_id"] = escrowId,
            ["order_id"] = orderId,
            ["seller_id"] = ReadString(request, "sellerId") ?? "seller-1",
            ["status"] = "HELD",
            ["total_amount_cents"] = totalAmountCents,
            ["seller_amount_cents"] = sellerAmountCents,
            ["platform_fee_cents"] = platformFeeCents,
            ["held_at"] = Now(),
            ["auto_release_days"] = await ResolveIntConfigAsync("escrow_auto_release_days", DefaultEscrowAutoReleaseDays, cancellationToken).ConfigureAwait(false)
        });
        var escrow = await _documents.UpsertAsync(MarketplacePaymentsConstants.EscrowHolds, escrowId, escrowBody, null, cancellationToken).ConfigureAwait(false);
        if (escrow.IsFailure)
        {
            return Failure<IReadOnlyDictionary<string, object?>>(escrow.Error!.Code, escrow.Error.Message, escrow.Error.Details);
        }

        await StoreAuditAsync(MarketplacePaymentsConstants.PaymentAudits, $"payment:{orderId}:{Guid.NewGuid():N}", "PAYMENT_CAPTURED", paymentBody, cancellationToken).ConfigureAwait(false);
        await StoreAuditAsync(MarketplacePaymentsConstants.EscrowAudits, $"escrow-held:{escrowId}:{Guid.NewGuid():N}", "ESCROW_HELD", escrowBody, cancellationToken).ConfigureAwait(false);
        return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>
        {
            ["orderId"] = orderId,
            ["escrowId"] = escrowId,
            ["platformFeeCents"] = platformFeeCents,
            ["sellerAmountCents"] = sellerAmountCents
        });
    }

    private async Task AppendNonRepudiationAuditAsync(string orderId, IReadOnlyDictionary<string, object?> scrubbedPayment, CancellationToken cancellationToken)
    {
        var existing = await _documents.SearchAsync(MarketplacePaymentsConstants.NonRepudiationAudits, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
        var last = existing.Data?.LastOrDefault();
        var previousHash = last is not null && last.Body.TryGetValue("record_hash", out var hash)
            ? Convert.ToString(hash, CultureInfo.InvariantCulture) ?? new string('0', 64)
            : new string('0', 64);
        var payload = JsonSerializer.Serialize(scrubbedPayment);
        await _documents.UpsertAsync(MarketplacePaymentsConstants.NonRepudiationAudits, $"audit:{orderId}:{Guid.NewGuid():N}", WithTenant(new Dictionary<string, object?>
        {
            ["order_id"] = orderId,
            ["previous_hash"] = previousHash,
            ["record_hash"] = Hash($"{previousHash}:{payload}"),
            ["payload_hash"] = Hash(payload),
            ["created_at"] = Now()
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MarketplacePaymentsOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new MarketplacePaymentsOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new MarketplacePaymentsOutcome(200, reservation.Data.CachedResponse, true, false)
            : null;
    }

    private async Task<int> ResolveIntConfigAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var doc = await _documents.GetAsync(MarketplacePaymentsConstants.FreedomConfig, $"config:{CurrentTenant.TenantId}:{key}", cancellationToken).ConfigureAwait(false);
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
            MarketplacePaymentsConstants.FlowId,
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
                ["xiigenflowid"] = MarketplacePaymentsConstants.FlowId
            });

    private static List<Dictionary<string, object?>> ValidateCheckout(IReadOnlyDictionary<string, object?> request)
    {
        var errors = ValidateRequired(request, "cartId", "itemId", "sellerId");
        if (ReadInt(request, "totalAmountCents") <= 0)
        {
            errors.Add(FieldError("totalAmountCents", "Amount must be greater than zero."));
        }
        if (ReadInt(request, "quantity") <= 0)
        {
            errors.Add(FieldError("quantity", "Quantity must be greater than zero."));
        }
        return errors;
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

    private static MarketplacePaymentsOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new MarketplacePaymentsOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static MarketplacePaymentsOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key =>
            string.Equals(key, "tenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "tenant_id", StringComparison.OrdinalIgnoreCase));
        return forbidden is null
            ? null
            : new MarketplacePaymentsOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
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

    private static string Last4(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : "0000";
    }

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

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
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && value is bool boolValue && boolValue;
}
