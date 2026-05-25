using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.CanvaTextAdapter.Application;

public sealed class CanvaTextAdapterService : MicroserviceBase<CanvaTextAdapterService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public CanvaTextAdapterService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<CanvaTextAdapterService> logger)
        : base(new ServiceDescriptor("canva-text-adapter-service", "Canva Text Adapter", CanvaTextAdapterConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<CanvaAdapterReadResult> ReadSelection(IReadOnlyList<CanvaTextElement> canvaElements)
    {
        var elements = new List<SharedElement>(canvaElements.Count);
        var styles = new List<SharedStyle>(canvaElements.Count);
        foreach (var canvaElement in canvaElements)
        {
            elements.Add(MapCanvaToElement(canvaElement).Data!);
            styles.Add(MapCanvaToStyle(canvaElement));
        }

        return Success(new CanvaAdapterReadResult(elements, styles, canvaElements));
    }

    public DataProcessResult<SharedElement> MapCanvaToElement(CanvaTextElement canvaElement)
    {
        return Success(new SharedElement("TEXT", canvaElement.Content, canvaElement.Width, canvaElement.Height));
    }

    public DataProcessResult<SharedStyle> MapCanvaToStyleResult(CanvaTextElement canvaElement)
    {
        return Success(MapCanvaToStyle(canvaElement));
    }

    public DataProcessResult<CanvaWritePayload> MapStyleToCanvaPayload(CanvaEnhancedOutput output)
    {
        var style = MapStyleToCanva(output.Style);
        return Success(new CanvaWritePayload("TEXT", [new CanvaWriteChild(output.Content, style.FontStyle, style.TextAlign, style.FontSize)]));
    }

    public async Task<DataProcessResult<CanvaAdapterWriteResult>> WriteToCanvasAsync(IReadOnlyList<CanvaEnhancedOutput> enhanced, ICanvaWriter writer, CancellationToken cancellationToken = default)
    {
        var written = 0;
        var failed = 0;
        var payloads = new List<CanvaWritePayload>(enhanced.Count);
        foreach (var item in enhanced)
        {
            var payload = MapStyleToCanvaPayload(item).Data!;
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

        return Success(new CanvaAdapterWriteResult(written, failed, payloads));
    }

    public async Task<DataProcessResult<CanvaTextAdapterOutcome>> RunAdapterAsync(IReadOnlyDictionary<string, object?> request, ICanvaWriter writer, CancellationToken cancellationToken = default)
    {
        var validation = ValidateRunRequest(request);
        if (validation is not null) return Success(validation);

        var selection = ReadSelection(ReadCanvaElements(request));
        var enhanced = ReadEnhancedOutputs(request);
        var write = await WriteToCanvasAsync(enhanced, writer, cancellationToken).ConfigureAwait(false);
        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"run:{ReadString(request, "runId")}"),
            ["runId"] = ReadString(request, "runId"),
            ["tenantScope"] = CurrentTenant.TenantId,
            ["readCount"] = selection.Data!.Elements.Count,
            ["styleCount"] = selection.Data.Styles.Count,
            ["written"] = write.Data!.Written,
            ["failed"] = write.Data.Failed,
            ["packageName"] = CanvaTextAdapterConstants.PackageName,
            ["version"] = 1,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        await _documents.UpsertAsync(CanvaTextAdapterConstants.AdapterRunsCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.canva-text-adapter.run-recorded", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<CanvaTextAdapterOutcome>> RecordPublicationAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var packageName = ReadString(request, "packageName");
        var version = ReadString(request, "version");
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (!string.Equals(packageName, CanvaTextAdapterConstants.PackageName, StringComparison.Ordinal)) errors.Add(FieldError("packageName", "packageName must be @xiigen/canva-text-elements-adapter."));
        if (!string.Equals(version, CanvaTextAdapterConstants.PackageVersion, StringComparison.Ordinal)) errors.Add(FieldError("version", "version must match the package readback version."));
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

        await _documents.UpsertAsync(CanvaTextAdapterConstants.PublicationAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.canva-text-adapter.package-published", ReadString(body, "id"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<CanvaTextAdapterOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = CanvaTextAdapterConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-41 C5 adapter invariants" }, false));
    }

    public DataProcessResult<CanvaTextAdapterOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = CanvaTextAdapterConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = CanvaTextAdapterConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<CanvaTextAdapterDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(CanvaTextAdapterConstants.AdapterRunsCollection, filter, cancellationToken).ConfigureAwait(false);
        var publications = await _documents.SearchAsync(CanvaTextAdapterConstants.PublicationAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new CanvaTextAdapterDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (publications.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["runCount"] = runs.Data?.Count ?? 0, ["publicationCount"] = publications.Data?.Count ?? 0, ["requiredRuleCount"] = CanvaTextAdapterConstants.RequiredBfaRules.Length }));
    }

    private static SharedStyle MapCanvaToStyle(CanvaTextElement canvaElement)
    {
        return new SharedStyle(
            canvaElement.FontSize,
            canvaElement.FontStyle.Family,
            canvaElement.FontStyle.Weight == 700 ? "Bold" : "Regular",
            canvaElement.TextAlign == "start" ? "LEFT" : canvaElement.TextAlign == "center" ? "CENTER" : "RIGHT",
            $"rgb({canvaElement.Color.R}, {canvaElement.Color.G}, {canvaElement.Color.B})");
    }

    private static (CanvaFontStyle FontStyle, string TextAlign, decimal FontSize) MapStyleToCanva(SharedStyle style)
    {
        return (
            new CanvaFontStyle(style.FontFamily, style.FontWeight == "Bold" ? 700 : 400),
            style.TextAlignHorizontal == "LEFT" ? "start" : style.TextAlignHorizontal == "CENTER" ? "center" : "end",
            style.FontSize);
    }

    private CanvaTextAdapterOutcome? ValidateRunRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "runId"))) errors.Add(FieldError("runId", "runId is required."));
        if (ReadCanvaElements(request).Count == 0) errors.Add(FieldError("canvaElements", "At least one Canva text element is required."));
        if (ReadEnhancedOutputs(request).Count == 0) errors.Add(FieldError("enhanced", "At least one enhanced output item is required."));
        return errors.Count > 0 ? ValidationFailed(errors) : null;
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{CanvaTextAdapterConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static CanvaTextAdapterOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static CanvaTextAdapterOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key, bool fallback = false) => source.TryGetValue(key, out var value) ? bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed : fallback;
    private static IReadOnlyList<CanvaTextElement> ReadCanvaElements(IReadOnlyDictionary<string, object?> source) => DeserializeArray<CanvaTextElement>(source, "canvaElements");
    private static IReadOnlyList<CanvaEnhancedOutput> ReadEnhancedOutputs(IReadOnlyDictionary<string, object?> source) => DeserializeArray<CanvaEnhancedOutput>(source, "enhanced");
    private static IReadOnlyList<CanvaTextAdapterRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => DeserializeArray<CanvaTextAdapterRuleSnapshot>(request, "rules");
    private static IReadOnlyList<CanvaTextAdapterPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => DeserializeArray<CanvaTextAdapterPlanStep>(request, "planSteps");
    private static IReadOnlyList<T> DeserializeArray<T>(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<T[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
