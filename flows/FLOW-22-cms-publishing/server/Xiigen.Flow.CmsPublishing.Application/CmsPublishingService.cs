using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.CmsPublishing.Application;

public sealed class CmsPublishingService : MicroserviceBase<CmsPublishingService>
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
        "contentTenantId",
        "content_tenant_id"
    ];

    private static readonly string[] PiiKeys =
    [
        "authorEmail",
        "authorIpAddress",
        "editorNotes",
        "email",
        "phone",
        "firstName",
        "lastName",
        "fullName",
        "name",
        "address",
        "ipAddress"
    ];

    private static readonly IReadOnlyDictionary<string, string> StageRoleMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["EDITOR_REVIEW"] = "content_editor",
        ["LEGAL_REVIEW"] = "legal_reviewer",
        ["FINAL_APPROVAL"] = "publisher"
    };

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new(StringComparer.Ordinal);

    public CmsPublishingService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<CmsPublishingService> logger)
        : base(
            new ServiceDescriptor("cms-publishing-service", "CMS Publishing", CmsPublishingConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<CmsPublishingOutcome>> PublishContentAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "contentId", "title", "body", "slug");
        var title = ReadString(request, "title");
        var body = ReadString(request, "body");
        var slug = ReadString(request, "slug");
        if (string.IsNullOrWhiteSpace(title))
        {
            validation.Add(FieldError("title", "Title is required."));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            validation.Add(FieldError("body", "Body is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            validation.Add(FieldError("slug", "Slug is required."));
        }

        if (validation.Count > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?>
            {
                ["code"] = "VALIDATION_FAILED",
                ["details"] = validation
            }, stateWritten: false));
        }

        if (RequireIdempotency(idempotencyKey) is { } missingIdempotency)
        {
            return Success(missingIdempotency);
        }

        var operationKey = OperationKey("publish", idempotencyKey!);
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<CmsPublishingOutcome>(reservation.Error!.Code, reservation.Error.Message, reservation.Error.Details);
        }

        if (reservation.Data!.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(Outcome(200, reservation.Data.CachedResponse, stateWritten: false));
        }

        var contentId = ReadString(request, "contentId")!;
        var lockKey = ResourceKey("content", contentId);
        var gate = _resourceLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await _documents.GetAsync(CmsPublishingConstants.ContentCollection, ContentDocumentId(contentId), cancellationToken).ConfigureAwait(false);
            if (existing.IsFailure)
            {
                return Failure<CmsPublishingOutcome>(existing.Error!.Code, existing.Error.Message, existing.Error.Details);
            }

            if (existing.Data is not null && string.Equals(existing.Data.Body.GetValueOrDefault("status") as string, "PUBLISHED", StringComparison.Ordinal))
            {
                var immutable = Outcome(409, new Dictionary<string, object?>
                {
                    ["code"] = "CONTENT_IMMUTABLE",
                    ["contentId"] = contentId,
                    ["currentVersion"] = existing.Data.Version
                }, stateWritten: false);
                await _idempotency.MarkFailedAsync(operationKey, "CONTENT_IMMUTABLE", "Published content cannot be modified.", cancellationToken).ConfigureAwait(false);
                return Success(immutable);
            }

            if (existing.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, existing.Data.Version, StringComparison.Ordinal))
            {
                var conflict = Outcome(409, new Dictionary<string, object?>
                {
                    ["code"] = "CONCURRENT_UPDATE",
                    ["currentVersion"] = existing.Data.Version
                }, stateWritten: false);
                await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Document version does not match.", cancellationToken).ConfigureAwait(false);
                return Success(conflict);
            }

            var now = DateTimeOffset.UtcNow;
            var content = new Dictionary<string, object?>
            {
                ["contentId"] = contentId,
                ["title"] = title,
                ["body"] = body,
                ["slug"] = slug,
                ["status"] = "PUBLISHED",
                ["tenantId"] = CurrentTenant.TenantId,
                ["versionNumber"] = existing.Data is null ? 1 : int.Parse(existing.Data.Version, CultureInfo.InvariantCulture) + 1,
                ["publishedAt"] = now.ToString("O", CultureInfo.InvariantCulture),
                ["immutabilityLocked"] = true,
                ["knowledgeScope"] = "PRIVATE"
            };

            var upsert = await _documents.UpsertAsync(
                CmsPublishingConstants.ContentCollection,
                ContentDocumentId(contentId),
                content,
                existing.Data is null ? null : existing.Data.Version,
                cancellationToken).ConfigureAwait(false);
            if (upsert.IsFailure)
            {
                var conflict = Outcome(409, new Dictionary<string, object?>
                {
                    ["code"] = "CONCURRENT_UPDATE",
                    ["currentVersion"] = upsert.Error!.Details?.GetValueOrDefault("currentVersion")
                }, stateWritten: false);
                await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Document version does not match.", cancellationToken).ConfigureAwait(false);
                return Success(conflict);
            }

            await WriteAuditAsync("CONTENT_PUBLISHED", contentId, new Dictionary<string, object?>
            {
                ["version"] = upsert.Data!.Version,
                ["slug"] = slug
            }, cancellationToken).ConfigureAwait(false);

            var envelope = Envelope("xiigen.cms.content.published", contentId, new Dictionary<string, object?>
            {
                ["contentId"] = contentId,
                ["slug"] = slug,
                ["version"] = upsert.Data.Version
            });
            await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);

            if (ReadBool(request, "simulateTimeout"))
            {
                return Success(Outcome(504, new Dictionary<string, object?>
                {
                    ["code"] = "TIMEOUT",
                    ["retryAfterMs"] = 5000,
                    ["outboxRetained"] = true
                }, stateWritten: true, outboxRetained: true));
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = contentId,
                ["version"] = upsert.Data.Version,
                ["status"] = "PUBLISHED",
                ["slug"] = slug,
                ["publishedAt"] = now.ToString("O", CultureInfo.InvariantCulture)
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(201, response, stateWritten: true, outboxRetained: true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<CmsPublishingOutcome>> RecordApprovalAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "contentId", "stage", "decision");
        var stage = ReadString(request, "stage") ?? string.Empty;
        var decision = ReadString(request, "decision") ?? string.Empty;
        if (!StageRoleMap.ContainsKey(stage))
        {
            validation.Add(FieldError("stage", "Unknown approval stage."));
        }

        if (!string.Equals(decision, "APPROVE", StringComparison.Ordinal) && !string.Equals(decision, "REJECT", StringComparison.Ordinal))
        {
            validation.Add(FieldError("decision", "Decision must be APPROVE or REJECT."));
        }

        if (validation.Count > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = validation }, false));
        }

        var requiredRole = StageRoleMap[stage];
        if (!CurrentTenant.Roles.Contains(requiredRole) && !CurrentTenant.Roles.Contains("tenant_admin") && !CurrentTenant.Roles.Contains("platform_admin"))
        {
            return Success(Outcome(403, new Dictionary<string, object?>
            {
                ["code"] = "WRONG_ROLE",
                ["requiredRole"] = requiredRole
            }, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } missingIdempotency)
        {
            return Success(missingIdempotency);
        }

        var contentId = ReadString(request, "contentId")!;
        var operationKey = OperationKey("approval", idempotencyKey!);
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.Data!.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(Outcome(200, reservation.Data.CachedResponse, false));
        }

        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var approvalId = $"{CurrentTenant.TenantId}:{contentId}:{stage}";
        var body = new Dictionary<string, object?>
        {
            ["approvalId"] = approvalId,
            ["contentId"] = contentId,
            ["stage"] = stage,
            ["decision"] = decision,
            ["tenantId"] = CurrentTenant.TenantId,
            ["decidedBy"] = CurrentTenant.UserId,
            ["decidedAt"] = now,
            ["knowledgeScope"] = "PRIVATE"
        };
        await _documents.UpsertAsync(CmsPublishingConstants.ApprovalCollection, approvalId, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        await WriteAuditAsync(decision == "APPROVE" ? "STAGE_APPROVED" : "STAGE_REJECTED", contentId, body, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope(decision == "APPROVE" ? "xiigen.cms.content.approved" : "xiigen.cms.content.rejected", contentId, body), cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["id"] = approvalId,
            ["contentId"] = contentId,
            ["stage"] = stage,
            ["decision"] = decision,
            ["decidedAt"] = now
        };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<CmsPublishingOutcome>> SchedulePublishAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "contentId", "publishAt");
        var publishAtText = ReadString(request, "publishAt");
        if (!DateTimeOffset.TryParse(publishAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publishAt) || publishAt <= DateTimeOffset.UtcNow)
        {
            validation.Add(FieldError("publishAt", "publishAt must be a future ISO-8601 timestamp."));
        }

        if (validation.Count > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = validation }, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } missingIdempotency)
        {
            return Success(missingIdempotency);
        }

        var contentId = ReadString(request, "contentId")!;
        var operationKey = OperationKey("schedule", $"{contentId}:{publishAt:O}");
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data!.Status != "reserved")
        {
            return Success(Outcome(200, new Dictionary<string, object?>
            {
                ["code"] = "CONTENT_ALREADY_SCHEDULED",
                ["contentId"] = contentId,
                ["publishAt"] = publishAt.ToString("O", CultureInfo.InvariantCulture)
            }, false));
        }

        var lockId = $"{CurrentTenant.TenantId}:{contentId}:{publishAt:O}";
        var body = new Dictionary<string, object?>
        {
            ["lockId"] = lockId,
            ["contentId"] = contentId,
            ["tenantId"] = CurrentTenant.TenantId,
            ["publishAt"] = publishAt.ToString("O", CultureInfo.InvariantCulture),
            ["expiresAt"] = DateTimeOffset.UtcNow.AddHours(24).ToString("O", CultureInfo.InvariantCulture),
            ["knowledgeScope"] = "PRIVATE"
        };
        await _documents.UpsertAsync(CmsPublishingConstants.ScheduleLockCollection, lockId, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        await WriteAuditAsync("CONTENT_SCHEDULED", contentId, body, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.cms.content.scheduled", contentId, body), cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["id"] = lockId,
            ["contentId"] = contentId,
            ["publishAt"] = publishAt.ToString("O", CultureInfo.InvariantCulture),
            ["status"] = "SCHEDULED"
        };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("schedule-http", idempotencyKey!), response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<CmsPublishingOutcome>> RecordAnalyticsAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "contentId", "metricType", "value");
        var metricType = ReadString(request, "metricType") ?? string.Empty;
        if (!new[] { "VIEW", "ENGAGEMENT", "SHARE" }.Contains(metricType, StringComparer.Ordinal))
        {
            validation.Add(FieldError("metricType", "Metric type must be VIEW, ENGAGEMENT, or SHARE."));
        }

        foreach (var key in request.Keys)
        {
            if (PiiKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                validation.Add(FieldError(key, "PII is not accepted in analytics records."));
            }
        }

        if (validation.Count > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = validation }, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } missingIdempotency)
        {
            return Success(missingIdempotency);
        }

        var operationKey = OperationKey("analytics", idempotencyKey!);
        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.Data!.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(Outcome(200, reservation.Data.CachedResponse, false));
        }

        var contentId = ReadString(request, "contentId")!;
        var metricId = $"{CurrentTenant.TenantId}:{contentId}:{metricType}:{Guid.NewGuid():N}";
        var body = new Dictionary<string, object?>
        {
            ["metricId"] = metricId,
            ["contentId"] = contentId,
            ["metricType"] = metricType,
            ["value"] = request["value"],
            ["userHash"] = HashUser(ReadString(request, "userId") ?? "anonymous"),
            ["tenantId"] = CurrentTenant.TenantId,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["piiScrubbed"] = true,
            ["knowledgeScope"] = "PRIVATE"
        };

        await _documents.UpsertAsync(CmsPublishingConstants.AnalyticsCollection, metricId, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.cms.analytics.recorded", contentId, body), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?>
        {
            ["id"] = metricId,
            ["contentId"] = contentId,
            ["metricType"] = metricType,
            ["piiScrubbed"] = true
        };
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public DataProcessResult<CmsPublishingOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskTypes = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = CmsPublishingConstants.RequiredTaskTypes.Where(task => !taskTypes.Contains(task)).ToArray();
        var multiResponsibility = steps.Where(step => step.Responsibility.Contains(" and ", StringComparison.OrdinalIgnoreCase) || step.Responsibility.Contains(',', StringComparison.Ordinal)).Select(step => step.Id).ToArray();

        if (missing.Length > 0 || multiResponsibility.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?>
            {
                ["code"] = "PLAN_COVERAGE_FAILED",
                ["missingTaskTypes"] = missing,
                ["multiResponsibilitySteps"] = multiResponsibility
            }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?>
        {
            ["code"] = "PLAN_COVERAGE_OK",
            ["taskTypeCount"] = taskTypes.Count,
            ["singleResponsibility"] = true,
            ["fabricFirst"] = true,
            ["dynamicEndpointOnly"] = true,
            ["dataProcessResult"] = true
        }, false));
    }

    public async Task<DataProcessResult<CmsPublishingDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = CurrentTenant.TenantId;
        var filter = new Dictionary<string, object?> { ["tenantId"] = tenantId };
        var content = await _documents.SearchAsync(CmsPublishingConstants.ContentCollection, filter, cancellationToken).ConfigureAwait(false);
        var approvals = await _documents.SearchAsync(CmsPublishingConstants.ApprovalCollection, filter, cancellationToken).ConfigureAwait(false);
        var schedules = await _documents.SearchAsync(CmsPublishingConstants.ScheduleLockCollection, filter, cancellationToken).ConfigureAwait(false);
        var analytics = await _documents.SearchAsync(CmsPublishingConstants.AnalyticsCollection, filter, cancellationToken).ConfigureAwait(false);

        return Success(new CmsPublishingDashboard(
            content.Data?.Select(ToDictionary).ToArray() ?? [],
            approvals.Data?.Select(ToDictionary).ToArray() ?? [],
            schedules.Data?.Select(ToDictionary).ToArray() ?? [],
            analytics.Data?.Select(ToDictionary).ToArray() ?? [],
            new Dictionary<string, object?>
            {
                ["contentCount"] = content.Data?.Count ?? 0,
                ["approvalCount"] = approvals.Data?.Count ?? 0,
                ["scheduledCount"] = schedules.Data?.Count ?? 0,
                ["analyticsCount"] = analytics.Data?.Count ?? 0
            }));
    }

    private async Task WriteAuditAsync(string action, string contentId, IReadOnlyDictionary<string, object?> metadata, CancellationToken cancellationToken)
    {
        var auditId = $"{CurrentTenant.TenantId}:{contentId}:{action}:{Guid.NewGuid():N}";
        var audit = new Dictionary<string, object?>
        {
            ["auditId"] = auditId,
            ["tenantId"] = CurrentTenant.TenantId,
            ["contentId"] = contentId,
            ["action"] = action,
            ["metadata"] = metadata,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["knowledgeScope"] = "PRIVATE"
        };
        await _documents.UpsertAsync(CmsPublishingConstants.AuditCollection, auditId, audit, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/flows/{CmsPublishingConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = CmsPublishingConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private CmsPublishingOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        foreach (var key in request.Keys)
        {
            if (ForbiddenPayloadKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                return Outcome(400, new Dictionary<string, object?>
                {
                    ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED",
                    ["details"] = new[] { FieldError(key, "Tenant scope is resolved by TenantContext, never request payload.") }
                }, stateWritten: false);
            }
        }

        return null;
    }

    private static CmsPublishingOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? Outcome(400, new Dictionary<string, object?>
            {
                ["code"] = "IDEMPOTENCY_KEY_REQUIRED",
                ["details"] = new[] { FieldError("X-Idempotency-Key", "Mutating CMS requests require an idempotency key.") }
            }, false)
            : null;

    private static CmsPublishingOutcome Outcome(int httpStatus, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) =>
        new(httpStatus, body, stateWritten, outboxRetained);

    private string OperationKey(string operation, string key) => $"{CmsPublishingConstants.FlowId}:{CurrentTenant.TenantId}:{operation}:{key}";

    private string ResourceKey(string operation, string id) => $"{CurrentTenant.TenantId}:{operation}:{id}";

    private string ContentDocumentId(string contentId) => $"{CurrentTenant.TenantId}:{contentId}";

    private static IReadOnlyDictionary<string, object?> ToDictionary(DocumentRecord record)
    {
        var copy = new Dictionary<string, object?>(record.Body)
        {
            ["id"] = record.Id,
            ["version"] = record.Version,
            ["updatedAt"] = record.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)
        };
        return copy;
    }

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields)
    {
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var field in fields)
        {
            if (!request.TryGetValue(field, out var value) || value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                errors.Add(FieldError(field, "Required field is missing."));
            }
        }

        return errors;
    }

    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) =>
        new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };

    private static string? ReadString(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            JsonElement element => element.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool flag => flag,
            JsonElement element when element.ValueKind == JsonValueKind.True => true,
            JsonElement element when element.ValueKind == JsonValueKind.False => false,
            string text => bool.TryParse(text, out var parsed) && parsed,
            _ => false
        };
    }

    private static IReadOnlyList<CmsPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("planSteps", out var raw) || raw is null)
        {
            return [];
        }

        if (raw is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => ReadPlanStep(item)).Where(step => step is not null).Select(step => step!).ToArray();
        }

        if (raw is IEnumerable<object?> objects)
        {
            return objects.Select(ReadPlanStep).Where(step => step is not null).Select(step => step!).ToArray();
        }

        return [];
    }

    private static CmsPlanStep? ReadPlanStep(object? raw)
    {
        if (raw is JsonElement element)
        {
            var id = element.TryGetProperty("id", out var idElement) ? idElement.ToString() : string.Empty;
            var taskTypeId = element.TryGetProperty("taskTypeId", out var taskElement) ? taskElement.ToString() : string.Empty;
            var name = element.TryGetProperty("name", out var nameElement) ? nameElement.ToString() : taskTypeId;
            var responsibility = element.TryGetProperty("responsibility", out var responsibilityElement) ? responsibilityElement.ToString() : name;
            return string.IsNullOrWhiteSpace(taskTypeId) ? null : new CmsPlanStep(id, taskTypeId, name, responsibility, new Dictionary<string, object?>());
        }

        if (raw is IReadOnlyDictionary<string, object?> dict)
        {
            var taskTypeId = ReadString(dict, "taskTypeId") ?? string.Empty;
            return string.IsNullOrWhiteSpace(taskTypeId)
                ? null
                : new CmsPlanStep(ReadString(dict, "id") ?? taskTypeId, taskTypeId, ReadString(dict, "name") ?? taskTypeId, ReadString(dict, "responsibility") ?? taskTypeId, new Dictionary<string, object?>());
        }

        return null;
    }

    private static string HashUser(string userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}