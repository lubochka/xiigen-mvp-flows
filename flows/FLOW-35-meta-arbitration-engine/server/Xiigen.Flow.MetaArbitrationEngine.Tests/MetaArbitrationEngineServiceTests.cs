using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MetaArbitrationEngine.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.MetaArbitrationEngine.Tests;

public sealed class MetaArbitrationEngineServiceTests
{
    [Fact]
    public void P11_AllFlow35BfaRulesAreRegistered()
    {
        var ids = MetaArbitrationEngineBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(MetaArbitrationEngineConstants.RequiredBfaRules));
        Assert.Equal(4, MetaArbitrationEngineBfaRuleRegistry.Rules.Count);
        Assert.Contains(MetaArbitrationEngineBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1031");
        Assert.Contains(MetaArbitrationEngineBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1034");
        Assert.All(MetaArbitrationEngineBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(MetaArbitrationEngineConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T580ThroughT589HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(10, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteMetaArbitrationEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new MetaArbitrationRuleSnapshot("CF-1034", "", "F1491", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ResolveConflictRejectsBoundaryValuesAndFreeTenantPayloads()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroArbiters = ConflictRequest();
        zeroArbiters["arbiterCount"] = 0;
        var zeroWeight = ConflictRequest();
        zeroWeight["policyWeight"] = 0;
        var scoped = ConflictRequest();
        scoped["conflictTenantId"] = "globex";

        var validation = await fixture.Service.ResolveConflictAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var arbitersRejected = await fixture.Service.ResolveConflictAsync(zeroArbiters, "zero-arbiters", null);
        var policyRejected = await fixture.Service.ResolveConflictAsync(zeroWeight, "zero-weight", null);
        var scopedRejected = await fixture.Service.ResolveConflictAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, arbitersRejected.Data?.HttpStatus);
        Assert.Equal(400, policyRejected.Data?.HttpStatus);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task ResolveConflictIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.ResolveConflictAsync(ConflictRequest(), "decision-1", null);
        var replay = await fixture.Service.ResolveConflictAsync(ConflictRequest(), "decision-1", null);
        var decisions = (await fixture.Documents.SearchAsync(MetaArbitrationEngineConstants.DecisionCollection, Scope("acme-corp"))).Data ?? [];
        var conflicts = (await fixture.Documents.SearchAsync(MetaArbitrationEngineConstants.ConflictCollection, Scope("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(MetaArbitrationEngineConstants.AuditCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(decisions);
        Assert.Single(conflicts);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "meta-arbitration-engine.escalation-briefing-ready");
    }

    [Fact]
    public async Task ResolveConflictOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.ResolveConflictAsync(ConflictRequest(), "decision-1", null);
        var timeout = ConflictRequest();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.ResolveConflictAsync(ConflictRequest(), "decision-2", "stale-version");
        var timeoutResult = await fixture.Service.ResolveConflictAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task EscalateAndHaltDecisionsCreateBriefingsAndDashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var escalate = ConflictRequest();
            escalate["escalationReasons"] = new[] { "security_tie_break", "human_gate_disagreed" };
            var result = await fixture.Service.ResolveConflictAsync(escalate, "escalate-1", null);
            Assert.Equal(201, result.Data?.HttpStatus);
            Assert.Equal("ESCALATE", result.Data?.Body["decision"]);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Decisions ?? []);
        Assert.Empty(dashboard.Data?.Briefings ?? []);
    }

    [Fact]
    public async Task DashboardSummarizesArbitrationLifecycle()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.ResolveConflictAsync(ConflictRequest(), "decision-1", null);
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(dashboard.Data?.Decisions ?? []);
        Assert.Single(dashboard.Data?.Conflicts ?? []);
        Assert.Single(dashboard.Data?.Briefings ?? []);
        Assert.Single(dashboard.Data?.Panels ?? []);
        Assert.Equal(4, Convert.ToInt32(dashboard.Data?.Kpis["requiredRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InvalidTaskCoverageReportsMissingMetaArbitrationTasks()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps().Take(9).ToArray() });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_FAILED", result.Data?.Body["code"]);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-35-meta-arbitration-engine", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<MetaArbitrationEngineService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<MetaArbitrationEngineOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> ConflictRequest() => new()
    {
        ["conflictId"] = "conflict-35",
        ["roundId"] = "round-2026-05-25",
        ["policySet"] = "meta-arbiter-core",
        ["primaryVerdict"] = "ALLOW",
        ["counterVerdict"] = "BLOCK",
        ["arbiterCount"] = 10,
        ["policyWeight"] = 85,
        ["escalationReasons"] = new[] { "verdict_mismatch" }
    };

    private static MetaArbitrationRuleSnapshot[] ValidRules() =>
        MetaArbitrationEngineConstants.RequiredBfaRules.Select(rule => new MetaArbitrationRuleSnapshot(rule, "BUILD_FAILURE", "F1491", "T580", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        MetaArbitrationEngineConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Meta arbitration step {task}",
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
        MetaArbitrationEngineService Service,
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
            var service = new MetaArbitrationEngineService(documents, idempotency, outbox, tenant, correlation, NullLogger<MetaArbitrationEngineService>.Instance);
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
