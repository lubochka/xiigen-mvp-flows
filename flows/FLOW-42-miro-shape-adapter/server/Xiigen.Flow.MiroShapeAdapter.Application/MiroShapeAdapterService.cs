using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.MiroShapeAdapter.Application;

public sealed class MiroShapeAdapterService : MicroserviceBase<MiroShapeAdapterService>
{
    private static readonly Regex AnchorRegex = new("href=[\"'](?<href>[^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public MiroShapeAdapterService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MiroShapeAdapterService> logger)
        : base(new ServiceDescriptor("miro-shape-adapter-service", "Miro Shape Adapter", MiroShapeAdapterConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<MiroAdapterReadResult> ReadBoard(IReadOnlyList<MiroBoardItem> boardItems)
    {
        var elements = new List<SharedArchitectElement>(boardItems.Count);
        var styles = new List<SharedArchitectStyle>(boardItems.Count);
        foreach (var item in boardItems)
        {
            elements.Add(MapMiroToElement(item).Data!);
            styles.Add(MapMiroToStyleResult(item).Data!);
        }

        return Success(new MiroAdapterReadResult(elements, styles, boardItems));
    }

    public DataProcessResult<SharedArchitectElement> MapMiroToElement(MiroBoardItem item)
    {
        var classification = ClassifyMiroShape(item).Data!;
        var parsed = ParseMiroContent(item.Content);
        var label = !string.IsNullOrWhiteSpace(item.Title) ? item.Title! : parsed.Text;
        if (string.IsNullOrWhiteSpace(label)) label = item.Id;

        return Success(new SharedArchitectElement(
            classification.SharedType,
            label,
            item.Style.FillColor,
            item.Position.X,
            item.Position.Y,
            item.Geometry.Width,
            item.Geometry.Height,
            item.StartItem?.Id,
            item.EndItem?.Id));
    }

    public DataProcessResult<SharedArchitectStyle> MapMiroToStyleResult(MiroBoardItem item)
    {
        var classification = ClassifyMiroShape(item).Data!;
        return Success(new SharedArchitectStyle(item.Style.FillColor, item.Style.BorderColor, item.Style.TextColor, classification.SharedType));
    }

    public DataProcessResult<MiroWritePayload> MapStyleToMiroPayload(ArchitectEnhancedOutput output)
    {
        var type = output.Element.Type switch
        {
            "COMPONENT" => "card",
            "RELATION" => "connector",
            "BOUNDARY" => "frame",
            "LABEL" => "text",
            _ => "shape"
        };

        return Success(new MiroWritePayload(
            type,
            output.Element.Label,
            output.Style,
            new MiroPosition(output.Element.X, output.Element.Y),
            new MiroGeometry(output.Element.Width, output.Element.Height)));
    }

    public async Task<DataProcessResult<MiroAdapterWriteResult>> WriteToBoardAsync(IReadOnlyList<ArchitectEnhancedOutput> enhanced, IMiroWriter writer, CancellationToken cancellationToken = default)
    {
        var written = 0;
        var failed = 0;
        var payloads = new List<MiroWritePayload>(enhanced.Count);
        foreach (var item in enhanced)
        {
            var payload = MapStyleToMiroPayload(item).Data!;
            payloads.Add(payload);
            var result = await writer.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Data)
            {
                written++;
            }
            else
            {
                failed++;
            }
        }

        return Success(new MiroAdapterWriteResult(written, failed, payloads));
    }

    public DataProcessResult<MiroShapeClassification> ClassifyMiroShape(MiroBoardItem item)
    {
        if (IsType(item, "frame")) return Success(new MiroShapeClassification("R1", "frame boundary", "BOUNDARY", 0.95m));
        if (IsType(item, "sticky_note")) return Success(new MiroShapeClassification("R2", "sticky note component", "COMPONENT", 0.95m));
        if (IsType(item, "connector")) return Success(new MiroShapeClassification("R3", "connector relation", "RELATION", 0.95m));
        if (IsType(item, "text") && item.Style.FontSize >= 24m) return Success(new MiroShapeClassification("R4", "heading label", "LABEL", 0.90m));
        if (IsType(item, "text")) return Success(new MiroShapeClassification("R5", "text label", "LABEL", 0.85m));
        if (IsType(item, "card")) return Success(new MiroShapeClassification("R6", "card component", "COMPONENT", 0.80m));
        if (IsType(item, "shape") && item.Geometry.Width * item.Geometry.Height >= 40000m && string.IsNullOrWhiteSpace(item.Content)) return Success(new MiroShapeClassification("R7", "container boundary", "BOUNDARY", 0.70m));
        if (IsType(item, "shape") && (IsShape(item, "circle") || IsShape(item, "ellipse"))) return Success(new MiroShapeClassification("R8", "avatar component", "COMPONENT", 0.70m));
        if (IsType(item, "shape") && item.Geometry.Width <= 120m && item.Geometry.Height <= 48m && !string.IsNullOrWhiteSpace(ParseMiroContent(item.Content).Text)) return Success(new MiroShapeClassification("R9", "badge label", "LABEL", 0.65m));
        return Success(new MiroShapeClassification("R10", "unknown structure", "STRUCTURE", 0.50m));
    }

    public DataProcessResult<IReadOnlyList<MiroShapeClassification>> ClassifyMiroShapes(IReadOnlyList<MiroBoardItem> items)
    {
        IReadOnlyList<MiroShapeClassification> classifications = items.Select(item => ClassifyMiroShape(item).Data!).ToArray();
        return Success(classifications);
    }

    public MiroParsedContent ParseMiroContent(string? content)
    {
        var source = content ?? string.Empty;
        var links = AnchorRegex.Matches(source).Select(match => match.Groups["href"].Value).ToArray();
        var normalized = source
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);
        normalized = TagRegex.Replace(normalized, string.Empty).Trim();
        return new MiroParsedContent(normalized, links);
    }

    public DataProcessResult<MiroSpatialAnalysis> AnalyzeSpatialLayout(IReadOnlyList<MiroBoardItem> items, decimal tolerance = 5m)
    {
        var childrenByParent = new Dictionary<string, List<MiroBoardItem>>(StringComparer.Ordinal);
        var childIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in items.OrderBy(Area))
        {
            var parent = items
                .Where(item => !string.Equals(item.Id, candidate.Id, StringComparison.Ordinal) && Area(item) > Area(candidate) && ContainsItem(item, candidate, tolerance))
                .OrderBy(Area)
                .FirstOrDefault();
            if (parent is null) continue;
            childIds.Add(candidate.Id);
            if (!childrenByParent.TryGetValue(parent.Id, out var children))
            {
                children = [];
                childrenByParent[parent.Id] = children;
            }

            children.Add(candidate);
        }

        MiroLayoutNode Build(MiroBoardItem item)
        {
            var children = childrenByParent.TryGetValue(item.Id, out var direct) ? direct.OrderBy(child => child.Position.Y).ThenBy(child => child.Position.X).ToArray() : [];
            var childNodes = children.Select(Build).ToArray();
            return new MiroLayoutNode(item, childNodes, InferLayoutMode(children), EstimateGap(children), EstimatePadding(item, children));
        }

        var roots = items.Where(item => !childIds.Contains(item.Id)).OrderByDescending(Area).Select(Build).ToArray();
        return Success(new MiroSpatialAnalysis(roots, items.Count, tolerance));
    }

