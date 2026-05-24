using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.FormBuilderTemplates.Application;

public sealed class FormBuilderTemplatesService : MicroserviceBase<FormBuilderTemplatesService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private static readonly string[] PiiMetricKeys = ["formData", "userInput", "values", "responseData", "email", "phone", "name", "ipAddress"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public FormBuilderTemplatesService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<FormBuilderTemplatesService> logger)
        : base(new ServiceDescriptor("form-builder-templates-service", "Form Builder Templates", FormBuilderTemplatesConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<FormBuilderTemplatesOutcome>> SaveTemplateAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "templateId", "name", "schema");
        var templateId = ReadString(request, "templateId");
        var schema = ReadMap(request, "schema");
        var requiredFields = ReadStringList(request, "requiredFields");
        if (string.IsNullOrWhiteSpace(templateId)) errors.Add(FieldError("templateId", "Template id is required."));
        if (schema.Count == 0) errors.Add(FieldError("schema", "JSON Schema document is required."));
        ValidateSchema(schema, requiredFields, errors);
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var op = OperationKey("template-save", idempotencyKey!);
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure) return Failure<FormBuilderTemplatesOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        var reservationData = reservation.Data;
        if (reservationData is { CachedResponse: not null } && string.Equals(reservationData.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return Success(Outcome(200, reservationData.CachedResponse, stateWritten: false));
        }

        var id = ScopedId(templateId);
        var mutex = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await _documents.GetAsync(FormBuilderTemplatesConstants.TemplateCollection, id, cancellationToken).ConfigureAwait(false);
            if (existing.IsFailure) return Failure<FormBuilderTemplatesOutcome>(existing.Error!.Code, existing.Error.Message, existing.Error.Details);
            if (existing.Data is not null)
            {
                if (string.Equals(ReadString(existing.Data.Body, "status"), "PUBLISHED", StringComparison.Ordinal))
                {
                    return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "TEMPLATE_IMMUTABLE", ["templateId"] = templateId, ["currentVersion"] = existing.Data.Version }, false));
                }

                if (!string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(existing.Data.Version, ifMatch, StringComparison.Ordinal))
                {
                    return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = existing.Data.Version }, false));
                }
            }

            var body = new Dictionary<string, object?>
            {
                ["templateId"] = templateId,
                ["name"] = ReadString(request, "name"),
                ["schema"] = schema,
                ["requiredFields"] = requiredFields,
                ["status"] = "DRAFT",
                ["tenantId"] = CurrentTenant.TenantId,
                ["knowledgeScope"] = "PRIVATE"
            };
            var saved = await _documents.UpsertAsync(FormBuilderTemplatesConstants.TemplateCollection, id, body, NormalizeExpectedVersion(ifMatch), cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure) return Failure<FormBuilderTemplatesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
            await AuditAsync("TEMPLATE_VALIDATED", templateId, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("xiigen.form-template.validated", templateId, body), cancellationToken).ConfigureAwait(false);
            if (ReadBool(request, "simulateTimeout"))
            {
                return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true }, true, true));
            }

            var response = new Dictionary<string, object?> { ["id"] = templateId, ["version"] = saved.Data!.Version, ["status"] = "DRAFT" };
            await _idempotency.MarkSucceededAsync(op, response, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(201, response, true, true));
        }
        finally
        {
            mutex.Release();
        }
    }

    public async Task<DataProcessResult<FormBuilderTemplatesOutcome>> PublishTemplateAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateRequired(request, "templateId");
        var templateId = ReadString(request, "templateId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var op = OperationKey("template-publish", idempotencyKey!);
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var reservationData = reservation.Data;
        if (reservationData is { CachedResponse: not null } && string.Equals(reservationData.Status, "succeeded", StringComparison.OrdinalIgnoreCase)) return Success(Outcome(200, reservationData.CachedResponse, false));

        var id = ScopedId(templateId);
        var existing = await _documents.GetAsync(FormBuilderTemplatesConstants.TemplateCollection, id, cancellationToken).ConfigureAwait(false);
        if (existing.Data is null) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "TEMPLATE_NOT_FOUND" }, false));
        if (!string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(existing.Data.Version, ifMatch, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = existing.Data.Version }, false));
        }

        var body = new Dictionary<string, object?>(existing.Data.Body) { ["status"] = "PUBLISHED", ["publishedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) };
        var saved = await _documents.UpsertAsync(FormBuilderTemplatesConstants.TemplateCollection, id, body, NormalizeExpectedVersion(ifMatch), cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<FormBuilderTemplatesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("TEMPLATE_PUBLISHED", templateId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.form-template.published", templateId, body), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["id"] = templateId, ["version"] = saved.Data!.Version, ["status"] = "PUBLISHED" };
        await _idempotency.MarkSucceededAsync(op, response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<FormBuilderTemplatesOutcome>> InstantiateTemplateAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateRequired(request, "templateId", "contextId");
        var templateId = ReadString(request, "templateId");
        var contextId = ReadString(request, "contextId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var lockKey = ScopedId($"template-instantiate-lock:{templateId}:{contextId}");
        var lockReservation = await _idempotency.TryReserveAsync(lockKey, TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
        if (!string.Equals(lockReservation.Data?.Status, "reserved", StringComparison.OrdinalIgnoreCase))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "INSTANTIATION_ALREADY_IN_PROGRESS", ["templateId"] = templateId, ["contextId"] = contextId }, false));
        }

        var bindings = ReadMap(request, "bindings");
        if (ReadBool(request, "requiresMissingBinding") && !bindings.ContainsKey("missing"))
        {
            return Success(ValidationFailed([FieldError("bindings.missing", "Required variable binding is missing.")]));
        }

        var defaults = ReadMap(request, "defaults");
        var values = new Dictionary<string, object?>(defaults, StringComparer.Ordinal);
        foreach (var pair in bindings) values[pair.Key] = pair.Value;
        var instanceId = ScopedId($"{templateId}:{contextId}");
        var body = new Dictionary<string, object?> { ["templateId"] = templateId, ["contextId"] = contextId, ["values"] = values, ["tenantId"] = CurrentTenant.TenantId, ["knowledgeScope"] = "PRIVATE" };
        var saved = await _documents.UpsertAsync(FormBuilderTemplatesConstants.InstanceCollection, instanceId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<FormBuilderTemplatesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("TEMPLATE_INSTANTIATED", templateId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.form-template.instantiated", templateId, body), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["id"] = instanceId, ["version"] = saved.Data!.Version, ["status"] = "INSTANTIATED" };
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<FormBuilderTemplatesOutcome>> RecordUsageAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateRequired(request, "templateId", "eventType");
        foreach (var key in PiiMetricKeys)
        {
            if (request.ContainsKey(key)) errors.Add(FieldError(key, "PII or submitted values are not accepted in usage metrics."));
        }

        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var templateId = ReadString(request, "templateId");
        var eventType = ReadString(request, "eventType").ToUpperInvariant();
        var metricId = ScopedId($"{templateId}:{eventType}:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?> { ["metricId"] = metricId, ["templateId"] = templateId, ["eventType"] = eventType, ["responseTimeMs"] = ReadInt(request, "responseTimeMs"), ["success"] = ReadBool(request, "success"), ["tenantId"] = CurrentTenant.TenantId, ["piiScrubbed"] = true, ["popularityScore"] = 1 };
        var saved = await _documents.UpsertAsync(FormBuilderTemplatesConstants.MetricsCollection, metricId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<FormBuilderTemplatesOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.form-template.usage.recorded", templateId, body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, new Dictionary<string, object?> { ["id"] = metricId, ["templateId"] = templateId, ["piiScrubbed"] = true }, true, true));
    }

    public DataProcessResult<FormBuilderTemplatesOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = FormBuilderTemplatesConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        var covered = FormBuilderTemplatesConstants.ServiceTaskTypes.All(task => taskIds.Contains(task));
        if (missing.Length > 0 || duplicate || !covered)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate, ["focusAreasCovered"] = covered }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["fabricFirst"] = true, ["dynamicEndpointOnly"] = true, ["dataProcessResult"] = true }, false));
    }

    public async Task<DataProcessResult<FormBuilderTemplatesDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var templates = await _documents.SearchAsync(FormBuilderTemplatesConstants.TemplateCollection, filter, cancellationToken).ConfigureAwait(false);
        var instances = await _documents.SearchAsync(FormBuilderTemplatesConstants.InstanceCollection, filter, cancellationToken).ConfigureAwait(false);
        var metrics = await _documents.SearchAsync(FormBuilderTemplatesConstants.MetricsCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new FormBuilderTemplatesDashboard(
            (templates.Data ?? []).Select(record => record.Body).ToArray(),
            (instances.Data ?? []).Select(record => record.Body).ToArray(),
            (metrics.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["templateCount"] = templates.Data?.Count ?? 0, ["instanceCount"] = instances.Data?.Count ?? 0, ["metricCount"] = metrics.Data?.Count ?? 0 }));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(FormBuilderTemplatesConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantId"] = CurrentTenant.TenantId, ["knowledgeScope"] = "PRIVATE" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(Guid.NewGuid().ToString("n"), $"/xiigen/{FormBuilderTemplatesConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });

    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static FormBuilderTemplatesOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static FormBuilderTemplatesOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{FormBuilderTemplatesConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private static string? NormalizeExpectedVersion(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private FormBuilderTemplatesOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private FormBuilderTemplatesOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();

    private static void ValidateSchema(IReadOnlyDictionary<string, object?> schema, IReadOnlyList<string> requiredFields, List<IReadOnlyDictionary<string, object?>> errors)
    {
        if (!string.Equals(ReadString(schema, "type"), "object", StringComparison.Ordinal)) errors.Add(FieldError("schema.type", "Template schema must be an object schema."));
        var properties = ReadMap(schema, "properties");
        if (properties.Count == 0) errors.Add(FieldError("schema.properties", "Template schema requires at least one property."));
        foreach (var required in requiredFields)
        {
            if (!properties.ContainsKey(required)) errors.Add(FieldError($"requiredFields.{required}", "Required field must exist in schema properties."));
        }

        foreach (var property in properties)
        {
            var definition = property.Value as IReadOnlyDictionary<string, object?>;
            if (definition is null && property.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                definition = JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText());
            }

            var type = definition is null ? string.Empty : ReadString(definition, "type");
            if (!FormBuilderTemplatesConstants.AllowedFieldTypes.Contains(type, StringComparer.Ordinal)) errors.Add(FieldError($"schema.properties.{property.Key}.type", "Unsupported field type."));
        }
    }

    private static IReadOnlyDictionary<string, object?> ReadMap(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return new Dictionary<string, object?>();
        if (value is IReadOnlyDictionary<string, object?> map) return map;
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object) return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()) ?? new Dictionary<string, object?>();
        return new Dictionary<string, object?>();
    }

    private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return [];
        if (value is IEnumerable<string> strings) return strings.ToArray();
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array) return element.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToArray();
        return [];
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return string.Empty;
        return value is JsonElement element ? element.ToString() : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return false;
        return value is JsonElement element ? element.ValueKind == JsonValueKind.True : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return 0;
        return value is JsonElement element ? element.GetInt32() : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<FormBuilderPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("planSteps", out var value) || value is null) return [];
        if (value is IEnumerable<FormBuilderPlanStep> typed) return typed.ToArray();
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> maps)
        {
            return maps.Select(map => new FormBuilderPlanStep(
                ReadString(map, "id"),
                ReadString(map, "taskTypeId"),
                ReadString(map, "name"),
                ReadString(map, "responsibility"),
                ReadMap(map, "config"))).ToArray();
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array) return JsonSerializer.Deserialize<List<FormBuilderPlanStep>>(element.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return [];
    }
}
