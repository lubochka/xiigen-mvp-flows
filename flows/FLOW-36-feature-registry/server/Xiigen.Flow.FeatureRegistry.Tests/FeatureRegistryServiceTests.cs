using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FeatureRegistry.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.FeatureRegistry.Tests;

public sealed class FeatureRegistryServiceTests
{
    [Fact]
    public void P11_AllFlow36BfaRulesAreRegistered()
    {
        var ids = FeatureRegistryBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(FeatureRegistryConstants.RequiredBfaRules));
        Assert.Equal(18, FeatureRegistryBfaRuleRegistry.Rules.Count);
        Assert.Contains(FeatureRegistryBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-808");
        Assert.Contains(FeatureRegistryBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1049");
        Assert.All(FeatureRegistryBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(FeatureRegistryConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T567ThroughT573HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(7, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteFeatureRegistryEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new FeatureRegistryRuleSnapshot("CF-1049", "", "F1510", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(18, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RegisterFeatureRejectsBoundaryValuesFreeTenantPayloadsAndTenantOverrides()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroSignal = FeatureRequest();
        zeroSignal["signalScore"] = 0;
        var negativeCost = FeatureRequest();
        negativeCost["costEstimate"] = -1;
        var scoped = FeatureRequest();
        scoped["featureTenantId"] = "globex";
        var overrideAttempt = FeatureRequest();
        overrideAttempt["tenantPortingCandidateOverride"] = false;

        var validation = await fixture.Service.RegisterFeatureAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var signalRejected = await fixture.Service.RegisterFeatureAsync(zeroSignal, "zero-signal", null);
        var costRejected = await fixture.Service.RegisterFeatureAsync(negativeCost, "negative-cost", null);
        var scopedRejected = await fixture.Service.RegisterFeatureAsync(scoped, "scoped", null);
        var overrideRejected = await fixture.Service.RegisterFeatureAsync(overrideAttempt, "override", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, signalRejected.Data?.HttpStatus);
        Assert.Equal(400, costRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
        Assert.Equal("MACHINE_CLASSIFICATION_LOCKED", overrideRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task RegisterFeatureIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-1", null);
        var replay = await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-1", null);
        var features = (await fixture.Documents.SearchAsync(FeatureRegistryConstants.FeatureCollection, Scope("acme-corp"))).Data ?? [];
        var signals = (await fixture.Documents.SearchAsync(FeatureRegistryConstants.SignalCollection, Scope("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(FeatureRegistryConstants.AuditCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(features);
        Assert.Single(signals);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "feature-registry.porting-decision-ready");
    }

    [Fact]
    public async Task RegisterFeatureOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-1", null);
        var timeout = FeatureRequest();
        timeout["ftId"] = "FT-568";
        timeout["simulateTimeout"] = true;

        var staleResult = await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-2", "stale-version");
        var timeoutResult = await fixture.Service.RegisterFeatureAsync(timeout, "timeout-1", null);

        Assert.Equal(409, staleResult.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", staleResult.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task PortingCandidateFalseProhibitsAdapterAndSimulationWork()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var prohibited = FeatureRequest();
        prohibited["portingCandidate"] = false;

        var result = await fixture.Service.RegisterFeatureAsync(prohibited, "prohibited-1", null);
        var adapters = (await fixture.Documents.SearchAsync(FeatureRegistryConstants.AdapterCollection, Scope("acme-corp"))).Data ?? [];
        var simulations = (await fixture.Documents.SearchAsync(FeatureRegistryConstants.SimulationCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PROHIBITED", result.Data?.Body["decision"]);
        Assert.Empty(adapters);
        Assert.Empty(simulations);
    }

    [Fact]
    public async Task DashboardSummarizesFeatureRegistryLifecycleAndIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var result = await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-1", null);
            Assert.Equal("APPROVE", result.Data?.Body["decision"]);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Features ?? []);
        Assert.Empty(dashboard.Data?.Adapters ?? []);
    }

    [Fact]
    public async Task PlatformSupportAndTenantAdminCanReadDashboardWithoutMutationAuthority()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RegisterFeatureAsync(FeatureRequest(), "feature-1", null);
        }

        using var support = fixture.PushContext("acme-corp", "platform_support");
        var supportDashboard = await fixture.Service.GetDashboardAsync();
        support.Dispose();
        using var tenantAdmin = fixture.PushContext("acme-corp", "tenant_admin");
        var tenantDashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(supportDashboard.Data?.Features ?? []);
        Assert.Single(tenantDashboard.Data?.Features ?? []);
    }

    [Fact]
    public void InvalidTaskCoverageReportsMissingFeatureRegistryTasks()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps().Take(6).ToArray() });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_FAILED", result.Data?.Body["code"]);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-36-feature-registry", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<FeatureRegistryService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<FeatureRegistryOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> FeatureRequest() => new()
    {
        ["ftId"] = "FT-567",
        ["capabilityName"] = "Feature extractor portability",
        ["sourcePlatform"] = "nestjs-react",
        ["targetPlatform"] = "dotnet-angular",
        ["classificationSource"] = "machine-registry",
        ["portingCandidate"] = true,
        ["signalScore"] = 91,
        ["costEstimate"] = 42
    };

    private static FeatureRegistryRuleSnapshot[] ValidRules() =>
        FeatureRegistryConstants.RequiredBfaRules.Select(rule => new FeatureRegistryRuleSnapshot(rule, "BUILD_FAILURE", "F1510", "T567", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        FeatureRegistryConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Feature registry step {task}",
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
        FeatureRegistryService Service,
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
            var service = new FeatureRegistryService(documents, idempotency, outbox, tenant, correlation, NullLogger<FeatureRegistryService>.Instance);
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
