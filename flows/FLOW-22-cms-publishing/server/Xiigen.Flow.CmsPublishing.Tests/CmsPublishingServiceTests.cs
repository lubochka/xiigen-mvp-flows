using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.CmsPublishing.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.CmsPublishing.Tests;

public sealed class CmsPublishingServiceTests
{
    [Fact]
    public void P11_Rules_AllCmsPublishingRules_AreRegistered()
    {
        var ids = CmsPublishingBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);
        var expected = new[] { "CF-22-1", "CF-22-2", "CF-22-3", "CF-22-4", "CF-973", "CF-974", "CF-975", "CF-976" };

        Assert.True(ids.SetEquals(expected));
        Assert.Equal(8, CmsPublishingBfaRuleRegistry.Rules.Count);
        Assert.All(CmsPublishingBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(CmsPublishingConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public void P1_AllT341ThroughT380TaskTypesHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(40, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CF22_1_ContentPublishCreatesImmutableVersionAndOutboxAfterState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");

        var result = await fixture.Service.PublishContentAsync(ContentRequest("article-1"), null, "publish-1");
        var records = (await fixture.Documents.SearchAsync(CmsPublishingConstants.ContentCollection, Tenant("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(CmsPublishingConstants.AuditCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", result.Data?.Body["status"]);
        Assert.Single(records);
        Assert.Single(audit, record => Convert.ToString(record.Body["action"], CultureInfo.InvariantCulture) == "CONTENT_PUBLISHED");
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.cms.content.published");
    }

    [Fact]
    public async Task CF973_IfMatchMismatchReturnsConcurrentUpdateWithoutPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");
        await fixture.Service.PublishContentAsync(ContentRequest("article-1"), null, "publish-1");

        var result = await fixture.Service.PublishContentAsync(ContentRequest("article-1", title: "Changed"), "stale", "publish-2");
        var records = (await fixture.Documents.SearchAsync(CmsPublishingConstants.ContentCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("CONTENT_IMMUTABLE", result.Data?.Body["code"]);
        Assert.Single(records);
        Assert.Equal("Launch notes", records[0].Body["title"]);
    }

    [Fact]
    public async Task CF974_PublishReplayReturnsCachedResponseAndNoDuplicateOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");

        var first = await fixture.Service.PublishContentAsync(ContentRequest("article-1"), null, "publish-1");
        var second = await fixture.Service.PublishContentAsync(ContentRequest("article-1"), null, "publish-1");
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(outbox, record => record.Event.Type == "xiigen.cms.content.published");
    }

    [Fact]
    public async Task CF975_ValidationRejectsEmptyPrimaryFieldBeforeAnyWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");
        var request = ContentRequest("article-1", title: string.Empty);

        var result = await fixture.Service.PublishContentAsync(request, null, "publish-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(CmsPublishingConstants.ContentCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF976_TimeoutKeepsStateAndOutboxForRetry()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");
        var request = ContentRequest("article-timeout");
        request["simulateTimeout"] = true;

        var result = await fixture.Service.PublishContentAsync(request, null, "publish-timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Single((await fixture.Documents.SearchAsync(CmsPublishingConstants.ContentCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF22_2_ApprovalRequiresStageRoleAndWritesAuditBeforeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "legal_reviewer");

        var result = await fixture.Service.RecordApprovalAsync(new Dictionary<string, object?>
        {
            ["contentId"] = "article-1",
            ["stage"] = "LEGAL_REVIEW",
            ["decision"] = "APPROVE"
        }, "approval-1");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("LEGAL_REVIEW", result.Data?.Body["stage"]);
        Assert.Single((await fixture.Documents.SearchAsync(CmsPublishingConstants.ApprovalCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == "xiigen.cms.content.approved");
    }

    [Fact]
    public async Task CF22_2_WrongStageRoleFailsClosed()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "content_author");

        var result = await fixture.Service.RecordApprovalAsync(new Dictionary<string, object?>
        {
            ["contentId"] = "article-1",
            ["stage"] = "FINAL_APPROVAL",
            ["decision"] = "APPROVE"
        }, "approval-1");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("WRONG_ROLE", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(CmsPublishingConstants.ApprovalCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF22_3_ScheduleUsesSetNxEquivalentLockAndDuplicateDoesNotDuplicateOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");
        var request = new Dictionary<string, object?>
        {
            ["contentId"] = "article-1",
            ["publishAt"] = DateTimeOffset.UtcNow.AddHours(2).ToString("O", CultureInfo.InvariantCulture)
        };

        var first = await fixture.Service.SchedulePublishAsync(request, "schedule-1");
        var second = await fixture.Service.SchedulePublishAsync(request, "schedule-2");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(CmsPublishingConstants.ScheduleLockCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == "xiigen.cms.content.scheduled");
    }

    [Fact]
    public async Task CF22_4_AnalyticsIsAppendOnlyAndRejectsPii()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "content_editor");

        var accepted = await fixture.Service.RecordAnalyticsAsync(new Dictionary<string, object?>
        {
            ["contentId"] = "article-1",
            ["metricType"] = "VIEW",
            ["value"] = 4,
            ["userId"] = "reader-1"
        }, "metric-1");
        var rejected = await fixture.Service.RecordAnalyticsAsync(new Dictionary<string, object?>
        {
            ["contentId"] = "article-1",
            ["metricType"] = "VIEW",
            ["value"] = 4,
            ["authorEmail"] = "a@example.test"
        }, "metric-2");
        var metrics = (await fixture.Documents.SearchAsync(CmsPublishingConstants.AnalyticsCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Single(metrics);
        Assert.True(Convert.ToBoolean(metrics[0].Body["piiScrubbed"], CultureInfo.InvariantCulture));
        Assert.False(metrics[0].Body.ContainsKey("authorEmail"));
        Assert.True(metrics[0].Body.ContainsKey("userHash"));
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "publisher"))
        {
            await fixture.Service.PublishContentAsync(ContentRequest("article-1"), null, "publish-1");
        }

        using var _ = fixture.PushContext("globex", "publisher");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Content ?? []);
    }

    [Fact]
    public async Task TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "publisher");
        var request = ContentRequest("article-1");
        request["tenantId"] = "globex";

        var result = await fixture.Service.PublishContentAsync(request, null, "publish-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(CmsPublishingConstants.ContentCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-22-cms-publishing", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<CmsPublishingService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<CmsPublishingOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> ContentRequest(string id, string title = "Launch notes") => new()
    {
        ["contentId"] = id,
        ["title"] = title,
        ["body"] = "A publishable page body with enough detail for review.",
        ["slug"] = $"content-{id}"
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        CmsPublishingConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"CMS plan step {task}",
            ["responsibility"] = $"Single responsibility for {task}"
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Tenant(string tenantId) =>
        new Dictionary<string, object?> { ["tenantId"] = tenantId };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        CmsPublishingService Service,
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
            var service = new CmsPublishingService(documents, idempotency, outbox, tenant, correlation, NullLogger<CmsPublishingService>.Instance);
            return new FlowFixture(service, documents, idempotency, outbox, tenant, correlation);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlationId = Guid.NewGuid().ToString("n");
            var correlationScope = CorrelationAccessor.Push(new CorrelationContext(correlationId, "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantAccessor.Push(new TenantContext(
                tenantId,
                "test-user",
                new HashSet<string>([role], StringComparer.Ordinal),
                correlationId,
                "test",
                DateTimeOffset.UtcNow));
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
            if (_disposed)
            {
                return;
            }

            _second.Dispose();
            _first.Dispose();
            _disposed = true;
        }
    }
}