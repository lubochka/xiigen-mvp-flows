using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.TenantLifecycleManager.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.TenantLifecycleManager.Tests;

public sealed class TenantLifecycleManagerServiceTests
{
    [Fact]
    public void P11_AllFLOW30BfaRulesAreRegistered()
    {
        var ids = TenantLifecycleManagerBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(TenantLifecycleManagerConstants.RequiredBfaRules));
        Assert.Equal(4, TenantLifecycleManagerBfaRuleRegistry.Rules.Count);
        Assert.Contains(TenantLifecycleManagerBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1011");
        Assert.Contains(TenantLifecycleManagerBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1014");
        Assert.All(TenantLifecycleManagerBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(TenantLifecycleManagerConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T470ThroughT479HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(10, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteTenantLifecycleEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new TenantLifecycleRuleSnapshot("CF-1014", "", "F30", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ProvisionTenantRejectsBoundaryValuesAndFreeTenantPayloads()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroQuota = Provision();
        zeroQuota["quotaUnits"] = 0;
        var scoped = Provision();
        scoped["tenantId"] = "globex";

        var validation = await fixture.Service.ProvisionTenantAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var quotaRejected = await fixture.Service.ProvisionTenantAsync(zeroQuota, "quota-zero", null);
        var scopedRejected = await fixture.Service.ProvisionTenantAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, quotaRejected.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", quotaRejected.Data?.Body["code"]);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task ProvisionTenantIsIdempotentAndWritesAuditBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.ProvisionTenantAsync(Provision(), "provision-1", null);
        var replay = await fixture.Service.ProvisionTenantAsync(Provision(), "provision-1", null);
        var lifecycles = (await fixture.Documents.SearchAsync(TenantLifecycleManagerConstants.LifecycleCollection, Tenant("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(TenantLifecycleManagerConstants.AuditCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(lifecycles);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "tenant-lifecycle.provision.requested");
    }

    [Fact]
    public async Task ProvisionTenantOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.ProvisionTenantAsync(Provision(), "provision-1", null);
        var timeout = Provision();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.ProvisionTenantAsync(Provision(), "provision-2", "stale-version");
        var timeoutResult = await fixture.Service.ProvisionTenantAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task HealthAndOffboardingAreTenantScopedAndIdempotent()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var health = await fixture.Service.RecordHealthAsync(Health(), "health-1");
            var healthReplay = await fixture.Service.RecordHealthAsync(Health(), "health-1");
            var offboarding = await fixture.Service.ScheduleOffboardingAsync(Offboarding(), "offboarding-1");

            Assert.Equal(201, health.Data?.HttpStatus);
            Assert.Equal(200, healthReplay.Data?.HttpStatus);
            Assert.Equal(201, offboarding.Data?.HttpStatus);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Lifecycles ?? []);
        Assert.Empty(dashboard.Data?.HealthChecks ?? []);
        Assert.Empty(dashboard.Data?.OffboardingRuns ?? []);
    }

    [Fact]
    public async Task DashboardSummarizesLifecycleHealthAndOffboarding()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.ProvisionTenantAsync(Provision(), "provision-1", null);
        await fixture.Service.RecordHealthAsync(Health(), "health-1");
        await fixture.Service.ScheduleOffboardingAsync(Offboarding(), "offboarding-1");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(dashboard.Data?.Lifecycles ?? []);
        Assert.Single(dashboard.Data?.HealthChecks ?? []);
        Assert.Single(dashboard.Data?.OffboardingRuns ?? []);
        Assert.Equal(4, Convert.ToInt32(dashboard.Data?.Kpis["requiredRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-30-tenant-lifecycle-manager", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<TenantLifecycleManagerService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<TenantLifecycleManagerOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Provision() => new()
    {
        ["targetTenantKey"] = "tenant-acme-lifecycle",
        ["planCode"] = "enterprise",
        ["region"] = "us-east",
        ["requestedBy"] = "platform-admin",
        ["quotaUnits"] = 42
    };

    private static Dictionary<string, object?> Health() => new()
    {
        ["targetTenantKey"] = "tenant-acme-lifecycle",
        ["probeId"] = "bootstrap-health",
        ["status"] = "healthy",
        ["healthScore"] = 96
    };

    private static Dictionary<string, object?> Offboarding() => new()
    {
        ["targetTenantKey"] = "tenant-acme-lifecycle",
        ["reason"] = "contract-ended",
        ["effectiveDate"] = "2026-06-30"
    };

    private static TenantLifecycleRuleSnapshot[] ValidRules() =>
        TenantLifecycleManagerConstants.RequiredBfaRules.Select(rule => new TenantLifecycleRuleSnapshot(rule, "BUILD_FAILURE", "F30", "T470", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        TenantLifecycleManagerConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Tenant lifecycle step {task}",
            ["responsibility"] = $"Single responsibility for {task}",
            ["config"] = new Dictionary<string, object?>()
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Tenant(string scope) => new Dictionary<string, object?> { ["tenantId"] = scope };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        TenantLifecycleManagerService Service,
        InMemoryDocumentStore Documents,
        InMemoryIdempotencyStore Idempotency,
        InMemoryOutboxStore Outbox,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new TenantLifecycleManagerService(documents, idempotency, outbox, tenant, correlation, NullLogger<TenantLifecycleManagerService>.Instance);
            return new FlowFixture(service, documents, idempotency, outbox, tenant, correlation);
        }

        public IDisposable PushContext(string scope, string role)
        {
            var correlationId = Guid.NewGuid().ToString("n");
            var correlationScope = CorrelationAccessor.Push(new CorrelationContext(correlationId, "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantAccessor.Push(new TenantContext(scope, "test-user", new HashSet<string>([role], StringComparer.Ordinal), correlationId, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(correlationScope, tenantScope);
        }
    }

    private sealed class CompositeScope : IDisposable
    {
        private readonly IDisposable _first;
        private readonly IDisposable _second;
        private bool _disposed;

        public CompositeScope(IDisposable first, IDisposable second)
        {
            _first = first;
            _second = second;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _second.Dispose();
            _first.Dispose();
            _disposed = true;
        }
    }
}
