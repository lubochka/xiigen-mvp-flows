using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SchemaRegistryDag.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SchemaRegistryDag.Tests;

public sealed class SchemaRegistryDagServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = SchemaRegistryDagBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(SchemaRegistryDagConstants.RuleIds));
        Assert.Equal(14, SchemaRegistryDagBfaRuleRegistry.Rules.Count);
        Assert.All(SchemaRegistryDagBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(SchemaRegistryDagConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF11_1_DagCycleDetectorUsesThreeStateDfsAndAllowsDiamond()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        Assert.Equal(201, (await fixture.Service.RegisterSchemaAsync(Schema("account", "1.0.0"), null, "account-v1")).Data?.HttpStatus);
        Assert.Equal(201, (await fixture.Service.RegisterSchemaAsync(Schema("invoice", "1.0.0", dependencies: ["account"]), null, "invoice-v1")).Data?.HttpStatus);
        Assert.Equal(201, (await fixture.Service.RegisterSchemaAsync(Schema("receipt", "1.0.0", dependencies: ["account"]), null, "receipt-v1")).Data?.HttpStatus);
        var diamond = await fixture.Service.RegisterSchemaAsync(Schema("ledger", "1.0.0", dependencies: ["invoice", "receipt"]), null, "ledger-v1");

        Assert.Equal(201, diamond.Data?.HttpStatus);

        var cycle = await fixture.Service.RegisterSchemaAsync(Schema("account", "2.0.0", dependencies: ["ledger"]), null, "account-v2-cycle");

        Assert.Equal(409, cycle.Data?.HttpStatus);
        Assert.Equal("DAG_CYCLE_DETECTED", cycle.Data?.Body["code"]);
        Assert.Contains("GRAY", (IEnumerable<string>)cycle.Data!.Body["states"]!);
    }

    [Fact]
    public async Task CF11_2_PublishUsesOptimisticVersionPinAndRebuildsTopology()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RegisterSchemaAsync(Schema("profile", "1.0.0"), null, "profile-v1");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", result.Data?.Body["status"]);
        Assert.NotNull(result.Data?.Body["versionPin"]);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.SchemaHistory, Tenant("acme-corp"))).Data ?? []);
        Assert.NotNull((await fixture.Documents.GetAsync(SchemaRegistryDagConstants.DagTopology, "active")).Data);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == SchemaRegistryDagConstants.SchemaPublishedEvent);
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == SchemaRegistryDagConstants.DagRebuiltEvent);
    }

    [Fact]
    public async Task CF11_3_BreakingChangeRoutesToApprovalBeforePublication()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        await fixture.Service.RegisterSchemaAsync(Schema("order", "1.0.0"), null, "order-v1");
        var pending = await fixture.Service.RegisterSchemaAsync(Schema("order", "2.0.0", changeType: "BREAKING", removedFields: ["legacyStatus"]), null, "order-v2-breaking");

        Assert.Equal(202, pending.Data?.HttpStatus);
        Assert.Equal("PENDING_APPROVAL", pending.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.ApprovalQueue, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["schema_type"] = "order", ["version"] = "2.0.0" })).Data ?? []);

        var approval = await fixture.Service.DecideApprovalAsync(new Dictionary<string, object?>
        {
            ["approvalId"] = pending.Data!.Body["approvalId"],
            ["decision"] = "APPROVE",
            ["actorId"] = "platform-admin-1"
        }, "approve-order-v2");

        Assert.Equal(200, approval.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", approval.Data?.Body["status"]);
        Assert.Equal(2, (await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data?.Count);
    }

    [Fact]
    public async Task CF11_4_SearchAndRenderDoNotLeakAcrossTenants()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.RegisterSchemaAsync(Schema("acme-private", "1.0.0"), null, "acme-private-v1");
        }

        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            var search = await fixture.Service.SearchSchemasAsync(new Dictionary<string, object?>());
            var render = await fixture.Service.RenderDagAsync();

            Assert.Empty(search.Data?.Schemas ?? []);
            Assert.Equal(0, render.Data?.NodeCount);
        }
    }

    [Fact]
    public async Task CF919_ConcurrentRegistration_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var first = fixture.Service.RegisterSchemaAsync(Schema("race", "1.0.0", simulateSlowWrite: true), "1", "race-a");
        var second = fixture.Service.RegisterSchemaAsync(Schema("race", "1.0.0", simulateSlowWrite: true), "1", "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF920_IdempotencyReplayReturnsCachedSchemaAndNoDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.RegisterSchemaAsync(Schema("retry", "1.0.0"), null, "same-key");
        var second = await fixture.Service.RegisterSchemaAsync(Schema("retry", "1.0.0"), null, "same-key");
        var third = await fixture.Service.RegisterSchemaAsync(Schema("retry", "1.0.0"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(200, third.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF921_InvalidRegistrationReturnsFieldDetailsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Schema("", "0");

        var result = await fixture.Service.RegisterSchemaAsync(request, null, "invalid-key");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.SchemaHistory, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF922_FabricTimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Schema("timeout", "1.0.0");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.RegisterSchemaAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SchemaRegistryDagConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF923_ApprovalConcurrency_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterSchemaAsync(Schema("approval-race", "1.0.0"), null, "approval-base");
        var pending = await fixture.Service.RegisterSchemaAsync(Schema("approval-race", "2.0.0", changeType: "BREAKING", removedFields: ["old"]), null, "approval-pending");
        var approvalId = Convert.ToString(pending.Data?.Body["approvalId"])!;

        var first = fixture.Service.DecideApprovalAsync(new Dictionary<string, object?> { ["approvalId"] = approvalId, ["decision"] = "APPROVE", ["simulateSlowWrite"] = true }, "approve-a");
        var second = fixture.Service.DecideApprovalAsync(new Dictionary<string, object?> { ["approvalId"] = approvalId, ["decision"] = "APPROVE", ["simulateSlowWrite"] = true }, "approve-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 200);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF924_ApprovalReplayReturnsCachedDecisionWithoutDoublePublish()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterSchemaAsync(Schema("approval-replay", "1.0.0"), null, "approval-replay-base");
        var pending = await fixture.Service.RegisterSchemaAsync(Schema("approval-replay", "2.0.0", changeType: "BREAKING", removedFields: ["old"]), null, "approval-replay-pending");
        var approvalId = Convert.ToString(pending.Data?.Body["approvalId"])!;

        var first = await fixture.Service.DecideApprovalAsync(new Dictionary<string, object?> { ["approvalId"] = approvalId, ["decision"] = "APPROVE" }, "approval-same-key");
        var second = await fixture.Service.DecideApprovalAsync(new Dictionary<string, object?> { ["approvalId"] = approvalId, ["decision"] = "APPROVE" }, "approval-same-key");

        Assert.Equal(200, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["status"], second.Data?.Body["status"]);
        Assert.Equal(2, (await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data?.Count);
    }

    [Fact]
    public async Task CF925_ValidationReturnsAllErrorsAndNoSchemaSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.RegisterSchemaAsync(Schema("validation", "1.0.0", required: ["id", "name"]), null, "validation-schema");

        var result = await fixture.Service.ValidateDocumentAsync(new Dictionary<string, object?>
        {
            ["schemaType"] = "validation",
            ["document"] = new Dictionary<string, object?> { ["id"] = "" }
        });

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.Equal(2, details.Count());
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.ValidationFailures, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.Schemas, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF926_MigrationTimeoutRecordsRunBeforeRetryEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = await fixture.Service.RunMigrationAsync(new Dictionary<string, object?>
        {
            ["schemaType"] = "migration",
            ["simulateFabricTimeout"] = true
        }, "migration-timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.StateStored);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Single((await fixture.Documents.SearchAsync(SchemaRegistryDagConstants.MigrationRuns, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == SchemaRegistryDagConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF927_DeprecationConcurrency_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        await fixture.Service.RegisterSchemaAsync(Schema("deprecated-race", "1.0.0"), null, "deprecated-base");

        var first = fixture.Service.DeprecateSchemaAsync(new Dictionary<string, object?> { ["schemaType"] = "deprecated-race", ["version"] = "1.0.0", ["simulateSlowWrite"] = true }, null, "deprecate-a");
        var second = fixture.Service.DeprecateSchemaAsync(new Dictionary<string, object?> { ["schemaType"] = "deprecated-race", ["version"] = "1.0.0", ["simulateSlowWrite"] = true }, null, "deprecate-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 200);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF928_RenderDagUsesCacheForRepeatedRequests()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        await fixture.Service.RegisterSchemaAsync(Schema("node-a", "1.0.0"), null, "node-a");
        await fixture.Service.RegisterSchemaAsync(Schema("node-b", "1.0.0", dependencies: ["node-a"]), null, "node-b");

        var first = await fixture.Service.RenderDagAsync("mermaid");
        var second = await fixture.Service.RenderDagAsync("mermaid");

        Assert.False(first.Data?.FromCache);
        Assert.True(second.Data?.FromCache);
        Assert.Contains("node-a", second.Data?.Content);
        Assert.Equal(2, second.Data?.NodeCount);
        Assert.Equal(1, second.Data?.EdgeCount);
    }

    private static Dictionary<string, object?> Schema(
        string schemaType,
        string version,
        string changeType = "ADDITIVE",
        string[]? dependencies = null,
        string[]? required = null,
        string[]? removedFields = null,
        bool simulateSlowWrite = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["schemaType"] = schemaType,
            ["version"] = version,
            ["changeType"] = changeType,
            ["dependencies"] = dependencies ?? [],
            ["jsonSchema"] = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["required"] = required ?? ["id"],
                ["properties"] = new Dictionary<string, object?>
                {
                    ["id"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["name"] = new Dictionary<string, object?> { ["type"] = "string" }
                }
            },
            ["actorId"] = "operator-1"
        };

        if (removedFields is not null)
        {
            request["removedFields"] = removedFields;
        }
        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        return request;
    }

    private static Dictionary<string, object?> Tenant(string tenantId) => new() { ["tenant_id"] = tenantId };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            SchemaRegistryDagService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public SchemaRegistryDagService Service { get; }
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
            var service = new SchemaRegistryDagService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<SchemaRegistryDagService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = $"corr-{Guid.NewGuid():N}";
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" || role == "public_marketplace_visitor" ? null : "user-1",
                new HashSet<string> { role },
                correlation,
                "test",
                DateTimeOffset.UtcNow));
            var correlationScope = CorrelationContextAccessor.Push(new CorrelationContext(correlation, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(tenant, correlationScope);
        }

        private sealed class CompositeScope : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;

            public CompositeScope(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _second.Dispose();
                _first.Dispose();
            }
        }
    }
}
