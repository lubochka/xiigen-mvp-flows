using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DataWarehouseAnalytics.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.DataWarehouseAnalytics.Tests;

public sealed class DataWarehouseAnalyticsServiceTests
{
    [Fact]
    public void P11_Rules_NewFunctionalCoverageRules_AreRegistered()
    {
        var ids = DataWarehouseAnalyticsBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(DataWarehouseAnalyticsConstants.RuleIds));
        Assert.Equal(5, DataWarehouseAnalyticsBfaRuleRegistry.Rules.Count);
        Assert.All(DataWarehouseAnalyticsBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(DataWarehouseAnalyticsConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task PlanPublicationCoversEveryTaskTypeAndStoresAuditBeforeCloudEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishPlanAsync(FullPlan("core"), null, "core-plan");
        var plans = (await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.WarehousePlans, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.AnalyticsAudits, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(20, result.Data?.Body["taskTypeCoverage"]);
        Assert.Equal(16, result.Data?.Body["eventSchemaCount"]);
        Assert.Single(plans);
        Assert.Single(audits);
        Assert.Contains(outbox, record => record.Event.Type == DataWarehouseAnalyticsConstants.PlanPublishedEvent);
    }

    [Fact]
    public async Task CF935_ConcurrentPlanPublicationAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = fixture.Service.PublishPlanAsync(FullPlan("race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.PublishPlanAsync(FullPlan("race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF936_PlanPublicationIdempotencyReplaysCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.PublishPlanAsync(FullPlan("retry"), null, "same-key");
        var second = await fixture.Service.PublishPlanAsync(FullPlan("retry"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["planId"], second.Data?.Body["planId"]);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.WarehousePlans, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.AnalyticsAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DataWarehouseAnalyticsConstants.PlanPublishedEvent);
    }

    [Fact]
    public async Task CF937_InvalidPlanReturnsFieldErrorsAndNoPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishPlanAsync(new Dictionary<string, object?>(), null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.Contains(details, detail => Convert.ToString(detail["field"]) == "planId");
        Assert.Empty((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.WarehousePlans, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.AnalyticsAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF938_PlanTimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var request = FullPlan("timeout");
        request["simulateFabricTimeout"] = true;
        var result = await fixture.Service.PublishPlanAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Equal(5000, result.Data?.Body["retryAfterMs"]);
        Assert.Empty((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.WarehousePlans, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DataWarehouseAnalyticsConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF939_StaleVersionWriteReturnsCurrentVersion()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.PublishPlanAsync(FullPlan("stale"), null, "stale-a");
        var second = await fixture.Service.PublishPlanAsync(FullPlan("stale"), "0", "stale-b");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.True(second.IsFailure);
        Assert.Equal("CONCURRENT_UPDATE", second.Error?.Code);
        Assert.Equal(first.Data?.Body["version"], second.Error?.Details?["currentVersion"]);
    }

    [Fact]
    public async Task PlanValidationRejectsProviderSdkImportsEntityControllersAndMissingDataProcessResult()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = FullPlan("bad-step");
        var firstStep = ((object[])request["steps"]!)[0] as Dictionary<string, object?>;
        firstStep!["providerImports"] = new[] { "Elastic.Clients.Elasticsearch" };
        firstStep["controllerMode"] = "entity-specific";
        firstStep["returnsDataProcessResult"] = false;

        var result = await fixture.Service.PublishPlanAsync(request, null, "bad-step");
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Contains(details, detail => Convert.ToString(detail["field"])!.Contains("providerImports", StringComparison.Ordinal));
        Assert.Contains(details, detail => Convert.ToString(detail["field"])!.Contains("controllerMode", StringComparison.Ordinal));
        Assert.Contains(details, detail => Convert.ToString(detail["field"])!.Contains("returnsDataProcessResult", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IngestEventStoresRawEventMetricFactAuditAndCloudEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.Service.PublishPlanAsync(FullPlan("ingest"), null, "ingest-plan");
        var result = await fixture.Service.IngestEventAsync(Event("FLOW-03", "event.promoted", "event-1", 7), "event-key");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.RawEvents, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.MetricFacts, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DataWarehouseAnalyticsConstants.EventIngestedEvent);
    }

    [Fact]
    public async Task IngestEventIdempotencyDoesNotDuplicateFacts()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.IngestEventAsync(Event("FLOW-04", "attendance.checked-in", "checkin-1", 3), "same-event");
        var second = await fixture.Service.IngestEventAsync(Event("FLOW-04", "attendance.checked-in", "checkin-1", 3), "same-event");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.RawEvents, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.MetricFacts, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task ExecuteQueryUsesFreedomLimitAndReportsMetricTotal()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Freedom.SetAsync("data_warehouse_query_result_limit", "25", "number");
        await fixture.Service.IngestEventAsync(Event("FLOW-05", "points.awarded", "points-1", 10), "points-a");
        await fixture.Service.IngestEventAsync(Event("FLOW-05", "points.awarded", "points-2", 12), "points-b");

        var result = await fixture.Service.ExecuteQueryAsync(new Dictionary<string, object?> { ["metricName"] = "event_count" });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal(2, result.Data?.Body["rowCount"]);
        Assert.Equal(22m, result.Data?.Body["total"]);
        Assert.Equal(25, result.Data?.Body["resultLimit"]);
        Assert.Single((await fixture.Documents.SearchAsync(DataWarehouseAnalyticsConstants.QueryAudits, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task DashboardAndQualityCheckDoNotLeakAcrossTenants()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.PublishPlanAsync(FullPlan("tenant-a"), null, "tenant-a-plan");
            await fixture.Service.IngestEventAsync(Event("FLOW-07", "feed.updated", "feed-1", 4), "tenant-a-event");
            await fixture.Service.RunQualityCheckAsync();
        }

        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Empty(dashboard.Data?.Plans ?? []);
            Assert.Empty(dashboard.Data?.RawEvents ?? []);
            Assert.Equal(0, Convert.ToInt32(dashboard.Data?.Kpis["rawEvents"], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public async Task RequestBodyTenantScopeIsRejectedBecauseTenantContextIsAuthoritative()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = FullPlan("tenant-body");
        request["tenantId"] = "northwind";

        var result = await fixture.Service.PublishPlanAsync(request, null, "tenant-body");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
    }

    private static Dictionary<string, object?> FullPlan(string planId, bool simulateSlowWrite = false) =>
        new()
        {
            ["planId"] = planId,
            ["warehouseZone"] = "analytics-core",
            ["steps"] = DataWarehouseAnalyticsConstants.RequiredTaskTypes
                .Select(taskType => (object)new Dictionary<string, object?>
                {
                    ["taskType"] = taskType,
                    ["responsibility"] = $"{taskType} warehouse responsibility",
                    ["providerImports"] = Array.Empty<string>(),
                    ["controllerMode"] = "dynamic",
                    ["returnsDataProcessResult"] = true
                })
                .ToArray(),
            ["eventSchemas"] = Enumerable.Range(1, 16).Select(index => $"xiigen.flow13.event-{index}.v1").ToArray(),
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> Event(string sourceFlowId, string eventType, string sourceRecordId, decimal metricValue) =>
        new()
        {
            ["sourceFlowId"] = sourceFlowId,
            ["eventType"] = eventType,
            ["sourceRecordId"] = sourceRecordId,
            ["metricName"] = "event_count",
            ["metricValue"] = metricValue,
            ["payload"] = new Dictionary<string, object?> { ["synthetic"] = true }
        };

    private static Dictionary<string, object?> Tenant(string tenantId) => new() { ["tenant_id"] = tenantId };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            DataWarehouseAnalyticsService service,
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
        public DataWarehouseAnalyticsService Service { get; }
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
            var service = new DataWarehouseAnalyticsService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<DataWarehouseAnalyticsService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenantId}", "test", DateTimeOffset.UtcNow));
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" ? null : "test-user",
                new HashSet<string> { role },
                $"corr-{tenantId}",
                "test",
                DateTimeOffset.UtcNow));
            return new CompositeDisposable(correlation, tenant);
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
