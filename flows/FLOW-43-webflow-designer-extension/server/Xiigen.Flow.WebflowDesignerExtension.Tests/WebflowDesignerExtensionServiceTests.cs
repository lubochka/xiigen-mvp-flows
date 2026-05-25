using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.WebflowDesignerExtension.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.WebflowDesignerExtension.Tests;

public sealed class WebflowDesignerExtensionServiceTests
{
    [Fact]
    public void P11_AllFlow43BfaRulesAreRegistered()
    {
        var ids = WebflowDesignerExtensionBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(WebflowDesignerExtensionConstants.RequiredBfaRules));
        Assert.Equal(7, WebflowDesignerExtensionBfaRuleRegistry.Rules.Count);
        Assert.Contains(WebflowDesignerExtensionBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-800_CSS_SHORTHANDS" && rule.Type == "CSS_SHORTHAND_EXPANSION");
        Assert.Contains(WebflowDesignerExtensionBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-801_ELEMENT_CLASSIFICATION" && rule.Type == "ELEMENT_PROPERTY_ROUTING");
        Assert.Contains(WebflowDesignerExtensionBfaRuleRegistry.Rules, rule => rule.RuleId == "CF-803_VALIDATION_DIFF" && rule.Type == "VALIDATION_DIFF");
        Assert.All(WebflowDesignerExtensionBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(WebflowDesignerExtensionConstants.FlowId, rule.FlowId);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("TENANT_CONTEXT", rule.RuleTenantScope);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public void P1_WebflowTasksHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var result = fixture.Service.ValidateTaskCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("TASK_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(4, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RulePackRequiresWebflowAdapterRules()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var invalid = ValidRules().Skip(1).ToList();
        invalid.Add(invalid[0] with { RuleId = invalid[1].RuleId });
        invalid.Add(new WebflowDesignerExtensionRuleSnapshot("CF-W1-SCOPE-01", "", "F1520", "", "", "Bad rule"));

        var rejected = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = invalid });
        var accepted = fixture.Service.ValidateRulePack(new Dictionary<string, object?> { ["rules"] = ValidRules() });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("RULE_PACK_INVALID", rejected.Data?.Body["code"]);
        Assert.Equal(200, accepted.Data?.HttpStatus);
        Assert.Equal(7, Convert.ToInt32(accepted.Data?.Body["ruleCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CssParserExpandsBorderFontSpacingAndShadow()
    {
        var fixture = FlowFixture.Create();

        Assert.Equal("initial", fixture.Service.ParseBorder("2px dashed").Data?["border-color"]);
        Assert.Equal("none", fixture.Service.ParseBorder("none").Data?["border-style"]);
        Assert.Equal("24px", fixture.Service.ParseFont("600 16px/24px Inter").Data?["line-height"]);
        Assert.Equal("12px", fixture.Service.ParseBoxSpacing("padding", "8px 12px").Data?["padding-right"]);
        Assert.Equal("0 1px 4px rgba(0,0,0,0.25), 0 8px 16px #000", fixture.Service.ParseShadow("0 1px 4px rgba(0,0,0,0.25), 0 8px 16px #000").Data?["box-shadow"]);
    }

    [Fact]
    public void ExpandShorthandsPreservesUnknownCssProperties()
    {
        var fixture = FlowFixture.Create();
        var result = fixture.Service.ExpandShorthands(new Dictionary<string, string>
        {
            ["border"] = "1px solid #000",
            ["padding"] = "8px 16px",
            ["--brand-token"] = "primary"
        });

        Assert.Equal("#000", result.Data?["border-color"]);
        Assert.Equal("16px", result.Data?["padding-left"]);
        Assert.Equal("primary", result.Data?["--brand-token"]);
    }

    [Theory]
    [InlineData("Section", "display")]
    [InlineData("Paragraph", "font-size")]
    [InlineData("Link", "color")]
    [InlineData("Button", "cursor")]
    [InlineData("Image", "object-fit")]
    [InlineData("Input", "appearance")]
    [InlineData("NavBar", "display")]
    [InlineData("TabLink", "color")]
    [InlineData("SliderArrow", "pointer-events")]
    [InlineData("DropdownToggle", "cursor")]
    [InlineData("LightboxCaption", "color")]
    [InlineData("CommerceCart", "display")]
    public void ElementPropertyRouterReturnsExpectedPropertyForSupportedTypes(string elementType, string expectedKey)
    {
        var fixture = FlowFixture.Create();

        var result = fixture.Service.MapElementProperties(elementType, BaseCss());

        Assert.True(result.Data?.ContainsKey(expectedKey), $"{elementType} should include {expectedKey}");
        Assert.NotEmpty(result.Data ?? new Dictionary<string, string>());
    }

    [Fact]
    public void UnknownElementPassesThroughExpandedProperties()
    {
        var fixture = FlowFixture.Create();

        var result = fixture.Service.MapElementProperties("Unknown", BaseCss());

        Assert.Equal("1px", result.Data?["border-width"]);
        Assert.Equal("cover", result.Data?["object-fit"]);
    }

    [Fact]
    public void ValidationDiffReportsPipelineOnlyNativeOnlyAndValueDifferences()
    {
        var fixture = FlowFixture.Create();

        var diff = fixture.Service.ComputeValidationDiff(
            new Dictionary<string, string> { ["color"] = "red", ["display"] = "flex" },
            new Dictionary<string, string> { ["color"] = "blue", ["height"] = "100px" });

        Assert.Equal("flex", diff.Data?.PipelineOnly["display"]);
        Assert.Equal("100px", diff.Data?.NativeOnly["height"]);
        Assert.Equal("red", diff.Data?.ValueDiff["color"].Pipeline);
        Assert.Equal("blue", diff.Data?.ValueDiff["color"].Native);
    }

    [Fact]
    public void ReadDesignerElementsRoutesPropertiesAndComputesDiffs()
    {
        var fixture = FlowFixture.Create();
        var element = Element("hero", "Section", BaseCss(), new Dictionary<string, string> { ["display"] = "grid" });

        var result = fixture.Service.ReadDesignerElements([element]);

        Assert.Single(result.Data?.Routes ?? []);
        Assert.Equal("hero", result.Data?.Routes[0].ElementId);
        Assert.Equal("flex", result.Data?.Routes[0].Properties["display"]);
        Assert.Equal("grid", result.Data?.Diffs[0].ValueDiff["display"].Native);
    }

    [Fact]
    public async Task RunAdapterPersistsTenantScopedRunAndOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(RunRequest());
        var runs = (await fixture.Documents.SearchAsync(WebflowDesignerExtensionConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(2, Convert.ToInt32(result.Data?.Body["elementCount"], CultureInfo.InvariantCulture));
        Assert.Equal(2, Convert.ToInt32(result.Data?.Body["routeCount"], CultureInfo.InvariantCulture));
        Assert.Single(runs);
        Assert.Single(outbox);
        Assert.Equal("xiigen.webflow-designer-extension.run-recorded", outbox[0].Event.Type);
    }

    [Fact]
    public async Task RunAdapterRejectsMissingFieldsWithoutPartialState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.RunAdapterAsync(new Dictionary<string, object?>());
        var runs = (await fixture.Documents.SearchAsync(WebflowDesignerExtensionConstants.AdapterRunsCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty(runs);
    }

    [Fact]
    public async Task PublicationRequiresPackageEvidenceAndRecordsGlobalApproval()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var rejected = await fixture.Service.RecordPublicationAsync(new Dictionary<string, object?> { ["packageName"] = WebflowDesignerExtensionConstants.PackageName });
        var accepted = await fixture.Service.RecordPublicationAsync(PublicationRequest());
        var publications = (await fixture.Documents.SearchAsync(WebflowDesignerExtensionConstants.PublicationAuditCollection, Scope("acme-corp"))).Data ?? [];

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Single(publications);
        Assert.Equal("@xiigen/webflow-designer-extension", accepted.Data?.Body["packageName"]);
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
    public void PackageMetadataMatchesWebflowCertificationBoundary()
    {
        Assert.Equal("@xiigen/webflow-designer-extension", WebflowDesignerExtensionConstants.PackageName);
        Assert.Contains("Unknown", WebflowDesignerExtensionConstants.SupportedElementTypes);
        Assert.True(WebflowDesignerExtensionConstants.SupportedElementTypes.Length >= 40);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-43-webflow-designer-extension", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("webflow.", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@webflow", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<WebflowDesignerExtensionService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<WebflowDesignerExtensionOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
        Assert.Contains("CloudEventEnvelope", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> BaseCss() => new(StringComparer.Ordinal)
    {
        ["display"] = "flex",
        ["flex-direction"] = "row",
        ["justify-content"] = "center",
        ["align-items"] = "center",
        ["gap"] = "12px",
        ["width"] = "320px",
        ["height"] = "120px",
        ["padding"] = "8px 16px",
        ["margin"] = "4px",
        ["color"] = "#111111",
        ["font-size"] = "16px",
        ["font-weight"] = "700",
        ["font-family"] = "Inter",
        ["background-color"] = "#ffffff",
        ["border"] = "1px solid #cccccc",
        ["border-radius"] = "6px",
        ["cursor"] = "pointer",
        ["pointer-events"] = "auto",
        ["object-fit"] = "cover",
        ["object-position"] = "center",
        ["aspect-ratio"] = "16 / 9",
        ["appearance"] = "none",
        ["outline"] = "2px solid transparent",
        ["resize"] = "vertical"
    };

    private static WebflowElement Element(string id, string elementType, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, string>? nativeCss = null) =>
        new(id, elementType, $"{elementType} {id}", properties, nativeCss ?? properties);

    private static Dictionary<string, object?> RunRequest() => new()
    {
        ["runId"] = "run-001",
        ["elements"] = new[]
        {
            Element("hero", "Section", BaseCss(), new Dictionary<string, string> { ["display"] = "grid" }),
            Element("cta", "Button", BaseCss(), new Dictionary<string, string> { ["cursor"] = "pointer" })
        }
    };

    private static Dictionary<string, object?> PublicationRequest() => new()
    {
        ["packageName"] = WebflowDesignerExtensionConstants.PackageName,
        ["version"] = WebflowDesignerExtensionConstants.PackageVersion,
        ["includesStep1Invariants"] = true,
        ["includesFunctionalSpec"] = true,
        ["localRegistryReadback"] = true,
        ["approvedForTenantAdoption"] = true,
        ["pricingModel"] = "PER_USE"
    };

    private static WebflowDesignerExtensionRuleSnapshot[] ValidRules() =>
        WebflowDesignerExtensionConstants.RequiredBfaRules.Select(rule => new WebflowDesignerExtensionRuleSnapshot(rule, "BUILD_FAILURE", "F1520", "webflow-designer-extension-001", "GLOBAL", $"Rule {rule}")).ToArray();

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        WebflowDesignerExtensionConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"Webflow Designer Extension step {task}",
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
        WebflowDesignerExtensionService Service,
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
            var service = new WebflowDesignerExtensionService(documents, outbox, tenant, correlation, NullLogger<WebflowDesignerExtensionService>.Instance);
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
