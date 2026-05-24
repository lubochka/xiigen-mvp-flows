using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SharableFlowsMarketplace.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.SharableFlowsMarketplace.Tests;

public sealed class SharableFlowsMarketplaceServiceTests
{
    [Fact]
    public void P11_AllFLOW32BfaRulesAreRegistered()
    {
        var ids = SharableFlowsMarketplaceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(SharableFlowsMarketplaceConstants.RequiredBfaRules));
        Assert.Equal(4, SharableFlowsMarketplaceBfaRuleRegistry.Rules.Count);
        Assert.Contains(SharableFlowsMarketplaceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1019");
        Assert.Contains(SharableFlowsMarketplaceBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1022");
        Assert.All(SharableFlowsMarketplaceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(SharableFlowsMarketplaceConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_T516ThroughT535HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(20, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackAcceptsCompleteMarketplaceEnvelopeAndRejectsInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new SharableFlowsRuleSnapshot("CF-1022", "", "F32", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(4, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task PublishPackageRejectsBoundaryValuesSecretsAndFreeTenantPayloads()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");
        var zeroArtifacts = Package();
        zeroArtifacts["artifactCount"] = 0;
        var secretPayload = Package();
        secretPayload["logicManifest"] = "contains-secret";
        var scoped = Package();
        scoped["publisherTenantId"] = "globex";

        var validation = await fixture.Service.PublishPackageAsync(new Dictionary<string, object?>(), "bad-empty", null);
        var zeroRejected = await fixture.Service.PublishPackageAsync(zeroArtifacts, "zero", null);
        var secretRejected = await fixture.Service.PublishPackageAsync(secretPayload, "secret", null);
        var scopedRejected = await fixture.Service.PublishPackageAsync(scoped, "scoped", null);

        Assert.Equal(400, validation.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", validation.Data?.Body["code"]);
        Assert.Equal(400, zeroRejected.Data?.HttpStatus);
        Assert.Equal(400, secretRejected.Data?.HttpStatus);
        Assert.Equal(400, scopedRejected.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task PublishPackageIsIdempotentAndStoresBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");

        var first = await fixture.Service.PublishPackageAsync(Package(), "publish-1", null);
        var replay = await fixture.Service.PublishPackageAsync(Package(), "publish-1", null);
        var packages = (await fixture.Documents.SearchAsync(SharableFlowsMarketplaceConstants.PackageCollection, Scope("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(SharableFlowsMarketplaceConstants.AuditCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Single(packages);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "sharable-flows.package.published");
    }

    [Fact]
    public async Task PublishPackageOptimisticConcurrencyReturns409AndTimeoutRetainsOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        await fixture.Service.PublishPackageAsync(Package(), "publish-1", null);
        var timeout = Package();
        timeout["simulateTimeout"] = true;

        var conflict = await fixture.Service.PublishPackageAsync(Package(), "publish-2", "stale-version");
        var timeoutResult = await fixture.Service.PublishPackageAsync(timeout, "timeout-1", null);

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(504, timeoutResult.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", timeoutResult.Data?.Body["code"]);
        Assert.True(timeoutResult.Data?.OutboxRetained);
    }

    [Fact]
    public async Task InstallRagSettlementAndSandboxAreTenantScopedAndIdempotent()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            var install = await fixture.Service.InstallTemplateAsync(Install(), "install-1");
            var installReplay = await fixture.Service.InstallTemplateAsync(Install(), "install-1");
            var sandbox = await fixture.Service.ProvisionSandboxAsync(Sandbox(), "sandbox-1");

            Assert.Equal(201, install.Data?.HttpStatus);
            Assert.Equal(200, installReplay.Data?.HttpStatus);
            Assert.Equal(201, sandbox.Data?.HttpStatus);
        }

        using (fixture.PushContext("acme-corp", "freelancer"))
        {
            var rag = await fixture.Service.PublishRagBlueprintAsync(RagBlueprint(), "rag-1");
            Assert.Equal(201, rag.Data?.HttpStatus);
        }

        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            var settlement = await fixture.Service.RecordUsageAndSettlementAsync(Settlement(), "settlement-1");
            Assert.Equal(201, settlement.Data?.HttpStatus);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Packages ?? []);
        Assert.Empty(dashboard.Data?.Installs ?? []);
        Assert.Empty(dashboard.Data?.Blueprints ?? []);
        Assert.Empty(dashboard.Data?.Settlements ?? []);
    }

    [Fact]
    public async Task RagBlueprintRejectsDatasetsAndSettlementUsesIntegerCents()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var dataset = RagBlueprint();
        dataset["includeDataset"] = true;
        var zeroMoney = Settlement();
        zeroMoney["amountCents"] = 0;

        var ragRejected = await fixture.Service.PublishRagBlueprintAsync(dataset, "rag-bad");
        var settlementRejected = await fixture.Service.RecordUsageAndSettlementAsync(zeroMoney, "settlement-bad");

        Assert.Equal(400, ragRejected.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", ragRejected.Data?.Body["code"]);
        Assert.Equal(400, settlementRejected.Data?.HttpStatus);
    }

    [Fact]
    public async Task DashboardSummarizesMarketplaceLifecycle()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.PublishPackageAsync(Package(), "publish-1", null);
        await fixture.Service.InstallTemplateAsync(Install(), "install-1");
        await fixture.Service.PublishRagBlueprintAsync(RagBlueprint(), "rag-1");
        await fixture.Service.RecordUsageAndSettlementAsync(Settlement(), "settlement-1");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Single(dashboard.Data?.Packages ?? []);
        Assert.Single(dashboard.Data?.Installs ?? []);
        Assert.Single(dashboard.Data?.Blueprints ?? []);
        Assert.Single(dashboard.Data?.Settlements ?? []);
        Assert.Equal(4, Convert.ToInt32(dashboard.Data?.Kpis["requiredRuleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-32-sharable-flows-marketplace", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<SharableFlowsMarketplaceService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<SharableFlowsMarketplaceOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Package() => new()
    {
        ["packageSlug"] = "curated-flow-store",
        ["displayName"] = "Curated Flow Store",
        ["semanticVersion"] = "1.0.0",
        ["license"] = "usage-share",
        ["pricingModel"] = "per-install",
        ["logicManifest"] = "flow definitions, prompts, schemas",
        ["ragBlueprint"] = "hybrid-search-blueprint",
        ["artifactCount"] = 4
    };

    private static Dictionary<string, object?> Install() => new()
    {
        ["packageSlug"] = "curated-flow-store",
        ["installMode"] = "linked-copy",
        ["dependencyPolicy"] = "prompt",
        ["connectorBindingRef"] = "vault://tenant/acme/flow32/binding"
    };

    private static Dictionary<string, object?> RagBlueprint() => new()
    {
        ["blueprintSlug"] = "hybrid-search-blueprint",
        ["retrievalStrategy"] = "hybrid-vector-graph",
        ["promptProfile"] = "marketplace-safe",
        ["schemaRef"] = "schema://FLOW-32/rag-blueprint"
    };

    private static Dictionary<string, object?> Settlement() => new()
    {
        ["packageSlug"] = "curated-flow-store",
        ["meterEvent"] = "template.install",
        ["periodKey"] = "2026-05",
        ["currency"] = "USD",
        ["amountCents"] = 1299
    };

    private static Dictionary<string, object?> Sandbox() => new()
    {
        ["packageSlug"] = "curated-flow-store",
        ["sandboxProfile"] = "preview-install",
        ["ttlHours"] = 24
    };

    private static SharableFlowsRuleSnapshot[] ValidRules() =>
        SharableFlowsMarketplaceConstants.RequiredBfaRules.Select(rule => new SharableFlowsRuleSnapshot(rule, "BUILD_FAILURE", "F32", "T516", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        SharableFlowsMarketplaceConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Sharable flows marketplace step {task}",
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
        SharableFlowsMarketplaceService Service,
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
            var service = new SharableFlowsMarketplaceService(documents, idempotency, outbox, tenant, correlation, NullLogger<SharableFlowsMarketplaceService>.Instance);
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