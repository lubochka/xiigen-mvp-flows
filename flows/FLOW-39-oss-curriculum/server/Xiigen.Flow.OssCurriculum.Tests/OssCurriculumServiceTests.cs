using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.OssCurriculum.Application;
using Xiigen.Flow.OssCurriculum.Infrastructure;
using Xiigen.Kernel;

namespace Xiigen.Flow.OssCurriculum.Tests;

public sealed class OssCurriculumServiceTests
{
    [Fact]
    public void P11_AllFlow39BfaRulesAreRegistered()
    {
        var ids = OssCurriculumBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(OssCurriculumConstants.RequiredBfaRules));
        Assert.Equal(6, OssCurriculumBfaRuleRegistry.Rules.Count);
        Assert.Contains(OssCurriculumBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-803" && rule.Type == "VALIDATION_ORDERING");
        Assert.Contains(OssCurriculumBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-804" && rule.Type == "SHADOW_RUN_TRACEABILITY");
        Assert.Contains(OssCurriculumBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1061" && rule.Type == "TIMEOUT_OUTBOX_RETENTION");
        Assert.All(OssCurriculumBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(OssCurriculumConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_OssCurriculumTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresExistingAndP10OssCurriculumRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new OssCurriculumRuleSnapshot("CF-1061", "", "F1560", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(6, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task AssignTierEnforcesValidationOrderingTierMapBoundaryAndFreeTenantPayloadRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var notValidated = TierRequest();
        notValidated["status"] = "DRAFT";
        var unknown = TierRequest();
        unknown["archetype"] = "UNKNOWN";
        var scoped = TierRequest();
        scoped["curriculumTenantId"] = "globex";

        var emptyRejected = await fixture.Service.AssignTierAsync(new Dictionary<string, object?>(), "tier:dpo-001", null);
        var notValidatedRejected = await fixture.Service.AssignTierAsync(notValidated, "tier:dpo-001", null);
        var unknownRejected = await fixture.Service.AssignTierAsync(unknown, "tier:dpo-001", null);
        var scopedRejected = await fixture.Service.AssignTierAsync(scoped, "tier:dpo-001", null);

        Assert.Equal("VALIDATION_FAILED", emptyRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", notValidatedRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", unknownRejected.Data?.Body["code"]);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task AssignTierIsIdempotentVersionAwareAndStoresBeforeQueueBroker()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.AssignTierAsync(TierRequest(), "tier:dpo-001", null);
        var replay = await fixture.Service.AssignTierAsync(TierRequest(), "tier:dpo-001", null);
        var stale = await fixture.Service.AssignTierAsync(TierRequest(), "tier:dpo-001", "stale-version");
        var tiers = (await fixture.Documents.SearchAsync(OssCurriculumConstants.CurriculumTierCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var broker = (await fixture.QueueBroker.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Equal(409, stale.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(first.Data?.Body["curriculumTier"], CultureInfo.InvariantCulture));
        Assert.Single(tiers);
        Assert.Single(outbox);
        Assert.Single(broker);
        Assert.Equal(outbox[0].Event.Id, broker[0].Event.Id);
    }

    [Fact]
    public async Task ShadowRunsRequireOssModelAndCycleIdAndUseQueueBrokerStub()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.SubmitShadowRunsAsync(ShadowRequest(), "shadow:dpo-001:cycle-001", null);
        var shadowRuns = (await fixture.Documents.SearchAsync(OssCurriculumConstants.ShadowRunCollection, Scope("acme-corp"))).Data ?? [];
        var broker = (await fixture.QueueBroker.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(3, shadowRuns.Count);
        Assert.All(shadowRuns, record =>
        {
            Assert.False(string.IsNullOrWhiteSpace(Convert.ToString(record.Body["ossModel"], CultureInfo.InvariantCulture)));
            Assert.Equal("cycle-001", record.Body["cycleId"]);
        });
        Assert.Single(broker);
        Assert.Equal("oss-curriculum.shadow-runs-submitted", broker[0].MessageType);
    }

    [Fact]
    public async Task CollectSignalsStoresExactlyThreeCanonicalLearningSignals()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.SubmitShadowRunsAsync(ShadowRequest(), "shadow:dpo-001:cycle-001", null);

        var result = await fixture.Service.CollectSignalsAsync(SignalRequest(), "signals:acme-corp:shadow:dpo-001:llama3:8b:cycle-001", null);
        var signals = (await fixture.Documents.SearchAsync(OssCurriculumConstants.LearningSignalCollection, Scope("acme-corp"))).Data ?? [];
        var signalPayload = signals.Single().Body["signals"] as IReadOnlyDictionary<string, object?>;

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.NotNull(signalPayload);
        Assert.Equal(OssCurriculumConstants.LearningSignalKeys.Order(StringComparer.Ordinal), signalPayload!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("STATIC", signalPayload["grade_trend"]);
    }

    [Fact]
    public async Task ProgressPlateauRecommendsPreSeedingOnlyAndNeverAutoActs()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        foreach (var cycle in new[] { "cycle-001", "cycle-002", "cycle-003" })
        {
            var request = SignalRequest(cycle, 0.7m);
            await fixture.Service.CollectSignalsAsync(request, $"signals:{request["shadowRunId"]}", null);
        }

        var result = await fixture.Service.TrackProgressAsync(ProgressRequest(), "progress:dpo-001:llama3:8b:cycle-004", null);

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PLATEAU_DETECTED", result.Data?.Body["progressStatus"]);
        Assert.Equal("PRE_SEEDING_RECOMMENDED", result.Data?.Body["recommendation"]);
        Assert.False(Convert.ToBoolean(result.Data?.Body["automaticActionTaken"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task TimeoutRetainsOutboxWithoutPublishingToQueueBroker()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var timeout = TierRequest();
        timeout["simulateTimeout"] = true;

        var result = await fixture.Service.AssignTierAsync(timeout, "tier:dpo-timeout", null);
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var broker = (await fixture.QueueBroker.ListAsync()).Data ?? [];

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", result.Data?.Body["code"]);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Single(outbox);
        Assert.Empty(broker);
    }

    [Fact]
    public async Task DashboardIsTenantScopedAndInspectorReadable()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.AssignTierAsync(TierRequest(), "tier:dpo-001", null);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.CurriculumTiers ?? []);
        Assert.Empty(dashboard.Data?.ShadowRuns ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-39-oss-curriculum", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<OssCurriculumService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<OssCurriculumOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
        Assert.Contains("IOssCurriculumQueueBroker", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> TierRequest() => new()
    {
        ["dpoTripleId"] = "dpo-001",
        ["taskTypeId"] = "T597-CURRICULUM-TIER-ASSIGNER",
        ["archetype"] = "ORCHESTRATION",
        ["status"] = "VALIDATED"
    };

    private static Dictionary<string, object?> ShadowRequest() => new()
    {
        ["dpoTripleId"] = "dpo-001",
        ["winningNodeId"] = "node-winning-001",
        ["cycleId"] = "cycle-001",
        ["curriculumTier"] = 4,
        ["taskTypeId"] = "T598-SHADOW-RUN-ORCHESTRATOR"
    };

    private static Dictionary<string, object?> SignalRequest(string cycleId = "cycle-001", decimal grade = 0.7m) => new()
    {
        ["shadowRunId"] = cycleId == "cycle-001" ? "acme-corp:shadow:dpo-001:llama3:8b:cycle-001" : $"shadow-{cycleId}",
        ["ossModel"] = "llama3:8b",
        ["cycleId"] = cycleId,
        ["grade"] = grade,
        ["ragContextSize"] = 12,
        ["graphContextSize"] = 7
    };

    private static Dictionary<string, object?> ProgressRequest() => new()
    {
        ["dpoTripleId"] = "dpo-001",
        ["ossModel"] = "llama3:8b",
        ["station"] = "DpoCaptureStation",
        ["cycleId"] = "cycle-004",
        ["minCycles"] = 3
    };

    private static OssCurriculumRuleSnapshot[] ValidRules() =>
        OssCurriculumConstants.RequiredBfaRules.Select(rule => new OssCurriculumRuleSnapshot(rule, "BUILD_FAILURE", "F1546", "OSS-CURRICULUM-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        OssCurriculumConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"OSS curriculum step {task}",
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
        OssCurriculumService Service,
        InMemoryDocumentStore Documents,
        InMemoryIdempotencyStore Idempotency,
        InMemoryOutboxStore Outbox,
        InMemoryOssCurriculumQueueBroker QueueBroker,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var queueBroker = new InMemoryOssCurriculumQueueBroker();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new OssCurriculumService(documents, idempotency, outbox, queueBroker, tenant, correlation, NullLogger<OssCurriculumService>.Instance);
            return new FlowFixture(service, documents, idempotency, outbox, queueBroker, tenant, correlation);
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
