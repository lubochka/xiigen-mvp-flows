using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.BfaCrossFlowGovernance.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.BfaCrossFlowGovernance.Tests;

public sealed class BfaCrossFlowGovernanceServiceTests
{
    [Fact]
    public void P11_AllFlow25BfaRulesAreRegistered()
    {
        var ids = BfaCrossFlowGovernanceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(BfaCrossFlowGovernanceConstants.RequiredBfaRules));
        Assert.Equal(72, BfaCrossFlowGovernanceBfaRuleRegistry.Rules.Count);
        Assert.Contains(BfaCrossFlowGovernanceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-473");
        Assert.Contains(BfaCrossFlowGovernanceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-544");
        Assert.All(BfaCrossFlowGovernanceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(BfaCrossFlowGovernanceConstants.FlowId, rule.FlowId);
            Assert.Equal("CROSS_FLOW", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void P11_AllStressTestsAreRequired()
    {
        Assert.Equal(25, BfaCrossFlowGovernanceConstants.RequiredStressTests.Length);
        Assert.Equal("ST-300", BfaCrossFlowGovernanceConstants.RequiredStressTests.First());
        Assert.Equal("ST-324", BfaCrossFlowGovernanceConstants.RequiredStressTests.Last());
    }

    [Fact]
    public void P1_T375ThroughT388HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(14, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRejectsMissingDuplicateAndInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "governance_operator");
        var rules = ValidRules().Skip(1).ToList();
        rules.Add(rules[0] with { RuleId = rules[1].RuleId });
        rules.Add(new BfaRuleSnapshot("CF-999", "", "FLOW-01", "", "", "Bad rule"));

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = rules });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", result.Data?.Body["code"]);
    }

    [Fact]
    public void RulePackAcceptsCompleteGovernanceEnvelope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "governance_operator");

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal(72, Convert.ToInt32(result.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SubmitChangeRequiresContentAddressedDiffAndWritesBeforeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "governance_operator");
        var bad = Change();
        bad["diffBlobRef"] = "raw-path";

        var rejected = await fixture.Service.SubmitChangeAsync(bad, "change-bad");
        var accepted = await fixture.Service.SubmitChangeAsync(Change(), "change-1");
        var changes = (await fixture.Documents.SearchAsync(BfaCrossFlowGovernanceConstants.ChangeCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("DIFF_BLOB_REF_NOT_CONTENT_ADDRESSED", rejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Single(changes);
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.bfa.change.intake-received");
    }

    [Fact]
    public async Task ConflictScanFailsClosedWithoutRulePack()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "arbiter_reviewer");

        var result = await fixture.Service.ScanConflictsAsync(Change(), "scan-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_MUST_PASS_BEFORE_SCAN", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(BfaCrossFlowGovernanceConstants.ConflictCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task ConflictScanDetectsRequiredFieldRemovalAndPersistsBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "arbiter_reviewer");
        var request = Change();
        request["rules"] = ValidRules();
        request["removedRequiredFields"] = new[] { "UserProfile.billingAddress" };

        var result = await fixture.Service.ScanConflictsAsync(request, "scan-1");
        var conflicts = (await fixture.Documents.SearchAsync(BfaCrossFlowGovernanceConstants.ConflictCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("CRITICAL", result.Data?.Body["compositeSeverity"]);
        Assert.Single(conflicts);
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.bfa.conflicts.scanned");
    }

    [Fact]
    public async Task ImpactAnalysisRejectsUnknownFlowIdsAndAcceptsCycleSafeTraversal()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "impact_analyst");

        var rejected = await fixture.Service.AnalyzeImpactAsync(new Dictionary<string, object?> { ["conflictScanId"] = "scan-1", ["impactedFlows"] = new[] { "billing-flow" } }, "impact-bad");
        var accepted = await fixture.Service.AnalyzeImpactAsync(new Dictionary<string, object?> { ["conflictScanId"] = "scan-1", ["impactedFlows"] = new[] { "FLOW-01", "FLOW-12", "FLOW-01" }, ["maxTraversalDepth"] = 3 }, "impact-1");

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("IMPACT_GRAPH_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.True(Convert.ToBoolean(accepted.Data?.Body["circularDependencyDetected"], CultureInfo.InvariantCulture));
        Assert.Equal(2, Convert.ToInt32(accepted.Data?.Body["totalUniqueFlows"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task StressRunBlocksMissingTestsAndAcceptsFullMatrix()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "arbiter_reviewer");

        var missing = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults().Take(24).ToArray() }, "stress-bad");
        var accepted = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults() }, "stress-1");

        Assert.Equal(400, missing.Data?.HttpStatus);
        Assert.Equal("STRESS_MATRIX_FAILED", missing.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(25, Convert.ToInt32(accepted.Data?.Body["stressTestCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReleaseGateFailsClosedUntilScanImpactAndStressEvidenceExist()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "release_manager");

        var result = await fixture.Service.ApproveReleaseAsync(Release("scan-missing", "impact-missing", "stress-missing"), "release-1");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("RELEASE_GATE_EVIDENCE_MISSING", result.Data?.Body["code"]);
    }

    [Fact]
    public async Task ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "release_manager");
        var scan = Change();
        scan["rules"] = ValidRules();
        scan["removedRequiredFields"] = new[] { "UserProfile.billingAddress" };
        var scanResult = await fixture.Service.ScanConflictsAsync(scan, "scan-1");
        var impactResult = await fixture.Service.AnalyzeImpactAsync(new Dictionary<string, object?> { ["conflictScanId"] = scanResult.Data?.Body["scanId"], ["impactedFlows"] = new[] { "FLOW-01", "FLOW-12" } }, "impact-1");
        var stressResult = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults() }, "stress-1");

        var approved = await fixture.Service.ApproveReleaseAsync(Release(
            Convert.ToString(scanResult.Data?.Body["scanId"], CultureInfo.InvariantCulture)!,
            Convert.ToString(impactResult.Data?.Body["impactId"], CultureInfo.InvariantCulture)!,
            Convert.ToString(stressResult.Data?.Body["stressRunId"], CultureInfo.InvariantCulture)!), "release-1");
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, approved.Data?.HttpStatus);
        Assert.Equal("APPROVED", approved.Data?.Body["status"]);
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.bfa.conflict.resolved");
    }

