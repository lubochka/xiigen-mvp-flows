using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SubscriptionBilling.Application;
using Xiigen.Flow.SubscriptionBilling.Infrastructure;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SubscriptionBilling.Tests;

public sealed class SubscriptionBillingServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = SubscriptionBillingBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(SubscriptionBillingConstants.RuleIds));
        Assert.Equal(10, SubscriptionBillingBfaRuleRegistry.Rules.Count);
        Assert.All(SubscriptionBillingBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(SubscriptionBillingConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF12_2_PlanPriceMustBePositiveIntegerCentsBeforeStateMutation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishPlanAsync(Plan("starter", 49.95), null, "bad-price");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Plans, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task PlanPublicationStoresAuditBeforePlanPublishedEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishPlanAsync(Plan("growth", 2900), null, "growth-plan");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Plans, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.BillingAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SubscriptionBillingConstants.PlanPublishedEvent);
    }

    [Fact]
    public async Task CF929_ConcurrentPlanPublicationAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var first = fixture.Service.PublishPlanAsync(Plan("race", 2500, simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.PublishPlanAsync(Plan("race", 2500, simulateSlowWrite: true), null, "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF930_PlanPublicationIdempotencyReplaysCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.PublishPlanAsync(Plan("retry", 3300), null, "same-key");
        var second = await fixture.Service.PublishPlanAsync(Plan("retry", 3300), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["planId"], second.Data?.Body["planId"]);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Plans, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.BillingAudits, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF931_InvalidPlanReturnsAllFieldErrorsAndNoPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishPlanAsync(new Dictionary<string, object?>(), null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.True(details.Count() >= 4);
        Assert.Empty((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Plans, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.BillingAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF932_PlanTimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var request = Plan("timeout", 2000);
        request["simulateFabricTimeout"] = true;
        var result = await fixture.Service.PublishPlanAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Equal(1500, result.Data?.Body["retryAfterMs"]);
        Assert.Empty((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Plans, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SubscriptionBillingConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF12_4_SubscriptionActivationRejectsRawPaymentTokensAndStoresOnlyVaultReference()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishPlanAsync(Plan("vaulted", 4100), null, "vaulted-plan");

        var rejected = await fixture.Service.SubscribeAsync(new Dictionary<string, object?>
        {
            ["planId"] = "vaulted",
            ["subscriberId"] = "subscriber-1",
            ["paymentMethodToken"] = "tok_raw",
            ["paymentMethodId"] = "pm_card"
        }, "raw-token");

        Assert.Equal(400, rejected.Data?.HttpStatus);

        var accepted = await fixture.Service.SubscribeAsync(Subscribe("vaulted", "subscriber-1", "pm_card"), "safe-token");
        var stored = (await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Subscriptions, Tenant("acme-corp"))).Data!.Single();

        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.False(stored.Body.ContainsKey("paymentMethodToken"));
        Assert.StartsWith("vault://payment-methods/", Convert.ToString(stored.Body["payment_method_ref"]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubscriptionActivationValidatesPaymentAndDoesNotLeakAcrossTenants()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.PublishPlanAsync(Plan("isolated", 5100), null, "isolated-plan");
            await fixture.Service.SubscribeAsync(Subscribe("isolated", "subscriber-1", "pm_card"), "isolated-subscription");
        }

        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Empty(dashboard.Data?.Plans ?? []);
            Assert.Empty(dashboard.Data?.Subscriptions ?? []);
        }
    }

    [Fact]
    public async Task TrialSubscriptionPreservesTrialStateAndEndDate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishPlanAsync(Plan("trial", 1200), null, "trial-plan");

        var request = Subscribe("trial", "subscriber-2", "pm_card");
        request["trialDays"] = 14;
        var result = await fixture.Service.SubscribeAsync(request, "trial-subscription");
        var subscription = (await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Subscriptions, Tenant("acme-corp"))).Data!.Single();

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("TRIALING", subscription.Body["status"]);
        Assert.NotNull(subscription.Body["trial_ends_at"]);
    }

    [Fact]
    public async Task CF12_1_CancelledSubscriptionVoidsInvoiceWithoutChargeAttemptOrAudit()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishPlanAsync(Plan("cancelled", 1900), null, "cancelled-plan");
        var subscribe = await fixture.Service.SubscribeAsync(Subscribe("cancelled", "subscriber-1", "pm_card"), "cancelled-subscription");
        await fixture.Service.CancelSubscriptionAsync(new Dictionary<string, object?> { ["subscriptionId"] = subscribe.Data!.Body["subscriptionId"] }, "cancel-key");

        var result = await fixture.Service.ProcessBillingCycleAsync(new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscribe.Data!.Body["subscriptionId"],
            ["period"] = "2026-05"
        }, "voided-cycle");

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("VOIDED", result.Data?.Body["status"]);
        Assert.DoesNotContain((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SubscriptionBillingConstants.InvoiceChargedEvent);
        Assert.DoesNotContain((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Invoices, Tenant("acme-corp"))).Data ?? [], record => Convert.ToString(record.Body["status"]) == "CHARGED");
    }

    [Fact]
    public async Task BillingSuccessStoresInvoiceAndAuditBeforeChargeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var subscriptionId = await CreateActiveSubscriptionAsync(fixture, "success", "subscriber-1", 2200);

        var result = await fixture.Service.ProcessBillingCycleAsync(new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscriptionId,
            ["period"] = "2026-05"
        }, "success-cycle");

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Invoices, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.BillingAudits, Tenant("acme-corp"))).Data ?? [], record => Convert.ToString(record.Body["audit_type"]) == "INVOICE_CHARGED");
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SubscriptionBillingConstants.InvoiceChargedEvent);
    }

    [Fact]
    public async Task CF933_BillingConcurrencyAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var subscriptionId = await CreateActiveSubscriptionAsync(fixture, "race-plan", "subscriber-1", 2400);
        var request = new Dictionary<string, object?> { ["subscriptionId"] = subscriptionId, ["period"] = "2026-05", ["simulateSlowWrite"] = true };

        var first = fixture.Service.ProcessBillingCycleAsync(request, "cycle-a");
        var second = fixture.Service.ProcessBillingCycleAsync(request, "cycle-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 200);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF934_BillingIdempotencyReplaysCachedInvoiceWithoutDoubleCharge()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var subscriptionId = await CreateActiveSubscriptionAsync(fixture, "replay-plan", "subscriber-1", 2600);
        var request = new Dictionary<string, object?> { ["subscriptionId"] = subscriptionId, ["period"] = "2026-05" };

        var first = await fixture.Service.ProcessBillingCycleAsync(request, "same-cycle");
        var second = await fixture.Service.ProcessBillingCycleAsync(request, "same-cycle");

        Assert.Equal(200, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["invoiceId"], second.Data?.Body["invoiceId"]);
        Assert.Single((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.Invoices, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SubscriptionBillingConstants.InvoiceChargedEvent);
    }

    [Fact]
    public async Task CF12_3_MrrNormalizesAnnualPlansAndUsesFreedomDunningSchedule()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Freedom.SetAsync("subscription_billing_dunning_schedule", "2,5,9,14", "string");
        await fixture.Service.PublishPlanAsync(Plan("monthly", 3000), null, "monthly-plan");
        await fixture.Service.PublishPlanAsync(Plan("annual", 120000, interval: "year"), null, "annual-plan");
        await fixture.Service.SubscribeAsync(Subscribe("monthly", "subscriber-1", "pm_card"), "monthly-sub");
        await fixture.Service.SubscribeAsync(Subscribe("annual", "subscriber-2", "pm_card"), "annual-sub");

        var mrr = await fixture.Service.RecalculateMrrAsync();
        var failedCharge = await fixture.Service.ProcessBillingCycleAsync(new Dictionary<string, object?>
        {
            ["subscriptionId"] = "sub-acme-corp-subscriber-1-monthly",
            ["period"] = "2026-05",
            ["paymentDeclined"] = true
        }, "failed-charge");

        Assert.Equal(13000, mrr.Data?.Body["monthly_recurring_revenue_cents"]);
        Assert.Equal(4, failedCharge.Data?.Body["maxAttempts"]);
        Assert.DoesNotContain((await fixture.Documents.SearchAsync(SubscriptionBillingConstants.MrrMetrics, Tenant("acme-corp"))).Data!.Single().Body.Keys, key => key.Contains("subscriber", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> CreateActiveSubscriptionAsync(FlowFixture fixture, string planId, string subscriberId, int priceCents)
    {
        await fixture.Service.PublishPlanAsync(Plan(planId, priceCents), null, $"{planId}-plan");
        var subscription = await fixture.Service.SubscribeAsync(Subscribe(planId, subscriberId, "pm_card"), $"{planId}-subscription");
        return Convert.ToString(subscription.Data!.Body["subscriptionId"])!;
    }

    private static Dictionary<string, object?> Plan(
        string planId,
        object priceCents,
        string interval = "month",
        bool simulateSlowWrite = false) =>
        new()
        {
            ["planId"] = planId,
            ["planName"] = $"{planId} plan",
            ["priceCents"] = priceCents,
            ["currency"] = "USD",
            ["billingInterval"] = interval,
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> Subscribe(string planId, string subscriberId, string paymentMethodId) =>
        new()
        {
            ["planId"] = planId,
            ["subscriberId"] = subscriberId,
            ["paymentMethodId"] = paymentMethodId
        };

    private static Dictionary<string, object?> Tenant(string tenantId) => new() { ["tenant_id"] = tenantId };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            SubscriptionBillingService service,
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
        public SubscriptionBillingService Service { get; }
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
            var payment = new InMemorySubscriptionPaymentFabric();
            var service = new SubscriptionBillingService(
                documents,
                idempotency,
                outbox,
                freedom,
                payment,
                tenantAccessor,
                correlationAccessor,
                NullLogger<SubscriptionBillingService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenantId}", "test", DateTimeOffset.UtcNow));
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" ? null : "test-user",
                new HashSet<string> { role },
                $"corr-{tenantId}",
                "test",
                DateTimeOffset.UtcNow));
            return new CompositeDisposable(correlation, tenant);
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
