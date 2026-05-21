using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.DurableSagasCompliance.Application;

public sealed class DurableSagasComplianceService : MicroserviceBase<DurableSagasComplianceService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StepLockTtl = TimeSpan.FromMinutes(10);
    private static readonly string[] ForbiddenPayloadKeys =
    [
        "tenantId",
        "tenant_id",
        "flowTenantId",
        "flow_tenant_id",
        "ownerTenantId",
        "owner_tenant_id",
        "masterTenantId",
        "master_tenant_id"
    ];

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new(StringComparer.Ordinal);

    public DurableSagasComplianceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DurableSagasComplianceService> logger)
        : base(
            new ServiceDescriptor("durable-sagas-compliance-service", "Durable Sagas Compliance", DurableSagasComplianceConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<DurableSagasComplianceOutcome>> StartSagaAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "sagaId", "sagaType");
        var steps = ReadSteps(request);
        validation.AddRange(ValidateSagaSteps(steps));
        if (validation.Count > 0)
        {
            return Success(new DurableSagasComplianceOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var sagaId = ReadString(request, "sagaId")!.Trim();
        var operationKey = $"{DurableSagasComplianceConstants.FlowSlug}:start:{CurrentTenant.TenantId}:{sagaId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = ErrorBody("TIMEOUT", "Saga state write timed out before completion.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["sagaId"] = sagaId
            });
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.SagaTimeoutRetainedEvent, sagaId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Saga state write timed out", cancellationToken).ConfigureAwait(false);
            return Success(new DurableSagasComplianceOutcome(504, timeoutBody, false, true));
        }

        var gate = GetResourceLock($"saga:{CurrentTenant.TenantId}:{sagaId}");
        if (!gate.Wait(0))
        {
            return Failure<DurableSagasComplianceOutcome>("CONCURRENT_UPDATE", "Concurrent saga write detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var current = await _documents.GetAsync(DurableSagasComplianceConstants.SagaStates, sagaId, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return Failure<DurableSagasComplianceOutcome>(current.Error!.Code, current.Error.Message, current.Error.Details);
            }

            if (current.Data is not null && !string.Equals(ReadString(current.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BOLA_VIOLATION", "Tenant does not own this saga run.");
                await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.SagaAlreadyRunningEvent, sagaId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new DurableSagasComplianceOutcome(403, rejected, true, true));
            }

            if (current.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<DurableSagasComplianceOutcome>("CONCURRENT_UPDATE", "Saga version does not match.", new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            if (current.Data is not null && string.Equals(ReadString(current.Data.Body, "status"), "RUNNING", StringComparison.Ordinal))
            {
                var alreadyRunning = new Dictionary<string, object?>
                {
                    ["id"] = sagaId,
                    ["sagaId"] = sagaId,
                    ["status"] = "RUNNING",
                    ["version"] = current.Data.Version
                };
                await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.SagaAlreadyRunningEvent, sagaId, alreadyRunning), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, alreadyRunning, cancellationToken).ConfigureAwait(false);
                return Success(new DurableSagasComplianceOutcome(200, alreadyRunning, false, true));
            }

            var startedAt = Now();
            var stepLockKey = $"step-lock:{sagaId}:0";
            await _documents.UpsertAsync(DurableSagasComplianceConstants.SagaStepLocks, stepLockKey, WithTenant(new Dictionary<string, object?>
            {
                ["lock_key"] = stepLockKey,
                ["saga_id"] = sagaId,
                ["step_index"] = 0,
                ["status"] = "HELD",
                ["locked_at"] = startedAt,
                ["expires_at"] = DateTimeOffset.UtcNow.Add(StepLockTtl).ToString("O", CultureInfo.InvariantCulture)
            }), null, cancellationToken).ConfigureAwait(false);

            var body = WithTenant(new Dictionary<string, object?>
            {
                ["saga_id"] = sagaId,
                ["saga_type"] = ReadString(request, "sagaType"),
                ["status"] = "RUNNING",
                ["current_step_index"] = 0,
                ["steps"] = steps.Select(StepToDictionary).ToArray(),
                ["compensation_stack"] = steps.Select(StepToCompensationDictionary).ToArray(),
                ["started_at"] = startedAt,
                ["updated_at"] = startedAt,
                ["knowledge_scope"] = "PRIVATE"
            });
            var stored = await _documents.UpsertAsync(DurableSagasComplianceConstants.SagaStates, sagaId, body, current.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<DurableSagasComplianceOutcome>("CONCURRENT_UPDATE", "Concurrent saga write detected.", new Dictionary<string, object?> { ["currentVersion"] = current.Data?.Version ?? "missing" });
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = sagaId,
                ["sagaId"] = sagaId,
                ["status"] = "RUNNING",
                ["version"] = stored.Data!.Version,
                ["currentStepIndex"] = 0,
                ["stepCount"] = steps.Count
            };
            await StoreAuditAsync(DurableSagasComplianceConstants.SagaAudits, $"saga:{sagaId}:{Guid.NewGuid():N}", "SAGA_STARTED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.SagaStartedEvent, sagaId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.SagaStepExecutedEvent, sagaId, new Dictionary<string, object?>(response) { ["stepIndex"] = 0 }), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new DurableSagasComplianceOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<DurableSagasComplianceOutcome>> CompensateSagaAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "sagaId");
        if (validation.Count > 0)
        {
            return Success(new DurableSagasComplianceOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var sagaId = ReadString(request, "sagaId")!.Trim();
        var operationKey = $"{DurableSagasComplianceConstants.FlowSlug}:compensate:{CurrentTenant.TenantId}:{sagaId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var saga = await _documents.GetAsync(DurableSagasComplianceConstants.SagaStates, sagaId, cancellationToken).ConfigureAwait(false);
        if (saga.Data is null)
        {
            return Success(new DurableSagasComplianceOutcome(404, ErrorBody("SAGA_NOT_FOUND", "Saga run was not found."), false, false));
        }
        if (!string.Equals(ReadString(saga.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
        {
            return Success(new DurableSagasComplianceOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this saga run."), false, false));
        }

        var stack = Enumerable.Reverse(ReadArray(saga.Data.Body, "compensation_stack")).ToArray();
        if (stack.Length == 0)
        {
            return Success(new DurableSagasComplianceOutcome(400, ErrorBody("COMPENSATION_STACK_EMPTY", "No compensation steps are registered."), false, false));
        }

        var executed = new List<Dictionary<string, object?>>();
        foreach (var step in stack)
        {
            var stepId = ReadString(step, "stepId") ?? ReadString(step, "step_id") ?? string.Empty;
            var lockKey = $"comp-lock:{sagaId}:{stepId}";
            var existingLock = await _documents.GetAsync(DurableSagasComplianceConstants.CompensationLocks, lockKey, cancellationToken).ConfigureAwait(false);
            if (existingLock.Data is not null && string.Equals(ReadString(existingLock.Data.Body, "status"), "HELD", StringComparison.Ordinal))
            {
                continue;
            }

            await _documents.UpsertAsync(DurableSagasComplianceConstants.CompensationLocks, lockKey, WithTenant(new Dictionary<string, object?>
            {
                ["lock_key"] = lockKey,
                ["saga_id"] = sagaId,
                ["step_id"] = stepId,
                ["status"] = "HELD",
                ["locked_at"] = Now()
            }), existingLock.Data?.Version, cancellationToken).ConfigureAwait(false);

            if (string.Equals(ReadString(request, "simulateCompensationFailureAt"), stepId, StringComparison.Ordinal))
            {
                var failure = ErrorBody("COMPENSATION_FAILED", "Compensation stopped on the first failed step.", new Dictionary<string, object?> { ["failedStep"] = stepId, ["executed"] = executed.ToArray() });
                await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.CompensationFailedEvent, sagaId, failure), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, "COMPENSATION_FAILED", "Compensation failed", cancellationToken).ConfigureAwait(false);
                return Success(new DurableSagasComplianceOutcome(409, failure, true, true));
            }

            var record = WithTenant(new Dictionary<string, object?>
            {
                ["saga_id"] = sagaId,
                ["step_id"] = stepId,
                ["status"] = "COMPENSATED",
                ["compensated_at"] = Now(),
                ["knowledge_scope"] = "PRIVATE"
            });
            await _documents.UpsertAsync(DurableSagasComplianceConstants.CompensationRecords, $"comp:{sagaId}:{stepId}", record, null, cancellationToken).ConfigureAwait(false);
            var emitted = new Dictionary<string, object?> { ["sagaId"] = sagaId, ["stepId"] = stepId, ["status"] = "COMPENSATED" };
            executed.Add(emitted);
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.CompensationStepExecutedEvent, sagaId, emitted), cancellationToken).ConfigureAwait(false);
        }

        var response = new Dictionary<string, object?>
        {
            ["id"] = sagaId,
            ["sagaId"] = sagaId,
            ["status"] = "COMPENSATION_COMPLETED",
            ["executed"] = executed.ToArray()
        };
        await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.CompensationCompletedEvent, sagaId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DurableSagasComplianceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<DurableSagasComplianceOutcome>> WriteComplianceRecordAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "sagaId", "eventType");
        if (validation.Count > 0)
        {
            return Success(new DurableSagasComplianceOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var sagaId = ReadString(request, "sagaId")!.Trim();
        var eventType = ReadString(request, "eventType")!.Trim();
        var operationKey = $"{DurableSagasComplianceConstants.FlowSlug}:compliance:{CurrentTenant.TenantId}:{sagaId}:{eventType}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var writtenAt = Now();
        var retentionDays = await ReadNumberFreedomAsync("flow19_compliance_retention_days", 365, cancellationToken).ConfigureAwait(false);
        var retentionExpiresAt = DateTimeOffset.Parse(writtenAt, CultureInfo.InvariantCulture).AddDays(retentionDays).ToString("O", CultureInfo.InvariantCulture);
        var recordId = $"audit:{sagaId}:{Hash($"{CurrentTenant.TenantId}:{sagaId}:{eventType}:{writtenAt}")[..16]}";
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["audit_id"] = recordId,
            ["saga_id"] = sagaId,
            ["event_type"] = eventType,
            ["actor"] = ReadString(request, "actor") ?? CurrentTenant.UserId ?? "system",
            ["written_at"] = writtenAt,
            ["retention_expires_at"] = retentionExpiresAt,
            ["legal_hold_active"] = ReadBool(request, "legalHoldActive"),
            ["audit_hash"] = Hash($"{CurrentTenant.TenantId}:{sagaId}:{eventType}:{writtenAt}"),
            ["payload_hash"] = Hash(JsonSerializer.Serialize(ReadDictionary(request, "payload"))),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        });

        var stored = await _documents.UpsertAsync(DurableSagasComplianceConstants.ComplianceRecords, recordId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            var failure = ErrorBody("COMPLIANCE_WRITE_FAILED", stored.Error!.Message);
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.ComplianceWriteFailedEvent, sagaId, failure), cancellationToken).ConfigureAwait(false);
            return Success(new DurableSagasComplianceOutcome(500, failure, false, true));
        }

        var response = new Dictionary<string, object?>
        {
            ["id"] = recordId,
            ["auditId"] = recordId,
            ["sagaId"] = sagaId,
            ["eventType"] = eventType,
            ["retentionExpiresAt"] = retentionExpiresAt,
            ["auditHash"] = body["audit_hash"],
            ["knowledgeScope"] = "PLATFORM_ONLY",
            ["version"] = stored.Data!.Version
        };
        await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.ComplianceRecordWrittenEvent, recordId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DurableSagasComplianceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<DurableSagasComplianceOutcome>> EnforceRetentionAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "auditId");
        if (validation.Count > 0)
        {
            return Success(new DurableSagasComplianceOutcome(400, ValidationBody(validation), false, false));
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var auditId = ReadString(request, "auditId")!.Trim();
        var operationKey = $"{DurableSagasComplianceConstants.FlowSlug}:retention:{CurrentTenant.TenantId}:{auditId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var record = await _documents.GetAsync(DurableSagasComplianceConstants.ComplianceRecords, auditId, cancellationToken).ConfigureAwait(false);
        if (record.Data is null)
        {
            return Success(new DurableSagasComplianceOutcome(404, ErrorBody("AUDIT_RECORD_NOT_FOUND", "Compliance record was not found."), false, false));
        }
        if (!string.Equals(ReadString(record.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
        {
            return Success(new DurableSagasComplianceOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this audit record."), false, false));
        }

        if (ReadBool(record.Data.Body, "legal_hold_active"))
        {
            var held = new Dictionary<string, object?> { ["id"] = auditId, ["auditId"] = auditId, ["status"] = "LEGAL_HOLD_ACTIVE" };
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.RetentionHoldActiveEvent, auditId, held), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, held, cancellationToken).ConfigureAwait(false);
            return Success(new DurableSagasComplianceOutcome(200, held, true, true));
        }

        var expires = DateTimeOffset.TryParse(ReadString(record.Data.Body, "retention_expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTimeOffset.MaxValue;
        if (expires > DateTimeOffset.UtcNow && !ReadBool(request, "forceExpiredForTest"))
        {
            var skipped = new Dictionary<string, object?> { ["id"] = auditId, ["auditId"] = auditId, ["status"] = "NOT_EXPIRED" };
            await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.PurgeSkippedEvent, auditId, skipped), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, skipped, cancellationToken).ConfigureAwait(false);
            return Success(new DurableSagasComplianceOutcome(200, skipped, true, true));
        }

        var archiveRef = $"archive:{auditId}:{Guid.NewGuid():N}";
        var archived = WithTenant(new Dictionary<string, object?>
        {
            ["archive_ref"] = archiveRef,
            ["audit_id"] = auditId,
            ["archived_at"] = Now(),
            ["source_hash"] = Hash(JsonSerializer.Serialize(record.Data.Body)),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        });
        await _documents.UpsertAsync(DurableSagasComplianceConstants.RetentionArchives, archiveRef, archived, null, cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["id"] = auditId,
            ["auditId"] = auditId,
            ["status"] = "PURGE_COMPLETED",
            ["archiveRef"] = archiveRef
        };
        await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.PurgeCompletedEvent, auditId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DurableSagasComplianceOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<DurableSagasComplianceOutcome>> QueueComplianceExportAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }
        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var operationKey = $"{DurableSagasComplianceConstants.FlowSlug}:export:{CurrentTenant.TenantId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var recordEstimate = ReadInt(request, "recordEstimate");
        var exportId = $"export-{Guid.NewGuid():N}";
        var background = recordEstimate > 100_000;
        var response = new Dictionary<string, object?>
        {
            ["id"] = exportId,
            ["exportId"] = exportId,
            ["status"] = background ? "QUEUED" : "READY",
            ["delivery"] = background ? "EMAIL_LINK" : "DOWNLOAD",
            ["format"] = ReadString(request, "format") ?? "csv"
        };
        await _documents.UpsertAsync(DurableSagasComplianceConstants.ExportJobs, exportId, WithTenant(response), null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(DurableSagasComplianceConstants.ExportQueuedEvent, exportId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new DurableSagasComplianceOutcome(background ? 202 : 201, response, true, true));
    }

    public async Task<DataProcessResult<DurableSagasComplianceDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var sagas = await SearchTenantAsync(DurableSagasComplianceConstants.SagaStates, cancellationToken).ConfigureAwait(false);
        var compliance = await SearchTenantAsync(DurableSagasComplianceConstants.ComplianceRecords, cancellationToken).ConfigureAwait(false);
        var compensations = await SearchTenantAsync(DurableSagasComplianceConstants.CompensationRecords, cancellationToken).ConfigureAwait(false);
        var archives = await SearchTenantAsync(DurableSagasComplianceConstants.RetentionArchives, cancellationToken).ConfigureAwait(false);
        var sagaBodies = (sagas.Data ?? []).Select(record => record.Body).ToArray();
        var complianceBodies = (compliance.Data ?? []).Select(record => record.Body).ToArray();
        var compensationBodies = (compensations.Data ?? []).Select(record => record.Body).ToArray();
        var archiveBodies = (archives.Data ?? []).Select(record => record.Body).ToArray();
        return Success(new DurableSagasComplianceDashboard(
            sagaBodies,
            complianceBodies,
            compensationBodies,
            archiveBodies,
            new Dictionary<string, object?>
            {
                ["runningSagas"] = sagaBodies.Count(body => string.Equals(ReadString(body, "status"), "RUNNING", StringComparison.Ordinal)),
                ["complianceRecords"] = complianceBodies.Length,
                ["compensatedSteps"] = compensationBodies.Length,
                ["retentionArchives"] = archiveBodies.Length
            }));
    }

    private SemaphoreSlim GetResourceLock(string key) => _resourceLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private async Task<DurableSagasComplianceOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new DurableSagasComplianceOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new DurableSagasComplianceOutcome(200, reservation.Data.CachedResponse, true, false)
            : null;
    }

    private async Task StoreAuditAsync(string collection, string id, string auditType, IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(collection, id, WithTenant(new Dictionary<string, object?>
        {
            ["audit_type"] = auditType,
            ["correlation_id"] = CurrentCorrelationId,
            ["subject"] = id,
            ["body_hash"] = Hash(JsonSerializer.Serialize(body)),
            ["created_at"] = Now(),
            ["knowledge_scope"] = "PLATFORM_ONLY"
        }), null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<double> ReadNumberFreedomAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return value.Data?.Value switch
        {
            int i => i,
            long l => l,
            double d => d,
            decimal m => (double)m,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => fallback
        };
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

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            DurableSagasComplianceConstants.FlowId,
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
                ["xiigenflowid"] = DurableSagasComplianceConstants.FlowId
            });

    private static List<Dictionary<string, object?>> ValidateSagaSteps(IReadOnlyList<SagaPlanStep> steps)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (steps.Count == 0)
        {
            errors.Add(FieldError("steps", "At least one saga step is required."));
            return errors;
        }

        foreach (var taskType in DurableSagasComplianceConstants.RequiredTaskTypes)
        {
            if (!steps.Any(step => string.Equals(step.TaskTypeId, taskType, StringComparison.Ordinal)))
            {
                errors.Add(FieldError("steps", $"Every FLOW-19 task type must have a plan step. Missing {taskType}."));
            }
        }

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
            {
                errors.Add(FieldError("step.id", "Step id is required."));
            }
            if (string.IsNullOrWhiteSpace(step.Responsibility))
            {
                errors.Add(FieldError("step.responsibility", "Each plan step must have one responsibility."));
            }
            if (step.Config.TryGetValue("providerSdkImport", out var providerImport) && IsTruthy(providerImport))
            {
                errors.Add(FieldError("step.config.providerSdkImport", "Plan steps must use fabric interfaces and cannot import provider SDKs."));
            }
            if (step.Config.TryGetValue("entitySpecificController", out var controller) && IsTruthy(controller))
            {
                errors.Add(FieldError("step.config.entitySpecificController", "Entity-specific controllers are forbidden."));
            }
            if (!string.Equals(ReadString(step.Config, "returns"), "DataProcessResult", StringComparison.Ordinal))
            {
                errors.Add(FieldError("step.config.returns", "Every step must return DataProcessResult."));
            }
        }

        return errors;
    }

    private static List<SagaPlanStep> ReadSteps(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "steps").Select(item => new SagaPlanStep(
            ReadString(item, "id") ?? string.Empty,
            ReadString(item, "taskTypeId") ?? ReadString(item, "task_type_id") ?? string.Empty,
            ReadString(item, "name") ?? string.Empty,
            ReadString(item, "responsibility") ?? string.Empty,
            ReadDictionary(item, "config"),
            ReadDictionary(item, "compensation"))).ToList();

    private static IReadOnlyDictionary<string, object?> StepToDictionary(SagaPlanStep step) => new Dictionary<string, object?>
    {
        ["id"] = step.Id,
        ["taskTypeId"] = step.TaskTypeId,
        ["name"] = step.Name,
        ["responsibility"] = step.Responsibility,
        ["config"] = step.Config
    };

    private static IReadOnlyDictionary<string, object?> StepToCompensationDictionary(SagaPlanStep step) => new Dictionary<string, object?>
    {
        ["stepId"] = step.Id,
        ["taskTypeId"] = step.TaskTypeId,
        ["strategy"] = ReadString(step.Compensation, "strategy") ?? "lifo",
        ["registered"] = true
    };

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

    private static IReadOnlyDictionary<string, object?> ReadJsonObject(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ReadJsonValue(prop.Value))
            : new Dictionary<string, object?>();

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

    private static DurableSagasComplianceOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new DurableSagasComplianceOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static DurableSagasComplianceOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key => ForbiddenPayloadKeys.Any(forbiddenKey => string.Equals(key, forbiddenKey, StringComparison.OrdinalIgnoreCase)));
        return forbidden is null
            ? null
            : new DurableSagasComplianceOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
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

    private static int ReadInt(IReadOnlyDictionary<string, object?> body, string key)
    {
        var text = ReadString(body, key);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> body, string key) =>
        body.TryGetValue(key, out var value) && (value is bool boolValue && boolValue || value is JsonElement { ValueKind: JsonValueKind.True });

    private static bool IsTruthy(object? value) => value switch
    {
        true => true,
        string text => string.Equals(text, "true", StringComparison.OrdinalIgnoreCase),
        JsonElement { ValueKind: JsonValueKind.True } => true,
        _ => false
    };
}
