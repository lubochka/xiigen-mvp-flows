using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MarketplacePluginAdapter.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.MarketplacePluginAdapter.Tests;

public sealed class MarketplacePluginAdapterServiceTests
{
    [Fact]
    public void P11_AllFlow33BfaRulesAreRegistered()
    {
        var ids = MarketplacePluginAdapterBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(MarketplacePluginAdapterConstants.RequiredBfaRules));
        Assert.Equal(4, MarketplacePluginAdapterBfaRuleRegistry.Rules.Count);
        Assert.Contains(MarketplacePluginAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1027");
        Assert.Contains(MarketplacePluginAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1030");
        Assert.All(MarketplacePluginAdapterBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(MarketplacePluginAdapterConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T543ThroughT546HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteBootstrapEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new SystemBootstrapRuleSnapshot("CF-1030", "", "F194", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task StartBootstrapRejectsBoundaryValuesAndFreeTenantPayloads()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroFactories = BootstrapRequest();
        zeroFactories["targetPlatformCount"] = 0;
        var zeroFlows = BootstrapRequest();
        zeroFlows["adapterSurfaceCount"] = 0;
        var scoped = BootstrapRequest();
        scoped["adapterTenantId"] = "globex";

        var validation = await fixture.Service.StartAdapterAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var factoriesRejected = await fixture.Service.StartAdapterAsync(zeroFactories, "zero-factories", null);
        var flowsRejected = await fixture.Service.StartAdapterAsync(zeroFlows, "zero-flows", null);
        var scopedRejected = await fixture.Service.StartAdapterAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, factoriesRejected.Data?.HttpStatus);
        Assert.Equal(400, flowsRejected.Data?.HttpStatus);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task StartBootstrapIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.StartAdapterAsync(BootstrapRequest(), "bootstrap-1", null);
        var replay = await fixture.Service.StartAdapterAsync(BootstrapRequest(), "bootstrap-1", null);
        var runs = (await fixture.Documents.SearchAsync(MarketplacePluginAdapterConstants.BootstrapCollection, Scope("acme-corp"))).Data ?? [];
        var registries = (await fixture.Documents.SearchAsync(MarketplacePluginAdapterConstants.RegistryCollection, Scope("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(MarketplacePluginAdapterConstants.AuditCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(runs);
        Assert.Single(registries);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "marketplace-plugin-adapter.packaged");
    }

    [Fact]
    public async Task StartBootstrapOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.StartAdapterAsync(BootstrapRequest(), "bootstrap-1", null);
        var timeout = BootstrapRequest();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.StartAdapterAsync(BootstrapRequest(), "bootstrap-2", "stale-version");
        var timeoutResult = await fixture.Service.StartAdapterAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task CapabilityGapsEmitSelfExtensionEventAndDashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var gapRequest = BootstrapRequest();
            gapRequest["validationFindings"] = new[] { "F190:IFlowDefinitionService", "F191:IFlowValidationService" };
            var result = await fixture.Service.StartAdapterAsync(gapRequest, "gap-1", null);
            Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("validation_gaps_detected", result.Data?.Body["adapterStatus"]);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.BootstrapRuns ?? []);
        Assert.Empty(dashboard.Data?.CapabilityGaps ?? []);
    }

    [Fact]
    public async Task DashboardSummarizesBootstrapLifecycle()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.StartAdapterAsync(BootstrapRequest(), "bootstrap-1", null);
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(dashboard.Data?.BootstrapRuns ?? []);
        Assert.Single(dashboard.Data?.Registries ?? []);
        Assert.Empty(dashboard.Data?.CapabilityGaps ?? []);
        Assert.Single(dashboard.Data?.SmokeRuns ?? []);
        Assert.Equal(4, Convert.ToInt32(dashboard.Data?.Kpis["requiredRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InvalidTaskCoverageReportsMissingBootstrapTasks()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps().Take(3).ToArray() });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_FAILED", result.Data?.Body["code"]);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-34-marketplace-plugin-adapter", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<MarketplacePluginAdapterService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<MarketplacePluginAdapterOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> BootstrapRequest() => new()
    {
        ["platformId"] = "canva-app",
        ["adapterSlug"] = "canva-text-elements-adapter",
        ["adapterMode"] = "MODE_B_THIN",
        ["pluginFamily"] = "UTILITY",
        ["apiMapRef"] = "contracts/api-maps/FT-C5.md",
        ["targetPlatformCount"] = 14,
        ["adapterSurfaceCount"] = 65,
        ["validationFindings"] = Array.Empty<string>()
    };

    private static SystemBootstrapRuleSnapshot[] ValidRules() =>
        MarketplacePluginAdapterConstants.RequiredBfaRules.Select(rule => new SystemBootstrapRuleSnapshot(rule, "BUILD_FAILURE", "F191", "T543", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        MarketplacePluginAdapterConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Marketplace plugin adapter step {task}",
            ["responsibility"] = $"Single responsibility for {task}",
            ["config"] = new Dictionary<string, object?>()
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Scope(string value) => new Dictionary<string, object?> { ["tenantScope"] = value };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        MarketplacePluginAdapterService Service,
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
            var service = new MarketplacePluginAdapterService(documents, idempotency, outbox, tenant, correlation, NullLogger<MarketplacePluginAdapterService>.Instance);
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
