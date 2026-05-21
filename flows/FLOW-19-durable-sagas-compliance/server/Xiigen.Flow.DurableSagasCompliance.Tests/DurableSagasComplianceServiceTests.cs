using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DurableSagasCompliance.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.DurableSagasCompliance.Tests;

public sealed class DurableSagasComplianceServiceTests
{
    [Fact]
    public void P11_Rules_AllDurableSagasComplianceRules_AreRegistered()
    {
        var ids = DurableSagasComplianceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(DurableSagasComplianceConstants.RuleIds));
        Assert.Equal(8, DurableSagasComplianceBfaRuleRegistry.Rules.Count);
        Assert.All(DurableSagasComplianceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(DurableSagasComplianceConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF19_1_StartSagaPersistsStepLockAndStateBeforeEvents()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");

        var result = await fixture.Service.StartSagaAsync(Saga("saga-a"), null, "start-a");
        var states = (await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaStates, Tenant("acme-corp"))).Data ?? [];
        var locks = (await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaStepLocks, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaAudits, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("RUNNING", result.Data?.Body["status"]);
        Assert.Single(states);
        Assert.Single(locks, record => Convert.ToString(record.Body["status"], CultureInfo.InvariantCulture) == "HELD");
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "SAGA_STARTED");
        Assert.Contains(outbox, record => record.Event.Type == DurableSagasComplianceConstants.SagaStartedEvent);
        Assert.Contains(outbox, record => record.Event.Type == DurableSagasComplianceConstants.SagaStepExecutedEvent);
    }

    [Fact]
    public async Task CF19_1_BolaMismatchRejectsBeforeStateMutation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");
        await fixture.Documents.UpsertAsync(DurableSagasComplianceConstants.SagaStates, "saga-cross", Tenant(new Dictionary<string, object?>
        {
            ["saga_id"] = "saga-cross",
            ["status"] = "RUNNING",
            ["steps"] = Steps()
        }, "northwind"), null);

        var result = await fixture.Service.StartSagaAsync(Saga("saga-cross"), null, "cross");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("BOLA_VIOLATION", result.Data?.Body["code"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.SagaAlreadyRunningEvent);
    }

    [Fact]
    public async Task CF961_ConcurrentSagaWritesAllowOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");

        var first = fixture.Service.StartSagaAsync(Saga("saga-race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.StartSagaAsync(Saga("saga-race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF962_IdempotencyReplaysSagaWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");

        var first = await fixture.Service.StartSagaAsync(Saga("saga-replay"), null, "same-key");
        var second = await fixture.Service.StartSagaAsync(Saga("saga-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Equal(2, ((await fixture.Outbox.ListAsync()).Data ?? []).Count(record => record.Event.Type is DurableSagasComplianceConstants.SagaStartedEvent or DurableSagasComplianceConstants.SagaStepExecutedEvent));
    }

    [Fact]
    public async Task CF963_InvalidSagaReturnsFieldErrorsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");

        var result = await fixture.Service.StartSagaAsync(new Dictionary<string, object?> { ["sagaId"] = "invalid", ["steps"] = Array.Empty<object>() }, null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaStates, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF964_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");
        var request = Saga("saga-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.StartSagaAsync(request, null, "timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.SagaTimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaStates, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF19_2_CompensationRunsSerialLifoAndEmitsCompletion()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");
        await fixture.Service.StartSagaAsync(Saga("saga-comp"), null, "start-comp");

        var result = await fixture.Service.CompensateSagaAsync(new Dictionary<string, object?> { ["sagaId"] = "saga-comp" }, "compensate");
        var executed = (object[])result.Data!.Body["executed"]!;
        var first = (IReadOnlyDictionary<string, object?>)executed[0];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("COMPENSATION_COMPLETED", result.Data?.Body["status"]);
        Assert.Equal("retention-enforcer", first["stepId"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.CompensationCompletedEvent);
    }

    [Fact]
    public async Task CF19_2_CompensationStopsOnFirstFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_ops");
        await fixture.Service.StartSagaAsync(Saga("saga-fail-comp"), null, "start-fail-comp");

        var result = await fixture.Service.CompensateSagaAsync(new Dictionary<string, object?>
        {
            ["sagaId"] = "saga-fail-comp",
            ["simulateCompensationFailureAt"] = "compliance-writer"
        }, "compensate-failure");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("COMPENSATION_FAILED", result.Data?.Body["code"]);
        Assert.Single((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.CompensationRecords, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.CompensationFailedEvent);
    }

    [Fact]
    public async Task CF19_3_ComplianceRecordIsAppendOnlyPlatformOnlyAndRetainedAtWriteTime()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_ops");
        await fixture.Freedom.SetAsync("flow19_compliance_retention_days", 30, "number");

        var result = await fixture.Service.WriteComplianceRecordAsync(Compliance("saga-a", "SagaCompleted"), "compliance-a");
        var records = (await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.ComplianceRecords, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PLATFORM_ONLY", result.Data?.Body["knowledgeScope"]);
        Assert.NotNull(result.Data?.Body["auditHash"]);
        Assert.Contains("retentionExpiresAt", result.Data!.Body.Keys);
        Assert.Single(records);
        Assert.Equal("PLATFORM_ONLY", records[0].Body["knowledge_scope"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.ComplianceRecordWrittenEvent);
    }

    [Fact]
    public async Task CF19_4_RetentionSkipsLegalHoldAndArchivesBeforePurge()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_ops");
        var held = await fixture.Service.WriteComplianceRecordAsync(Compliance("saga-hold", "HoldRequested", legalHold: true), "hold-write");
        var heldId = Convert.ToString(held.Data!.Body["auditId"], CultureInfo.InvariantCulture)!;
        var heldResult = await fixture.Service.EnforceRetentionAsync(new Dictionary<string, object?> { ["auditId"] = heldId, ["forceExpiredForTest"] = true }, "hold-retention");

        var purge = await fixture.Service.WriteComplianceRecordAsync(Compliance("saga-purge", "SagaCompleted"), "purge-write");
        var purgeId = Convert.ToString(purge.Data!.Body["auditId"], CultureInfo.InvariantCulture)!;
        var purgeResult = await fixture.Service.EnforceRetentionAsync(new Dictionary<string, object?> { ["auditId"] = purgeId, ["forceExpiredForTest"] = true }, "purge-retention");

        Assert.Equal(200, heldResult.Data?.HttpStatus);
        Assert.Equal("LEGAL_HOLD_ACTIVE", heldResult.Data?.Body["status"]);
        Assert.Equal(201, purgeResult.Data?.HttpStatus);
        Assert.Equal("PURGE_COMPLETED", purgeResult.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.RetentionArchives, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.RetentionHoldActiveEvent);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.PurgeCompletedEvent);
    }

    [Fact]
    public async Task ExportLargeAuditTrailQueuesBackgroundJob()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "compliance_officer");

        var result = await fixture.Service.QueueComplianceExportAsync(new Dictionary<string, object?>
        {
            ["recordEstimate"] = 150000,
            ["format"] = "pdf"
        }, "export-large");

        Assert.Equal(202, result.Data?.HttpStatus);
        Assert.Equal("QUEUED", result.Data?.Body["status"]);
        Assert.Equal("EMAIL_LINK", result.Data?.Body["delivery"]);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DurableSagasComplianceConstants.ExportQueuedEvent);
    }

    [Fact]
    public async Task DashboardReadsCurrentTenantOnly()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_ops"))
        {
            await fixture.Service.StartSagaAsync(Saga("saga-acme"), null, "acme-start");
        }
        using (fixture.PushContext("northwind", "tenant_ops"))
        {
            await fixture.Service.StartSagaAsync(Saga("saga-northwind"), null, "northwind-start");
        }
        using (fixture.PushContext("acme-corp", "tenant_ops"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Single(dashboard.Data?.SagaRuns ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["runningSagas"]));
        }
    }