    [Fact]
    public async Task TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "governance_operator");
        var request = Change();
        request["tenantId"] = "globex";

        var result = await fixture.Service.SubmitChangeAsync(request, "change-tenant");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", result.Data?.Body["code"]);
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "arbiter_reviewer"))
        {
            var request = Change();
            request["rules"] = ValidRules();
            request["removedRequiredFields"] = new[] { "UserProfile.billingAddress" };
            await fixture.Service.ScanConflictsAsync(request, "scan-1");
        }

        using var _ = fixture.PushContext("globex", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Conflicts ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-25-bfa-cross-flow-governance", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("googleapis", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<BfaCrossFlowGovernanceService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<BfaCrossFlowGovernanceOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Change() => new()
    {
        ["proposedChangeId"] = "change-1",
        ["changeType"] = "ENTITY_SCHEMA",
        ["diffBlobRef"] = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ["sourceFlow"] = "FLOW-01"
    };

    private static Dictionary<string, object?> Release(string scanId, string impactId, string stressRunId) => new()
    {
        ["proposedChangeId"] = "change-1",
        ["decision"] = "REFACTOR_FLOWS",
        ["rationale"] = "Refactor affected flows through governed release path.",
        ["conflictScanId"] = scanId,
        ["impactId"] = impactId,
        ["stressRunId"] = stressRunId
    };

    private static BfaRuleSnapshot[] ValidRules() =>
        BfaCrossFlowGovernanceConstants.RequiredBfaRules.Select(rule => new BfaRuleSnapshot(rule, "CRITICAL", "FLOW-25", "ALL_FLOWS", "GLOBAL", $"Rule {rule}")).ToArray();

    private static BfaStressResult[] StressResults() =>
        BfaCrossFlowGovernanceConstants.RequiredStressTests.Select(test => new BfaStressResult(test, "PASS", "governance")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        BfaCrossFlowGovernanceConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"BFA governance plan step {task}",
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
        BfaCrossFlowGovernanceService Service,
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
            var service = new BfaCrossFlowGovernanceService(documents, idempotency, outbox, tenant, correlation, NullLogger<BfaCrossFlowGovernanceService>.Instance);
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
