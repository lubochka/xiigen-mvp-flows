using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SaasMultiTenancy.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SaasMultiTenancy.Tests;

public sealed class SaasMultiTenancyServiceTests
{
    [Fact]
    public void P11_Rules_AllSaasMultiTenancyRules_AreRegistered()
    {
        var ids = SaasMultiTenancyBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(SaasMultiTenancyConstants.RuleIds));
        Assert.Equal(8, SaasMultiTenancyBfaRuleRegistry.Rules.Count);
        Assert.All(SaasMultiTenancyBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(SaasMultiTenancyConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF151_ProvisioningSeedsFreedomActivatesAuditsMaterializesQuotasAndThenEmits()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ProvisionTenantAsync(Provision("acme"), null, "provision-acme");
        var tenants = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? [];
        var freedom = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.FreedomConfig, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.ProvisionAudits, Tenant("acme-corp"))).Data ?? [];
        var quotas = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.QuotaCounters, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("ACTIVE", result.Data?.Body["status"]);
        Assert.Equal("trusted-tier-definition", result.Data?.Body["masterTenantIdSource"]);
        Assert.Single(tenants);
        Assert.Equal(4, freedom.Count);
        Assert.Single(audits);
        Assert.Single(quotas);
        Assert.Contains(outbox, record => record.Event.Type == SaasMultiTenancyConstants.TenantQuotaMaterializedEvent);
        Assert.True(outbox.Last().Event.Type == SaasMultiTenancyConstants.TenantProvisionedEvent);
    }

    [Fact]
    public async Task CF945_ConcurrentProvisioningAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = fixture.Service.ProvisionTenantAsync(Provision("race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.ProvisionTenantAsync(Provision("race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF946_IdempotencyReplaysCachedProvisioningWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.ProvisionTenantAsync(Provision("retry"), null, "same-key");
        var second = await fixture.Service.ProvisionTenantAsync(Provision("retry"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.ProvisionAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.QuotaCounters, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SaasMultiTenancyConstants.TenantProvisionedEvent);
    }

    [Fact]
    public async Task CF947_InvalidProvisioningReturnsFieldErrorsAndNoPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ProvisionTenantAsync(new Dictionary<string, object?>(), null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.Contains(details, detail => Convert.ToString(detail["field"]) == "tenantSlug");
        Assert.Empty((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF948_ProvisioningTimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Provision("timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.ProvisionTenantAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Equal(5000, result.Data?.Body["retryAfterMs"]);
        Assert.Empty((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SaasMultiTenancyConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF152_MachineLockedConfigurationKeysAreRejectedAndAudited()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.UpdateConfigurationAsync(new Dictionary<string, object?>
        {
            ["key"] = "masterTenantId",
            ["value"] = "attacker-master",
            ["operatorId"] = "ops-1"
        }, null);

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("MACHINE_LOCKED_KEY", result.Data?.Body["code"]);
        Assert.Single((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.ConfigAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SaasMultiTenancyConstants.ConfigKeyImmutableEvent);
    }

    [Fact]
    public async Task MutableConfigurationUsesOptimisticConcurrencyAndRejectsStaleWrites()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.UpdateConfigurationAsync(new Dictionary<string, object?>
        {
            ["key"] = "branding_primary_color",
            ["value"] = "#2457a6"
        }, null);
        var stale = await fixture.Service.UpdateConfigurationAsync(new Dictionary<string, object?>
        {
            ["key"] = "branding_primary_color",
            ["value"] = "#101010"
        }, "0");

        Assert.Equal(200, first.Data?.HttpStatus);
        Assert.True(stale.IsFailure);
        Assert.Equal("CONCURRENT_UPDATE", stale.Error?.Code);
        Assert.Equal(first.Data?.Body["version"], stale.Error?.Details?["currentVersion"]);
    }

    [Fact]
    public async Task CF153_QuotaMaterializationUsesOneMultiExecBatchFromTierDefinitions()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.MaterializeQuotasAsync(new Dictionary<string, object?> { ["subscriptionTier"] = "growth" });
        var quotas = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.QuotaCounters, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("MULTI_EXEC", result.Data?.Body["batchMode"]);
        Assert.Equal(3, result.Data?.Body["commandCount"]);
        Assert.Single(quotas);
        Assert.Equal("MULTI_EXEC", quotas[0].Body["batch_mode"]);
    }

    [Fact]
    public async Task CF153_SuspensionUpdatesStatusAndCascadesWithoutDeletingTenant()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ChangeLifecycleAsync(new Dictionary<string, object?>
        {
            ["action"] = "SUSPEND",
            ["currentStatus"] = "ACTIVE",
            ["reason"] = "payment-risk"
        });
        var tenants = (await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("SUSPENDED", result.Data?.Body["status"]);
        Assert.Equal(true, result.Data?.Body["cascadeToSubscriptions"]);
        Assert.Single(tenants);
        Assert.Contains(outbox, record => record.Event.Type == SaasMultiTenancyConstants.TenantSuspendedEvent);
        Assert.DoesNotContain(outbox, record => record.Event.Type == SaasMultiTenancyConstants.DataPurgeRequestedEvent);
    }

    [Fact]
    public async Task TerminationEmitsTenantTerminatedAndDataPurgeRequested()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ChangeLifecycleAsync(new Dictionary<string, object?>
        {
            ["action"] = "TERMINATE",
            ["currentStatus"] = "ACTIVE"
        });
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("CANCELLED", result.Data?.Body["status"]);
        Assert.Contains(outbox, record => record.Event.Type == SaasMultiTenancyConstants.TenantTerminatedEvent);
        Assert.Contains(outbox, record => record.Event.Type == SaasMultiTenancyConstants.DataPurgeRequestedEvent);
    }

    [Fact]
    public async Task PausedSubscriptionStateIsRejectedAsTenantLifecycleState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ChangeLifecycleAsync(new Dictionary<string, object?>
        {
            ["action"] = "SUSPEND",
            ["currentStatus"] = "PAUSED"
        });

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("INVALID_TRANSITION", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SaasMultiTenancyConstants.TenantLifecycleRejectedEvent);
    }

    [Fact]
    public async Task CF154_RequestBodyTenantScopeAndMasterTenantOverrideAreRejected()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var tenantRequest = Provision("bad-tenant");
        tenantRequest["tenantId"] = "northwind";
        var masterRequest = Provision("bad-master");
        masterRequest["masterTenantId"] = "attacker-master";

        var tenantResult = await fixture.Service.ProvisionTenantAsync(tenantRequest, null, "bad-tenant");
        var masterResult = await fixture.Service.ProvisionTenantAsync(masterRequest, null, "bad-master");

        Assert.Equal(400, tenantResult.Data?.HttpStatus);
        Assert.Equal(400, masterResult.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_FORBIDDEN", tenantResult.Data?.Body["code"]);
        Assert.Equal("TENANT_SCOPE_FORBIDDEN", masterResult.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(SaasMultiTenancyConstants.Tenants, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task DashboardDoesNotLeakAcrossTenants()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.ProvisionTenantAsync(Provision("tenant-a"), null, "tenant-a");
        }

        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();

            Assert.Empty(dashboard.Data?.Tenants ?? []);
            Assert.Empty(dashboard.Data?.FreedomConfig ?? []);
            Assert.Equal(0, Convert.ToInt32(dashboard.Data?.Kpis["activeTenants"], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static Dictionary<string, object?> Provision(string slug, bool simulateSlowWrite = false) =>
        new()
        {
            ["tenantSlug"] = slug,
            ["workspaceName"] = $"{slug} Workspace",
            ["subscriptionTier"] = "growth",
            ["billingContact"] = "ops@example.test",
            ["operatorId"] = "operator-1",
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> Tenant(string tenant) => new() { ["tenant_id"] = tenant };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            SaasMultiTenancyService service,
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
        public SaasMultiTenancyService Service { get; }
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
            var service = new SaasMultiTenancyService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<SaasMultiTenancyService>.Instance);

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
