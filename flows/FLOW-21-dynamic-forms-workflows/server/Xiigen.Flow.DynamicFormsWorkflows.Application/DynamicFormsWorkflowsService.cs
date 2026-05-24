using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.DynamicFormsWorkflows.Application;

public sealed class DynamicFormsWorkflowsService : MicroserviceBase<DynamicFormsWorkflowsService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] ForbiddenPayloadKeys =
    [
        "tenantId",
        "tenant_id",
        "flowTenantId",
        "flow_tenant_id",
        "ownerTenantId",
        "owner_tenant_id",
        "formTenantId",
        "form_tenant_id"
    ];

    private static readonly string[] PiiKeys =
    [
        "email",
        "phone",
        "firstName",
        "lastName",
        "fullName",
        "name",
        "address",
        "ssn",
        "passport",
        "nationalId"
    ];

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new(StringComparer.Ordinal);

    public DynamicFormsWorkflowsService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DynamicFormsWorkflowsService> logger)
        : base(
            new ServiceDescriptor("dynamic-forms-workflows-service", "Dynamic Forms Workflows", DynamicFormsWorkflowsConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> PublishSchemaAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "formId", "schemaVersion");
        var fields = ReadFields(request);
        var workflowRules = ReadWorkflowRules(request);
        var planSteps = ReadPlanSteps(request);
        if (fields.Count == 0)
        {
            validation.Add(FieldError("fields", "At least one field definition is required."));
        }
        if (workflowRules.Count == 0)
        {
            validation.Add(FieldError("workflowRules", "At least one workflow rule is required."));
        }
        ValidatePlanStepCoverage(planSteps, validation);
        if (validation.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var formId = ReadString(request, "formId")!.Trim();
        var schemaVersion = ReadString(request, "schemaVersion")!.Trim();
        var operationKey = $"{DynamicFormsWorkflowsConstants.FlowSlug}:schema:{CurrentTenant.TenantId}:{formId}:{schemaVersion}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeout = ErrorBody("TIMEOUT", "Workflow schema publisher timed out before the schema could commit.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["formId"] = formId
            });
            await _outbox.AppendAsync(CreateEvent(DynamicFormsWorkflowsConstants.TimeoutRetainedEvent, formId, timeout), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Workflow schema publisher timed out", cancellationToken).ConfigureAwait(false);
            return Success(new DynamicFormsWorkflowsOutcome(504, timeout, false, true));
        }

        var gate = GetResourceLock($"schema:{CurrentTenant.TenantId}:{formId}");
        if (!gate.Wait(0))
        {
            return Success(Conflict("CONCURRENT_UPDATE", "A schema publish for this form is already in progress.", "locked"));
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var existing = await _documents.GetAsync(DynamicFormsWorkflowsConstants.FormSchemas, formId, cancellationToken).ConfigureAwait(false);
            if (existing.Data is not null && !BelongsToCurrentTenant(existing.Data.Body))
            {
                return Success(new DynamicFormsWorkflowsOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this form schema."), false, false));
            }
            if (existing.Data is not null && !string.Equals(ifMatch, existing.Data.Version, StringComparison.Ordinal))
            {
                return Success(Conflict("CONCURRENT_UPDATE", "Published schema version requires the latest If-Match value.", existing.Data.Version));
            }

            var publishedAt = Now();
            var schemaBody = WithTenant(new Dictionary<string, object?>
            {
                ["id"] = formId,
                ["form_id"] = formId,
                ["schema_version"] = schemaVersion,
                ["status"] = "PUBLISHED",
                ["fields"] = fields.Select(FieldBody).ToArray(),
                ["workflow_rules"] = workflowRules.Select(RuleBody).ToArray(),
                ["plan_step_count"] = planSteps.Count,
                ["published_at"] = publishedAt,
                ["published_by"] = CurrentTenant.UserId ?? "system"
            });
            var stored = await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.FormSchemas, formId, schemaBody, existing.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Success(Conflict("CONCURRENT_UPDATE", "Published schema version could not be updated.", existing.Data?.Version ?? "unknown"));
            }

            var versionId = $"{formId}:{schemaVersion}";
            await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.PublishedSchemaVersions, versionId, schemaBody, null, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["id"] = formId,
                ["formId"] = formId,
                ["status"] = "PUBLISHED",
                ["version"] = stored.Data!.Version,
                ["schemaVersion"] = schemaVersion,
                ["fieldCount"] = fields.Count,
                ["workflowRuleCount"] = workflowRules.Count
            };
            await StoreAuditAsync($"schema:{versionId}", "SCHEMA_PUBLISHED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(DynamicFormsWorkflowsConstants.SchemaPublishedEvent, formId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new DynamicFormsWorkflowsOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> SubmitFormAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "submissionId", "formId");
        var values = ReadDictionary(request, "values");
        if (values.Count == 0)
        {
            validation.Add(FieldError("values", "Submission values are required."));
        }
        if (validation.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var submissionId = ReadString(request, "submissionId")!.Trim();
        var formId = ReadString(request, "formId")!.Trim();
        var operationKey = $"{DynamicFormsWorkflowsConstants.FlowSlug}:submission:{CurrentTenant.TenantId}:{submissionId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var schema = await _documents.GetAsync(DynamicFormsWorkflowsConstants.FormSchemas, formId, cancellationToken).ConfigureAwait(false);
        if (schema.Data is null)
        {
            return Success(new DynamicFormsWorkflowsOutcome(404, ErrorBody("SCHEMA_NOT_FOUND", "Published form schema was not found."), false, false));
        }
        if (!BelongsToCurrentTenant(schema.Data.Body))
        {
            return Success(new DynamicFormsWorkflowsOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this form schema."), false, false));
        }

        var fields = ReadFields(schema.Data.Body, "fields");
        var role = CurrentTenant.Roles.FirstOrDefault() ?? "anonymous";
        var fieldErrors = ValidateSubmissionValues(fields, values, role);
        if (!DynamicFormsWorkflowsConstants.OperationsRoles.Contains(role, StringComparer.Ordinal) &&
            values.Keys.Any(key => string.Equals(key, "adminNotes", StringComparison.OrdinalIgnoreCase)))
        {
            fieldErrors.Add(FieldError("adminNotes", "Field is not visible to the current role."));
        }
        if (fieldErrors.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(fieldErrors), false, false));
        }

        var body = WithTenant(new Dictionary<string, object?>
        {
            ["id"] = submissionId,
            ["submission_id"] = submissionId,
            ["form_id"] = formId,
            ["schema_version"] = ReadString(schema.Data.Body, "schema_version"),
            ["values"] = values,
            ["viewer_role"] = role,
            ["status"] = "SUBMITTED",
            ["submitted_at"] = Now()
        });
        var stored = await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.FormSubmissions, submissionId, body, null, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["id"] = submissionId,
            ["submissionId"] = submissionId,
            ["formId"] = formId,
            ["status"] = "SUBMITTED",
            ["version"] = stored.Data!.Version
        };
        await StoreAuditAsync($"submission:{submissionId}", "SUBMISSION_ACCEPTED", response, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(DynamicFormsWorkflowsConstants.SubmissionAcceptedEvent, submissionId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DynamicFormsWorkflowsOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> DispatchAutomationAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "submissionId", "ruleId");
        if (validation.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var submissionId = ReadString(request, "submissionId")!.Trim();
        var ruleId = ReadString(request, "ruleId")!.Trim();
        var operationKey = $"{DynamicFormsWorkflowsConstants.FlowSlug}:automation:{CurrentTenant.TenantId}:{submissionId}:{ruleId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var submission = await _documents.GetAsync(DynamicFormsWorkflowsConstants.FormSubmissions, submissionId, cancellationToken).ConfigureAwait(false);
        if (submission.Data is null)
        {
            return Success(new DynamicFormsWorkflowsOutcome(404, ErrorBody("SUBMISSION_NOT_FOUND", "Submission was not found."), false, false));
        }
        if (!BelongsToCurrentTenant(submission.Data.Body))
        {
            return Success(new DynamicFormsWorkflowsOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this submission."), false, false));
        }

        var lockId = $"{submissionId}:{ruleId}";
        var gate = GetResourceLock($"automation:{CurrentTenant.TenantId}:{lockId}");
        if (!gate.Wait(0))
        {
            return Success(Conflict("CONCURRENT_UPDATE", "Automation dispatch lock is already held.", "locked"));
        }

        try
        {
            var existing = await _documents.GetAsync(DynamicFormsWorkflowsConstants.AutomationLocks, lockId, cancellationToken).ConfigureAwait(false);
            if (existing.Data is not null)
            {
                var duplicate = new Dictionary<string, object?>
                {
                    ["id"] = lockId,
                    ["submissionId"] = submissionId,
                    ["ruleId"] = ruleId,
                    ["status"] = "AUTOMATION_ALREADY_DISPATCHED"
                };
                await _idempotency.MarkSucceededAsync(operationKey, duplicate, cancellationToken).ConfigureAwait(false);
                return Success(new DynamicFormsWorkflowsOutcome(200, duplicate, true, false));
            }

            var branch = ResolveAutomationBranch(request, submission.Data.Body);
            var lockBody = WithTenant(new Dictionary<string, object?>
            {
                ["id"] = lockId,
                ["submission_id"] = submissionId,
                ["rule_id"] = ruleId,
                ["order"] = 3,
                ["setnx_equivalent"] = true,
                ["branch"] = branch,
                ["locked_at"] = Now()
            });
            await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.AutomationLocks, lockId, lockBody, null, cancellationToken).ConfigureAwait(false);
            await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.AutomationDispatches, lockId, lockBody, null, cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?>
            {
                ["id"] = lockId,
                ["submissionId"] = submissionId,
                ["ruleId"] = ruleId,
                ["status"] = "AUTOMATION_DISPATCHED",
                ["branch"] = branch,
                ["order"] = 3
            };
            await StoreAuditAsync($"automation:{lockId}", "AUTOMATION_DISPATCHED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(DynamicFormsWorkflowsConstants.AutomationDispatchedEvent, lockId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new DynamicFormsWorkflowsOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<DynamicFormsWorkflowsOutcome>> CollectAnalyticsAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "submissionId", "metricName");
        var metrics = ReadDictionary(request, "metrics");
        if (ContainsPii(metrics, out var piiField) || ContainsPii(request, out piiField))
        {
            validation.Add(FieldError(piiField, "PII is forbidden in analytics payloads. Submit aggregate counters only."));
        }
        if (validation.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var submissionId = ReadString(request, "submissionId")!.Trim();
        var metricName = ReadString(request, "metricName")!.Trim();
        var operationKey = $"{DynamicFormsWorkflowsConstants.FlowSlug}:analytics:{CurrentTenant.TenantId}:{submissionId}:{metricName}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var analyticsId = $"{submissionId}:{metricName}:{Guid.NewGuid():N}";
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["id"] = analyticsId,
            ["submission_id"] = submissionId,
            ["metric_name"] = metricName,
            ["metrics"] = metrics,
            ["pii_persisted"] = false,
            ["recorded_at"] = Now()
        });
        await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.AnalyticsEvents, analyticsId, body, null, cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["id"] = analyticsId,
            ["submissionId"] = submissionId,
            ["status"] = "ANALYTICS_RECORDED",
            ["piiPersisted"] = false
        };
        await _outbox.AppendAsync(CreateEvent(DynamicFormsWorkflowsConstants.AnalyticsCollectedEvent, analyticsId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DynamicFormsWorkflowsOutcome(201, response, true, true));
    }

    public DataProcessResult<DynamicFormsWorkflowsOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> body)
    {
        if (RejectsTenantScope(body) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var steps = ReadPlanSteps(body);
        var errors = new List<Dictionary<string, object?>>();
        ValidatePlanStepCoverage(steps, errors);
        if (errors.Count > 0)
        {
            return Success(new DynamicFormsWorkflowsOutcome(400, ValidationBody(errors), false, false));
        }

        return Success(new DynamicFormsWorkflowsOutcome(200, new Dictionary<string, object?>
        {
            ["status"] = "PLAN_COVERED",
            ["stepCount"] = steps.Count,
            ["requiredTaskTypes"] = DynamicFormsWorkflowsConstants.RequiredPlanTaskTypes.Length,
            ["enforcementTaskTypes"] = DynamicFormsWorkflowsConstants.EnforcementTaskTypes.Length
        }, false, false));
    }

    public async Task<DataProcessResult<DynamicFormsWorkflowsDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var schemas = await SearchTenantAsync(DynamicFormsWorkflowsConstants.FormSchemas, cancellationToken).ConfigureAwait(false);
        var submissions = await SearchTenantAsync(DynamicFormsWorkflowsConstants.FormSubmissions, cancellationToken).ConfigureAwait(false);
        var automations = await SearchTenantAsync(DynamicFormsWorkflowsConstants.AutomationDispatches, cancellationToken).ConfigureAwait(false);
        var analytics = await SearchTenantAsync(DynamicFormsWorkflowsConstants.AnalyticsEvents, cancellationToken).ConfigureAwait(false);
        var schemaBodies = (schemas.Data ?? []).Select(record => record.Body).ToArray();
        var submissionBodies = (submissions.Data ?? []).Select(record => record.Body).ToArray();
        var automationBodies = (automations.Data ?? []).Select(record => record.Body).ToArray();
        var analyticsBodies = (analytics.Data ?? []).Select(record => record.Body).ToArray();
        return Success(new DynamicFormsWorkflowsDashboard(
            schemaBodies,
            submissionBodies,
            automationBodies,
            analyticsBodies,
            new Dictionary<string, object?>
            {
                ["publishedSchemas"] = schemaBodies.Length,
                ["submissions"] = submissionBodies.Length,
                ["automationDispatches"] = automationBodies.Length,
                ["analyticsEvents"] = analyticsBodies.Length,
                ["piiPersisted"] = false
            }));
    }

    private static void ValidatePlanStepCoverage(IReadOnlyList<DynamicFormsPlanStep> steps, List<Dictionary<string, object?>> errors)
    {
        var allRequired = DynamicFormsWorkflowsConstants.RequiredPlanTaskTypes.Concat(DynamicFormsWorkflowsConstants.EnforcementTaskTypes).ToArray();
        var grouped = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (var taskType in allRequired)
        {
            if (!grouped.TryGetValue(taskType, out var count))
            {
                errors.Add(FieldError($"planSteps.{taskType}", "Required task type has no plan step."));
                continue;
            }
            if (count != 1)
            {
                errors.Add(FieldError($"planSteps.{taskType}", "Each task type must map to exactly one plan step."));
            }
        }

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Responsibility))
            {
                errors.Add(FieldError($"planSteps.{step.TaskTypeId}.responsibility", "Plan step must state a single responsibility."));
            }
        }
    }

    private static List<Dictionary<string, object?>> ValidateSubmissionValues(
        IReadOnlyList<FormFieldDefinition> fields,
        IReadOnlyDictionary<string, object?> values,
        string role)
    {
        var errors = new List<Dictionary<string, object?>>();
        foreach (var field in fields)
        {
            var visible = field.VisibleToRoles.Count == 0 || field.VisibleToRoles.Contains(role, StringComparer.Ordinal);
            if (!visible && values.ContainsKey(field.Key))
            {
                errors.Add(FieldError(field.Key, "Field is not visible to the current role."));
                continue;
            }
            if (field.Required && visible && (!values.TryGetValue(field.Key, out var value) || IsBlank(value)))
            {
                errors.Add(FieldError(field.Key, "Required field is missing."));
                continue;
            }
            if (values.TryGetValue(field.Key, out var submitted) && !IsValidType(field.Type, submitted))
            {
                errors.Add(FieldError(field.Key, $"Expected {field.Type} value."));
            }
        }
        return errors;
    }

    private static bool IsValidType(string type, object? value) => type.ToLowerInvariant() switch
    {
        "string" or "text" => value is string || value is JsonElement { ValueKind: JsonValueKind.String },
        "number" => double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        "boolean" or "bool" => value is bool || value is JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False },
        _ => true
    };

    private SemaphoreSlim GetResourceLock(string key) => _resourceLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private async Task<DynamicFormsWorkflowsOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new DynamicFormsWorkflowsOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new DynamicFormsWorkflowsOutcome(200, reservation.Data.CachedResponse, true, false)
            : null;
    }

    private async Task StoreAuditAsync(string id, string auditType, IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(DynamicFormsWorkflowsConstants.AuditRecords, id, WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = Hash(JsonSerializer.Serialize(body)),
            ["created_at"] = Now(),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataProcessResult<IReadOnlyList<DocumentRecord>>> SearchTenantAsync(string collection, CancellationToken cancellationToken) =>
        await _documents.SearchAsync(collection, new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false);

    private IReadOnlyDictionary<string, object?> WithTenant(IReadOnlyDictionary<string, object?> body)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = CurrentTenant.TenantId
        };
        return copy;
    }

    private bool BelongsToCurrentTenant(IReadOnlyDictionary<string, object?> body) =>
        string.Equals(ReadString(body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal);

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            DynamicFormsWorkflowsConstants.FlowId,
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["tenantid"] = CurrentTenant.TenantId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId,
                ["xiigenflowid"] = DynamicFormsWorkflowsConstants.FlowId
            });

    private static DynamicFormsWorkflowsOutcome Conflict(string code, string message, string? currentVersion) =>
        new(409, ErrorBody(code, message, new Dictionary<string, object?> { ["currentVersion"] = currentVersion }), false, false);

    private static DynamicFormsWorkflowsOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new DynamicFormsWorkflowsOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static DynamicFormsWorkflowsOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key => ForbiddenPayloadKeys.Any(forbiddenKey => string.Equals(key, forbiddenKey, StringComparison.OrdinalIgnoreCase)));
        return forbidden is null
            ? null
            : new DynamicFormsWorkflowsOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
    }

    private static string ResolveAutomationBranch(IReadOnlyDictionary<string, object?> request, IReadOnlyDictionary<string, object?> submission)
    {
        var requested = ReadString(request, "branch");
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        var values = ReadDictionary(submission, "values");
        return ReadString(values, "priority") switch
        {
            "urgent" or "high" => "accelerated-review",
            _ => "standard-review"
        };
    }

    private static IReadOnlyDictionary<string, object?> FieldBody(FormFieldDefinition field) =>
        new Dictionary<string, object?>
        {
            ["key"] = field.Key,
            ["type"] = field.Type,
            ["required"] = field.Required,
            ["visibleToRoles"] = field.VisibleToRoles.ToArray(),
            ["rules"] = field.Rules
        };

    private static IReadOnlyDictionary<string, object?> RuleBody(WorkflowRuleDefinition rule) =>
        new Dictionary<string, object?>
        {
            ["ruleId"] = rule.RuleId,
            ["trigger"] = rule.Trigger,
            ["targetState"] = rule.TargetState,
            ["condition"] = rule.Condition
        };

    private static List<FormFieldDefinition> ReadFields(IReadOnlyDictionary<string, object?> body, string key = "fields") =>
        ReadArray(body, key).Select(item => new FormFieldDefinition(
            ReadString(item, "key") ?? ReadString(item, "fieldKey") ?? string.Empty,
            ReadString(item, "type") ?? "string",
            ReadBool(item, "required"),
            ReadStringArray(item, "visibleToRoles"),
            ReadDictionary(item, "rules"))).Where(field => !string.IsNullOrWhiteSpace(field.Key)).ToList();

    private static List<WorkflowRuleDefinition> ReadWorkflowRules(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "workflowRules").Select(item => new WorkflowRuleDefinition(
            ReadString(item, "ruleId") ?? ReadString(item, "id") ?? string.Empty,
            ReadString(item, "trigger") ?? "submit",
            ReadString(item, "targetState") ?? "submitted",
            ReadDictionary(item, "condition"))).Where(rule => !string.IsNullOrWhiteSpace(rule.RuleId)).ToList();

    private static List<DynamicFormsPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "planSteps").Select(item => new DynamicFormsPlanStep(
            ReadString(item, "id") ?? string.Empty,
            ReadString(item, "taskTypeId") ?? ReadString(item, "task_type_id") ?? string.Empty,
            ReadString(item, "name") ?? string.Empty,
            ReadString(item, "responsibility") ?? string.Empty,
            ReadDictionary(item, "config"))).ToList();

    private static List<IReadOnlyDictionary<string, object?>> ReadArray(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> direct)
        {
            return direct.ToList();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(ReadJsonObject).ToList<IReadOnlyDictionary<string, object?>>();
        }
        if (value is object[] objects)
        {
            return objects.OfType<IReadOnlyDictionary<string, object?>>().ToList();
        }
        return [];
    }

    private static IReadOnlyDictionary<string, object?> ReadDictionary(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>();
        }
        if (value is IReadOnlyDictionary<string, object?> direct)
        {
            return direct;
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return ReadJsonObject(element);
        }
        return new Dictionary<string, object?>();
    }

    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }
        if (value is IEnumerable<string> strings)
        {
            return strings.Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => item.ToString()).Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
        }
        return [];
    }

    private static IReadOnlyDictionary<string, object?> ReadJsonObject(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ReadJsonValue(prop.Value))
            : new Dictionary<string, object?>();

    private static object? ReadJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
        JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
        JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => ReadJsonObject(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ReadJsonValue).ToArray(),
        _ => value.ToString()
    };

    private static List<Dictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields)
    {
        var errors = new List<Dictionary<string, object?>>();
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(ReadString(request, field)))
            {
                errors.Add(FieldError(field, "Value is required."));
            }
        }
        return errors;
    }

    private static IReadOnlyDictionary<string, object?> ValidationBody(IEnumerable<Dictionary<string, object?>> details) =>
        new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details.ToArray() };

    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message, IReadOnlyDictionary<string, object?>? extra = null)
    {
        var body = new Dictionary<string, object?> { ["code"] = code, ["message"] = message };
        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                body[pair.Key] = pair.Value;
            }
        }
        return body;
    }

    private static Dictionary<string, object?> FieldError(string field, string reason) =>
        new() { ["field"] = field, ["reason"] = reason };

    private static bool ContainsPii(IReadOnlyDictionary<string, object?> body, out string sensitiveField)
    {
        foreach (var pair in body)
        {
            if (PiiKeys.Any(key => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)))
            {
                sensitiveField = pair.Key;
                return true;
            }
            if (pair.Value is IReadOnlyDictionary<string, object?> nested && ContainsPii(nested, out sensitiveField))
            {
                return true;
            }
            if (pair.Value is JsonElement element && element.ValueKind == JsonValueKind.Object &&
                ContainsPii(ReadJsonObject(element), out sensitiveField))
            {
                return true;
            }
        }

        sensitiveField = string.Empty;
        return false;
    }

    private static bool IsBlank(object? value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text) || value is JsonElement { ValueKind: JsonValueKind.Null };

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? ReadString(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }
        return value is JsonElement element ? element.ToString() : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && (value is bool boolValue && boolValue || value is JsonElement { ValueKind: JsonValueKind.True });
}
