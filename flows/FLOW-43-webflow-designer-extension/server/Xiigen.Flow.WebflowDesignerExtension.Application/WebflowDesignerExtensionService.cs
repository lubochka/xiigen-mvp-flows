using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.WebflowDesignerExtension.Application;

public sealed class WebflowDesignerExtensionService : MicroserviceBase<WebflowDesignerExtensionService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public WebflowDesignerExtensionService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<WebflowDesignerExtensionService> logger)
        : base(new ServiceDescriptor("webflow-designer-extension-service", "Webflow Designer Extension", WebflowDesignerExtensionConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> ExpandShorthands(IReadOnlyDictionary<string, string> input)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in input)
        {
            IReadOnlyDictionary<string, string>? expanded = key switch
            {
                "border" => ParseBorder(value).Data,
                "font" => ParseFont(value).Data,
                "padding" => ParseBoxSpacing("padding", value).Data,
                "margin" => ParseBoxSpacing("margin", value).Data,
                "box-shadow" => ParseShadow(value).Data,
                _ => null
            };

            if (expanded is null)
            {
                result[key] = value;
                continue;
            }

            foreach (var entry in expanded) result[entry.Key] = entry.Value;
        }

        return Success<IReadOnlyDictionary<string, string>>(result);
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> ParseBorder(string value)
    {
        var tokens = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 3)
        {
            return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
            {
                ["border-width"] = tokens[0],
                ["border-style"] = tokens[1],
                ["border-color"] = string.Join(' ', tokens.Skip(2))
            });
        }

        if (tokens.Length == 2)
        {
            return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
            {
                ["border-width"] = tokens[0],
                ["border-style"] = tokens[1],
                ["border-color"] = "initial"
            });
        }

        return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { ["border-style"] = tokens.FirstOrDefault() ?? string.Empty });
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> ParseFont(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Regex.IsMatch(token, "^(bold|bolder|lighter|[1-9]00)$", RegexOptions.IgnoreCase)) { result["font-weight"] = token; continue; }
            if (Regex.IsMatch(token, "^(italic|oblique|normal)$", RegexOptions.IgnoreCase)) { result["font-style"] = token; continue; }
            if (Regex.IsMatch(token, "^(small-caps)$", RegexOptions.IgnoreCase)) { result["font-variant"] = token; continue; }
            if (token.Contains('/', StringComparison.Ordinal))
            {
                var parts = token.Split('/', 2);
                if (!string.IsNullOrWhiteSpace(parts[0])) result["font-size"] = parts[0];
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) result["line-height"] = parts[1];
                continue;
            }

            if (char.IsDigit(token[0]) || token.EndsWith("px", StringComparison.Ordinal) || token.EndsWith("em", StringComparison.Ordinal) || token.EndsWith("%", StringComparison.Ordinal))
            {
                result["font-size"] = token;
                continue;
            }

            result["font-family"] = result.TryGetValue("font-family", out var current) ? current + " " + token : token;
        }

        return Success<IReadOnlyDictionary<string, string>>(result);
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> ParseBoxSpacing(string property, string value)
    {
        var tokens = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var top = tokens.ElementAtOrDefault(0) ?? string.Empty;
        var right = tokens.Length switch { 1 => top, 2 => tokens[1], 3 => tokens[1], _ => tokens.ElementAtOrDefault(1) ?? top };
        var bottom = tokens.Length switch { 1 => top, 2 => top, 3 => tokens[2], _ => tokens.ElementAtOrDefault(2) ?? top };
        var left = tokens.Length switch { 1 => top, 2 => tokens[1], 3 => tokens[1], _ => tokens.ElementAtOrDefault(3) ?? right };
        return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
        {
            [$"{property}-top"] = top,
            [$"{property}-right"] = right,
            [$"{property}-bottom"] = bottom,
            [$"{property}-left"] = left
        });
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> ParseShadow(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "none", StringComparison.Ordinal)) return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { ["box-shadow"] = "none" });
        var parts = SplitOnCommaOutsideParens(value).Select(item => item.Trim()).Where(item => item.Length > 0);
        return Success<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { ["box-shadow"] = string.Join(", ", parts) });
    }

    public DataProcessResult<IReadOnlyDictionary<string, string>> MapElementProperties(string elementType, IReadOnlyDictionary<string, string> propertyMap)
    {
        var expanded = ExpandShorthands(propertyMap).Data!;
        var keys = PropertyKeysFor(elementType, expanded);
        var routed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in keys.Distinct(StringComparer.Ordinal))
        {
            if (expanded.TryGetValue(key, out var value)) routed[key] = value;
        }

        if (routed.Count == 0 && !string.Equals(elementType, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entry in expanded.Take(1)) routed[entry.Key] = entry.Value;
        }

        if (string.Equals(elementType, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entry in expanded) routed[entry.Key] = entry.Value;
        }

        return Success<IReadOnlyDictionary<string, string>>(routed);
    }

    public DataProcessResult<WebflowValidationDiff> ComputeValidationDiff(IReadOnlyDictionary<string, string> pipelineOutput, IReadOnlyDictionary<string, string> webflowNative)
    {
        var pipelineOnly = new Dictionary<string, string>(StringComparer.Ordinal);
        var nativeOnly = new Dictionary<string, string>(StringComparer.Ordinal);
        var valueDiff = new Dictionary<string, WebflowValueDiff>(StringComparer.Ordinal);
        foreach (var (key, pipelineValue) in pipelineOutput)
        {
            if (!webflowNative.TryGetValue(key, out var nativeValue)) pipelineOnly[key] = pipelineValue;
            else if (!string.Equals(nativeValue, pipelineValue, StringComparison.Ordinal)) valueDiff[key] = new WebflowValueDiff(pipelineValue, nativeValue);
        }

        foreach (var (key, nativeValue) in webflowNative)
        {
            if (!pipelineOutput.ContainsKey(key)) nativeOnly[key] = nativeValue;
        }

        return Success(new WebflowValidationDiff(pipelineOnly, nativeOnly, valueDiff));
    }

    public DataProcessResult<WebflowAdapterReadResult> ReadDesignerElements(IReadOnlyList<WebflowElement> elements)
    {
        var routes = new List<WebflowPropertyRoute>(elements.Count);
        var diffs = new List<WebflowValidationDiff>(elements.Count);
        foreach (var element in elements)
        {
            var routed = MapElementProperties(element.ElementType, element.Properties).Data!;
            routes.Add(new WebflowPropertyRoute(element.Id, element.ElementType, routed));
            diffs.Add(ComputeValidationDiff(routed, element.NativeCss).Data!);
        }

        return Success(new WebflowAdapterReadResult(routes, diffs, elements));
    }

    public async Task<DataProcessResult<WebflowDesignerExtensionOutcome>> RunAdapterAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRunRequest(request);
        if (validation is not null) return Success(validation);

        var elements = ReadElements(request);
        var result = ReadDesignerElements(elements).Data!;
        var mismatchCount = result.Diffs.Sum(diff => diff.PipelineOnly.Count + diff.NativeOnly.Count + diff.ValueDiff.Count);
        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"run:{ReadString(request, "runId")}"),
            ["runId"] = ReadString(request, "runId"),
            ["tenantScope"] = CurrentTenant.TenantId,
            ["elementCount"] = elements.Count,
            ["routeCount"] = result.Routes.Count,
            ["diffCount"] = mismatchCount,
            ["packageName"] = WebflowDesignerExtensionConstants.PackageName,
            ["version"] = WebflowDesignerExtensionConstants.PackageVersion,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(WebflowDesignerExtensionConstants.AdapterRunsCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.webflow-designer-extension.run-recorded", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<WebflowDesignerExtensionOutcome>> RecordPublicationAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var packageName = ReadString(request, "packageName");
        var version = ReadString(request, "version");
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (!string.Equals(packageName, WebflowDesignerExtensionConstants.PackageName, StringComparison.Ordinal)) errors.Add(FieldError("packageName", "packageName must be @xiigen/webflow-designer-extension."));
        if (!string.Equals(version, WebflowDesignerExtensionConstants.PackageVersion, StringComparison.Ordinal)) errors.Add(FieldError("version", "version must match the package readback version."));
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

        await _documents.UpsertAsync(WebflowDesignerExtensionConstants.PublicationAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.webflow-designer-extension.package-published", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<WebflowDesignerExtensionOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = WebflowDesignerExtensionConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-43 Webflow adapter invariants" }, false));
    }

    public DataProcessResult<WebflowDesignerExtensionOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = WebflowDesignerExtensionConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = WebflowDesignerExtensionConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<WebflowDesignerExtensionDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(WebflowDesignerExtensionConstants.AdapterRunsCollection, filter, cancellationToken).ConfigureAwait(false);
        var publications = await _documents.SearchAsync(WebflowDesignerExtensionConstants.PublicationAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new WebflowDesignerExtensionDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (publications.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["runCount"] = runs.Data?.Count ?? 0, ["publicationCount"] = publications.Data?.Count ?? 0, ["requiredRuleCount"] = WebflowDesignerExtensionConstants.RequiredBfaRules.Length }));
    }

    private static IReadOnlyList<string> PropertyKeysFor(string elementType, IReadOnlyDictionary<string, string> expanded)
    {
        var container = new[] { "display", "flex-direction", "flex-wrap", "justify-content", "align-items", "align-content", "gap", "grid-template-columns", "grid-template-rows", "grid-gap", "width", "height", "min-width", "min-height", "max-width", "max-height", "padding-top", "padding-right", "padding-bottom", "padding-left", "margin-top", "margin-right", "margin-bottom", "margin-left", "position", "top", "right", "bottom", "left", "z-index", "overflow", "overflow-x", "overflow-y", "background-color", "background-image", "background-size", "background-position", "border-width", "border-style", "border-color", "border-radius", "box-shadow", "opacity", "transform", "transition" };
        var text = new[] { "color", "font-family", "font-size", "font-weight", "font-style", "line-height", "letter-spacing", "text-align", "text-decoration", "text-transform", "white-space", "word-break" }.Concat(expanded.Keys.Where(key => key.StartsWith("padding", StringComparison.Ordinal) || key.StartsWith("margin", StringComparison.Ordinal))).ToArray();
        var interactive = container.Concat(["cursor", "pointer-events"]).ToArray();
        var media = container.Concat(["object-fit", "object-position", "aspect-ratio"]).ToArray();
        var form = text.Concat(["appearance", "outline", "resize"]).ToArray();

        return elementType switch
        {
            "Heading" or "Paragraph" or "TextBlock" or "RichText" or "LightboxCaption" => text,
            "Link" or "NavLink" or "TabLink" or "DropdownLink" or "LightboxLink" => text,
            "Button" or "NavButton" or "SliderArrow" or "SliderNav" or "Dropdown" or "DropdownToggle" or "DropdownList" => interactive,
            "Image" or "Video" or "BackgroundVideo" => media,
            "Input" or "Textarea" or "Select" or "Checkbox" or "Radio" => form,
            _ => container
        };
    }

    private WebflowDesignerExtensionOutcome? ValidateRunRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "runId"))) errors.Add(FieldError("runId", "runId is required."));
        if (ReadElements(request).Count == 0) errors.Add(FieldError("elements", "At least one Webflow Designer element is required."));
        return errors.Count > 0 ? ValidationFailed(errors) : null;
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{WebflowDesignerExtensionConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static WebflowDesignerExtensionOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static WebflowDesignerExtensionOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string message) => new Dictionary<string, object?> { ["field"] = field, ["message"] = message };
        private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "")
    {
        if (!source.TryGetValue(key, out var value) || value is null) return fallback;
        return value is JsonElement element ? element.GetString() ?? fallback : Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return false;
        return value is JsonElement element ? element.ValueKind == JsonValueKind.True : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }
    private static IReadOnlyList<WebflowElement> ReadElements(IReadOnlyDictionary<string, object?> source) => DeserializeArray<WebflowElement>(source, "elements");
    private static IReadOnlyList<WebflowDesignerExtensionRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> source) => DeserializeArray<WebflowDesignerExtensionRuleSnapshot>(source, "rules");
    private static IReadOnlyList<WebflowDesignerExtensionPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> source) => DeserializeArray<WebflowDesignerExtensionPlanStep>(source, "planSteps");
    private static IReadOnlyList<T> DeserializeArray<T>(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<T[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];

    private static IReadOnlyList<string> SplitOnCommaOutsideParens(string value)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch == '(') { depth++; continue; }
            if (ch == ')') { depth--; continue; }
            if (ch == ',' && depth == 0)
            {
                parts.Add(value[start..index]);
                start = index + 1;
            }
        }

        parts.Add(value[start..]);
        return parts;
    }
}
