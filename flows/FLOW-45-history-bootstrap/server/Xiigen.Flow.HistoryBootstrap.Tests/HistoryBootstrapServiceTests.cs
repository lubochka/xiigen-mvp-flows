using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.HistoryBootstrap.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.HistoryBootstrap.Tests;

public sealed class HistoryBootstrapServiceTests
{
    [Fact]
    public void P11_AllFlow45BfaRulesAreRegistered()
    {
        var ids = HistoryBootstrapBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(HistoryBootstrapConstants.RequiredBfaRules));
        Assert.Equal(7, HistoryBootstrapBfaRuleRegistry.Rules.Count);
        Assert.Contains(HistoryBootstrapBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-803_BOOTSTRAP_BEFORE_FLOW_EXECUTION" && rule.Type == "BOOTSTRAP_PREREQUISITE");
        Assert.Contains(HistoryBootstrapBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-804_ARCH_PATTERN_GLOBAL_SCOPE" && rule.Type == "GLOBAL_SCOPE_ONLY");
        Assert.Contains(HistoryBootstrapBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-HB3-SUMMARY_STORE_BEFORE_QUEUE" && rule.Type == "OUTBOX_ORDER");
        Assert.All(HistoryBootstrapBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(HistoryBootstrapConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T601ThroughT604HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresHistoryBootstrapRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).Select(rule => (IReadOnlyDictionary<string, object?>)rule).ToList();
        invalid.Add(invalid[0]);
        invalid.Add(new Dictionary<string, object?> { ["ruleId"] = "CF-HB5-TENANT_SCOPED_DASHBOARD", ["severity"] = "", ["factory"] = "F1604", ["trigger"] = "", ["scope"] = "" });

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(7, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void SeedSourceValidationAllowsOnlyArchitectureHistoryCorpus()
    {
        var fixture = FlowFixture.Create();

        Assert.True(fixture.Service.ValidateSeedSource("history-seeds/arch-philosophy.json").Data);
        Assert.True(fixture.Service.ValidateSeedSource("docs/xiigenDesign/ENGINE_ARCHITECTURE_MERGED.md").Data);
        Assert.False(fixture.Service.ValidateSeedSource("../tenant-runtime/secrets.json").Data);
    }

    [Fact]
    public void ArchitectureSeedParsingForcesGlobalScopeAndFlowScopedConnection()
    {
        var fixture = FlowFixture.Create();

        var parsed = fixture.Service.ParseArchitectureSeed(SeedRequest()).Data!;

        Assert.Equal(4, parsed.Count);
        Assert.All(parsed, pattern =>
        {
            Assert.Equal("GLOBAL", pattern.KnowledgeScope);
            Assert.Equal("FLOW_SCOPED", pattern.ConnectionType);
            Assert.False(pattern.Metadata.ContainsKey("tenantId"));
        });
    }

    [Fact]
    public async Task SeedDocumentsRoutesDesignReasoningAndArchitecturePatterns()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.SeedDocumentsAsync(SeedRequest());
        var planning = (await fixture.Documents.SearchAsync(HistoryBootstrapConstants.PlanningDecisionsCollection, Scope("acme-corp"))).Data ?? [];
        var rag = (await fixture.Documents.SearchAsync(HistoryBootstrapConstants.RagPatternsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["patternsSeeded"], CultureInfo.InvariantCulture));
        Assert.Single(planning);
        Assert.Equal(3, rag.Count);
        Assert.All(rag, record => Assert.Equal("GLOBAL", record.Body["knowledgeScope"]));
    }

    [Fact]
    public async Task RetrieveArchitecturePatternsReturnsOnlyGlobalScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.SeedDocumentsAsync(SeedRequest());

        var result = await fixture.Service.RetrieveArchitecturePatternsAsync("ARCH_PATTERN");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.TotalReturned >= 2);
        Assert.All(result.Data.Patterns, pattern => Assert.Equal("GLOBAL", pattern["knowledgeScope"]));
    }

    [Fact]
    public async Task SummarizeGroupsByPatternTypeAndStoresBeforeQueue()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var patterns = SeedRequest()["patterns"]!;

        var result = await fixture.Service.SummarizePatternsAsync(new Dictionary<string, object?> { ["summarizationRunId"] = "sum-001", ["patterns"] = patterns });
        var summaries = (await fixture.Documents.SearchAsync(HistoryBootstrapConstants.PhilosophySummariesCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.GroupsCreated);
        Assert.Equal(4, result.Data.PatternsProcessed);
        Assert.Equal(3, summaries.Count);
        Assert.Single(outbox);
        Assert.Equal("xiigen.history-bootstrap.summary-ready", outbox[0].Event.Type);
    }

    [Fact]
    public async Task SummarizeIsIdempotentPerRunId()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var request = new Dictionary<string, object?> { ["summarizationRunId"] = "sum-002", ["patterns"] = SeedRequest()["patterns"]! };

        var first = await fixture.Service.SummarizePatternsAsync(request);
        var second = await fixture.Service.SummarizePatternsAsync(request);
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.GroupsCreated, second.Data!.GroupsCreated);
        Assert.Single(outbox);
    }

