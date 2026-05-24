using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DesignIntelligenceEngine.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignIntelligenceEngine.Tests;

public sealed class DesignIntelligenceEngineServiceTests
{
    [Fact]
    public void P11_AllFLOW31BfaRulesAreRegistered()
    {
        var ids = DesignIntelligenceEngineBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(DesignIntelligenceEngineConstants.RequiredBfaRules));
        Assert.Equal(4, DesignIntelligenceEngineBfaRuleRegistry.Rules.Count);
        Assert.Contains(DesignIntelligenceEngineBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1015");
        Assert.Contains(DesignIntelligenceEngineBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1018");
        Assert.All(DesignIntelligenceEngineBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(DesignIntelligenceEngineConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T489ThroughT515HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(27, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteDesignIntelligenceEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new DesignIntelligenceRuleSnapshot("CF-1018", "", "F31", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AnalyzeDesignRejectsBoundaryValuesAndFreeTenantPayloads()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroConfidence = Analysis();
        zeroConfidence["confidenceScore"] = 0;
        var scoped = Analysis();
        scoped["tenantId"] = "globex";

        var validation = await fixture.Service.AnalyzeDesignAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var confidenceRejected = await fixture.Service.AnalyzeDesignAsync(zeroConfidence, "confidence-zero", null);
        var scopedRejected = await fixture.Service.AnalyzeDesignAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, confidenceRejected.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", confidenceRejected.Data?.Body["code"]);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task AnalyzeDesignIsIdempotentAndWritesAuditBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.AnalyzeDesignAsync(Analysis(), "analysis-1", null);
        var replay = await fixture.Service.AnalyzeDesignAsync(Analysis(), "analysis-1", null);
        var analyses = (await fixture.Documents.SearchAsync(DesignIntelligenceEngineConstants.AnalysisCollection, Scope("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(DesignIntelligenceEngineConstants.AuditCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(analyses);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "design-intelligence.analysis.completed");
    }

    [Fact]
    public async Task AnalyzeDesignOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.AnalyzeDesignAsync(Analysis(), "analysis-1", null);
        var timeout = Analysis();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.AnalyzeDesignAsync(Analysis(), "analysis-2", "stale-version");
        var timeoutResult = await fixture.Service.AnalyzeDesignAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task TokensComplianceAndGovernanceAreTenantScopedAndIdempotent()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var tokens = await fixture.Service.ExtractTokensAsync(TokenRun(), "tokens-1");
            var tokensReplay = await fixture.Service.ExtractTokensAsync(TokenRun(), "tokens-1");
            var compliance = await fixture.Service.ValidateComplianceAsync(Compliance(), "compliance-1");
            var governance = await fixture.Service.PublishGovernanceAsync(Governance(), "governance-1");

            Assert.Equal(201, tokens.Data?.HttpStatus);
            Assert.Equal(200, tokensReplay.Data?.HttpStatus);
            Assert.Equal(201, compliance.Data?.HttpStatus);
            Assert.Equal(201, governance.Data?.HttpStatus);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Analyses ?? []);
        Assert.Empty(dashboard.Data?.TokenRuns ?? []);
        Assert.Empty(dashboard.Data?.ComplianceRuns ?? []);
        Assert.Empty(dashboard.Data?.GovernanceReports ?? []);
    }

    [Fact]
    public async Task DashboardSummarizesDesignIntelligenceLifecycle()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.AnalyzeDesignAsync(Analysis(), "analysis-1", null);
        await fixture.Service.ExtractTokensAsync(TokenRun(), "tokens-1");
        await fixture.Service.ValidateComplianceAsync(Compliance(), "compliance-1");
        await fixture.Service.PublishGovernanceAsync(Governance(), "governance-1");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(dashboard.Data?.Analyses ?? []);
        Assert.Single(dashboard.Data?.TokenRuns ?? []);
        Assert.Single(dashboard.Data?.ComplianceRuns ?? []);
        Assert.Single(dashboard.Data?.GovernanceReports ?? []);
        Assert.Equal(4, Convert.ToInt32(dashboard.Data?.Kpis["requiredRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-31-design-intelligence-engine", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<DesignIntelligenceEngineService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<DesignIntelligenceEngineOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Analysis() => new()
    {
        ["designFileKey"] = "figma-31-homepage",
        ["screenName"] = "checkout-home",
        ["componentMap"] = new Dictionary<string, object?> { ["PrimaryButton"] = "button.primary", ["SummaryPanel"] = "panel.summary" },
        ["tokenSet"] = new Dictionary<string, object?> { ["color.primary"] = "#3366ff", ["space.grid"] = "8" },
        ["requestedBy"] = "platform-admin",
        ["confidenceScore"] = 91
    };

    private static Dictionary<string, object?> TokenRun() => new()
    {
        ["designFileKey"] = "figma-31-homepage",
        ["extractionProfile"] = "figma31-default",
        ["tokenSet"] = new Dictionary<string, object?> { ["color.primary"] = "#3366ff", ["radius.card"] = "8" }
    };

    private static Dictionary<string, object?> Compliance() => new()
    {
        ["designFileKey"] = "figma-31-homepage",
        ["ruleSet"] = "design-governance-default",
        ["componentMap"] = new Dictionary<string, object?> { ["PrimaryButton"] = "button.primary" }
    };

    private static Dictionary<string, object?> Governance() => new()
    {
        ["designFileKey"] = "figma-31-homepage",
        ["reportName"] = "homepage-governance",
        ["releaseChannel"] = "platform-admin"
    };

    private static DesignIntelligenceRuleSnapshot[] ValidRules() =>
        DesignIntelligenceEngineConstants.RequiredBfaRules.Select(rule => new DesignIntelligenceRuleSnapshot(rule, "BUILD_FAILURE", "F31", "T489", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        DesignIntelligenceEngineConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Design intelligence step {task}",
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
        DesignIntelligenceEngineService Service,
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
            var service = new DesignIntelligenceEngineService(documents, idempotency, outbox, tenant, correlation, NullLogger<DesignIntelligenceEngineService>.Instance);
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
