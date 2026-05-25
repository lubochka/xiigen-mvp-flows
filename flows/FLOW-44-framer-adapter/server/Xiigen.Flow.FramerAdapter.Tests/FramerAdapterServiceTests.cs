using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FramerAdapter.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.FramerAdapter.Tests;

public sealed class FramerAdapterServiceTests
{
    [Fact]
    public void P11_AllFlow44BfaRulesAreRegistered()
    {
        var ids = FramerAdapterBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(FramerAdapterConstants.RequiredBfaRules));
        Assert.Equal(7, FramerAdapterBfaRuleRegistry.Rules.Count);
        Assert.Contains(FramerAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-804_TEXT_NODE_FILTER" && rule.Type == "TEXT_NODE_FILTER");
        Assert.Contains(FramerAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-805_TEXT_STYLE_MAPPING" && rule.Type == "TEXT_STYLE_MAPPING");
        Assert.Contains(FramerAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-806_PLUGIN_DATA_STORAGE" && rule.Type == "PLUGIN_DATA_STORAGE");
        Assert.All(FramerAdapterBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(FramerAdapterConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_FramerTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresFramerAdapterRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new FramerAdapterRuleSnapshot("CF-FR1-SCOPE-01", "", "F1530", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(7, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TextNodeFilterAcceptsTextAndRejectsNonTextNodes()
    {
        var fixture = FlowFixture.Create();

        Assert.True(fixture.Service.IsFramerTextNode(TextNode()).Data);
        Assert.True(fixture.Service.IsFramerTextNode(TextNode(type: "Text")).Data);
        Assert.False(fixture.Service.IsFramerTextNode(FrameNode()).Data);
        Assert.False(fixture.Service.IsFramerTextNode(TextNode(id: "")).Data);
    }

    [Fact]
    public void ReadTextNodesFiltersMixedSelectionAndPreservesElementFields()
    {
        var fixture = FlowFixture.Create();

        var result = fixture.Service.ReadTextNodes([FrameNode(), TextNode()]);

        Assert.Single(result.Data?.SourceNodes ?? []);
        Assert.Equal("txt-1", result.Data?.Elements[0].Id);
        Assert.Equal("Hero Title", result.Data?.Elements[0].Name);
        Assert.Equal("Build beautifully", result.Data?.Elements[0].Text);
        Assert.Equal(320, result.Data?.Elements[0].Width);
        Assert.Equal(64, result.Data?.Elements[0].Height);
    }

    [Fact]
    public void TextElementFallsBackToIdCharactersAndZeroDimensions()
    {
        var fixture = FlowFixture.Create();

        var element = fixture.Service.MapFramerTextNodeToElement(TextNode(name: null, text: null, characters: "Characters", width: null, height: null)).Data!;

        Assert.Equal("txt-1", element.Name);
        Assert.Equal("Characters", element.Text);
        Assert.Equal(0, element.Width);
        Assert.Equal(0, element.Height);
    }

    [Theory]
    [InlineData(100, "Thin")]
    [InlineData(200, "ExtraLight")]
    [InlineData("bold", "Bold")]
    [InlineData("Extra Bold", "ExtraBold")]
    [InlineData(451, "Medium")]
    [InlineData(20, "Thin")]
    [InlineData(1200, "Black")]
    [InlineData("unknown", "Regular")]
    [InlineData("600", "SemiBold")]
    public void FontWeightMappingIsDeterministic(object value, string expected)
    {
        var fixture = FlowFixture.Create();

        Assert.Equal(expected, fixture.Service.FontWeightToStyle(value).Data);
    }

    [Fact]
    public void NormalizeNumberParsesNumberLikeStrings()
    {
        var fixture = FlowFixture.Create();

        Assert.Equal(24, fixture.Service.NormalizeNumber("24px", 16).Data);
    }

    [Fact]
    public void PaintMappingConvertsRgbRgbaAndHex()
    {
        var fixture = FlowFixture.Create();

        Assert.Equal(new FramerSolidPaint("solid", "#ff8000", 0.5), fixture.Service.RgbaToPaint("rgba(255, 128, 0, 0.5)").Data);
        Assert.Equal(new FramerSolidPaint("solid", "#111827", 1), fixture.Service.RgbaToPaint("rgb(17, 24, 39)").Data);
        Assert.Null(fixture.Service.RgbaToPaint("currentColor").Data);
        Assert.Equal(new FramerSolidPaint("solid", "#abcdef", 1), fixture.Service.NormalizePaint("#ABCDEF").Data);
        Assert.Equal(new FramerSolidPaint("solid", "#00ff88", 1), fixture.Service.NormalizePaint("#0f8").Data);
        Assert.Equal(new FramerSolidPaint("solid", "#000000", 1), fixture.Service.NormalizePaint("brand-primary").Data);
        Assert.Equal(0.42, fixture.Service.NormalizePaint(null, 0.42).Data?.Opacity);
    }

    [Fact]
    public void StyleMappingPrefersFillsOverColorAndPreservesTypography()
    {
        var fixture = FlowFixture.Create();
        var node = TextNode(color: "#ffffff", fills: [new Dictionary<string, object?> { ["color"] = "#101010", ["opacity"] = 0.25 }]);

        var style = fixture.Service.MapFramerTextNodeToStyle(node).Data!;

        Assert.Equal("#101010", style.Color);
        Assert.Equal(0.25, style.Opacity);
        Assert.Equal("Inter", style.FontFamily);
        Assert.Equal(32, style.FontSize);
        Assert.Equal(700, style.FontWeight);
        Assert.Equal(40, Convert.ToDouble(style.LineHeight, CultureInfo.InvariantCulture));
        Assert.Equal(-0.2, Convert.ToDouble(style.LetterSpacing, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TextAttributeBuilderAndUpdateMirrorFramerRuntimePayload()
    {
        var fixture = FlowFixture.Create();
        var element = fixture.Service.MapFramerTextNodeToElement(TextNode()).Data!;
        var style = fixture.Service.MapFramerTextNodeToStyle(TextNode()).Data!;

        var withoutText = fixture.Service.BuildFramerTextAttributes(style).Data!;
        var update = fixture.Service.CreateTextUpdate(new FramerTextOutput(element, style, "Updated copy")).Data!;

        Assert.False(withoutText.Values.ContainsKey("text"));
        Assert.Equal("txt-1", update.NodeId);
        Assert.Equal("Updated copy", update.Attributes.Values["text"]);
        Assert.Equal("Updated copy", update.Attributes.Values["characters"]);
        Assert.Equal("Bold", update.Attributes.Values["fontStyle"]);
    }

    [Fact]
    public void WriteTextElementsCountsSuccessfulAndFailedWritesWithoutThrowing()
    {
        var fixture = FlowFixture.Create();
        var output = OutputFixture(fixture);

        var success = fixture.Service.WriteTextElements([output, output]).Data!;
        var failed = fixture.Service.WriteTextElements([output], new HashSet<string>(["txt-1"], StringComparer.Ordinal)).Data!;

        Assert.Equal(2, success.Written);
        Assert.Equal(0, success.Failed);
        Assert.Equal(0, failed.Written);
        Assert.Equal(1, failed.Failed);
        Assert.Equal("#111827", ((FramerSolidPaint[])success.Updates[0].Attributes.Values["fills"]!)[0].Color);
    }

    [Fact]
    public void PluginDataStorageUsesSerializedStateOnly()
    {
        var fixture = FlowFixture.Create();

        Assert.Empty(fixture.Service.ReadStoredTextAdapterState(null).Data ?? new Dictionary<string, object?>());
        Assert.Empty(fixture.Service.ReadStoredTextAdapterState("not-json").Data ?? new Dictionary<string, object?>());
        var serialized = fixture.Service.WriteStoredTextAdapterState(new Dictionary<string, object?> { ["count"] = 2 }).Data!;

        Assert.Equal(2, ((System.Text.Json.JsonElement)fixture.Service.ReadStoredTextAdapterState(serialized).Data!["count"]!).GetInt32());
    }

    [Fact]
    public async Task RunAdapterPersistsTenantScopedRunAndOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(RunRequest());
        var runs = (await fixture.Documents.SearchAsync(FramerAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(2, Convert.ToInt32(result.Data?.Body["sourceNodeCount"], CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(result.Data?.Body["textNodeCount"], CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(result.Data?.Body["writeCount"], CultureInfo.InvariantCulture));
        Assert.Single(runs);
        Assert.Single(outbox);
        Assert.Equal("xiigen.framer-adapter.run-recorded", outbox[0].Event.Type);
    }

    [Fact]
    public async Task RunAdapterRejectsMissingFieldsWithoutPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(new Dictionary<string, object?>());
        var runs = (await fixture.Documents.SearchAsync(FramerAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty(runs);
    }

    [Fact]
    public async Task PublicationRequiresPackageEvidenceAndRecordsGlobalApproval()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var rejected = await fixture.Service.RecordPublicationAsync(new Dictionary<string, object?> { ["packageName"] = FramerAdapterConstants.PackageName });
        var accepted = await fixture.Service.RecordPublicationAsync(PublicationRequest());
        var publications = (await fixture.Documents.SearchAsync(FramerAdapterConstants.PublicationAuditCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Single(publications);
        Assert.Equal("@xiigen/framer-text-elements-adapter", accepted.Data?.Body["packageName"]);
        Assert.Equal(true, accepted.Data?.OutboxRetained);
    }

    [Fact]
    public async Task DashboardIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "platform_admin"))
        {
            await fixture.Service.RunAdapterAsync(RunRequest());
            await fixture.Service.RecordPublicationAsync(PublicationRequest());
        }

        using var _ = fixture.PushContext("globex", "platform_support");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.AdapterRuns ?? []);
        Assert.Empty(dashboard.Data?.Publications ?? []);
    }

    [Fact]
    public void PackageMetadataMatchesFramerCertificationBoundary()
    {
        Assert.Equal("@xiigen/framer-text-elements-adapter", FramerAdapterConstants.PackageName);
        Assert.Contains("Black", FramerAdapterConstants.FontStyleNames);
        Assert.Contains("CF-806_PLUGIN_DATA_STORAGE", FramerAdapterConstants.RequiredBfaRules);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-44-framer-adapter", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("figma.", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@figma", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("framerPlugin", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<FramerAdapterService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<FramerAdapterOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static FramerNode TextNode(
        string id = "txt-1",
        string type = "text",
        string? name = "Hero Title",
        string? text = "Build beautifully",
        string? characters = null,
        double? width = 320,
        double? height = 64,
        string? color = "rgba(17, 24, 39, 0.88)",
        IReadOnlyList<object?>? fills = null) =>
        new(id, type, name, text, characters, width, height, "Inter", 32, 700, 40, -0.2, color, fills, 0.92);

    private static FramerNode FrameNode() =>
        new("frame-1", "frame", "Card", null, null, 640, 480, null, null, null, null, null, null, null, null);

    private static FramerTextOutput OutputFixture(FlowFixture fixture)
    {
        var element = fixture.Service.MapFramerTextNodeToElement(TextNode()).Data!;
        var style = fixture.Service.MapFramerTextNodeToStyle(TextNode()).Data!;
        return new FramerTextOutput(element, style, "Updated copy");
    }

    private static Dictionary<string, object?> RunRequest() => new()
    {
        ["runId"] = "run-001",
        ["nodes"] = new[] { FrameNode(), TextNode() }
    };

    private static Dictionary<string, object?> PublicationRequest() => new()
    {
        ["packageName"] = FramerAdapterConstants.PackageName,
        ["version"] = FramerAdapterConstants.PackageVersion,
        ["includesStep1Invariants"] = true,
        ["includesFunctionalSpec"] = true,
        ["localRegistryReadback"] = true,
        ["approvedForTenantAdoption"] = true,
        ["pricingModel"] = "PER_USE"
    };

    private static FramerAdapterRuleSnapshot[] ValidRules() =>
        FramerAdapterConstants.RequiredBfaRules.Select(rule => new FramerAdapterRuleSnapshot(rule, "BUILD_FAILURE", "F1530", "framer-adapter-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        FramerAdapterConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Framer Adapter step {task}",
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
        FramerAdapterService Service,
        InMemoryDocumentStore Documents,
        InMemoryOutboxStore Outbox,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var outbox = new InMemoryOutboxStore();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new FramerAdapterService(documents, outbox, tenant, correlation, NullLogger<FramerAdapterService>.Instance);
            return new FlowFixture(service, documents, outbox, tenant, correlation);
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
