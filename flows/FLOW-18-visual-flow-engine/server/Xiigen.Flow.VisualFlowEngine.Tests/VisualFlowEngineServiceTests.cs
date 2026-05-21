using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.VisualFlowEngine.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.VisualFlowEngine.Tests;

public sealed class VisualFlowEngineServiceTests
{
    [Fact]
    public void P11_Rules_AllVisualFlowEngineRules_AreRegistered()
    {
        var ids = VisualFlowEngineBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(VisualFlowEngineConstants.RuleIds));
        Assert.Equal(8, VisualFlowEngineBfaRuleRegistry.Rules.Count);
        Assert.All(VisualFlowEngineBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(VisualFlowEngineConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF18_1_SaveCanvasValidatesTaskNodesAuditsBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");

        var result = await fixture.Service.SaveCanvasAsync(Canvas("flow-a"), null, "canvas-a");
        var canvases = (await fixture.Documents.SearchAsync(VisualFlowEngineConstants.FlowCanvases, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(VisualFlowEngineConstants.CanvasAudits, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("DRAFT", result.Data?.Body["status"]);
        Assert.Single(canvases);
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "CANVAS_UPDATED");
        Assert.Equal(VisualFlowEngineConstants.FlowCanvasUpdatedEvent, outbox.Last().Event.Type);
    }

    [Fact]
    public async Task CF18_1_BolaMismatchRejectsBeforeStateMutation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        await fixture.Documents.UpsertAsync(VisualFlowEngineConstants.FlowCanvases, "flow-cross", Tenant(new Dictionary<string, object?>
        {
            ["flow_id"] = "flow-cross",
            ["status"] = "DRAFT",
            ["nodes"] = Nodes(),
            ["edges"] = Array.Empty<IReadOnlyDictionary<string, object?>>()
        }, "northwind"), null);

        var result = await fixture.Service.SaveCanvasAsync(Canvas("flow-cross"), null, "cross");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("BOLA_VIOLATION", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.FlowCanvasUpdateFailedEvent);
    }

    [Fact]
    public async Task CF18_1_PublishedCanvasCannotBeModified()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        var saved = await fixture.Service.SaveCanvasAsync(Canvas("flow-locked"), null, "locked-save");
        var version = Convert.ToString(saved.Data!.Body["version"], CultureInfo.InvariantCulture);
        await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "flow-locked", ["expectedVersion"] = version }, version, "locked-publish");

