using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.RagQualityFeedback.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.RagQualityFeedback.Tests;

public sealed class RagQualityFeedbackServiceTests
{
    [Fact]
    public void P11_AllFlow38BfaRulesAreRegistered()
    {
        var ids = RagQualityFeedbackBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(RagQualityFeedbackConstants.RequiredBfaRules));
        Assert.Equal(4, RagQualityFeedbackBfaRuleRegistry.Rules.Count);
        Assert.Contains(RagQualityFeedbackBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1054" && rule.Type == "OPTIMISTIC_CONCURRENCY");
        Assert.Contains(RagQualityFeedbackBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1057" && rule.Type == "TIMEOUT_OUTBOX_RETENTION");
        Assert.All(RagQualityFeedbackBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(RagQualityFeedbackConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_RagQualityFeedbackTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(3, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteRagQualityFeedbackEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new RagQualityFeedbackRuleSnapshot("CF-1057", "", "F1511", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RecordFeedbackRejectsBoundaryValuesFreeTenantPayloadsAndMissingIdempotency()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroScore = FeedbackRequest();
        zeroScore["af9Score"] = 0;
        var invalidQuality = FeedbackRequest();
        invalidQuality["previousQualityScore"] = 1.2m;
        var invalidOutcome = FeedbackRequest();
        invalidOutcome["outcome"] = "UNKNOWN";
        var scoped = FeedbackRequest();
        scoped["feedbackTenantId"] = "globex";

        var emptyRejected = await fixture.Service.RecordFeedbackAsync(new Dictionary<string, object?>(), "cycle-001:pattern-001", null);
        var scoreRejected = await fixture.Service.RecordFeedbackAsync(zeroScore, "cycle-001:pattern-001", null);
        var qualityRejected = await fixture.Service.RecordFeedbackAsync(invalidQuality, "cycle-001:pattern-001", null);
        var outcomeRejected = await fixture.Service.RecordFeedbackAsync(invalidOutcome, "cycle-001:pattern-001", null);
        var scopedRejected = await fixture.Service.RecordFeedbackAsync(scoped, "cycle-001:pattern-001", null);
        var idempotencyRejected = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), null, null);
        var wrongKeyRejected = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "wrong-key", null);

        Assert.Equal("VALIDATION_FAILED", emptyRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", scoreRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", qualityRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", outcomeRejected.Data?.Body["code"]);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", idempotencyRejected.Data?.Body["code"]);
        Assert.Equal("IDEMPOTENCY_KEY_INVALID", wrongKeyRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task RecordFeedbackIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", null);
        var replay = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", null);
        var updates = (await fixture.Documents.SearchAsync(RagQualityFeedbackConstants.QualityUpdateCollection, Scope("acme-corp"))).Data ?? [];
        var outcomes = (await fixture.Documents.SearchAsync(RagQualityFeedbackConstants.CycleOutcomeCollection, Scope("acme-corp"))).Data ?? [];
        var rules = (await fixture.Documents.SearchAsync(RagQualityFeedbackConstants.DistilledRulesCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(updates);
        Assert.Single(outcomes);
        Assert.Single(rules);
        Assert.Contains(outbox, record => record.Event.Type == "rag-quality-feedback.quality-updated");
        Assert.Equal(0.65m, first.Data?.Body["newQualityScore"]);
    }

    [Fact]
    public async Task RecordFeedbackOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", null);
        var timeout = FeedbackRequest("cycle-timeout", "pattern-timeout");
        timeout["simulateTimeout"] = true;

        var staleResult = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", "stale-version");
        var timeoutResult = await fixture.Service.RecordFeedbackAsync(timeout, "cycle-timeout:pattern-timeout", null);

        Assert.Equal(409, staleResult.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", staleResult.Data?.Body["code"]);
        Assert.Equal("CF-1054", staleResult.Data?.Body["ruleId"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Theory]
    [InlineData("SUCCESS_WITHIN_BUDGET", "0.95", "1.00", 1)]
    [InlineData("WASTED_CYCLE", "0.02", "0.00", 0)]
    [InlineData("ESCALATION_REQUIRED", "0.40", "0.40", 0)]
    public async Task QualityScoreDeltaIsMachineFixedClampedAndExtractionIsSuccessOnly(string outcome, string previous, string expected, int extractedRules)
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = FeedbackRequest($"cycle-{outcome}", $"pattern-{outcome}");
        request["outcome"] = outcome;
        request["previousQualityScore"] = decimal.Parse(previous, CultureInfo.InvariantCulture);

        var result = await fixture.Service.RecordFeedbackAsync(request, $"cycle-{outcome}:pattern-{outcome}", null);

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), result.Data?.Body["newQualityScore"]);
        Assert.Equal(extractedRules, Convert.ToInt32(result.Data?.Body["extractedRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task NoDpoTripleProducesNoOutcomeNoUpdate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var request = FeedbackRequest();
        request["hasDpoTriple"] = false;

        var result = await fixture.Service.RecordFeedbackAsync(request, "cycle-001:pattern-001", null);
        var updates = (await fixture.Documents.SearchAsync(RagQualityFeedbackConstants.QualityUpdateCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("NO_OUTCOME_NO_UPDATE", result.Data?.Body["code"]);
        Assert.Empty(updates);
    }

    [Fact]
    public async Task DashboardSummarizesRagQualityFeedbackLifecycleAndIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var result = await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", null);
            Assert.Equal("SUCCESS_WITHIN_BUDGET", result.Data?.Body["outcome"]);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.CycleOutcomes ?? []);
        Assert.Empty(dashboard.Data?.QualityUpdates ?? []);
        Assert.Empty(dashboard.Data?.BatchRecords ?? []);
        Assert.Empty(dashboard.Data?.DistilledRules ?? []);
    }

    [Fact]
    public async Task PlatformSupportTenantAdminAndPlatformAdminCanReadDashboard()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RecordFeedbackAsync(FeedbackRequest(), "cycle-001:pattern-001", null);
        }

        using var support = fixture.PushContext("acme-corp", "platform_support");
        var supportDashboard = await fixture.Service.GetDashboardAsync();
        support.Dispose();
        using var tenantAdmin = fixture.PushContext("acme-corp", "tenant_admin");
        var tenantDashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(supportDashboard.Data?.QualityUpdates ?? []);
        Assert.Single(tenantDashboard.Data?.QualityUpdates ?? []);
    }

    [Fact]
    public void InvalidTaskCoverageReportsMissingRagQualityFeedbackTasks()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps().Take(2).ToArray() });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_FAILED", result.Data?.Body["code"]);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-38-rag-quality-feedback", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<RagQualityFeedbackService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<RagQualityFeedbackOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> FeedbackRequest(string cycleId = "cycle-001", string patternId = "pattern-001") => new()
    {
        ["cycleId"] = cycleId,
        ["patternId"] = patternId,
        ["retrievalId"] = $"retrieval-{cycleId}",
        ["outcome"] = "SUCCESS_WITHIN_BUDGET",
        ["previousQualityScore"] = 0.60m,
        ["af9Score"] = 0.91m,
        ["hasDpoTriple"] = true,
        ["distilledRuleCandidate"] = "Prefer high-confidence accepted retrieval patterns before adding fresh prompt context.",
        ["distilledRuleConfidence"] = 0.91m
    };

    private static RagQualityFeedbackRuleSnapshot[] ValidRules() =>
        RagQualityFeedbackConstants.RequiredBfaRules.Select(rule => new RagQualityFeedbackRuleSnapshot(rule, "BUILD_FAILURE", "F1511", "RAG-QUALITY-FEEDBACK-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        RagQualityFeedbackConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"RAG quality feedback step {task}",
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
        RagQualityFeedbackService Service,
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
            var service = new RagQualityFeedbackService(documents, idempotency, outbox, tenant, correlation, NullLogger<RagQualityFeedbackService>.Instance);
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