    public bool ContainsItem(MiroBoardItem parent, MiroBoardItem child, decimal tolerance = 5m)
    {
        if (string.Equals(parent.Id, child.Id, StringComparison.Ordinal)) return false;
        return child.Position.X >= parent.Position.X - tolerance &&
            child.Position.Y >= parent.Position.Y - tolerance &&
            child.Position.X + child.Geometry.Width <= parent.Position.X + parent.Geometry.Width + tolerance &&
            child.Position.Y + child.Geometry.Height <= parent.Position.Y + parent.Geometry.Height + tolerance;
    }

    public async Task<DataProcessResult<MiroShapeAdapterOutcome>> RunAdapterAsync(IReadOnlyDictionary<string, object?> request, IMiroWriter writer, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRunRequest(request);
        if (validation is not null) return Success(validation);

        var boardItems = ReadBoardItems(request);
        var selection = ReadBoard(boardItems);
        var classifications = ClassifyMiroShapes(boardItems);
        var spatial = AnalyzeSpatialLayout(boardItems);
        var enhanced = ReadEnhancedOutputs(request);
        var write = await WriteToBoardAsync(enhanced, writer, cancellationToken).ConfigureAwait(false);
        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"run:{ReadString(request, "runId")}"),
            ["runId"] = ReadString(request, "runId"),
            ["tenantScope"] = CurrentTenant.TenantId,
            ["readCount"] = selection.Data!.Elements.Count,
            ["styleCount"] = selection.Data.Styles.Count,
            ["classificationCount"] = classifications.Data!.Count,
            ["rootCount"] = spatial.Data!.Roots.Count,
            ["written"] = write.Data!.Written,
            ["failed"] = write.Data.Failed,
            ["packageName"] = MiroShapeAdapterConstants.PackageName,
            ["version"] = MiroShapeAdapterConstants.PackageVersion,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(MiroShapeAdapterConstants.AdapterRunsCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.miro-shape-adapter.run-recorded", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<MiroShapeAdapterOutcome>> RecordPublicationAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var packageName = ReadString(request, "packageName");
        var version = ReadString(request, "version");
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (!string.Equals(packageName, MiroShapeAdapterConstants.PackageName, StringComparison.Ordinal)) errors.Add(FieldError("packageName", "packageName must be @xiigen/miro-ai-architect."));
        if (!string.Equals(version, MiroShapeAdapterConstants.PackageVersion, StringComparison.Ordinal)) errors.Add(FieldError("version", "version must match the package readback version."));
        if (!ReadBool(request, "includesStep1Invariants")) errors.Add(FieldError("includesStep1Invariants", "STEP-1-INVARIANTS.md must be included in the package."));
        if (!ReadBool(request, "includesFunctionalSpec")) errors.Add(FieldError("includesFunctionalSpec", "FUNCTIONAL-SPEC.md must be included in the package."));
        if (!ReadBool(request, "localRegistryReadback")) errors.Add(FieldError("localRegistryReadback", "Local registry readback must confirm the published version."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));

        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"publication:{packageName}:{version}"),
            ["packageName"] = packageName,
            ["version"] = version,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["scope"] = "GLOBAL",
            ["approvedForTenantAdoption"] = ReadBool(request, "approvedForTenantAdoption"),
            ["pricingModel"] = ReadString(request, "pricingModel", "PER_USE"),
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(MiroShapeAdapterConstants.PublicationAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.miro-shape-adapter.package-published", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<MiroShapeAdapterOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = MiroShapeAdapterConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-42 Miro adapter invariants" }, false));
    }

    public DataProcessResult<MiroShapeAdapterOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = MiroShapeAdapterConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = MiroShapeAdapterConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<MiroShapeAdapterDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(MiroShapeAdapterConstants.AdapterRunsCollection, filter, cancellationToken).ConfigureAwait(false);
        var publications = await _documents.SearchAsync(MiroShapeAdapterConstants.PublicationAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new MiroShapeAdapterDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (publications.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["runCount"] = runs.Data?.Count ?? 0, ["publicationCount"] = publications.Data?.Count ?? 0, ["requiredRuleCount"] = MiroShapeAdapterConstants.RequiredBfaRules.Length }));
    }

    private static string InferLayoutMode(IReadOnlyList<MiroBoardItem> children)
    {
        if (children.Count < 2) return "absolute";
        var xRange = children.Max(child => child.Position.X) - children.Min(child => child.Position.X);
        var yRange = children.Max(child => child.Position.Y) - children.Min(child => child.Position.Y);
        if (yRange < 20m || xRange / Math.Max(yRange, 1m) > 2m) return "row";
        if (xRange < 20m || yRange / Math.Max(xRange, 1m) > 2m) return "column";
        return "absolute";
    }

    private static decimal EstimateGap(IReadOnlyList<MiroBoardItem> children)
    {
        if (children.Count < 2) return 0m;
        var byX = children.OrderBy(child => child.Position.X).ToArray();
        var gaps = new List<decimal>();
        for (var index = 1; index < byX.Length; index++)
        {
            gaps.Add(Math.Max(0m, byX[index].Position.X - (byX[index - 1].Position.X + byX[index - 1].Geometry.Width)));
        }

        return decimal.Round(gaps.Average(), 2);
    }

    private static MiroPadding EstimatePadding(MiroBoardItem parent, IReadOnlyList<MiroBoardItem> children)
    {
        if (children.Count == 0) return new MiroPadding(0m, 0m, 0m, 0m);
        var left = children.Min(child => child.Position.X) - parent.Position.X;
        var top = children.Min(child => child.Position.Y) - parent.Position.Y;
        var right = parent.Position.X + parent.Geometry.Width - children.Max(child => child.Position.X + child.Geometry.Width);
        var bottom = parent.Position.Y + parent.Geometry.Height - children.Max(child => child.Position.Y + child.Geometry.Height);
        return new MiroPadding(top, right, bottom, left);
    }

    private MiroShapeAdapterOutcome? ValidateRunRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "runId"))) errors.Add(FieldError("runId", "runId is required."));
        if (ReadBoardItems(request).Count == 0) errors.Add(FieldError("boardItems", "At least one Miro board item is required."));
        if (ReadEnhancedOutputs(request).Count == 0) errors.Add(FieldError("enhanced", "At least one enhanced architecture output item is required."));
        return errors.Count > 0 ? ValidationFailed(errors) : null;
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{MiroShapeAdapterConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static MiroShapeAdapterOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static MiroShapeAdapterOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key, bool fallback = false) => source.TryGetValue(key, out var value) ? bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed : fallback;
    private static IReadOnlyList<MiroBoardItem> ReadBoardItems(IReadOnlyDictionary<string, object?> source) => DeserializeArray<MiroBoardItem>(source, "boardItems");
    private static IReadOnlyList<ArchitectEnhancedOutput> ReadEnhancedOutputs(IReadOnlyDictionary<string, object?> source) => DeserializeArray<ArchitectEnhancedOutput>(source, "enhanced");
    private static IReadOnlyList<MiroShapeAdapterRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => DeserializeArray<MiroShapeAdapterRuleSnapshot>(request, "rules");
    private static IReadOnlyList<MiroShapeAdapterPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => DeserializeArray<MiroShapeAdapterPlanStep>(request, "planSteps");
    private static IReadOnlyList<T> DeserializeArray<T>(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<T[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static bool IsType(MiroBoardItem item, string type) => string.Equals(item.Type, type, StringComparison.OrdinalIgnoreCase);
    private static bool IsShape(MiroBoardItem item, string shape) => string.Equals(item.Shape, shape, StringComparison.OrdinalIgnoreCase);
    private static decimal Area(MiroBoardItem item) => item.Geometry.Width * item.Geometry.Height;
}
