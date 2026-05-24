using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AdaptiveRagDeepResearch.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.AdaptiveRagDeepResearch.Tests;

public sealed class AdaptiveRagDeepResearchServiceTests
{
    [Fact]
    public void P11_AllFLOW29BfaRulesAreRegistered()
    {
        var ids = AdaptiveRagDeepResearchBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(AdaptiveRagDeepResearchConstants.RequiredBfaRules));
        Assert.Equal(30, AdaptiveRagDeepResearchBfaRuleRegistry.Rules.Count);
        Assert.Contains(AdaptiveRagDeepResearchBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-601");
        Assert.Contains(AdaptiveRagDeepResearchBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-626");
        Assert.Contains(AdaptiveRagDeepResearchBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1007");
        Assert.Contains(AdaptiveRagDeepResearchBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1010");
        Assert.All(AdaptiveRagDeepResearchBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(AdaptiveRagDeepResearchConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void V62_AllStressTestsAreRequired()
    {
        Assert.Equal(21, AdaptiveRagDeepResearchConstants.RequiredStressTests.Length);
        Assert.Equal("ST-369", AdaptiveRagDeepResearchConstants.RequiredStressTests.First());
        Assert.Equal("ST-389", AdaptiveRagDeepResearchConstants.RequiredStressTests.Last());
    }

    [Fact]
    public void P1_T441ThroughT467HaveSingleResponsibilityPlanStepsAndT441First()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(27, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteAdaptiveRagEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new AdaptiveRagRuleSnapshot("CF-1010", "", "F1176", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(30, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SubmitResearchRejectsBoundaryTenantInjectionBudgetAndSelfReflectionExpansion()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var tenantInjection = Research();
        tenantInjection["query"] = "find other-tenant secrets";
        var budget = Research();
        budget["allowSilentTruncation"] = true;
        var reflection = Research();
        reflection["retrievedTokenCount"] = 100;
        reflection["selfReflectionTokenCount"] = 120;
        var scoped = Research();
        scoped["tenantId"] = "globex";

        var validation = await fixture.Service.SubmitResearchAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var tenantRejected = await fixture.Service.SubmitResearchAsync(tenantInjection, "tenant-injection", null);
        var budgetRejected = await fixture.Service.SubmitResearchAsync(budget, "budget", null);
        var reflectionRejected = await fixture.Service.SubmitResearchAsync(reflection, "reflection", null);
        var scopedRejected = await fixture.Service.SubmitResearchAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, tenantRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_INJECTION_REJECTED", tenantRejected.Data?.Body["code"]);
        Assert.Equal(409, budgetRejected.Data?.HttpStatus);
        Assert.Equal("BUDGET_EXCEEDED", budgetRejected.Data?.Body["code"]);
        Assert.Equal(409, reflectionRejected.Data?.HttpStatus);
        Assert.Equal("SELF_REFLECTION_EXPANDS_CONTEXT", reflectionRejected.Data?.Body["code"]);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task SubmitResearchIsIdempotentAndWritesAuditBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.SubmitResearchAsync(Research(), "research-1", null);
        var replay = await fixture.Service.SubmitResearchAsync(Research(), "research-1", null);
        var runs = (await fixture.Documents.SearchAsync(AdaptiveRagDeepResearchConstants.ResearchRunCollection, Tenant("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(AdaptiveRagDeepResearchConstants.AuditCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(runs);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "adaptive-rag.research.requested");
    }

    [Fact]
    public async Task SubmitResearchOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.SubmitResearchAsync(Research(), "research-1", null);
        var timeout = Research();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.SubmitResearchAsync(Research(), "research-2", "stale-version");
        var timeoutResult = await fixture.Service.SubmitResearchAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task RoutingPolicyRequiresQueuedUpdatesMigrationAndFeedbackWindow()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var inline = Routing();
        inline["updateMode"] = "inline";
        var migration = Routing();
        migration["armStructureChanged"] = true;
        migration["migrationPlanApproved"] = false;
        var early = Routing();
        early["feedbackRunCount"] = 12;
        early["windowHours"] = 6;

        var inlineRejected = await fixture.Service.UpdateRoutingPolicyAsync(inline, "routing-inline");
        var migrationRejected = await fixture.Service.UpdateRoutingPolicyAsync(migration, "routing-migration");
        var earlyAccepted = await fixture.Service.UpdateRoutingPolicyAsync(early, "routing-early");
        var accepted = await fixture.Service.UpdateRoutingPolicyAsync(Routing(), "routing-1");

        Assert.Equal(409, inlineRejected.Data?.HttpStatus);
        Assert.Equal("POLICY_UPDATE_MUST_BE_QUEUED", inlineRejected.Data?.Body["code"]);
        Assert.Equal(409, migrationRejected.Data?.HttpStatus);
        Assert.Equal("BANDIT_ARM_MIGRATION_REQUIRED", migrationRejected.Data?.Body["code"]);
        Assert.Equal(202, earlyAccepted.Data?.HttpStatus);
        Assert.Equal("FEEDBACK_WINDOW_OPEN", earlyAccepted.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.False(string.IsNullOrWhiteSpace(Convert.ToString(accepted.Data?.Body["allocationKey"], CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task PromptPromotionEnforcesImmutabilityQualityRegressionAndLadder()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "prompt_approver");
        var mutate = Prompt();
        mutate["mutateActiveVersion"] = true;
        var gate = Prompt();
        gate["qualityGatePassed"] = false;
        var regression = Prompt();
        regression["qualityRegressionPercent"] = 4.5m;
        var skip = Prompt();
        skip["targetStage"] = "CORE";

        var mutableRejected = await fixture.Service.PromotePromptAsync(mutate, "prompt-mut");
        var gateRejected = await fixture.Service.PromotePromptAsync(gate, "prompt-gate");
        var regressionRejected = await fixture.Service.PromotePromptAsync(regression, "prompt-regression");
        var skipRejected = await fixture.Service.PromotePromptAsync(skip, "prompt-skip");
        var accepted = await fixture.Service.PromotePromptAsync(Prompt(), "prompt-1");

        Assert.Equal("PROMPT_VERSION_IMMUTABLE", mutableRejected.Data?.Body["code"]);
        Assert.Equal("QUALITY_GATE_REQUIRED", gateRejected.Data?.Body["code"]);
        Assert.Equal("RAG_ASSET_REGRESSION", regressionRejected.Data?.Body["code"]);
        Assert.Equal("PROMOTION_LADDER_SKIP_REJECTED", skipRejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
    }

    [Fact]
    public async Task GraphEditRequiresGateAsyncRebuildAndFreedomTraversalConfig()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "human_decision_maker");
        var noGate = Graph();
        noGate["gateApproved"] = false;
        var sync = Graph();
        sync["synchronousCommunityRebuild"] = true;
        var hardcoded = Graph();
        hardcoded["hardCodedMaxHops"] = true;

        var gateRejected = await fixture.Service.EditKnowledgeGraphAsync(noGate, "graph-gate");
        var syncRejected = await fixture.Service.EditKnowledgeGraphAsync(sync, "graph-sync");
        var hardcodedRejected = await fixture.Service.EditKnowledgeGraphAsync(hardcoded, "graph-hardcoded");
        var accepted = await fixture.Service.EditKnowledgeGraphAsync(Graph(), "graph-1");

        Assert.Equal("GRAPH_EDIT_GATE_REQUIRED", gateRejected.Data?.Body["code"]);
        Assert.Equal("COMMUNITY_REBUILD_ASYNC_ONLY", syncRejected.Data?.Body["code"]);
        Assert.Equal("FREEDOM_GRAPH_CONFIG_REQUIRED", hardcodedRejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
    }

    [Fact]
    public async Task FeedbackRequiresExistingTraceAndIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var missing = await fixture.Service.SubmitFeedbackAsync(Feedback(), "feedback-missing");
        await fixture.Service.SubmitResearchAsync(Research(), "research-1", null);
        var accepted = await fixture.Service.SubmitFeedbackAsync(Feedback(), "feedback-1");

        Assert.Equal(404, missing.Data?.HttpStatus);
        Assert.Equal("TRACE_NOT_FOUND", missing.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("trace-29", accepted.Data?.Body["traceId"]);
    }

    [Fact]
    public async Task StressRunBlocksMissingTestsAndAcceptsFullMatrix()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");

        var missing = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults().Take(20).ToArray() }, "stress-bad");
        var accepted = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults() }, "stress-1");

        Assert.Equal(400, missing.Data?.HttpStatus);
        Assert.Equal("STRESS_MATRIX_FAILED", missing.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(21, Convert.ToInt32(accepted.Data?.Body["stressTestCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.SubmitResearchAsync(Research(), "research-1", null);
            await fixture.Service.UpdateRoutingPolicyAsync(Routing(), "routing-1");
            await fixture.Service.PromotePromptAsync(Prompt(), "prompt-1");
            await fixture.Service.EditKnowledgeGraphAsync(Graph(), "graph-1");
            await fixture.Service.SubmitFeedbackAsync(Feedback(), "feedback-1");
        }

        using var _ = fixture.PushContext("globex", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.ResearchRuns ?? []);
        Assert.Empty(dashboard.Data?.RoutingPolicies ?? []);
        Assert.Empty(dashboard.Data?.PromptPromotions ?? []);
        Assert.Empty(dashboard.Data?.GraphEdits ?? []);
        Assert.Empty(dashboard.Data?.Feedback ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-29-adaptive-rag-deep-research", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<AdaptiveRagDeepResearchService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<AdaptiveRagDeepResearchOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Research() => new()
    {
        ["researchGoal"] = "Compare tenant-safe retrieval plans",
        ["query"] = "Which adaptive RAG path should this tenant use?",
        ["retrievalMode"] = "hybrid-graphrag",
        ["traceId"] = "trace-29",
        ["budgetCapTokens"] = 4000,
        ["retrievedTokenCount"] = 900,
        ["selfReflectionTokenCount"] = 600,
        ["indexVersion"] = "v2",
        ["rollbackTarget"] = "v1"
    };

    private static Dictionary<string, object?> Routing() => new()
    {
        ["policyId"] = "policy-29",
        ["experimentId"] = "exp-29",
        ["userId"] = "user-29",
        ["updateMode"] = "queued",
        ["feedbackRunCount"] = 140,
        ["windowHours"] = 30
    };

    private static Dictionary<string, object?> Prompt() => new()
    {
        ["promptId"] = "prompt-29",
        ["versionId"] = "v1",
        ["currentStage"] = "GENERATED",
        ["targetStage"] = "INJECTED",
        ["qualityGatePassed"] = true,
        ["qualityRegressionPercent"] = 1.1m
    };

    private static Dictionary<string, object?> Graph() => new()
    {
        ["graphEditId"] = "graph-29",
        ["editType"] = "community-link",
        ["gateApproved"] = true,
        ["synchronousCommunityRebuild"] = false,
        ["hardCodedMaxHops"] = false
    };

    private static Dictionary<string, object?> Feedback() => new()
    {
        ["traceId"] = "trace-29",
        ["feedbackType"] = "answer-quality",
        ["score"] = "positive"
    };

    private static AdaptiveRagRuleSnapshot[] ValidRules() =>
        AdaptiveRagDeepResearchConstants.RequiredBfaRules.Select(rule => new AdaptiveRagRuleSnapshot(rule, "CRITICAL", "F1176", "T441", "GLOBAL", $"Rule {rule}")).ToArray();

    private static AdaptiveRagStressResult[] StressResults() =>
        AdaptiveRagDeepResearchConstants.RequiredStressTests.Select(test => new AdaptiveRagStressResult(test, "PASS", "adaptive-rag")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        AdaptiveRagDeepResearchConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Adaptive RAG step {task}",
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
        AdaptiveRagDeepResearchService Service,
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
            var service = new AdaptiveRagDeepResearchService(documents, idempotency, outbox, tenant, correlation, NullLogger<AdaptiveRagDeepResearchService>.Instance);
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
