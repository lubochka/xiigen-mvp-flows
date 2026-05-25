using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.ClientPush.Application;
using Xiigen.Flow.ClientPush.Infrastructure;
using Xiigen.Kernel;

namespace Xiigen.Flow.ClientPush.Tests;

public sealed class ClientPushServiceTests
{
    [Fact]
    public void P11_AllFlow40BfaRulesAreRegistered()
    {
        var ids = ClientPushBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(ClientPushConstants.RequiredBfaRules));
        Assert.Equal(6, ClientPushBfaRuleRegistry.Rules.Count);
        Assert.Contains(ClientPushBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-797" && rule.Type == "CROSS_DOMAIN_READ");
        Assert.Contains(ClientPushBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-798" && rule.Type == "TENANT_ISOLATION");
        Assert.Contains(ClientPushBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1065" && rule.Type == "DNA8_ORDERING");
        Assert.All(ClientPushBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(ClientPushConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_ClientPushTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(3, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresExistingAndP10ClientPushRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new ClientPushRuleSnapshot("CF-1065", "", "F1518", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(6, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RegisterConnectionRequiresAuthStateRejectsTenantPayloadAndExpiredCorrelation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var scoped = RegisterRequest();
        scoped["connectionTenantId"] = "globex";
        var expired = RegisterRequest();
        expired["expiredCorrelation"] = true;

        var emptyRejected = await fixture.Service.RegisterConnectionAsync(new Dictionary<string, object?>(), "connection:corr-001", null);
        var scopedRejected = await fixture.Service.RegisterConnectionAsync(scoped, "connection:corr-001", null);
        var expiredRejected = await fixture.Service.RegisterConnectionAsync(expired, "connection:corr-001", null);

        Assert.Equal("VALIDATION_FAILED", emptyRejected.Data?.Body["code"]);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedRejected.Data?.Body["code"]);
        Assert.Equal(404, expiredRejected.Data?.HttpStatus);
        Assert.Equal("EXPIRED_CORRELATION_ID", expiredRejected.Data?.Body["code"]);
    }

    [Fact]
    public async Task RegisterConnectionIsIdempotentVersionAwareAndTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var first = await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", null);
        var replay = await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", null);
        var stale = await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", "stale-version");
        var connections = (await fixture.Documents.SearchAsync(ClientPushConstants.ConnectionsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Equal(409, stale.Data?.HttpStatus);
        Assert.Single(connections);
        Assert.Single(fixture.Pool.GetActiveConnections().Data ?? []);
    }

    [Fact]
    public async Task BridgeEventStoresOutboxBeforeTenantScopedPoolPush()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", null);

        var result = await fixture.Service.BridgeEventAsync(BridgeRequest(), "bridge:corr-001:event-001", null);
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var deliveries = (await fixture.Documents.SearchAsync(ClientPushConstants.DeliveryAuditCollection, Scope("acme-corp"))).Data ?? [];
        var pushed = fixture.Pool.EventsForCurrentTenant("corr-001");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(true, result.Data?.Body["delivered"]);
        Assert.Equal(2, outbox.Count);
        Assert.Single(deliveries);
        Assert.Single(pushed);
        Assert.Equal(outbox.Last().Event.Id, pushed[0].Id);
    }

    [Fact]
    public async Task BridgeEventDiscardsMissingConnectionWithoutRetryAndBlocksUserFacingEmitDomains()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var prohibited = BridgeRequest();
        prohibited["targetDomain"] = "xiigen.commerce.orders";

        var missing = await fixture.Service.BridgeEventAsync(BridgeRequest(), "bridge:corr-001:event-001", null);
        var blocked = await fixture.Service.BridgeEventAsync(prohibited, "bridge:corr-001:event-001", null);
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(200, missing.Data?.HttpStatus);
        Assert.Equal(false, missing.Data?.Body["delivered"]);
        Assert.Equal("connection_not_found_no_retry", missing.Data?.Body["reason"]);
        Assert.Equal(400, blocked.Data?.HttpStatus);
        Assert.Equal("PROHIBITED_EMIT_DOMAIN", blocked.Data?.Body["code"]);
        Assert.Empty(outbox);
    }

    [Fact]
    public async Task BridgeEventBlocksCrossTenantCorrelationDelivery()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", null);
        }

        using var _ = fixture.PushContext("globex", "platform_admin");
        var blocked = await fixture.Service.BridgeEventAsync(BridgeRequest(), "bridge:corr-001:event-001", null);

        Assert.Equal(403, blocked.Data?.HttpStatus);
        Assert.Equal("CROSS_TENANT_DELIVERY_ATTEMPT", blocked.Data?.Body["code"]);
    }

    [Fact]
    public async Task KeepaliveReadsConfigCleansDroppedConnectionsBeforeEmitAndIsIdempotent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterConnectionAsync(RegisterRequest("corr-active"), "connection:corr-active", null);
        await fixture.Service.RegisterConnectionAsync(RegisterRequest("corr-dropped"), "connection:corr-dropped", null);
        fixture.Pool.AgeConnectionForCurrentTenant("corr-dropped", TimeSpan.FromMinutes(10));

        var first = await fixture.Service.RunKeepaliveAsync(new Dictionary<string, object?> { ["runId"] = "run-001", ["keepaliveIntervalMs"] = 30000, ["droppedThresholdMs"] = 1000 }, "keepalive:run-001");
        var replay = await fixture.Service.RunKeepaliveAsync(new Dictionary<string, object?> { ["runId"] = "run-001", ["keepaliveIntervalMs"] = 30000, ["droppedThresholdMs"] = 1000 }, "keepalive:run-001");
        var active = fixture.Pool.GetActiveConnections().Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.Equal(1, Convert.ToInt32(first.Data?.Body["pinged"], CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(first.Data?.Body["cleaned"], CultureInfo.InvariantCulture));
        Assert.Single(active);
        Assert.Equal("corr-active", active[0].CorrelationId);
    }

    [Fact]
    public async Task TimeoutRetainsOutboxWithoutPublishingToPool()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var timeout = RegisterRequest();
        timeout["simulateTimeout"] = true;

        var result = await fixture.Service.RegisterConnectionAsync(timeout, "connection:corr-timeout", null);
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var active = fixture.Pool.GetActiveConnections().Data ?? [];

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", result.Data?.Body["code"]);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Single(outbox);
        Assert.Empty(active);
    }

    [Fact]
    public async Task DashboardIsTenantScopedAndInspectorReadable()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RegisterConnectionAsync(RegisterRequest(), "connection:corr-001", null);
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Connections ?? []);
        Assert.Empty(dashboard.Data?.Deliveries ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-40-client-push", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using OpenTelemetry", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<ClientPushService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<ClientPushOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
        Assert.Contains("IClientPushConnectionPool", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> RegisterRequest(string correlationId = "corr-001") => new()
    {
        ["correlationId"] = correlationId,
        ["activeFlowState"] = new Dictionary<string, object?> { ["flow"] = "FLOW-40", ["state"] = "viewing-dashboard" }
    };

    private static Dictionary<string, object?> BridgeRequest() => new()
    {
        ["correlationId"] = "corr-001",
        ["eventId"] = "event-001",
        ["eventType"] = "xiigen.client-push.state.updated",
        ["data"] = new Dictionary<string, object?> { ["message"] = "state changed" }
    };

    private static ClientPushRuleSnapshot[] ValidRules() =>
        ClientPushConstants.RequiredBfaRules.Select(rule => new ClientPushRuleSnapshot(rule, "BUILD_FAILURE", "F1518", "client-push-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        ClientPushConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Client Push step {task}",
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
        ClientPushService Service,
        InMemoryDocumentStore Documents,
        InMemoryIdempotencyStore Idempotency,
        InMemoryOutboxStore Outbox,
        InMemoryClientPushConnectionPool Pool,
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
            var pool = new InMemoryClientPushConnectionPool(tenant);
            var service = new ClientPushService(documents, idempotency, outbox, pool, tenant, correlation, NullLogger<ClientPushService>.Instance);
            return new FlowFixture(service, documents, idempotency, outbox, pool, tenant, correlation);
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
