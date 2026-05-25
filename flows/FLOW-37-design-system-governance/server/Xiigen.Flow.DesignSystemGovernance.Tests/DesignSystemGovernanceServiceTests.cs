using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DesignSystemGovernance.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignSystemGovernance.Tests;

public sealed class DesignSystemGovernanceServiceTests
{
    [Fact]
    public void P11_AllFlow37BfaRulesAreRegistered()
    {
        var ids = DesignSystemGovernanceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(DesignSystemGovernanceConstants.RequiredBfaRules));
        Assert.Equal(4, DesignSystemGovernanceBfaRuleRegistry.Rules.Count);
        Assert.Contains(DesignSystemGovernanceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1050" && rule.Type == "OPTIMISTIC_CONCURRENCY");
        Assert.Contains(DesignSystemGovernanceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1053" && rule.Type == "TIMEOUT_OUTBOX_RETENTION");
        Assert.All(DesignSystemGovernanceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(DesignSystemGovernanceConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_DesignSystemGovernanceTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(3, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteDesignSystemGovernanceEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new DesignSystemGovernanceRuleSnapshot("CF-1053", "", "F1510", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RegisterGovernanceRejectsBoundaryValuesFreeTenantPayloadsAndMissingIdempotency()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var zeroCompatibility = GovernanceRequest();
        zeroCompatibility["compatibilityScore"] = 0;
        var negativeDebt = GovernanceRequest();
        negativeDebt["designDebtScore"] = -1;
        var zeroConsistency = GovernanceRequest();
        zeroConsistency["tokenConsistencyScore"] = 0;
        var scoped = GovernanceRequest();
        scoped["designTenantId"] = "globex";

        var emptyRejected = await fixture.Service.RegisterGovernanceAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var compatibilityRejected = await fixture.Service.RegisterGovernanceAsync(zeroCompatibility, "zero-compatibility", null);
        var debtRejected = await fixture.Service.RegisterGovernanceAsync(negativeDebt, "negative-debt", null);
        var consistencyRejected = await fixture.Service.RegisterGovernanceAsync(zeroConsistency, "zero-consistency", null);
        var scopedRejected = await fixture.Service.RegisterGovernanceAsync(scoped, "scoped", null);
        var idempotencyRejected = await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), null, null);

        Assert.Equal(400, emptyRejected.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", emptyRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", compatibilityRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", debtRejected.Data?.Body["code"]);
        Assert.Equal("VALIDATION_FAILED", consistencyRejected.Data?.Body["code"]);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", idempotencyRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task RegisterGovernanceIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-1", null);
        var replay = await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-1", null);
        var classifications = (await fixture.Documents.SearchAsync(DesignSystemGovernanceConstants.ClassificationCollection, Scope("acme-corp"))).Data ?? [];
        var prompts = (await fixture.Documents.SearchAsync(DesignSystemGovernanceConstants.PromptCollection, Scope("acme-corp"))).Data ?? [];
        var debt = (await fixture.Documents.SearchAsync(DesignSystemGovernanceConstants.DebtCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(classifications);
        Assert.Single(prompts);
        Assert.Single(debt);
        Assert.Contains(outbox, record => record.Event.Type == "design-system-governance.classification-ready");
    }

    [Fact]
    public async Task RegisterGovernanceOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-1", null);
        var timeout = GovernanceRequest("tokens-dark-mode");
        timeout["simulateTimeout"] = true;

        var staleResult = await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-2", "stale-version");
        var timeoutResult = await fixture.Service.RegisterGovernanceAsync(timeout, "timeout-1", null);

        Assert.Equal(409, staleResult.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", staleResult.Data?.Body["code"]);
        Assert.Equal("CF-1050", staleResult.Data?.Body["ruleId"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Theory]
    [InlineData(82, 30, 91, "APPROVED")]
    [InlineData(66, 70, 72, "NEEDS_REVIEW")]
    [InlineData(40, 12, 88, "BLOCKED")]
    public async Task ClassificationReflectsStackCompatibilityDebtAndTokenConsistency(int compatibility, int debt, int consistency, string expected)
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var request = GovernanceRequest($"system-{expected.ToLowerInvariant()}");
        request["compatibilityScore"] = compatibility;
        request["designDebtScore"] = debt;
        request["tokenConsistencyScore"] = consistency;

        var result = await fixture.Service.RegisterGovernanceAsync(request, $"key-{expected}", null);

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(expected, result.Data?.Body["classification"]);
    }

    [Fact]
    public async Task DashboardSummarizesDesignSystemGovernanceLifecycleAndIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var result = await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-1", null);
            Assert.Equal("APPROVED", result.Data?.Body["classification"]);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Classifications ?? []);
        Assert.Empty(dashboard.Data?.Prompts ?? []);
        Assert.Empty(dashboard.Data?.DebtReports ?? []);
        Assert.Empty(dashboard.Data?.CompatibilityReports ?? []);
    }

    [Fact]
    public async Task PlatformSupportTenantAdminAndPlatformAdminCanReadDashboard()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RegisterGovernanceAsync(GovernanceRequest(), "governance-1", null);
        }

        using var support = fixture.PushContext("acme-corp", "platform_support");
        var supportDashboard = await fixture.Service.GetDashboardAsync();
        support.Dispose();
        using var tenantAdmin = fixture.PushContext("acme-corp", "tenant_admin");
        var tenantDashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(supportDashboard.Data?.Classifications ?? []);
        Assert.Single(tenantDashboard.Data?.Classifications ?? []);
    }

    [Fact]
    public void InvalidTaskCoverageReportsMissingDesignSystemGovernanceTasks()
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
        var flowRoot = Path.Combine(root, "flows", "FLOW-37-design-system-governance", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<DesignSystemGovernanceService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<DesignSystemGovernanceOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> GovernanceRequest(string designSystemId = "tokens-accessible-core") => new()
    {
        ["designSystemId"] = designSystemId,
        ["stackFamily"] = "dotnet-angular",
        ["tokenSetName"] = "accessible-core",
        ["genesisFrame"] = "angular-standalone-flow-shell",
        ["targetStack"] = "angular-22",
        ["compatibilityScore"] = 92,
        ["designDebtScore"] = 24,
        ["tokenConsistencyScore"] = 96,
        ["excludedIncompatibilities"] = new[] { "physical-left-right-spacing", "unstyled-generated-cards" }
    };

    private static DesignSystemGovernanceRuleSnapshot[] ValidRules() =>
        DesignSystemGovernanceConstants.RequiredBfaRules.Select(rule => new DesignSystemGovernanceRuleSnapshot(rule, "BUILD_FAILURE", "F1510", "DESIGN-SYSTEM-CLASSIFICATION-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        DesignSystemGovernanceConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Design-system governance step {task}",
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
        DesignSystemGovernanceService Service,
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
            var service = new DesignSystemGovernanceService(documents, idempotency, outbox, tenant, correlation, NullLogger<DesignSystemGovernanceService>.Instance);
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
