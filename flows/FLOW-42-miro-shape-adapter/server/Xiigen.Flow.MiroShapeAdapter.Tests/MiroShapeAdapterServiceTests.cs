using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MiroShapeAdapter.Application;
using Xiigen.Flow.MiroShapeAdapter.Infrastructure;
using Xiigen.Kernel;

namespace Xiigen.Flow.MiroShapeAdapter.Tests;

public sealed class MiroShapeAdapterServiceTests
{
    [Fact]
    public void P11_AllFlow42BfaRulesAreRegistered()
    {
        var ids = MiroShapeAdapterBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(MiroShapeAdapterConstants.RequiredBfaRules));
        Assert.Equal(9, MiroShapeAdapterBfaRuleRegistry.Rules.Count);
        Assert.Contains(MiroShapeAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-M1-READ-01" && rule.Type == "MIRO_BOARD_MAPPING");
        Assert.Contains(MiroShapeAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-M1-CLASSIFY-01" && rule.Type == "MIRO_SHAPE_CLASSIFIER");
        Assert.Contains(MiroShapeAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-M1-SPATIAL-01" && rule.Type == "MIRO_SPATIAL_ANALYSIS");
        Assert.Contains(MiroShapeAdapterBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-M1-SDK-BOUNDARY-01" && rule.Type == "SDK_BOUNDARY");
        Assert.All(MiroShapeAdapterBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(MiroShapeAdapterConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_MiroShapeAdapterTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresMiroAdapterRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new MiroShapeAdapterRuleSnapshot("CF-M1-SDK-BOUNDARY-01", "", "F1518", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(9, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ReadPathMapsMiroBoardItemsToSharedArchitectureContracts()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var card = BoardItem("card-1", "card", title: "Checkout module", x: 100m, y: 50m, width: 240m, height: 120m);
        var connector = BoardItem("conn-1", "connector", start: "card-1", end: "card-2");
        var selection = fixture.Service.ReadBoard([card, connector]);

        Assert.Equal("COMPONENT", selection.Data?.Elements[0].Type);
        Assert.Equal("Checkout module", selection.Data?.Elements[0].Label);
        Assert.Equal("#F6F7FB", selection.Data?.Elements[0].Color);
        Assert.Equal(100m, selection.Data?.Elements[0].X);
        Assert.Equal(240m, selection.Data?.Elements[0].Width);
        Assert.Equal("RELATION", selection.Data?.Elements[1].Type);
        Assert.Equal("card-1", selection.Data?.Elements[1].FromId);
        Assert.Equal("card-2", selection.Data?.Elements[1].ToId);
        Assert.Equal("COMPONENT", selection.Data?.Styles[0].ElementType);
        Assert.Equal("#1D2433", selection.Data?.Styles[0].TextColor);
        Assert.Equal(2, selection.Data?.SourceItems.Count);
    }

    [Fact]
    public void ContentParserExtractsLinksAndPlainText()
    {
        var fixture = FlowFixture.Create();

        var parsed = fixture.Service.ParseMiroContent("<p>Hello&nbsp;<a href=\"https://xiigen.local\">XIIGen</a><br>Done &amp; safe</p>");

        Assert.Equal("Hello XIIGen\nDone & safe", parsed.Text);
        Assert.Equal("https://xiigen.local", Assert.Single(parsed.Links));
    }

    [Fact]
    public void ShapeClassifierAppliesR1ThroughR10PriorityRules()
    {
        var fixture = FlowFixture.Create();

        AssertClassification(fixture, BoardItem("frame", "frame"), "R1", "BOUNDARY", 0.95m);
        AssertClassification(fixture, BoardItem("sticky", "sticky_note"), "R2", "COMPONENT", 0.95m);
        AssertClassification(fixture, BoardItem("connector", "connector"), "R3", "RELATION", 0.95m);
        AssertClassification(fixture, BoardItem("heading", "text", fontSize: 32m), "R4", "LABEL", 0.90m);
        AssertClassification(fixture, BoardItem("text", "text"), "R5", "LABEL", 0.85m);
        AssertClassification(fixture, BoardItem("card", "card"), "R6", "COMPONENT", 0.80m);
        AssertClassification(fixture, BoardItem("container", "shape", content: "", width: 240m, height: 240m), "R7", "BOUNDARY", 0.70m);
        AssertClassification(fixture, BoardItem("avatar", "shape", shape: "circle"), "R8", "COMPONENT", 0.70m);
        AssertClassification(fixture, BoardItem("badge", "shape", content: "Admin", width: 100m, height: 32m), "R9", "LABEL", 0.65m);
        AssertClassification(fixture, BoardItem("unknown", "shape", width: 180m, height: 80m), "R10", "STRUCTURE", 0.50m);
    }

    [Fact]
    public void SpatialAnalyzerBuildsContainmentTreeAndRowLayout()
    {
        var fixture = FlowFixture.Create();
        var frame = BoardItem("frame", "frame", x: 0m, y: 0m, width: 600m, height: 300m);
        var first = BoardItem("first", "card", x: 40m, y: 50m, width: 120m, height: 80m);
        var second = BoardItem("second", "card", x: 190m, y: 55m, width: 120m, height: 80m);

        var result = fixture.Service.AnalyzeSpatialLayout([frame, first, second]);

        var root = Assert.Single(result.Data?.Roots ?? []);
        Assert.Equal("frame", root.Item.Id);
        Assert.Equal("row", root.LayoutMode);
        Assert.Equal(30m, root.Gap);
        Assert.Equal(40m, root.Padding.Left);
        Assert.True(fixture.Service.ContainsItem(frame, first));
        Assert.False(fixture.Service.ContainsItem(first, frame));
    }

    [Fact]
    public void SpatialAnalyzerRecognizesColumnAndAbsoluteLayouts()
    {
        var fixture = FlowFixture.Create();
        var frame = BoardItem("frame", "frame", x: 0m, y: 0m, width: 600m, height: 600m);
        var top = BoardItem("top", "card", x: 40m, y: 50m, width: 120m, height: 80m);
        var bottom = BoardItem("bottom", "card", x: 42m, y: 180m, width: 120m, height: 80m);
        var scattered = BoardItem("scattered", "card", x: 300m, y: 300m, width: 100m, height: 60m);

        Assert.Equal("column", fixture.Service.AnalyzeSpatialLayout([frame, top, bottom]).Data?.Roots[0].LayoutMode);
        Assert.Equal("absolute", fixture.Service.AnalyzeSpatialLayout([frame, top, scattered]).Data?.Roots[0].LayoutMode);
    }

    [Fact]
    public void WritePathMapsSharedArchitectureOutputToMiroPayload()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var output = new ArchitectEnhancedOutput(
            new SharedArchitectElement("COMPONENT", "Billing service", "#FFFFFF", 20m, 40m, 180m, 90m),
            new SharedArchitectStyle("#FFFFFF", "#334155", "#0F172A", "COMPONENT"));

        var payload = fixture.Service.MapStyleToMiroPayload(output);

        Assert.Equal("card", payload.Data?.Type);
        Assert.Equal("Billing service", payload.Data?.Title);
        Assert.Equal(20m, payload.Data?.Position.X);
        Assert.Equal(90m, payload.Data?.Geometry.Height);
        Assert.Equal("#334155", payload.Data?.Style.BorderColor);
    }

    [Fact]
    public async Task WritePathCountsWriterFailuresWithoutAbortingBatch()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        fixture.Writer.FailNext = true;

        var result = await fixture.Service.WriteToBoardAsync([
            Enhanced("First", "COMPONENT"),
            Enhanced("Second", "BOUNDARY")
        ], fixture.Writer);

        Assert.Equal(1, result.Data?.Written);
        Assert.Equal(1, result.Data?.Failed);
        Assert.Equal(2, result.Data?.Payloads.Count);
        Assert.Single(fixture.Writer.Payloads);
        Assert.Equal("Second", fixture.Writer.Payloads[0].Title);
    }

    [Fact]
    public async Task RunAdapterPersistsTenantScopedRunAndOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(RunRequest(), fixture.Writer);
        var runs = (await fixture.Documents.SearchAsync(MiroShapeAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(2, Convert.ToInt32(result.Data?.Body["readCount"], CultureInfo.InvariantCulture));
        Assert.Equal(2, Convert.ToInt32(result.Data?.Body["classificationCount"], CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(result.Data?.Body["written"], CultureInfo.InvariantCulture));
        Assert.Single(runs);
        Assert.Single(outbox);
        Assert.Equal("xiigen.miro-shape-adapter.run-recorded", outbox[0].Event.Type);
    }

    [Fact]
    public async Task RunAdapterRejectsMissingFieldsWithoutPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(new Dictionary<string, object?>(), fixture.Writer);
        var runs = (await fixture.Documents.SearchAsync(MiroShapeAdapterConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty(runs);
    }

    [Fact]
    public async Task PublicationRequiresPackageEvidenceAndRecordsGlobalApproval()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var rejected = await fixture.Service.RecordPublicationAsync(new Dictionary<string, object?> { ["packageName"] = MiroShapeAdapterConstants.PackageName });
        var accepted = await fixture.Service.RecordPublicationAsync(PublicationRequest());
        var publications = (await fixture.Documents.SearchAsync(MiroShapeAdapterConstants.PublicationAuditCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Single(publications);
        Assert.Equal("@xiigen/miro-ai-architect", accepted.Data?.Body["packageName"]);
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
        var flowRoot = Path.Combine(root, "flows", "FLOW-42-miro-shape-adapter", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("using Miro", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@mirohq", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<MiroShapeAdapterService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<MiroShapeAdapterOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static void AssertClassification(FlowFixture fixture, MiroBoardItem item, string ruleId, string sharedType, decimal confidence)
    {
        var result = fixture.Service.ClassifyMiroShape(item);
        Assert.Equal(ruleId, result.Data?.RuleId);
        Assert.Equal(sharedType, result.Data?.SharedType);
        Assert.Equal(confidence, result.Data?.Confidence);
    }

    private static MiroBoardItem BoardItem(
        string id,
        string type,
        string? title = null,
        string? content = "<p>Hello XIIGen</p>",
        string? shape = null,
        decimal x = 0m,
        decimal y = 0m,
        decimal width = 160m,
        decimal height = 80m,
        decimal? fontSize = null,
        string? start = null,
        string? end = null) =>
        new(id, type, title, content, shape, new MiroBoardStyle("#F6F7FB", "#CBD5E1", "#1D2433", fontSize ?? 16m, 4m), new MiroPosition(x, y), new MiroGeometry(width, height), start is null ? null : new MiroConnectorEndpoint(start), end is null ? null : new MiroConnectorEndpoint(end));

    private static ArchitectEnhancedOutput Enhanced(string label, string type) =>
        new(new SharedArchitectElement(type, label, "#FFFFFF", 10m, 20m, 120m, 60m), new SharedArchitectStyle("#FFFFFF", "#334155", "#0F172A", type));

    private static Dictionary<string, object?> RunRequest() => new()
    {
        ["runId"] = "run-001",
        ["boardItems"] = new[] { BoardItem("frame", "frame", width: 400m, height: 300m), BoardItem("card", "card", x: 40m, y: 50m) },
        ["enhanced"] = new[] { Enhanced("Enhanced component", "COMPONENT") }
    };

    private static Dictionary<string, object?> PublicationRequest() => new()
    {
        ["packageName"] = MiroShapeAdapterConstants.PackageName,
        ["version"] = MiroShapeAdapterConstants.PackageVersion,
        ["includesStep1Invariants"] = true,
        ["includesFunctionalSpec"] = true,
        ["localRegistryReadback"] = true,
        ["approvedForTenantAdoption"] = true,
        ["pricingModel"] = "PER_USE"
    };

    private static MiroShapeAdapterRuleSnapshot[] ValidRules() =>
        MiroShapeAdapterConstants.RequiredBfaRules.Select(rule => new MiroShapeAdapterRuleSnapshot(rule, "BUILD_FAILURE", "F1518", "miro-shape-adapter-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        MiroShapeAdapterConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Miro Shape Adapter step {task}",
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
        MiroShapeAdapterService Service,
        InMemoryDocumentStore Documents,
        InMemoryOutboxStore Outbox,
        InMemoryMiroWriter Writer,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var outbox = new InMemoryOutboxStore();
            var writer = new InMemoryMiroWriter();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new MiroShapeAdapterService(documents, outbox, tenant, correlation, NullLogger<MiroShapeAdapterService>.Instance);
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