        var result = await fixture.Service.SaveCanvasAsync(Canvas("flow-locked"), null, "locked-edit");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("FLOW_IMMUTABLE", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.FlowImmutableRejectedEvent);
    }

    [Fact]
    public async Task CF957_ConcurrentCanvasWritesAllowOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");

        var first = fixture.Service.SaveCanvasAsync(Canvas("flow-race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.SaveCanvasAsync(Canvas("flow-race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF958_IdempotencyReplaysCanvasWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");

        var first = await fixture.Service.SaveCanvasAsync(Canvas("flow-replay"), null, "same-key");
        var second = await fixture.Service.SaveCanvasAsync(Canvas("flow-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.CanvasAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.FlowCanvasUpdatedEvent);
    }

    [Fact]
    public async Task CF959_InvalidCanvasReturnsFieldErrorsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");

        var result = await fixture.Service.SaveCanvasAsync(new Dictionary<string, object?> { ["flowId"] = "invalid", ["nodes"] = Array.Empty<object>() }, null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.FlowCanvases, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF960_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        var request = Canvas("flow-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.SaveCanvasAsync(request, null, "timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.FlowCanvases, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF18_2_PublicationRejectsCycleBeforeTypeCheck()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        var saved = await fixture.Service.SaveCanvasAsync(Canvas("flow-cycle", edges: CycleEdges()), null, "cycle-save");
        var version = Convert.ToString(saved.Data!.Body["version"], CultureInfo.InvariantCulture);

        var result = await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "flow-cycle", ["expectedVersion"] = version }, version, "cycle-publish");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("CYCLE_DETECTED", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.CycleDetectedEvent);
        Assert.DoesNotContain((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.TypeMismatchEvent);
    }

    [Fact]
    public async Task CF18_2_PublicationRejectsTypeMismatchAfterAcyclicCheck()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        var saved = await fixture.Service.SaveCanvasAsync(Canvas("flow-mismatch", edges: MismatchedEdges()), null, "mismatch-save");
        var version = Convert.ToString(saved.Data!.Body["version"], CultureInfo.InvariantCulture);

        var result = await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "flow-mismatch", ["expectedVersion"] = version }, version, "mismatch-publish");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("TYPE_MISMATCH", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.TypeMismatchEvent);
    }

    [Fact]
    public async Task CF18_2_PublicationUsesOptimisticConcurrencyAndEmitsOnSuccess()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "flow_author");
        var saved = await fixture.Service.SaveCanvasAsync(Canvas("flow-publish"), null, "publish-save");
        var version = Convert.ToString(saved.Data!.Body["version"], CultureInfo.InvariantCulture);

        var conflict = await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "flow-publish", ["expectedVersion"] = "stale" }, "stale", "publish-conflict");
        var published = await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "flow-publish", ["expectedVersion"] = version }, version, "publish-ok");

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
        Assert.Equal(201, published.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", published.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.PublicationAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Equal(VisualFlowEngineConstants.FlowPublishedEvent, (await fixture.Outbox.ListAsync()).Data!.Last().Event.Type);
    }

    [Fact]
    public async Task CF18_3_NodeTypeRegistrationWritesDualRecordsAndAudit()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.RegisterNodeTypeAsync(NodeType("email-send"), "node-type-a");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("REGISTERED", result.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.NodeTypes, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.NodeCapabilities, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.NodeTypeAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.NodeTypeRegisteredEvent);
    }

    [Fact]
    public async Task CF18_3_NodeTypeRegistrationReleasesLockOnDualWriteFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var request = NodeType("broken-node");
        request["simulateCapabilityWriteFailure"] = true;

        var result = await fixture.Service.RegisterNodeTypeAsync(request, "node-type-fail");
        var locks = (await fixture.Documents.SearchAsync(VisualFlowEngineConstants.NodeTypeLocks, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(500, result.Data?.HttpStatus);
        Assert.Equal("NODE_TYPE_WRITE_FAILED", result.Data?.Body["code"]);
        Assert.Single(locks, record => Convert.ToString(record.Body["status"], CultureInfo.InvariantCulture) == "RELEASED_AFTER_FAILURE");
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == VisualFlowEngineConstants.NodeTypeRegistrationFailedEvent);
    }

    [Fact]
    public async Task CF18_3_DuplicateNodeTypeLockIsRejected()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.RegisterNodeTypeAsync(NodeType("exclusive-node"), "exclusive-a");
        var second = await fixture.Service.RegisterNodeTypeAsync(NodeType("exclusive-node"), "exclusive-b");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(409, second.Data?.HttpStatus);
        Assert.Equal("NODE_TYPE_ALREADY_EXISTS", second.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF18_4_CodeInjectionRequiresHashRejectsRawSourceAndKeepsVersionLock()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var invalid = await fixture.Service.InjectCodeAsync(new Dictionary<string, object?>
        {
            ["nodeId"] = "node-a",
            ["version"] = "v1",
            ["targetPath"] = "src/node.ts",
            ["sourceCode"] = "console.log('secret')"
        }, "inject-invalid");
        var valid = await fixture.Service.InjectCodeAsync(Injection("node-a", "v1"), "inject-a");
        var duplicate = await fixture.Service.InjectCodeAsync(Injection("node-a", "v1"), "inject-b");

        Assert.Equal(400, invalid.Data?.HttpStatus);
        Assert.Equal(201, valid.Data?.HttpStatus);
        Assert.Equal(409, duplicate.Data?.HttpStatus);
        Assert.Equal("INJECTION_CONFLICT", duplicate.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF18_4_CodeInjectionWritesPreAuditResultAuditAndEventWithoutSourceCode()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.InjectCodeAsync(Injection("node-b", "v2"), "inject-b");
        var audits = (await fixture.Documents.SearchAsync(VisualFlowEngineConstants.InjectionAudits, Tenant("acme-corp"))).Data ?? [];
        var results = (await fixture.Documents.SearchAsync(VisualFlowEngineConstants.InjectionResults, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var serialized = JsonSerializer.Serialize(outbox.Select(record => record.Event.Data));

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(2, audits.Count);
        Assert.Single(results);
        Assert.DoesNotContain("sourceCode", serialized, StringComparison.Ordinal);
        Assert.Contains(outbox, record => record.Event.Type == VisualFlowEngineConstants.CodeInjectedEvent);
    }

    [Fact]
    public async Task RunTestProducesTraceAndDashboardReadsCurrentTenantOnly()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "flow_author"))
        {
            await fixture.Service.SaveCanvasAsync(Canvas("flow-acme"), null, "acme-canvas");
            await fixture.Service.RegisterNodeTypeAsync(NodeType("acme-node"), "acme-node");
            await fixture.Service.InjectCodeAsync(Injection("acme-node", "v1"), "acme-inject");
            var run = await fixture.Service.RunTestAsync(new Dictionary<string, object?> { ["flowId"] = "flow-acme" });
            Assert.Equal(200, run.Data?.HttpStatus);
        }

        using (fixture.PushContext("northwind", "flow_author"))
        {
            await fixture.Service.SaveCanvasAsync(Canvas("flow-northwind"), null, "northwind-canvas");
        }

        using (fixture.PushContext("acme-corp", "flow_author"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Single(dashboard.Data?.Canvases ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["draftFlows"]));
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["registeredNodeTypes"]));
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["successfulTestRuns"]));
        }
    }

    [Fact]
    public async Task TenantScopePayloadFieldsAreRejectedAcrossAllMutations()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var canvas = await fixture.Service.SaveCanvasAsync(new Dictionary<string, object?>(Canvas("forbidden-canvas")) { ["tenantId"] = "northwind" }, null, "forbidden-canvas");
        var publish = await fixture.Service.PublishFlowAsync(new Dictionary<string, object?> { ["flowId"] = "forbidden-canvas", ["tenant_id"] = "northwind" }, null, "forbidden-publish");
        var nodeType = await fixture.Service.RegisterNodeTypeAsync(new Dictionary<string, object?>(NodeType("forbidden-node")) { ["masterTenantId"] = "master" }, "forbidden-node");
        var injection = await fixture.Service.InjectCodeAsync(new Dictionary<string, object?>(Injection("forbidden-node", "v1")) { ["ownerTenantId"] = "northwind" }, "forbidden-inject");
        var test = await fixture.Service.RunTestAsync(new Dictionary<string, object?> { ["flowId"] = "forbidden-canvas", ["flowTenantId"] = "northwind" });

        foreach (var result in new[] { canvas, publish, nodeType, injection, test })
        {
            Assert.Equal(400, result.Data?.HttpStatus);
            Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
        }
        Assert.Empty((await fixture.Documents.SearchAsync(VisualFlowEngineConstants.FlowCanvases, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public void ServiceDoesNotImportProviderSdkOrExposeRawTenantArguments()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.VisualFlowEngine.Application", "VisualFlowEngineService.cs"));

        Assert.DoesNotContain("Elastic.Clients", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NEST", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ.Client", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Canvas(
        string flowId,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? edges = null,
        bool simulateSlowWrite = false) =>
        new()
        {
            ["flowId"] = flowId,
            ["title"] = $"Canvas {flowId}",
            ["mode"] = "Design",
            ["simulateSlowWrite"] = simulateSlowWrite,
            ["nodes"] = Nodes(),
            ["edges"] = edges ?? ValidEdges()
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Nodes() =>
    [
        Node("n1", "http-trigger", "trigger", "T617", "json", "draft", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Node("n2", "publish-gate", "gate", "T618", "draft", "approval", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Node("n3", "node-registry", "action", "T619", "approval", "registry", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Node("n4", "code-injector", "output", "T620", "registry", "json", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" })
    ];

    private static IReadOnlyDictionary<string, object?> Node(
        string id,
        string nodeTypeId,
        string category,
        string taskType,
        string inputType,
        string outputType,
        IReadOnlyDictionary<string, object?> config) =>
        new Dictionary<string, object?>
        {
            ["id"] = id,
            ["nodeTypeId"] = nodeTypeId,
            ["category"] = category,
            ["taskTypeId"] = taskType,
            ["inputType"] = inputType,
            ["outputType"] = outputType,
            ["config"] = config
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ValidEdges() =>
    [
        Edge("n1", "n2", "draft", "draft"),
        Edge("n2", "n3", "approval", "approval"),
        Edge("n3", "n4", "registry", "registry")
    ];

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> CycleEdges() =>
    [
        Edge("n1", "n2", "draft", "draft"),
        Edge("n2", "n3", "approval", "approval"),
        Edge("n3", "n1", "registry", "registry")
    ];

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> MismatchedEdges() =>
    [
        Edge("n1", "n2", "draft", "approval"),
        Edge("n2", "n3", "approval", "approval"),
        Edge("n3", "n4", "registry", "registry")
    ];

    private static IReadOnlyDictionary<string, object?> Edge(string from, string to, string sourceOutputType, string targetInputType) =>
        new Dictionary<string, object?>
        {
            ["from"] = from,
            ["to"] = to,
            ["sourceOutputType"] = sourceOutputType,
            ["targetInputType"] = targetInputType
        };

    private static Dictionary<string, object?> NodeType(string nodeTypeId) =>
        new()
        {
            ["nodeTypeId"] = nodeTypeId,
            ["category"] = "action",
            ["schema"] = new Dictionary<string, object?> { ["type"] = "object" },
            ["capabilities"] = new[] { "write", "test" }
        };

    private static Dictionary<string, object?> Injection(string nodeId, string version) =>
        new()
        {
            ["nodeId"] = nodeId,
            ["version"] = version,
            ["codeHash"] = $"hash-{nodeId}-{version}",
            ["targetPath"] = $"src/{nodeId}.ts"
        };

    private static Dictionary<string, object?> Tenant(string tenant) => new() { ["tenant_id"] = tenant };

    private static IReadOnlyDictionary<string, object?> Tenant(IReadOnlyDictionary<string, object?> body, string tenant)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = tenant
        };
        return copy;
    }

    private static int ToInt(object? value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            VisualFlowEngineService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public VisualFlowEngineService Service { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var freedom = new InMemoryFreedomConfigService();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var service = new VisualFlowEngineService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<VisualFlowEngineService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenant, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenant}", "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantContextAccessor.Push(new TenantContext(
                tenant,
                role == "anonymous" ? null : "test-user",
                new HashSet<string>([role], StringComparer.Ordinal),
                $"corr-{tenant}",
                "test",
                DateTimeOffset.UtcNow));
            return new CompositeDisposable(correlation, tenantScope);
        }
    }

    private sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
