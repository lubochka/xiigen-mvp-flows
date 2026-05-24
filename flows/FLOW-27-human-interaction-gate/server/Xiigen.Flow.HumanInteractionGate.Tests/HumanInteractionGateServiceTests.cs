using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.HumanInteractionGate.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.HumanInteractionGate.Tests;

public sealed class HumanInteractionGateServiceTests
{
    [Fact]
    public void P11_AllFLOW27BfaRulesAreRegistered()
    {
        var ids = HumanInteractionGateBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(HumanInteractionGateConstants.RequiredBfaRules));
        Assert.Equal(28, HumanInteractionGateBfaRuleRegistry.Rules.Count);
        Assert.Contains(HumanInteractionGateBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-545");
        Assert.Contains(HumanInteractionGateBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-565");
        Assert.Contains(HumanInteractionGateBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-996");
        Assert.Contains(HumanInteractionGateBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1002");
        Assert.All(HumanInteractionGateBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(HumanInteractionGateConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void P11_AllStressTestsAreRequired()
    {
        Assert.Equal(21, HumanInteractionGateConstants.RequiredStressTests.Length);
        Assert.Equal("ST-325", HumanInteractionGateConstants.RequiredStressTests.First());
        Assert.Equal("ST-345", HumanInteractionGateConstants.RequiredStressTests.Last());
    }

    [Fact]
    public void P1_T413ThroughT422HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(10, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRejectsMissingDuplicateAndInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        var rules = ValidRules().Skip(1).ToList();
        rules.Add(rules[0] with { RuleId = rules[1].RuleId });
        rules.Add(new HumanGateRuleSnapshot("CF-1002", "", "F1128", "", "", "Bad rule"));

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = rules });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", result.Data?.Body["code"]);
    }

    [Fact]
    public void RulePackAcceptsCompleteHumanGateEnvelope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal(28, Convert.ToInt32(result.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task OpenGateIsIdempotentPerRunAndNodeAndWritesBeforeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.OpenGateAsync(Gate(), "open-1");
        var second = await fixture.Service.OpenGateAsync(Gate(), "open-2");
        var tasks = (await fixture.Documents.SearchAsync(HumanInteractionGateConstants.TaskCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(tasks);
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.human-gate.user-task.created");
    }

    [Fact]
    public async Task OpenGateRejectsCrossTenantGroupsAndImpossibleQuorum()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var crossTenant = Gate();
        crossTenant["assigneePolicy"] = new Dictionary<string, object?> { ["type"] = "group", ["targets"] = new[] { "tenant-B:admins" } };
        var impossible = Gate();
        impossible["assigneePolicy"] = new Dictionary<string, object?> { ["type"] = "quorum", ["needed"] = 3, ["targets"] = new[] { "u1", "u2" } };

        var rejectedCrossTenant = await fixture.Service.OpenGateAsync(crossTenant, "open-cross");
        var rejectedQuorum = await fixture.Service.OpenGateAsync(impossible, "open-quorum");

        Assert.Equal(400, rejectedCrossTenant.Data?.HttpStatus);
        Assert.Equal("CROSS_TENANT_GROUP_REJECTED", rejectedCrossTenant.Data?.Body["code"]);
        Assert.Equal(409, rejectedQuorum.Data?.HttpStatus);
        Assert.Equal("QUORUM_EXCEEDS_ELIGIBLE_USERS", rejectedQuorum.Data?.Body["code"]);
    }

    [Fact]
    public async Task ClaimAndAnswerRejectExpiredOrMissingClaimWhenRequired()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var gate = await fixture.Service.OpenGateAsync(Gate(), "open-1");
        var taskId = Convert.ToString(gate.Data?.Body["taskId"], CultureInfo.InvariantCulture)!;

        var rejected = await fixture.Service.SubmitAnswerAsync(new Dictionary<string, object?>
        {
            ["taskId"] = taskId,
            ["answererUserId"] = "u1",
            ["decision"] = "APPROVE",
            ["requireClaim"] = true
        }, "answer-no-claim");

        var claim = await fixture.Service.ClaimTaskAsync(new Dictionary<string, object?> { ["taskId"] = taskId, ["claimantUserId"] = "u1", ["ttlMinutes"] = 5 }, "claim-1");
        var accepted = await fixture.Service.SubmitAnswerAsync(new Dictionary<string, object?>
        {
            ["taskId"] = taskId,
            ["answererUserId"] = "u1",
            ["decision"] = "APPROVE",
            ["requireClaim"] = true
        }, "answer-1");

        Assert.Equal(409, rejected.Data?.HttpStatus);
        Assert.Equal("CLAIM_REQUIRED_OR_EXPIRED", rejected.Data?.Body["code"]);
        Assert.Equal(201, claim.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("answered", accepted.Data?.Body["status"]);
    }

    [Fact]
    public async Task QuorumPreservesPartialAnswersAndClosesOnce()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var gateRequest = Gate();
        gateRequest["nodeId"] = "quorum-node";
        gateRequest["assigneePolicy"] = new Dictionary<string, object?> { ["type"] = "quorum", ["needed"] = 2, ["targets"] = new[] { "u1", "u2", "u3" } };
        var gate = await fixture.Service.OpenGateAsync(gateRequest, "open-quorum");
        var taskId = Convert.ToString(gate.Data?.Body["taskId"], CultureInfo.InvariantCulture)!;

        var first = await fixture.Service.SubmitAnswerAsync(new Dictionary<string, object?> { ["taskId"] = taskId, ["answererUserId"] = "u1", ["decision"] = "APPROVE" }, "answer-u1");
        var second = await fixture.Service.SubmitAnswerAsync(new Dictionary<string, object?> { ["taskId"] = taskId, ["answererUserId"] = "u2", ["decision"] = "APPROVE" }, "answer-u2");
        var duplicate = await fixture.Service.SubmitAnswerAsync(new Dictionary<string, object?> { ["taskId"] = taskId, ["answererUserId"] = "u2", ["decision"] = "APPROVE" }, "answer-u2b");

        Assert.Equal("waiting_for_user", first.Data?.Body["status"]);
        Assert.Equal("answered", second.Data?.Body["status"]);
        Assert.Equal(409, duplicate.Data?.HttpStatus);
        Assert.Equal("ALREADY_ANSWERED", duplicate.Data?.Body["code"]);
    }

    [Fact]
    public async Task CompletionGateFailsClosedWhileBlockingTasksOrJudgeGatesRemain()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.OpenGateAsync(Gate(), "open-1");

        var blocked = await fixture.Service.EvaluateCompletionAsync(Completion(pendingJudge: ["phase-gate-final"]), "completion-1");
        var readyRequest = Completion();
        readyRequest["runId"] = "run-ready";
        var ready = await fixture.Service.EvaluateCompletionAsync(readyRequest, "completion-2");

        Assert.Equal(201, blocked.Data?.HttpStatus);
        Assert.False(Convert.ToBoolean(blocked.Data?.Body["ready"], CultureInfo.InvariantCulture));
        Assert.Equal("BLOCKED_ON_DEPENDENCY", blocked.Data?.Body["status"]);
        Assert.True(Convert.ToBoolean(ready.Data?.Body["ready"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Gate();
        request["tenantId"] = "globex";

        var result = await fixture.Service.OpenGateAsync(request, "open-tenant");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", result.Data?.Body["code"]);
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
            await fixture.Service.OpenGateAsync(Gate(), "open-1");
        }

        using var _ = fixture.PushContext("globex", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.OpenTasks ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-27-human-interaction-gate", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("googleapis", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<HumanInteractionGateService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<HumanInteractionGateOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Gate() => new()
    {
        ["runId"] = "run-1",
        ["nodeId"] = "human-gate-1",
        ["title"] = "Approve generated flow promotion",
        ["blockingPolicy"] = "blocking",
        ["assigneePolicy"] = new Dictionary<string, object?> { ["type"] = "single", ["targets"] = new[] { "u1" } }
    };

    private static Dictionary<string, object?> Completion(string[]? pendingJudge = null) => new()
    {
        ["runId"] = "run-1",
        ["terminalNodes"] = new[]
        {
            new Dictionary<string, object?> { ["nodeId"] = "final-validation", ["required"] = true, ["status"] = "SUCCEEDED" }
        },
        ["pendingJudgeGateIds"] = pendingJudge ?? []
    };

    private static HumanGateRuleSnapshot[] ValidRules() =>
        HumanInteractionGateConstants.RequiredBfaRules.Select(rule => new HumanGateRuleSnapshot(rule, "CRITICAL", "F1104", "T413", "GLOBAL", $"Rule {rule}")).ToArray();

    private static HumanGateStressResult[] StressResults() =>
        HumanInteractionGateConstants.RequiredStressTests.Select(test => new HumanGateStressResult(test, "PASS", "human-gate")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        HumanInteractionGateConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Human gate step {task}",
            ["responsibility"] = $"Single responsibility for {task}"
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Tenant(string scope) => new Dictionary<string, object?> { ["tenantId"] = scope };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        HumanInteractionGateService Service,
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
            var service = new HumanInteractionGateService(documents, idempotency, outbox, tenant, correlation, NullLogger<HumanInteractionGateService>.Instance);
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