    [Fact]
    public async Task RunBootstrapPersistsCompletionBeforeCloudEventAndIsIdempotent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = SeedRequest();
        request["bootstrapRunId"] = "run-001";

        var first = await fixture.Service.RunBootstrapAsync(request);
        var second = await fixture.Service.RunBootstrapAsync(request);
        var completions = (await fixture.Documents.SearchAsync(HistoryBootstrapConstants.BootstrapCompletionsCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal("COMPLETE", first.Data?.Body["completionStatus"]);
        Assert.Single(completions);
        Assert.Equal(2, outbox.Count);
        Assert.Equal("xiigen.history-bootstrap.completed", outbox[^1].Event.Type);
    }

    [Fact]
    public async Task RunBootstrapRejectsMissingRunIdWithoutPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.RunBootstrapAsync(new Dictionary<string, object?>());
        var completions = (await fixture.Documents.SearchAsync(HistoryBootstrapConstants.BootstrapCompletionsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("MISSING_BOOTSTRAP_RUN_ID", result.Data?.Body["code"]);
        Assert.Empty(completions);
    }

    [Fact]
    public async Task DashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var request = SeedRequest();
            request["bootstrapRunId"] = "run-tenant-a";
            await fixture.Service.RunBootstrapAsync(request);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Completions ?? []);
        Assert.Empty(dashboard.Data?.Summaries ?? []);
    }

    [Fact]
    public void PackageMetadataMatchesHistoryBootstrapBoundary()
    {
        Assert.Equal("@xiigen/history-bootstrap", HistoryBootstrapConstants.PackageName);
        Assert.Contains("T604-HistoryBootstrapOrchestrator", HistoryBootstrapConstants.RequiredTaskTypes);
        Assert.Contains("CF-804_ARCH_PATTERN_GLOBAL_SCOPE", HistoryBootstrapConstants.RequiredBfaRules);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-45-history-bootstrap", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<HistoryBootstrapService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<HistoryBootstrapOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> SeedRequest() => new()
    {
        ["source"] = "history-seeds/arch-philosophy.json",
        ["patterns"] = new[]
        {
            new Dictionary<string, object?> { ["patternId"] = "HB-001", ["patternType"] = "ARCH_PATTERN", ["title"] = "Fabric first", ["problem"] = "SDK leakage", ["solution"] = "Fabric interface", ["taskTypeId"] = "T601-BootstrapFromDocuments" },
            new Dictionary<string, object?> { ["patternId"] = "HB-002", ["patternType"] = "DESIGN_REASONING", ["title"] = "Plan gates", ["problem"] = "missing verification", ["solution"] = "phase gates", ["taskTypeId"] = "T602-ArchPhilosophyRetriever" },
            new Dictionary<string, object?> { ["patternId"] = "HB-003", ["patternType"] = "ARCH_PATTERN", ["title"] = "Outbox", ["problem"] = "queue before state", ["solution"] = "state before outbox", ["taskTypeId"] = "T603-PhilosophyPatternSummarizer" },
            new Dictionary<string, object?> { ["patternId"] = "HB-004", ["patternType"] = "RAG_PATTERN", ["title"] = "History RAG", ["problem"] = "empty memory", ["solution"] = "bootstrap patterns", ["taskTypeId"] = "T604-HistoryBootstrapOrchestrator" }
        }
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ValidRules() =>
        HistoryBootstrapConstants.RequiredBfaRules.Select(rule => new Dictionary<string, object?>
        {
            ["ruleId"] = rule,
            ["severity"] = "BUILD_FAILURE",
            ["factory"] = "F1604",
            ["trigger"] = "history-bootstrap",
            ["scope"] = "GLOBAL",
            ["description"] = $"Rule {rule}"
        }).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        HistoryBootstrapConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"History Bootstrap step {task}",
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
        HistoryBootstrapService Service,
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
            var service = new HistoryBootstrapService(documents, outbox, tenant, correlation, NullLogger<HistoryBootstrapService>.Instance);
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
