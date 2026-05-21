using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MarketplacePayments.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.MarketplacePayments.Tests;

public sealed class MarketplacePaymentsServiceTests
{
    [Fact]
    public void P11_Rules_AllMarketplacePaymentRules_AreRegistered()
    {
        var ids = MarketplacePaymentsBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(MarketplacePaymentsConstants.RuleIds));
        Assert.Equal(11, MarketplacePaymentsBfaRuleRegistry.Rules.Count);
        Assert.All(MarketplacePaymentsBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(MarketplacePaymentsConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF16_1_CheckoutValidatesOwnershipLocksInventoryAuditsThenEmits()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.CheckoutAsync(Checkout("cart-a"), null, "checkout-a");
        var inventory = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.Inventory, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.CheckoutAudits, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("CONFIRMED", result.Data?.Body["status"]);
        Assert.Equal("BALANCED", result.Data?.Body["feeSplitStatus"]);
        Assert.Single(inventory);
        Assert.Single(audits);
        Assert.Contains(outbox, record => record.Event.Type == MarketplacePaymentsConstants.CheckoutReservedEvent);
        Assert.Equal(MarketplacePaymentsConstants.MarketplaceOrderConfirmedEvent, outbox.Last().Event.Type);
    }

    [Fact]
    public async Task CF16_4_BolaMismatchRejectsBeforeInventoryReservation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Checkout("cart-b");
        request["buyerTenantId"] = "northwind";

        var result = await fixture.Service.CheckoutAsync(request, null, "checkout-b");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("BOLA_VIOLATION", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.Inventory, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == MarketplacePaymentsConstants.CheckoutRejectedEvent);
    }

