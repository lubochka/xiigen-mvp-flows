using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.PlatformAgent.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.PlatformAgent.Tests;

public sealed class PlatformAgentServiceTests
{
    [Fact]
    public void P11_AllFlow46BfaRulesAreRegistered()
    {
        var ids = PlatformAgentBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(PlatformAgentConstants.RequiredBfaRules));
        Assert.Equal(17, PlatformAgentBfaRuleRegistry.Rules.Count);
        Assert.Contains(PlatformAgentBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-839" && rule.Type == "TENANT_ISOLATION");
        Assert.Contains(PlatformAgentBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-840" && rule.Type == "COST_GATE");
        Assert.Contains(PlatformAgentBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-841" && rule.Type == "PRIVACY");
        Assert.All(PlatformAgentBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(PlatformAgentConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void T650ThroughT656HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(7, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresAllPlatformAgentRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");
        var invalid = ValidRules().Skip(1).Select(rule => (IReadOnlyDictionary<string, object?>)rule).ToList();
        invalid.Add(invalid[0]);

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(17, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RunAgentRequiresMasterTenantContext()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.RunAgentAsync(new Dictionary<string, object?> { ["sessionId"] = "s-denied", ["userIntent"] = "review platform action" });

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("NOT_ADMIN", result.Data?.Body["code"]);
    }

    [Fact]
    public async Task SuperJudgeZeroEvidenceDefersToAf9WithZeroCost()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");
        var enriched = new Dictionary<string, object?> { ["platformPatternsMatched"] = 0, ["platformPatterns"] = Array.Empty<object>() };

        var result = await fixture.Service.EvaluateSuperJudgeAsync("s-zero", new Dictionary<string, object?> { ["af9Verdict"] = "PASS" }, enriched);

        Assert.True(result.IsSuccess);
        Assert.Equal("DEFER_TO_AF9", result.Data!["verdict"]);
        Assert.Equal(0, Convert.ToInt32(result.Data["cost"], CultureInfo.InvariantCulture));
        Assert.Equal(0, Convert.ToInt32(result.Data["llmCalls"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SuperJudgeOverrideWritesDpoTripleWithConstraint()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");
        var enriched = new Dictionary<string, object?> { ["platformPatternsMatched"] = 1 };

        var result = await fixture.Service.EvaluateSuperJudgeAsync("s-override", new Dictionary<string, object?> { ["superJudgeVerdict"] = "OVERRIDE_BLOCK", ["discriminatingConstraint"] = "global pattern forbids tenant leak" }, enriched);
        var triples = (await fixture.Documents.SearchAsync(PlatformAgentConstants.TrainingDataCollection, new Dictionary<string, object?> { ["tenantScope"] = "master" })).Data ?? [];

        Assert.True(result.IsSuccess);
        Assert.Equal("OVERRIDE_BLOCK", result.Data!["verdict"]);
        Assert.Single(triples);
        Assert.Equal("global pattern forbids tenant leak", triples[0].Body["discriminating_constraint"]);
    }

    [Fact]
    public async Task TenantScopeGatewayExceptionWritesAuditBeforeTargetAction()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.PublishActionAsync("s-action", new Dictionary<string, object?> { ["actionId"] = "a-propose", ["actionType"] = "PROPOSE_EDIT", ["targetTenantId"] = "acme-corp", ["payload"] = new Dictionary<string, object?> { ["draft"] = true } });
        var actions = (await fixture.Documents.SearchAsync(PlatformAgentConstants.AgentActionsCollection, new Dictionary<string, object?> { ["tenantScope"] = "master" })).Data ?? [];
        var drafts = (await fixture.Documents.SearchAsync(PlatformAgentConstants.TenantTopologiesCollection, new Dictionary<string, object?> { ["targetTenantId"] = "acme-corp" })).Data ?? [];

        Assert.True(result.IsSuccess);
        Assert.Contains(actions, record => Equals(record.Body["actionType"], "SCOPE_SWITCH"));
        Assert.Single(drafts);
        Assert.Equal("EMITTED", result.Data!["status"]);
    }

    [Fact]
    public async Task ActionPublishingStoresStateBeforeCloudEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.PublishActionAsync("s-global", new Dictionary<string, object?> { ["actionId"] = "a-global", ["actionType"] = "APPLY_GLOBAL", ["payload"] = new Dictionary<string, object?> { ["template"] = "global" } });
        var templates = (await fixture.Documents.SearchAsync(PlatformAgentConstants.FlowTemplatesCollection, new Dictionary<string, object?> { ["tenantScope"] = "master" })).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.True(result.IsSuccess);
        Assert.Single(templates);
        Assert.Single(outbox);
        Assert.Equal("xiigen.platform-agent.action-proposed", outbox[0].Event.Type);
    }

    [Fact]
    public async Task PatternContributorPathASharesGlobalRecordsWithoutConsent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.ContributePatternAsync("s-contrib", new Dictionary<string, object?> { ["patternId"] = "pat-a", ["path"] = "A", ["solution"] = new Dictionary<string, object?> { ["title"] = "Reusable agent pattern" } });
        var rag = (await fixture.Documents.SearchAsync(PlatformAgentConstants.RagPatternsCollection, new Dictionary<string, object?> { ["knowledgeScope"] = "GLOBAL" })).Data ?? [];

        Assert.True(result.IsSuccess);
        Assert.Equal("RECORDED", result.Data!["status"]);
        Assert.Single(rag);
    }

    [Fact]
    public async Task PatternContributorSanitizerFailureAbortsWithoutGlobalWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.ContributePatternAsync("s-fail", new Dictionary<string, object?> { ["patternId"] = "pat-fail", ["path"] = "B", ["sourceTenantId"] = "acme-corp", ["solution"] = new Dictionary<string, object?> { ["unableToStripField"] = true, ["tenantDomain"] = "acme.example" } });
        var rag = (await fixture.Documents.SearchAsync(PlatformAgentConstants.RagPatternsCollection, new Dictionary<string, object?> { ["patternId"] = "pat-fail" })).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(PlatformAgentConstants.ContributionsCollection, new Dictionary<string, object?> { ["patternId"] = "pat-fail" })).Data ?? [];

        Assert.True(result.IsFailure);
        Assert.Equal("SANITIZATION_FAILED", result.Error?.Code);
        Assert.Empty(rag);
        Assert.Single(audit);
        Assert.Equal("SANITIZATION_FAILED", audit[0].Body["status"]);
    }

    [Fact]
    public async Task RunAgentPersistsSessionBeforeCompletionEventAndIsIdempotent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");
        await fixture.Documents.UpsertAsync(PlatformAgentConstants.RagPatternsCollection, "pat-global", new Dictionary<string, object?> { ["patternId"] = "pat-global", ["knowledgeScope"] = "GLOBAL", ["tenantScope"] = "master", ["tags"] = new[] { "agent" } });
        var request = new Dictionary<string, object?> { ["sessionId"] = "s-run", ["userIntent"] = "agent review", ["superJudgeVerdict"] = "DEFER_TO_AF9" };

        var first = await fixture.Service.RunAgentAsync(request);
        var second = await fixture.Service.RunAgentAsync(request);
        var sessions = (await fixture.Documents.SearchAsync(PlatformAgentConstants.SessionsCollection, new Dictionary<string, object?> { ["sessionId"] = "s-run" })).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(sessions);
        Assert.Single(outbox);
        Assert.Equal("xiigen.platform-agent.session-completed", outbox[0].Event.Type);
    }

    [Fact]
    public async Task DashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("master", "platform_admin"))
        {
            await fixture.Service.RunAgentAsync(new Dictionary<string, object?> { ["sessionId"] = "s-master", ["userIntent"] = "agent review" });
        }

        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Sessions ?? []);
        Assert.Empty(dashboard.Data?.Actions ?? []);
    }

    [Fact]
    public void PackageMetadataMatchesPlatformAgentBoundary()
    {
        Assert.Equal("@xiigen/platform-agent", PlatformAgentConstants.PackageName);
        Assert.Contains("T653-SuperJudgeArbiter", PlatformAgentConstants.RequiredTaskTypes);
        Assert.Contains("CF-839", PlatformAgentConstants.RequiredBfaRules);
        Assert.Contains("OVERRIDE_BLOCK", PlatformAgentConstants.SuperJudgeVerdicts);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndBoundedExceptionDocumented()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-46-platform-agent", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<PlatformAgentService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<PlatformAgentOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
        Assert.Contains("SCOPE_SWITCH", source, StringComparison.Ordinal);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ValidRules() =>
        PlatformAgentConstants.RequiredBfaRules.Select(rule => new Dictionary<string, object?>
        {
            ["ruleId"] = rule,
            ["severity"] = "BUILD_FAILURE",
            ["factory"] = "F1604",
            ["trigger"] = "platform-agent",
            ["scope"] = "GLOBAL",
            ["description"] = $"Rule {rule}"
        }).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        PlatformAgentConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Platform Agent step {task}",
            ["responsibility"] = $"Single responsibility for {task}",
            ["config"] = new Dictionary<string, object?>()
        }).ToArray();

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        PlatformAgentService Service,
        InMemoryDocumentStore Documents,
        InMemoryOutboxStore Outbox,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var outbox = new InMemoryOutboxStore();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new PlatformAgentService(documents, outbox, tenant, correlation, NullLogger<PlatformAgentService>.Instance);
            return new FlowFixture(service, documents, outbox, tenant, correlation);
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