using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.DataWarehouseAnalytics.Application;

public sealed class DataWarehouseAnalyticsService : MicroserviceBase<DataWarehouseAnalyticsService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _planLocks = new(StringComparer.Ordinal);

    public DataWarehouseAnalyticsService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DataWarehouseAnalyticsService> logger)
        : base(
            new ServiceDescriptor("data-warehouse-analytics-service", "Data Warehouse Analytics", DataWarehouseAnalyticsConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> PublishPlanAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidatePlanRequest(request);
        if (validation.Count > 0)
        {
            return Success(new DataWarehouseAnalyticsOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var planId = ReadString(request, "planId")!.Trim();
        var operationKey = $"{DataWarehouseAnalyticsConstants.FlowSlug}:publish-plan:{CurrentTenant.TenantId}:{planId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }

        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new DataWarehouseAnalyticsOutcome(200, reservation.Data.CachedResponse, true, false));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var body = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["message"] = "Warehouse plan publication timed out before document commit.",
                ["retryAfterMs"] = 5000,
                ["planId"] = planId
            };
            await _outbox.AppendAsync(CreateEvent(DataWarehouseAnalyticsConstants.TimeoutRetainedEvent, planId, body), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Warehouse plan publication timed out", cancellationToken).ConfigureAwait(false);
            return Success(new DataWarehouseAnalyticsOutcome(504, body, false, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:warehouse-plan:{planId}";
        var gate = _planLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0))
        {
            return Failure<DataWarehouseAnalyticsOutcome>("CONCURRENT_UPDATE", "Concurrent warehouse plan publication detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var current = await _documents.GetAsync(DataWarehouseAnalyticsConstants.WarehousePlans, PlanDocumentId(planId), cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return Failure<DataWarehouseAnalyticsOutcome>(current.Error!.Code, current.Error.Message, current.Error.Details);
            }

            if (current.Data is not null && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<DataWarehouseAnalyticsOutcome>(
                    "CONCURRENT_UPDATE",
                    "Warehouse plan version does not match",
                    new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            var planBody = CreatePlanBody(request, planId);
            var stored = await _documents.UpsertAsync(
                DataWarehouseAnalyticsConstants.WarehousePlans,
                PlanDocumentId(planId),
                planBody,
                ifMatch,
                cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<DataWarehouseAnalyticsOutcome>(stored.Error!.Code, stored.Error.Message, stored.Error.Details);
            }

            await StoreAuditAsync($"plan-{planId}-{Guid.NewGuid():N}", "WAREHOUSE_PLAN_PUBLISHED", planBody, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["status"] = "ACTIVE",
                ["version"] = stored.Data!.Version,
                ["taskTypeCoverage"] = DataWarehouseAnalyticsConstants.RequiredTaskTypes.Length,
                ["eventSchemaCount"] = ReadStrings(request, "eventSchemas").Count,
                ["warehouseZone"] = ReadString(request, "warehouseZone") ?? "analytics-core"
            };

            await _outbox.AppendAsync(CreateEvent(DataWarehouseAnalyticsConstants.PlanPublishedEvent, planId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new DataWarehouseAnalyticsOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> IngestEventAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateEventIngestionRequest(request);
        if (validation.Count > 0)
        {
            return Success(new DataWarehouseAnalyticsOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var sourceFlowId = ReadString(request, "sourceFlowId")!.Trim();
        var eventType = ReadString(request, "eventType")!.Trim();
        var sourceRecordId = ReadString(request, "sourceRecordId")!.Trim();
        var operationKey = $"{DataWarehouseAnalyticsConstants.FlowSlug}:ingest:{CurrentTenant.TenantId}:{sourceFlowId}:{eventType}:{sourceRecordId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.FromCache && reservation.Data?.CachedResponse is not null)
        {
            return Success(new DataWarehouseAnalyticsOutcome(200, reservation.Data.CachedResponse, true, false));
        }

        var eventId = $"evt-{sourceFlowId}-{sourceRecordId}-{Guid.NewGuid():N}";
        var metricValue = ReadDecimal(request, "metricValue");
        var rawBody = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["event_id"] = eventId,
            ["source_flow_id"] = sourceFlowId,
            ["event_type"] = eventType,
            ["source_record_id"] = sourceRecordId,
            ["metric_name"] = ReadString(request, "metricName") ?? "event_count",
            ["metric_value"] = metricValue,
            ["occurred_at"] = ReadString(request, "occurredAt") ?? DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["raw_payload"] = request.TryGetValue("payload", out var payload) ? payload : new Dictionary<string, object?>()
        };
        var factBody = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["fact_id"] = $"fact-{eventId}",
            ["source_flow_id"] = sourceFlowId,
            ["event_type"] = eventType,
            ["metric_name"] = rawBody["metric_name"],
            ["metric_value"] = metricValue,
            ["warehouse_zone"] = ReadString(request, "warehouseZone") ?? "analytics-core",
            ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var storedEvent = await _documents.UpsertAsync(DataWarehouseAnalyticsConstants.RawEvents, eventId, rawBody, null, cancellationToken).ConfigureAwait(false);
        if (storedEvent.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(storedEvent.Error!.Code, storedEvent.Error.Message, storedEvent.Error.Details);
        }

        var storedFact = await _documents.UpsertAsync(DataWarehouseAnalyticsConstants.MetricFacts, $"fact-{eventId}", factBody, null, cancellationToken).ConfigureAwait(false);
        if (storedFact.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(storedFact.Error!.Code, storedFact.Error.Message, storedFact.Error.Details);
        }

        await StoreAuditAsync($"ingest-{eventId}", "WAREHOUSE_EVENT_INGESTED", rawBody, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["eventId"] = eventId,
            ["factId"] = $"fact-{eventId}",
            ["status"] = "INGESTED",
            ["metricValue"] = metricValue
        };
        await _outbox.AppendAsync(CreateEvent(DataWarehouseAnalyticsConstants.EventIngestedEvent, eventId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DataWarehouseAnalyticsOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> ExecuteQueryAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var metricName = ReadString(request, "metricName") ?? "event_count";
        var facts = await _documents.SearchAsync(
            DataWarehouseAnalyticsConstants.MetricFacts,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["metric_name"] = metricName },
            cancellationToken).ConfigureAwait(false);
        if (facts.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(facts.Error!.Code, facts.Error.Message, facts.Error.Details);
        }

        var total = (facts.Data ?? []).Sum(record => ReadDecimal(record.Body, "metric_value"));
        var resultLimit = await ResolveResultLimitAsync(cancellationToken).ConfigureAwait(false);
        var body = new Dictionary<string, object?>
        {
            ["queryId"] = $"query-{Guid.NewGuid():N}",
            ["metricName"] = metricName,
            ["rowCount"] = facts.Data?.Count ?? 0,
            ["total"] = total,
            ["resultLimit"] = resultLimit
        };
        await _documents.UpsertAsync(DataWarehouseAnalyticsConstants.QueryAudits, Convert.ToString(body["queryId"], CultureInfo.InvariantCulture)!, WithTenant(body), null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(DataWarehouseAnalyticsConstants.QueryExecutedEvent, Convert.ToString(body["queryId"], CultureInfo.InvariantCulture)!, body), cancellationToken).ConfigureAwait(false);
        return Success(new DataWarehouseAnalyticsOutcome(200, body, true, true));
    }

    public async Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> RunQualityCheckAsync(CancellationToken cancellationToken = default)
    {
        var plans = await SearchTenantAsync(DataWarehouseAnalyticsConstants.WarehousePlans, cancellationToken).ConfigureAwait(false);
        var events = await SearchTenantAsync(DataWarehouseAnalyticsConstants.RawEvents, cancellationToken).ConfigureAwait(false);
        if (plans.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(plans.Error!.Code, plans.Error.Message, plans.Error.Details);
        }

        if (events.IsFailure)
        {
            return Failure<DataWarehouseAnalyticsOutcome>(events.Error!.Code, events.Error.Message, events.Error.Details);
        }

        var planData = plans.Data ?? [];
        var eventData = events.Data ?? [];
        var body = new Dictionary<string, object?>
        {
            ["qualityCheckId"] = $"quality-{Guid.NewGuid():N}",
            ["status"] = planData.Count > 0 && eventData.Count > 0 ? "PASS" : "WARN",
            ["planCount"] = planData.Count,
            ["rawEventCount"] = eventData.Count,
            ["freshness"] = eventData.Count > 0 ? "current" : "no-events",
            ["schemaDrift"] = "none"
        };
        await _documents.UpsertAsync(DataWarehouseAnalyticsConstants.QualityAudits, Convert.ToString(body["qualityCheckId"], CultureInfo.InvariantCulture)!, WithTenant(body), null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(DataWarehouseAnalyticsConstants.QualityCheckCompletedEvent, Convert.ToString(body["qualityCheckId"], CultureInfo.InvariantCulture)!, body), cancellationToken).ConfigureAwait(false);
        return Success(new DataWarehouseAnalyticsOutcome(200, body, true, true));
    }

    public async Task<DataProcessResult<DataWarehouseAnalyticsDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var plans = await SearchTenantAsync(DataWarehouseAnalyticsConstants.WarehousePlans, cancellationToken).ConfigureAwait(false);
        var events = await SearchTenantAsync(DataWarehouseAnalyticsConstants.RawEvents, cancellationToken).ConfigureAwait(false);
        var facts = await SearchTenantAsync(DataWarehouseAnalyticsConstants.MetricFacts, cancellationToken).ConfigureAwait(false);
        var quality = await SearchTenantAsync(DataWarehouseAnalyticsConstants.QualityAudits, cancellationToken).ConfigureAwait(false);
        if (plans.IsFailure || events.IsFailure || facts.IsFailure || quality.IsFailure)
        {
            var error = plans.Error ?? events.Error ?? facts.Error ?? quality.Error!;
            return Failure<DataWarehouseAnalyticsDashboard>(error.Code, error.Message, error.Details);
        }

        var planData = plans.Data ?? [];
        var eventData = events.Data ?? [];
        var factData = facts.Data ?? [];
        var qualityData = quality.Data ?? [];
        var eventCount = eventData.Count;
        var totalMetric = factData.Sum(record => ReadDecimal(record.Body, "metric_value"));
        var kpis = new Dictionary<string, object?>
        {
            ["activePlans"] = planData.Count,
            ["rawEvents"] = eventCount,
            ["metricFacts"] = factData.Count,
            ["totalMetricValue"] = totalMetric,
            ["qualityStatus"] = qualityData.LastOrDefault()?.Body.TryGetValue("status", out var status) == true ? status : "not-run"
        };

        return Success(new DataWarehouseAnalyticsDashboard(
            planData.Select(record => record.Body).ToArray(),
            eventData.Select(record => record.Body).ToArray(),
            factData.Select(record => record.Body).ToArray(),
            qualityData.Select(record => record.Body).ToArray(),
            kpis));
    }

    private async Task<DataProcessResult<IReadOnlyList<DocumentRecord>>> SearchTenantAsync(string collection, CancellationToken cancellationToken)
    {
        return await _documents.SearchAsync(collection, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> ResolveResultLimitAsync(CancellationToken cancellationToken)
    {
        var configured = await _freedom.GetAsync("data_warehouse_query_result_limit", cancellationToken).ConfigureAwait(false);
        if (configured.IsSuccess && configured.Data?.Value is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Max(1, value);
        }

        if (configured.IsSuccess && configured.Data?.Value is int intValue)
        {
            return Math.Max(1, intValue);
        }

        return 500;
    }

    private Dictionary<string, object?> CreatePlanBody(IReadOnlyDictionary<string, object?> request, string planId)
    {
        var steps = ReadObjects(request, "steps")
            .Select(step => new Dictionary<string, object?>
            {
                ["task_type"] = ReadString(step, "taskType"),
                ["responsibility"] = ReadString(step, "responsibility"),
                ["fabric_first"] = !ReadStrings(step, "providerImports").Any(),
                ["controller_mode"] = ReadString(step, "controllerMode") ?? "dynamic",
                ["returns_data_process_result"] = ReadBool(step, "returnsDataProcessResult")
            })
            .ToArray();

        return new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["plan_id"] = planId,
            ["warehouse_zone"] = ReadString(request, "warehouseZone") ?? "analytics-core",
            ["status"] = "ACTIVE",
            ["steps"] = steps,
            ["task_type_coverage"] = steps.Select(step => Convert.ToString(step["task_type"], CultureInfo.InvariantCulture)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray(),
            ["event_schemas"] = ReadStrings(request, "eventSchemas").ToArray(),
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private async Task StoreAuditAsync(
        string id,
        string auditType,
        IReadOnlyDictionary<string, object?> body,
        CancellationToken cancellationToken)
    {
        var audit = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = JsonSerializer.Serialize(body).GetHashCode(StringComparison.Ordinal),
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(DataWarehouseAnalyticsConstants.AnalyticsAudits, id, audit, null, cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyDictionary<string, object?> WithTenant(IReadOnlyDictionary<string, object?> body)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = CurrentTenant.TenantId
        };
        return copy;
    }

    private static List<Dictionary<string, object?>> ValidatePlanRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        AddRequiredString(errors, request, "planId");
        var steps = ReadObjects(request, "steps");
        if (steps.Count == 0)
        {
            errors.Add(FieldError("steps", "At least one plan step is required."));
        }

        var taskTypes = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var taskType = ReadString(step, "taskType");
            if (string.IsNullOrWhiteSpace(taskType))
            {
                errors.Add(FieldError($"steps[{index}].taskType", "Task type is required."));
            }
            else
            {
                taskTypes.Add(taskType);
            }

            if (ReadStrings(step, "taskTypes").Count > 0)
            {
                errors.Add(FieldError($"steps[{index}].taskTypes", "Each plan step must be scoped to one task type only."));
            }

            AddRequiredString(errors, step, $"steps[{index}].responsibility", "responsibility");
            if (ReadStrings(step, "providerImports").Any())
            {
                errors.Add(FieldError($"steps[{index}].providerImports", "Provider SDK imports are not allowed in plan steps."));
            }

            var controllerMode = ReadString(step, "controllerMode");
            if (string.Equals(controllerMode, "entity-specific", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(FieldError($"steps[{index}].controllerMode", "Entity-specific controllers are not allowed."));
            }

            if (!ReadBool(step, "returnsDataProcessResult"))
            {
                errors.Add(FieldError($"steps[{index}].returnsDataProcessResult", "Every service step must return DataProcessResult."));
            }
        }

        var missingTaskTypes = DataWarehouseAnalyticsConstants.RequiredTaskTypes.Where(taskType => !taskTypes.Contains(taskType)).ToArray();
        if (missingTaskTypes.Length > 0)
        {
            errors.Add(FieldError("steps", $"Missing task type coverage: {string.Join(",", missingTaskTypes)}."));
        }

        var schemas = ReadStrings(request, "eventSchemas");
        if (schemas.Count < 16)
        {
            errors.Add(FieldError("eventSchemas", "At least 16 CloudEvent schemas are required for FLOW-13."));
        }

        return errors;
    }

    private static List<Dictionary<string, object?>> ValidateEventIngestionRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        AddRequiredString(errors, request, "sourceFlowId");
        AddRequiredString(errors, request, "eventType");
        AddRequiredString(errors, request, "sourceRecordId");
        if (ReadDecimal(request, "metricValue") < 0)
        {
            errors.Add(FieldError("metricValue", "Metric value must be zero or greater."));
        }

        return errors;
    }

    private static DataWarehouseAnalyticsOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new DataWarehouseAnalyticsOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static DataWarehouseAnalyticsOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var hasTenantScope = request.Keys.Any(key =>
            string.Equals(key, "tenantId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "tenant_id", StringComparison.OrdinalIgnoreCase));
        return hasTenantScope
            ? new DataWarehouseAnalyticsOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", "Tenant scope is resolved from TenantContext, never request body."), false, false)
            : null;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            DataWarehouseAnalyticsConstants.FlowId,
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId,
                ["xiigenflowid"] = DataWarehouseAnalyticsConstants.FlowId
            });

    private static IReadOnlyDictionary<string, object?> ValidationBody(IEnumerable<Dictionary<string, object?>> details) =>
        new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details.ToArray() };

    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message) =>
        new Dictionary<string, object?> { ["code"] = code, ["message"] = message };

    private static Dictionary<string, object?> FieldError(string field, string reason) =>
        new() { ["field"] = field, ["reason"] = reason };

    private static void AddRequiredString(List<Dictionary<string, object?>> errors, IReadOnlyDictionary<string, object?> request, string field, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(ReadString(request, key ?? field)))
        {
            errors.Add(FieldError(field, "Value is required."));
        }
    }

    private static string PlanDocumentId(string planId) => $"plan:{planId}";

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static decimal ReadDecimal(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture),
            int intValue => intValue,
            long longValue => longValue,
            string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadObjects(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is IReadOnlyList<IReadOnlyDictionary<string, object?>> typed)
        {
            return typed;
        }

        if (value is IEnumerable<object?> objects)
        {
            return objects
                .OfType<IReadOnlyDictionary<string, object?>>()
                .ToArray();
        }

        return [];
    }

    private static IReadOnlyList<string> ReadStrings(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        if (value is IEnumerable<string> strings)
        {
            return strings.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        }

        if (value is IEnumerable<object?> objects)
        {
            return objects.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)).Where(item => !string.IsNullOrWhiteSpace(item)).Cast<string>().ToArray();
        }

        var scalar = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(scalar) ? [] : [scalar];
    }
}