    [Fact]
    public async Task TenantScopePayloadFieldsAreRejectedAcrossMutations()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_ops");

        var start = await fixture.Service.StartSagaAsync(new Dictionary<string, object?>(Saga("forbidden-saga")) { ["tenantId"] = "northwind" }, null, "forbidden-start");
        var comp = await fixture.Service.CompensateSagaAsync(new Dictionary<string, object?> { ["sagaId"] = "forbidden-saga", ["tenant_id"] = "northwind" }, "forbidden-comp");
        var audit = await fixture.Service.WriteComplianceRecordAsync(new Dictionary<string, object?>(Compliance("forbidden-saga", "X")) { ["masterTenantId"] = "master" }, "forbidden-audit");
        var retention = await fixture.Service.EnforceRetentionAsync(new Dictionary<string, object?> { ["auditId"] = "x", ["ownerTenantId"] = "northwind" }, "forbidden-retention");

        foreach (var result in new[] { start, comp, audit, retention })
        {
            Assert.Equal(400, result.Data?.HttpStatus);
            Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
        }
        Assert.Empty((await fixture.Documents.SearchAsync(DurableSagasComplianceConstants.SagaStates, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public void ServiceDoesNotImportProviderSdkOrExposeRawTenantArguments()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.DurableSagasCompliance.Application", "DurableSagasComplianceService.cs"));

        Assert.DoesNotContain("Elastic.Clients", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NEST", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ.Client", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Saga(string sagaId, bool simulateSlowWrite = false) =>
        new()
        {
            ["sagaId"] = sagaId,
            ["sagaType"] = "payout-processing",
            ["simulateSlowWrite"] = simulateSlowWrite,
            ["steps"] = Steps()
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps() =>
    [
        Step("saga-orchestrator", "T621", "Start durable run", "persist before dispatch", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Step("compensation-engine", "T622", "Register compensation", "serial LIFO rollback", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Step("compliance-writer", "T623", "Write audit", "immutable compliance record", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }),
        Step("retention-enforcer", "T624", "Apply retention", "dual gate archive", new Dictionary<string, object?> { ["returns"] = "DataProcessResult" })
    ];

    private static IReadOnlyDictionary<string, object?> Step(
        string id,
        string taskTypeId,
        string name,
        string responsibility,
        IReadOnlyDictionary<string, object?> config) =>
        new Dictionary<string, object?>
        {
            ["id"] = id,
            ["taskTypeId"] = taskTypeId,
            ["name"] = name,
            ["responsibility"] = responsibility,
            ["config"] = config,
            ["compensation"] = new Dictionary<string, object?> { ["strategy"] = taskTypeId == "T621" ? "pre-stored" : "lifo" }
        };

    private static Dictionary<string, object?> Compliance(string sagaId, string eventType, bool legalHold = false) =>
        new()
        {
            ["sagaId"] = sagaId,
            ["eventType"] = eventType,
            ["actor"] = "platform-ops",
            ["legalHoldActive"] = legalHold,
            ["payload"] = new Dictionary<string, object?> { ["status"] = eventType }
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
            DurableSagasComplianceService service,
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
        public DurableSagasComplianceService Service { get; }
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
            var service = new DurableSagasComplianceService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<DurableSagasComplianceService>.Instance);

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
