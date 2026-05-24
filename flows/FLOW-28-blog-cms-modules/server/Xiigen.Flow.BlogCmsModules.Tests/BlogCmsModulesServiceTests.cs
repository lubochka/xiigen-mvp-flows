using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.BlogCmsModules.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.BlogCmsModules.Tests;

public sealed class BlogCmsModulesServiceTests
{
    [Fact]
    public void P11_AllFLOW28BfaRulesAreRegistered()
    {
        var ids = BlogCmsModulesBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(BlogCmsModulesConstants.RequiredBfaRules));
        Assert.Equal(39, BlogCmsModulesBfaRuleRegistry.Rules.Count);
        Assert.Contains(BlogCmsModulesBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-566");
        Assert.Contains(BlogCmsModulesBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-600");
        Assert.Contains(BlogCmsModulesBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1003");
        Assert.Contains(BlogCmsModulesBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-1006");
        Assert.All(BlogCmsModulesBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(BlogCmsModulesConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void P11_AllStressTestsAreRequired()
    {
        Assert.Equal(24, BlogCmsModulesConstants.RequiredStressTests.Length);
        Assert.Equal("ST-345", BlogCmsModulesConstants.RequiredStressTests.First());
        Assert.Equal("ST-368", BlogCmsModulesConstants.RequiredStressTests.Last());
    }

    [Fact]
    public void P1_T423ThroughT440HaveSingleResponsibilityPlanStepsAndT440First()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(18, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRejectsMissingDuplicateAndInvalidRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");
        var rules = ValidRules().Skip(1).ToList();
        rules.Add(rules[0] with { RuleId = rules[1].RuleId });
        rules.Add(new BlogCmsRuleSnapshot("CF-1006", "", "F1138", "", "", "Bad rule"));

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = rules });

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", result.Data?.Body["code"]);
    }

    [Fact]
    public void RulePackAcceptsCompleteBlogCmsEnvelope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");

        var result = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal(39, Convert.ToInt32(result.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CreateContentRejectsUnsafeHtmlReservedRoutesAndTenantPayload()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var unsafeBody = Content();
        unsafeBody["body"] = "<script>alert(1)</script>";
        var reserved = Content();
        reserved["slug"] = "/api/news";
        var scoped = Content();
        scoped["tenantId"] = "globex";

        var unsafeResult = await fixture.Service.CreateContentAsync(unsafeBody, "create-unsafe", null);
        var reservedResult = await fixture.Service.CreateContentAsync(reserved, "create-reserved", null);
        var scopedResult = await fixture.Service.CreateContentAsync(scoped, "create-scoped", null);

        Assert.Equal(400, unsafeResult.Data?.HttpStatus);
        Assert.Equal("XSS_REJECTED", unsafeResult.Data?.Body["code"]);
        Assert.Equal(409, reservedResult.Data?.HttpStatus);
        Assert.Equal("ROUTE_PREFIX_RESERVED", reservedResult.Data?.Body["code"]);
        Assert.Equal(400, scopedResult.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", scopedResult.Data?.Body["code"]);
    }

    [Fact]
    public async Task CreateContentIsIdempotentAndWritesBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.CreateContentAsync(Content(), "create-1", null);
        var second = await fixture.Service.CreateContentAsync(Content(), "create-1", null);
        var content = (await fixture.Documents.SearchAsync(BlogCmsModulesConstants.ContentCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(BlogCmsModulesConstants.AuditCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(content);
        Assert.NotEmpty(audit);
        Assert.Contains(outbox, record => record.Event.Type == "blog.content.draft-saved");
    }

    [Fact]
    public async Task CreateContentOptimisticConcurrencyReturns409()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.CreateContentAsync(Content(), "create-1", null);

        var conflict = await fixture.Service.CreateContentAsync(Content(), "create-2", "stale-version");

        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", conflict.Data?.Body["code"]);
    }

    [Fact]
    public async Task AutosaveUsesBlogSessionPrefixAndRejectsUnsafeHtml()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");
        var draft = new Dictionary<string, object?> { ["contentId"] = "content-1", ["revisionId"] = "rev-1", ["body"] = "Draft body" };
        var unsafeDraft = new Dictionary<string, object?>(draft) { ["body"] = "javascript:alert(1)" };

        var rejected = await fixture.Service.AutosaveDraftAsync(unsafeDraft, "draft-bad");
        var accepted = await fixture.Service.AutosaveDraftAsync(draft, "draft-1");
        var replay = await fixture.Service.AutosaveDraftAsync(draft, "draft-2");

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.StartsWith("blog:session:acme-corp:", Convert.ToString(accepted.Data?.Body["sessionKey"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task PublishUsesRevisionLockAndSeparateReviewerPools()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var created = await fixture.Service.CreateContentAsync(Content(), "create-1", null);
        var contentId = Convert.ToString(created.Data?.Body["contentId"], CultureInfo.InvariantCulture)!;

        var deadlock = await fixture.Service.PublishContentAsync(new Dictionary<string, object?> { ["contentId"] = contentId, ["approverPool"] = "editors", ["moderationPool"] = "editors" }, "publish-bad", "1");
        var conflict = await fixture.Service.PublishContentAsync(new Dictionary<string, object?> { ["contentId"] = contentId, ["approverPool"] = "editors", ["moderationPool"] = "legal" }, "publish-conflict", "0");
        var accepted = await fixture.Service.PublishContentAsync(new Dictionary<string, object?> { ["contentId"] = contentId, ["approverPool"] = "editors", ["moderationPool"] = "legal", ["permalinkChanged"] = true }, "publish-1", "1");

        Assert.Equal(409, deadlock.Data?.HttpStatus);
        Assert.Equal("REVIEWER_POOL_DEADLOCK", deadlock.Data?.Body["code"]);
        Assert.Equal(409, conflict.Data?.HttpStatus);
        Assert.Equal("PUBLISH_CONFLICT", conflict.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("published", accepted.Data?.Body["status"]);
        Assert.True(Convert.ToBoolean(accepted.Data?.Body["canonicalCacheInvalidated"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CommentsRequireBudgetPrecheckAndRespectThreadDepth()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var comment = Comment();
        var deep = Comment();
        deep["threadDepth"] = 5;
        deep["maxDepth"] = 3;
        var budget = Comment();
        budget["aiSpamCheckRequested"] = true;
        budget["aiBudgetPrechecked"] = false;

        var deepRejected = await fixture.Service.SubmitCommentAsync(deep, "comment-deep");
        var budgetRejected = await fixture.Service.SubmitCommentAsync(budget, "comment-budget");
        var accepted = await fixture.Service.SubmitCommentAsync(comment, "comment-1");

        Assert.Equal(400, deepRejected.Data?.HttpStatus);
        Assert.Equal("THREAD_DEPTH_LIMIT", deepRejected.Data?.Body["code"]);
        Assert.Equal(402, budgetRejected.Data?.HttpStatus);
        Assert.Equal("AI_BUDGET_PRECHECK_REQUIRED", budgetRejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("user", accepted.Data?.Body["rateLimitMode"]);
    }

    [Fact]
    public async Task StressRunBlocksMissingTestsAndAcceptsFullMatrix()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_support");

        var missing = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults().Take(23).ToArray() }, "stress-bad");
        var accepted = await fixture.Service.RunStressTestsAsync(new Dictionary<string, object?> { ["stressResults"] = StressResults() }, "stress-1");

        Assert.Equal(400, missing.Data?.HttpStatus);
        Assert.Equal("STRESS_MATRIX_FAILED", missing.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(24, Convert.ToInt32(accepted.Data?.Body["stressTestCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.CreateContentAsync(Content(), "create-1", null);
            await fixture.Service.AutosaveDraftAsync(new Dictionary<string, object?> { ["contentId"] = "content-1", ["revisionId"] = "rev-1", ["body"] = "Draft" }, "draft-1");
            await fixture.Service.SubmitCommentAsync(Comment(), "comment-1");
        }

        using var _ = fixture.PushContext("globex", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Content ?? []);
        Assert.Empty(dashboard.Data?.Drafts ?? []);
        Assert.Empty(dashboard.Data?.Comments ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-28-blog-cms-modules", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("googleapis", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<BlogCmsModulesService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<BlogCmsModulesOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Content() => new()
    {
        ["title"] = "Composable tenant publishing",
        ["body"] = "Article body with safe markdown",
        ["slug"] = "tenant-publishing",
        ["visibility"] = "public",
        ["authorUserId"] = "author-1"
    };

    private static Dictionary<string, object?> Comment() => new()
    {
        ["contentId"] = "content-1",
        ["commentHtml"] = "Helpful comment",
        ["rateLimitKey"] = "user:reader-1",
        ["threadDepth"] = 1,
        ["maxDepth"] = 3,
        ["aiSpamCheckRequested"] = true,
        ["aiBudgetPrechecked"] = true
    };

    private static BlogCmsRuleSnapshot[] ValidRules() =>
        BlogCmsModulesConstants.RequiredBfaRules.Select(rule => new BlogCmsRuleSnapshot(rule, "CRITICAL", "F1138", "T423", "GLOBAL", $"Rule {rule}")).ToArray();

    private static BlogCmsStressResult[] StressResults() =>
        BlogCmsModulesConstants.RequiredStressTests.Select(test => new BlogCmsStressResult(test, "PASS", "blog-cms")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        new[] { "T440" }.Concat(BlogCmsModulesConstants.RequiredTaskTypes.Where(task => task != "T440")).Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Blog CMS step {task}",
            ["responsibility"] = $"Single responsibility for {task}",
            ["config"] = new Dictionary<string, object?>()
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Tenant(string scope) => new Dictionary<string, object?> { ["tenantId"] = scope };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        BlogCmsModulesService Service,
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
            var service = new BlogCmsModulesService(documents, idempotency, outbox, tenant, correlation, NullLogger<BlogCmsModulesService>.Instance);
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
