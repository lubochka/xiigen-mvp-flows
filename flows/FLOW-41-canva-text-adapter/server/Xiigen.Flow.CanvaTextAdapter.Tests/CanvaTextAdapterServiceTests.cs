using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.CanvaTextAdapter.Application;
using Xiigen.Flow.CanvaTextAdapter.Infrastructure;
using Xiigen.Kernel;

namespace Xiigen.Flow.CanvaTextAdapter.Tests;

public sealed class CanvaTextAdapterServiceTests
{
    [Fact]
    public void P11_AllFlow41BfaRulesAreRegistered()
    {
        var ids = CanvaTextAdapterBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(CanvaTextAdapterConstants.RequiredBfaRules));
        Assert.Equal(7, CanvaTextAdapterBfaRuleRegistry.Rules.Count);
        Assert.Contains(CanvaTextAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-C5-READ-01" && rule.Type == "CANVA_READ_MAPPING");
        Assert.Contains(CanvaTextAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-C5-WRITE-01" && rule.Type == "CANVA_WRITE_MAPPING");
        Assert.Contains(CanvaTextAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-C5-SDK-BOUNDARY-01" && rule.Type == "SDK_BOUNDARY");
        Assert.All(CanvaTextAdapterBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(CanvaTextAdapterConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_CanvaTextAdapterTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresC5AdapterRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new CanvaTextAdapterRuleSnapshot("CF-C5-SDK-BOUNDARY-01", "", "F1518", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(7, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ReadPathMapsCanvaTextElementsToSharedContract()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var element = fixture.Service.MapCanvaToElement(CanvaElement());
        var style = fixture.Service.MapCanvaToStyleResult(CanvaElement());
        var selection = fixture.Service.ReadSelection([CanvaElement()]);

        Assert.Equal("TEXT", element.Data?.Type);
        Assert.Equal("Hello XIIGen", element.Data?.Characters);
        Assert.Equal(200m, element.Data?.Width);
        Assert.Equal(24m, element.Data?.Height);
        Assert.Equal("Inter", style.Data?.FontFamily);
        Assert.Equal("Regular", style.Data?.FontWeight);
        Assert.Equal("LEFT", style.Data?.TextAlignHorizontal);
        Assert.Equal("rgb(34, 34, 34)", style.Data?.Color);
        Assert.Single(selection.Data?.Elements ?? []);
        Assert.Single(selection.Data?.Styles ?? []);
    }

    [Fact]
    public void ReadPathHandlesBoldCenterEndAndEmptyContent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var empty = CanvaElement(content: "", weight: 700, textAlign: "center");
        var end = CanvaElement(textAlign: "end");

        Assert.Equal("", fixture.Service.MapCanvaToElement(empty).Data?.Characters);
        Assert.Equal("Bold", fixture.Service.MapCanvaToStyleResult(empty).Data?.FontWeight);
        Assert.Equal("CENTER", fixture.Service.MapCanvaToStyleResult(empty).Data?.TextAlignHorizontal);
        Assert.Equal("RIGHT", fixture.Service.MapCanvaToStyleResult(end).Data?.TextAlignHorizontal);
    }

    [Fact]
    public void WritePathMapsSharedStyleBackToCanvaPayload()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var payload = fixture.Service.MapStyleToCanvaPayload(new CanvaEnhancedOutput("New heading", SharedStyle(fontWeight: "Bold", align: "CENTER", fontSize: 32m)));

        Assert.Equal("TEXT", payload.Data?.Type);
        Assert.Equal("New heading", payload.Data?.Children[0].Text);
        Assert.Equal(700, payload.Data?.Children[0].FontStyle.Weight);
        Assert.Equal("center", payload.Data?.Children[0].TextAlign);
        Assert.Equal(32m, payload.Data?.Children[0].FontSize);
    }

    [Fact]
    public async Task WritePathCountsWriterFailuresWithoutAbortingBatch()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        fixture.Writer.FailNext = true;

        var result = await fixture.Service.WriteToCanvasAsync([
            new CanvaEnhancedOutput("First", SharedStyle()),
            new CanvaEnhancedOutput("Second", SharedStyle(fontWeight: "Bold"))
        ], fixture.Writer);

        Assert.Equal(1, result.Data?.Written);
        Assert.Equal(1, result.Data?.Failed);
        Assert.Equal(2, result.Data?.Payloads.Count);
        Assert.Single(fixture.Writer.Payloads);
        Assert.Equal("Second", fixture.Writer.Payloads[0].Children[0].Text);
    }

    [Fact]
    public async Task RunAdapterPersistsTenantScopedRunAndOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(RunRequest(), fixture.Writer);
        var runs = (await fixture.Documents.SearchAsync(CanvaTextAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(1, Convert.ToInt32(result.Data?.Body["readCount"], CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(result.Data?.Body["written"], CultureInfo.InvariantCulture));
        Assert.Single(runs);
        Assert.Single(outbox);
        Assert.Equal("xiigen.canva-text-adapter.run-recorded", outbox[0].Event.Type);
    }

    [Fact]
    public async Task RunAdapterRejectsMissingFieldsWithoutPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(new Dictionary<string, object?>(), fixture.Writer);
        var runs = (await fixture.Documents.SearchAsync(CanvaTextAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty(runs);
    }

    [Fact]
    public async Task PublicationRequiresPackageEvidenceAndRecordsGlobalApproval()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var rejected = await fixture.Service.RecordPublicationAsync(new Dictionary<string, object?> { ["packageName"] = CanvaTextAdapterConstants.PackageName });
        var accepted = await fixture.Service.RecordPublicationAsync(PublicationRequest());
        var publications = (await fixture.Documents.SearchAsync(CanvaTextAdapterConstants.PublicationAuditCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Single(publications);
        Assert.Equal("PER_USE", accepted.Data?.Body["pricingModel"]);
        Assert.Equal(true, accepted.Data?.OutboxRetained);
    }

    [Fact]
    public async Task DashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RunAdapterAsync(RunRequest(), fixture.Writer);
            await fixture.Service.RecordPublicationAsync(PublicationRequest());
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.AdapterRuns ?? []);
        Assert.Empty(dashboard.Data?.Publications ?? []);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-41-canva-text-adapter", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("using Canva", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Canva.Design", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<CanvaTextAdapterService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<CanvaTextAdapterOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static CanvaTextElement CanvaElement(string content = "Hello XIIGen", int weight = 400, string textAlign = "start") =>
        new("element-001", content, 16m, new CanvaFontStyle("Inter", weight), textAlign, new CanvaRgbColor(34, 34, 34), 200m, 24m);

    private static SharedStyle SharedStyle(string fontWeight = "Regular", string align = "LEFT", decimal fontSize = 16m) =>
        new(fontSize, "Inter", fontWeight, align, "rgb(34, 34, 34)");

    private static Dictionary<string, object?> RunRequest() => new()
    {
        ["runId"] = "run-001",
        ["canvaElements"] = new[] { CanvaElement() },
        ["enhanced"] = new[] { new CanvaEnhancedOutput("Enhanced text", SharedStyle()) }
    };

    private static Dictionary<string, object?> PublicationRequest() => new()
    {
        ["packageName"] = CanvaTextAdapterConstants.PackageName,
        ["version"] = CanvaTextAdapterConstants.PackageVersion,
        ["includesStep1Invariants"] = true,
        ["includesFunctionalSpec"] = true,
        ["localRegistryReadback"] = true,
        ["approvedForTenantAdoption"] = true,
        ["pricingModel"] = "PER_USE"
    };

    private static CanvaTextAdapterRuleSnapshot[] ValidRules() =>
        CanvaTextAdapterConstants.RequiredBfaRules.Select(rule => new CanvaTextAdapterRuleSnapshot(rule, "BUILD_FAILURE", "F1518", "canva-text-adapter-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        CanvaTextAdapterConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Canva Text Adapter step {task}",
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
        CanvaTextAdapterService Service,
        InMemoryDocumentStore Documents,
        InMemoryOutboxStore Outbox,
        InMemoryCanvaWriter Writer,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var outbox = new InMemoryOutboxStore();
            var writer = new InMemoryCanvaWriter();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new CanvaTextAdapterService(documents, outbox, tenant, correlation, NullLogger<CanvaTextAdapterService>.Instance);
            return new FlowFixture(service, documents, outbox, writer, tenant, correlation);
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
