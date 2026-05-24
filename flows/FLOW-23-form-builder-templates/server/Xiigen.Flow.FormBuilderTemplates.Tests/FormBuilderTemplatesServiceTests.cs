using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FormBuilderTemplates.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.FormBuilderTemplates.Tests;

public sealed class FormBuilderTemplatesServiceTests
{
    [Fact]
    public void P11_Rules_AllFormBuilderTemplateRules_AreRegistered()
    {
        var ids = FormBuilderTemplatesBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);
        var expected = new[]
        {
            "CF-23-1", "CF-23-2", "CF-23-3", "CF-23-4",
            "CF-435", "CF-448", "CF-446", "CF-449", "CF-445", "CF-433", "CF-447", "CF-444",
            "CF-977", "CF-978", "CF-979", "CF-980"
        };

        Assert.True(ids.SetEquals(expected));
        Assert.Equal(16, FormBuilderTemplatesBfaRuleRegistry.Rules.Count);
        Assert.All(FormBuilderTemplatesBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(FormBuilderTemplatesConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void P1_AllT381ThroughT420AndServiceTaskTypesHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(44, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CF23_1_SaveTemplateValidatesSchemaThenWritesAuditAndOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");

        var result = await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");
        var templates = (await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, Tenant("acme-corp"))).Data ?? [];
        var audit = (await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.AuditCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("DRAFT", result.Data?.Body["status"]);
        Assert.Single(templates);
        Assert.Single(audit, record => Convert.ToString(record.Body["action"], CultureInfo.InvariantCulture) == "TEMPLATE_VALIDATED");
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.form-template.validated");
    }

    [Fact]
    public async Task CF977_IfMatchMismatchReturnsConcurrentUpdateWithoutOverwrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");
        var first = await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");

        var changed = TemplateRequest("contact-form", "Changed Form");
        var result = await fixture.Service.SaveTemplateAsync(changed, "stale-version", "save-2");
        var templates = (await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", result.Data?.Body["code"]);
        Assert.Single(templates);
        Assert.Equal("Customer intake", templates[0].Body["name"]);
    }

    [Fact]
    public async Task CF978_IdempotencyReplayReturnsCachedResponseAndNoDuplicateOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");

        var first = await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");
        var second = await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Single(outbox, record => record.Event.Type == "xiigen.form-template.validated");
    }

    [Fact]
    public async Task CF979_ValidationRejectsEmptyPrimaryFieldBeforeAnyWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");
        var request = TemplateRequest("contact-form", string.Empty);

        var result = await fixture.Service.SaveTemplateAsync(request, null, "save-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF980_TimeoutKeepsStateAndOutboxForRetry()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");
        var request = TemplateRequest("timeout-form");
        request["simulateTimeout"] = true;

        var result = await fixture.Service.SaveTemplateAsync(request, null, "save-timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Single((await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF23_2_PublishedTemplatesAreImmutable()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_publisher");
        await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");
        var published = await fixture.Service.PublishTemplateAsync(new Dictionary<string, object?> { ["templateId"] = "contact-form" }, null, "publish-1");

        var result = await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form", "Edited"), null, "save-2");

        Assert.Equal(201, published.Data?.HttpStatus);
        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("TEMPLATE_IMMUTABLE", result.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF23_3_InstantiationMergesDefaultsAndBindingsThroughTenantScopedLock()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "form_operator");
        var result = await fixture.Service.InstantiateTemplateAsync(new Dictionary<string, object?>
        {
            ["templateId"] = "contact-form",
            ["contextId"] = "lead-42",
            ["defaults"] = new Dictionary<string, object?> { ["status"] = "new", ["priority"] = "normal" },
            ["bindings"] = new Dictionary<string, object?> { ["priority"] = "urgent", ["owner"] = "ops" }
        }, "instantiate-1");
        var instances = (await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.InstanceCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Single(instances);
        var values = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(instances[0].Body["values"]);
        Assert.Equal("urgent", values["priority"]);
        Assert.Equal("ops", values["owner"]);
    }

    [Fact]
    public async Task CF23_4_UsageMetricsAreAppendOnlyAndRejectPii()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "form_operator");

        var accepted = await fixture.Service.RecordUsageAsync(new Dictionary<string, object?>
        {
            ["templateId"] = "contact-form",
            ["eventType"] = "submitted",
            ["responseTimeMs"] = 184,
            ["success"] = true
        }, "metric-1");
        var rejected = await fixture.Service.RecordUsageAsync(new Dictionary<string, object?>
        {
            ["templateId"] = "contact-form",
            ["eventType"] = "submitted",
            ["email"] = "person@example.test"
        }, "metric-2");
        var metrics = (await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.MetricsCollection, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Single(metrics);
        Assert.True(Convert.ToBoolean(metrics[0].Body["piiScrubbed"], CultureInfo.InvariantCulture));
        Assert.False(metrics[0].Body.ContainsKey("email"));
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "template_designer"))
        {
            await fixture.Service.SaveTemplateAsync(TemplateRequest("contact-form"), null, "save-1");
        }

        using var _ = fixture.PushContext("globex", "template_designer");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.Templates ?? []);
    }

    [Fact]
    public async Task TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "template_designer");
        var request = TemplateRequest("contact-form");
        request["tenantId"] = "globex";

        var result = await fixture.Service.SaveTemplateAsync(request, null, "save-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-23-form-builder-templates", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<FormBuilderTemplatesService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<FormBuilderTemplatesOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> TemplateRequest(string id, string name = "Customer intake") => new()
    {
        ["templateId"] = id,
        ["name"] = name,
        ["schema"] = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["fullName"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["priority"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["subscribe"] = new Dictionary<string, object?> { ["type"] = "boolean" }
            }
        },
        ["requiredFields"] = new[] { "fullName" }
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        FormBuilderTemplatesConstants.RequiredTaskTypes.Concat(FormBuilderTemplatesConstants.ServiceTaskTypes).Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Form template plan step {task}",
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
        FormBuilderTemplatesService Service,
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
            var service = new FormBuilderTemplatesService(documents, idempotency, outbox, tenant, correlation, NullLogger<FormBuilderTemplatesService>.Instance);
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
