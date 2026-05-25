using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.FramerAdapter.Application;

public sealed class FramerAdapterService : MicroserviceBase<FramerAdapterService>
{
    private static readonly IReadOnlyDictionary<int, string> FontWeightStyleMap = new Dictionary<int, string>
    {
        [100] = "Thin",
        [200] = "ExtraLight",
        [300] = "Light",
        [400] = "Regular",
        [500] = "Medium",
        [600] = "SemiBold",
        [700] = "Bold",
        [800] = "ExtraBold",
        [900] = "Black"
    };

    private static readonly IReadOnlyDictionary<string, int> NamedWeightMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["thin"] = 100,
        ["extralight"] = 200,
        ["light"] = 300,
        ["regular"] = 400,
        ["normal"] = 400,
        ["medium"] = 500,
        ["semibold"] = 600,
        ["bold"] = 700,
        ["extrabold"] = 800,
        ["black"] = 900
    };

    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public FramerAdapterService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<FramerAdapterService> logger)
        : base(new ServiceDescriptor("framer-adapter-service", "Framer Text Elements Adapter", FramerAdapterConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<bool> IsFramerTextNode(FramerNode node)
    {
        return Success(!string.IsNullOrWhiteSpace(node.Id) && string.Equals(node.Type, "text", StringComparison.OrdinalIgnoreCase));
    }

    public DataProcessResult<int> NormalizeFontWeight(object? value)
    {
        if (value is string text)
        {
            var key = Regex.Replace(text, "\\s+", string.Empty);
            if (NamedWeightMap.TryGetValue(key, out var named)) return Success(named);
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedText)) return Success(ClampWeight(parsedText));
        }

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String) return NormalizeFontWeight(element.GetString());
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var numericElement)) return Success(ClampWeight(numericElement));
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return Success(ClampWeight(convertible.ToDouble(CultureInfo.InvariantCulture)));
            }
            catch (InvalidCastException)
            {
                return Success(400);
            }
            catch (FormatException)
            {
                return Success(400);
            }
        }

        return Success(400);
    }

    public DataProcessResult<string> FontWeightToStyle(object? value)
    {
        var normalized = NormalizeFontWeight(value).Data;
        return Success(FontWeightStyleMap.TryGetValue(normalized, out var style) ? style : "Regular");
    }

    public DataProcessResult<double> NormalizeNumber(object? value, double fallback)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var numericElement)) return Success(numericElement);
            if (element.ValueKind == JsonValueKind.String) return NormalizeNumber(element.GetString(), fallback);
        }

        if (value is double doubleValue && double.IsFinite(doubleValue)) return Success(doubleValue);
        if (value is int intValue) return Success((double)intValue);
        if (value is string text)
        {
            var match = Regex.Match(text, "[-+]?[0-9]*\\.?[0-9]+");
            if (match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return Success(parsed);
        }

        return Success(fallback);
    }

    public DataProcessResult<FramerSolidPaint?> RgbaToPaint(string value)
    {
        var match = Regex.Match(value.Trim(), "^rgba?\\(\\s*([0-9.]+)\\s*,\\s*([0-9.]+)\\s*,\\s*([0-9.]+)(?:\\s*,\\s*([0-9.]+))?\\s*\\)$", RegexOptions.IgnoreCase);
        if (!match.Success) return Success<FramerSolidPaint?>(null);
        var red = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var green = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var blue = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var opacity = match.Groups[4].Success ? double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 1;
        return Success<FramerSolidPaint?>(new FramerSolidPaint("solid", $"#{HexPair(red)}{HexPair(green)}{HexPair(blue)}", Math.Clamp(opacity, 0, 1)));
    }

    public DataProcessResult<FramerSolidPaint> NormalizePaint(object? input, double fallbackOpacity = 1)
    {
        if (input is JsonElement element) input = JsonElementToObject(element);
        if (input is string text)
        {
            var rgba = RgbaToPaint(text).Data;
            if (rgba is not null) return Success(rgba);
            var hex = NormalizeHexColor(text);
            return Success(new FramerSolidPaint("solid", hex ?? "#000000", fallbackOpacity));
        }

        if (input is IReadOnlyDictionary<string, object?> map)
        {
            var color = ReadString(map, "color");
            var opacity = ReadDouble(map, "opacity", fallbackOpacity);
            if (!string.IsNullOrWhiteSpace(color)) return NormalizePaint(color, opacity);
        }

        return Success(new FramerSolidPaint("solid", "#000000", fallbackOpacity));
    }

    public DataProcessResult<FramerTextElement> MapFramerTextNodeToElement(FramerNode node)
    {
        return Success(new FramerTextElement(
            node.Id,
            string.IsNullOrWhiteSpace(node.Name) ? node.Id : node.Name,
            !string.IsNullOrEmpty(node.Text) ? node.Text : node.Characters ?? string.Empty,
            NormalizeNumber(node.Width, 0).Data,
            NormalizeNumber(node.Height, 0).Data));
    }

    public DataProcessResult<FramerTextStyle> MapFramerTextNodeToStyle(FramerNode node)
    {
        var opacity = node.Opacity ?? 1;
        var paintSource = node.Fills?.FirstOrDefault() ?? node.Color;
        var paint = NormalizePaint(paintSource, opacity).Data!;
        var weight = NormalizeFontWeight(node.FontWeight).Data;
        return Success(new FramerTextStyle(
            node.FontFamily ?? "Inter",
            NormalizeNumber(node.FontSize, 16).Data,
            weight,
            FontWeightStyleMap.TryGetValue(weight, out var style) ? style : "Regular",
            node.LineHeight ?? "normal",
            node.LetterSpacing ?? 0,
            paint.Color,
            paint.Opacity,
            paint));
    }

    public DataProcessResult<FramerTextReadResult> ReadTextNodes(IReadOnlyList<FramerNode> nodes)
    {
        var textNodes = nodes.Where(node => IsFramerTextNode(node).Data == true).ToArray();
        return Success(new FramerTextReadResult(
            textNodes.Select(node => MapFramerTextNodeToElement(node).Data!).ToArray(),
            textNodes.Select(node => MapFramerTextNodeToStyle(node).Data!).ToArray(),
            textNodes));
    }

    public DataProcessResult<FramerTextAttributes> BuildFramerTextAttributes(FramerTextStyle style, string? nextText = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["fontFamily"] = style.FontFamily,
            ["fontSize"] = style.FontSize,
            ["fontWeight"] = style.FontWeight,
            ["fontStyle"] = style.FontStyleName,
            ["lineHeight"] = style.LineHeight,
            ["letterSpacing"] = style.LetterSpacing,
            ["fills"] = new[] { style.Paint },
            ["opacity"] = style.Opacity
        };
        if (nextText is not null)
        {
            values["text"] = nextText;
            values["characters"] = nextText;
        }

        return Success(new FramerTextAttributes(values));
    }

    public DataProcessResult<FramerTextUpdate> CreateTextUpdate(FramerTextOutput output)
    {
        var nextText = output.NextText ?? output.Element.Text;
        return Success(new FramerTextUpdate(output.Element.Id, BuildFramerTextAttributes(output.Style, nextText).Data!));
    }

    public DataProcessResult<FramerTextWriteResult> WriteTextElements(IReadOnlyList<FramerTextOutput> outputs, IReadOnlySet<string>? failingNodeIds = null)
    {
        var updates = outputs.Select(output => CreateTextUpdate(output).Data!).ToArray();
        var failed = failingNodeIds is null ? 0 : updates.Count(update => failingNodeIds.Contains(update.NodeId));
        return Success(new FramerTextWriteResult(updates.Length - failed, failed, updates));
    }

    public DataProcessResult<IReadOnlyDictionary<string, object?>> ReadStoredTextAdapterState(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>());
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(raw);
            return Success<IReadOnlyDictionary<string, object?>>(parsed ?? new Dictionary<string, object?>());
        }
        catch (JsonException)
        {
            return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>());
        }
    }

    public DataProcessResult<string> WriteStoredTextAdapterState(IReadOnlyDictionary<string, object?> value)
    {
        return Success(JsonSerializer.Serialize(value));
    }

    public async Task<DataProcessResult<FramerAdapterOutcome>> RunAdapterAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRunRequest(request);
        if (validation is not null) return Success(validation);

        var nodes = ReadNodes(request);
        var result = ReadTextNodes(nodes).Data!;
        var outputs = result.Elements.Zip(result.Styles, (element, style) => new FramerTextOutput(element, style, $"{element.Text} updated")).ToArray();
        var write = WriteTextElements(outputs).Data!;
        var state = new Dictionary<string, object?> { ["textNodeCount"] = result.Elements.Count, ["written"] = write.Written, ["failed"] = write.Failed };
        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"run:{ReadString(request, "runId")}"),
            ["runId"] = ReadString(request, "runId"),
            ["tenantScope"] = CurrentTenant.TenantId,
            ["sourceNodeCount"] = nodes.Count,
            ["textNodeCount"] = result.Elements.Count,
            ["writeCount"] = write.Written,
            ["failedWriteCount"] = write.Failed,
            ["storedState"] = WriteStoredTextAdapterState(state).Data,
            ["packageName"] = FramerAdapterConstants.PackageName,
            ["version"] = FramerAdapterConstants.PackageVersion,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(FramerAdapterConstants.AdapterRunsCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.framer-adapter.run-recorded", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<FramerAdapterOutcome>> RecordPublicationAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var packageName = ReadString(request, "packageName");
        var version = ReadString(request, "version");
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (!string.Equals(packageName, FramerAdapterConstants.PackageName, StringComparison.Ordinal)) errors.Add(FieldError("packageName", "packageName must be @xiigen/framer-text-elements-adapter."));
        if (!string.Equals(version, FramerAdapterConstants.PackageVersion, StringComparison.Ordinal)) errors.Add(FieldError("version", "version must match the package readback version."));
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

        await _documents.UpsertAsync(FramerAdapterConstants.PublicationAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.framer-adapter.package-published", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<FramerAdapterOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = FramerAdapterConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-44 Framer text adapter invariants" }, false));
    }

    public DataProcessResult<FramerAdapterOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = FramerAdapterConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = FramerAdapterConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<FramerAdapterDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(FramerAdapterConstants.AdapterRunsCollection, filter, cancellationToken).ConfigureAwait(false);
        var publications = await _documents.SearchAsync(FramerAdapterConstants.PublicationAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new FramerAdapterDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (publications.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["runCount"] = runs.Data?.Count ?? 0, ["publicationCount"] = publications.Data?.Count ?? 0, ["requiredRuleCount"] = FramerAdapterConstants.RequiredBfaRules.Length }));
    }

    private FramerAdapterOutcome? ValidateRunRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "runId"))) errors.Add(FieldError("runId", "runId is required."));
        if (ReadNodes(request).Count == 0) errors.Add(FieldError("nodes", "At least one Framer node is required."));
        return errors.Count > 0 ? ValidationFailed(errors) : null;
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{FramerAdapterConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static FramerAdapterOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static FramerAdapterOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string message) => new Dictionary<string, object?> { ["field"] = field, ["message"] = message };
    private static IReadOnlyList<FramerNode> ReadNodes(IReadOnlyDictionary<string, object?> source) => DeserializeArray<FramerNode>(source, "nodes");
    private static IReadOnlyList<FramerAdapterRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> source) => DeserializeArray<FramerAdapterRuleSnapshot>(source, "rules");
    private static IReadOnlyList<FramerAdapterPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> source) => DeserializeArray<FramerAdapterPlanStep>(source, "planSteps");
    private static IReadOnlyList<T> DeserializeArray<T>(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<T[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];

    private static int ClampWeight(double value)
    {
        var rounded = (int)Math.Round(value / 100, MidpointRounding.AwayFromZero) * 100;
        return Math.Min(900, Math.Max(100, rounded));
    }

    private static string HexPair(double value)
    {
        var rounded = (int)Math.Round(Math.Min(255, Math.Max(0, value)), MidpointRounding.AwayFromZero);
        return rounded.ToString("x2", CultureInfo.InvariantCulture);
    }

    private static string? NormalizeHexColor(string value)
    {
        var trimmed = value.Trim();
        if (Regex.IsMatch(trimmed, "^#[0-9a-f]{6}$", RegexOptions.IgnoreCase)) return trimmed.ToLowerInvariant();
        if (Regex.IsMatch(trimmed, "^#[0-9a-f]{3}$", RegexOptions.IgnoreCase))
        {
            return $"#{trimmed[1]}{trimmed[1]}{trimmed[2]}{trimmed[2]}{trimmed[3]}{trimmed[3]}".ToLowerInvariant();
        }

        return null;
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDouble(out var number) ? number : null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => JsonElementToObject(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            _ => null
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "")
    {
        if (!source.TryGetValue(key, out var value) || value is null) return fallback;
        return value is JsonElement element ? element.GetString() ?? fallback : Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> source, string key, double fallback)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return fallback;
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var elementValue)) return elementValue;
        return value is IConvertible convertible ? convertible.ToDouble(CultureInfo.InvariantCulture) : fallback;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return false;
        return value is JsonElement element ? element.ValueKind == JsonValueKind.True : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
}