    [Fact]
    public async Task CF949_ConcurrentCheckoutAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.Service.CheckoutAsync(Checkout("cart-race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.CheckoutAsync(Checkout("cart-race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF950_IdempotencyReplaysCheckoutWithoutDuplicateChargeEscrowAuditOrEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.CheckoutAsync(Checkout("cart-replay"), null, "same-key");
        var second = await fixture.Service.CheckoutAsync(Checkout("cart-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.PaymentRecords, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.EscrowHolds, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.CheckoutAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == MarketplacePaymentsConstants.MarketplaceOrderConfirmedEvent);
    }

    [Fact]
    public async Task CF951_InvalidCheckoutReturnsFieldErrorsAndNoPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.CheckoutAsync(new Dictionary<string, object?>(), null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.Contains(details, detail => Convert.ToString(detail["field"], CultureInfo.InvariantCulture) == "cartId");
        Assert.Empty((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.PaymentRecords, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF952_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Checkout("cart-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.CheckoutAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == MarketplacePaymentsConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.PaymentRecords, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF16_2_PaymentSplitSumsToTotalScrubsPiiAndUsesAppendOnlyHashChain()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Service.CheckoutAsync(Checkout("cart-pii-a", total: 10000), null, "pii-a");
        await fixture.Service.CheckoutAsync(Checkout("cart-pii-b", total: 12500), null, "pii-b");
        var payments = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.PaymentRecords, Tenant("acme-corp"))).Data ?? [];
        var chain = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.NonRepudiationAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.All(payments, payment =>
        {
            var platform = ToInt(payment.Body["platform_fee_cents"]);
            var seller = ToInt(payment.Body["seller_amount_cents"]);
            var total = ToInt(payment.Body["total_amount_cents"]);
            Assert.Equal(total, platform + seller);
            Assert.False(ContainsForbiddenPaymentData(payment.Body));
            Assert.True(payment.Body.ContainsKey("card_last4"));
        });
        Assert.Equal(2, chain.Count);
        Assert.Equal(new string('0', 64), chain[0].Body["previous_hash"]);
        Assert.Equal(chain[0].Body["record_hash"], chain[1].Body["previous_hash"]);
    }

    [Fact]
    public async Task CF16_3_ReleaseEscrowAuditsBeforeReleaseEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var checkout = await fixture.Service.CheckoutAsync(Checkout("cart-release"), null, "checkout-release");
        var escrowId = Convert.ToString(checkout.Data!.Body["escrowId"], CultureInfo.InvariantCulture)!;

        var result = await fixture.Service.ReleaseEscrowAsync(new Dictionary<string, object?>
        {
            ["escrowId"] = escrowId,
            ["orderId"] = checkout.Data.Body["id"]
        }, "release-key");
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.EscrowAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("RELEASED", result.Data?.Body["status"]);
        Assert.Contains(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "ESCROW_RELEASED");
        Assert.Equal(MarketplacePaymentsConstants.EscrowReleasedEvent, outbox.Last().Event.Type);
    }

    [Fact]
    public async Task CF262_CF265_DisputeFreezesSellerPayoutAndNotifiesBeforeDisputeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var result = await fixture.Service.DisputeEscrowAsync(new Dictionary<string, object?>
        {
            ["escrowId"] = "escrow-1",
            ["orderId"] = "order-1",
            ["sellerId"] = "seller-1",
            ["reason"] = "not-delivered"
        });
        var holds = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.SellerPayoutHolds, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Single(holds);
        Assert.Equal("FROZEN", holds[0].Body["status"]);
        Assert.Equal(true, holds[0].Body["notified"]);
        Assert.Equal(MarketplacePaymentsConstants.PayoutHoldNotifiedEvent, outbox[^2].Event.Type);
        Assert.Equal(MarketplacePaymentsConstants.EscrowDisputedEvent, outbox[^1].Event.Type);
    }

    [Fact]
    public async Task CF16_3_ReversalRunsCompensationInLifoOrder()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.ReversePaymentAsync(new Dictionary<string, object?>
        {
            ["orderId"] = "order-2",
            ["escrowId"] = "escrow-2"
        });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal(MarketplacePaymentsConstants.CompensationStepNames, Assert.IsAssignableFrom<string[]>(result.Data!.Body["compensationSteps"]));
        var records = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.CompensationStepRecords, Tenant("acme-corp"))).Data ?? [];
        Assert.Equal(MarketplacePaymentsConstants.CompensationStepNames, records.Select(record => Convert.ToString(record.Body["step"], CultureInfo.InvariantCulture)).ToArray());
    }

    [Fact]
    public async Task PayoutWriterIsIdempotentAndStoresOnlyVaultReference()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");
        await fixture.Documents.UpsertAsync(MarketplacePaymentsConstants.SellerVaultRefs, "vault:seller-1", Tenant(new Dictionary<string, object?>
        {
            ["seller_id"] = "seller-1",
            ["payoutVaultRef"] = "vault-ref:acme:seller-1"
        }, "acme-corp"), null);

        var first = await fixture.Service.WritePayoutAsync(Payout("order-3"), "payout-key");
        var second = await fixture.Service.WritePayoutAsync(Payout("order-3"), "payout-key");
        var payouts = (await fixture.Documents.SearchAsync(MarketplacePaymentsConstants.PayoutRecords, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(200, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(payouts);
        Assert.Equal("vault-ref:acme:seller-1", payouts[0].Body["payout_vault_ref"]);
        Assert.DoesNotContain("bank", JsonSerializer.Serialize(payouts[0].Body), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardReadsOnlyCurrentTenantPaymentsEscrowsPayoutsAndRefunds()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_user"))
        {
            await fixture.Service.CheckoutAsync(Checkout("cart-acme"), null, "acme-key");
        }

        using (fixture.PushContext("northwind", "tenant_user"))
        {
            await fixture.Service.CheckoutAsync(Checkout("cart-northwind"), null, "northwind-key");
            var dashboard = await fixture.Service.GetDashboardAsync();

            Assert.Single(dashboard.Data?.Payments ?? []);
            Assert.Single(dashboard.Data?.Escrows ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["heldEscrows"]));
        }
    }

    [Fact]
    public void CF268_ServiceDoesNotImportOrMutateSocialGraph()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.MarketplacePayments.Application", "MarketplacePaymentsService.cs"));

        Assert.DoesNotContain("F234", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SocialGraph", source, StringComparison.Ordinal);
        Assert.DoesNotContain("friend", source, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> Checkout(string cartId, int total = 10000, bool simulateSlowWrite = false) =>
        new()
        {
            ["cartId"] = cartId,
            ["itemId"] = "item-1",
            ["sellerId"] = "seller-1",
            ["totalAmountCents"] = total,
            ["quantity"] = 1,
            ["cardNumber"] = "4242424242424242",
            ["cvv"] = "123",
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> Payout(string orderId) =>
        new()
        {
            ["orderId"] = orderId,
            ["escrowId"] = $"escrow-{orderId}",
            ["sellerId"] = "seller-1",
            ["sellerAmountCents"] = 9700
        };

    private static Dictionary<string, object?> Tenant(string tenant) => new() { ["tenant_id"] = tenant };

    private static IReadOnlyDictionary<string, object?> Tenant(IReadOnlyDictionary<string, object?> body, string tenant)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = tenant
        };
        return copy;
    }

    private static int ToInt(object? value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static bool ContainsForbiddenPaymentData(IReadOnlyDictionary<string, object?> body)
    {
        var json = JsonSerializer.Serialize(body);
        return json.Contains("cardNumber", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("cvv", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("bankAccountNumber", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("4242424242424242", StringComparison.Ordinal);
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            MarketplacePaymentsService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public MarketplacePaymentsService Service { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var freedom = new InMemoryFreedomConfigService();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var service = new MarketplacePaymentsService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<MarketplacePaymentsService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenant, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenant}", "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantContextAccessor.Push(new TenantContext(
                tenant,
                role == "anonymous" ? null : "test-user",
                new HashSet<string> { role },
                $"corr-{tenant}",
                "test",
                DateTimeOffset.UtcNow));
            return new CompositeDisposable(correlation, tenantScope);
        }
    }

    private sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
