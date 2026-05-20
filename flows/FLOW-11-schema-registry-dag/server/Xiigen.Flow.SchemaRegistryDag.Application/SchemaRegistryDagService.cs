using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.SchemaRegistryDag.Application;

public sealed class SchemaRegistryDagService : MicroserviceBase<SchemaRegistryDagService>
{
    private static readonly ConcurrentDictionary<string, byte> RegistrationLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> ApprovalLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> DeprecationLocks = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public SchemaRegistryDagService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<SchemaRegistryDagService> logger)
        : base(
            new ServiceDescriptor("FLOW-11", nameof(SchemaRegistryDagService), SchemaRegistryDagConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> RegisterSchemaAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRegistrationRequest(request);
        if (validation is not null)
        {
            return Success(validation);
        }

        var schemaType = ReadString(request, "schemaType")!.Trim();
        var version = NormalizeVersion(ReadString(request, "version")!);
        var changeType = NormalizeChangeType(ReadString(request, "changeType"));
        var schemaId = SchemaDocumentId(schemaType, version);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<SchemaRegistryDagOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                FieldDetails("X-Idempotency-Key", "required"));
        }

        var operationKey = $"{SchemaRegistryDagConstants.FlowSlug}:register:{CurrentTenant.TenantId}:{schemaId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<SchemaRegistryDagOutcome>(reservation.Error?.Code ?? "IDEMPOTENCY_FAILED", reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.RegistrationRequestedEvent, schemaId, new Dictionary<string, object?>
        {
            ["schemaType"] = schemaType,
            ["version"] = version,
            ["changeType"] = changeType
        }), cancellationToken).ConfigureAwait(false);

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = new Dictionary<string, object?>
            {
                ["code"] = "TIMEOUT",
                ["schemaType"] = schemaType,
                ["version"] = version,
                ["retryAfterMs"] = 5000
            };
            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.TimeoutRetainedEvent, schemaId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Schema registry write fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.GatewayTimeout, timeoutBody, false, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:{schemaType}:{version}";
        if (!RegistrationLocks.TryAdd(lockKey, 0))
        {
            return Failure<SchemaRegistryDagOutcome>("CONCURRENT_UPDATE", "Concurrent schema registration detected", new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var versionCheck = await ValidateVersionMonotonicAsync(schemaType, version, cancellationToken).ConfigureAwait(false);
            if (versionCheck is not null)
            {
                await _idempotency.MarkFailedAsync(operationKey, "VERSION_NOT_MONOTONIC", "Version must increase monotonically", cancellationToken).ConfigureAwait(false);
                return Success(versionCheck);
            }

            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.VersionCheckInlineEvent, schemaId, new Dictionary<string, object?>
            {
                ["schemaType"] = schemaType,
                ["version"] = version,
                ["result"] = "PASS"
            }), cancellationToken).ConfigureAwait(false);

            var dependencies = ReadStringArray(request, "dependencies");
            var cycleCheck = await ValidateCycleFreeAsync(schemaType, dependencies, cancellationToken).ConfigureAwait(false);
            if (cycleCheck is not null)
            {
                await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaRejectedEvent, schemaId, cycleCheck.Body), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, "DAG_CYCLE_DETECTED", "Dependency graph contains a cycle", cancellationToken).ConfigureAwait(false);
                return Success(cycleCheck);
            }

            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.CycleCheckInlineEvent, schemaId, new Dictionary<string, object?>
            {
                ["schemaType"] = schemaType,
                ["states"] = new[] { "WHITE", "GRAY", "BLACK" },
                ["result"] = "PASS"
            }), cancellationToken).ConfigureAwait(false);

            var compatibility = await ClassifyCompatibilityAsync(schemaType, changeType, request, cancellationToken).ConfigureAwait(false);
            if (compatibility.IsFailure)
            {
                await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaRejectedEvent, schemaId, compatibility.Error?.Details ?? new Dictionary<string, object?>()), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkFailedAsync(operationKey, compatibility.Error?.Code ?? "COMPATIBILITY_FAILED", compatibility.Error?.Message ?? "Compatibility failed", cancellationToken).ConfigureAwait(false);
                return Failure<SchemaRegistryDagOutcome>(compatibility.Error?.Code ?? "COMPATIBILITY_FAILED", compatibility.Error?.Message ?? "Compatibility failed", compatibility.Error?.Details);
            }

            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.CompatibilityCheckInlineEvent, schemaId, new Dictionary<string, object?>
            {
                ["schemaType"] = schemaType,
                ["changeType"] = changeType,
                ["classification"] = compatibility.Data?["classification"] ?? "UNKNOWN"
            }), cancellationToken).ConfigureAwait(false);

            var body = CreateSchemaBody(request, schemaType, version, changeType, dependencies, "VALIDATED");
            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaValidatedEvent, schemaId, MinimalSchemaEvent(schemaType, version, "VALIDATED", changeType)), cancellationToken).ConfigureAwait(false);

            if (string.Equals(changeType, "BREAKING", StringComparison.Ordinal))
            {
                var approvalId = ApprovalDocumentId(schemaType, version);
                var pending = new Dictionary<string, object?>(body)
                {
                    ["status"] = "PENDING_APPROVAL",
                    ["approval_id"] = approvalId,
                    ["approval_window_ms"] = await ResolveIntAsync("schema_registry_approval_window_ms", 86400000, cancellationToken).ConfigureAwait(false)
                };
                var approval = await _documents.UpsertAsync(SchemaRegistryDagConstants.ApprovalQueue, approvalId, pending, null, cancellationToken).ConfigureAwait(false);
                if (approval.IsFailure)
                {
                    return Failure<SchemaRegistryDagOutcome>(approval.Error?.Code ?? "APPROVAL_WRITE_FAILED", approval.Error?.Message ?? "Approval queue write failed", approval.Error?.Details);
                }

                await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaApprovalRequiredEvent, schemaId, new Dictionary<string, object?>
                {
                    ["schemaType"] = schemaType,
                    ["version"] = version,
                    ["approvalId"] = approvalId,
                    ["changeType"] = changeType
                }), cancellationToken).ConfigureAwait(false);

                var pendingResponse = new Dictionary<string, object?>
                {
                    ["id"] = schemaId,
                    ["schemaType"] = schemaType,
                    ["version"] = version,
                    ["status"] = "PENDING_APPROVAL",
                    ["approvalId"] = approvalId,
                    ["versionPin"] = approval.Data?.Version ?? "1"
                };
                await _idempotency.MarkSucceededAsync(operationKey, pendingResponse, cancellationToken).ConfigureAwait(false);
                return Success(new SchemaRegistryDagOutcome(StatusCodes.Accepted, pendingResponse, true, true));
            }

            var publish = await PublishSchemaBodyAsync(schemaId, body, ifMatch, cancellationToken).ConfigureAwait(false);
            if (publish.IsFailure)
            {
                return Failure<SchemaRegistryDagOutcome>(publish.Error?.Code ?? "CONCURRENT_UPDATE", publish.Error?.Message ?? "Schema publish failed", publish.Error?.Details);
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = schemaId,
                ["schemaType"] = schemaType,
                ["version"] = version,
                ["status"] = "PUBLISHED",
                ["changeType"] = changeType,
                ["versionPin"] = publish.Data?["versionPin"] ?? "1"
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Created, response, true, true));
        }
        finally
        {
            RegistrationLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> DecideApprovalAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var approvalId = (ReadString(request, "approvalId") ?? string.Empty).Trim();
        var decision = (ReadString(request, "decision") ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(approvalId) || !new[] { "APPROVE", "REJECT", "DEFER" }.Contains(decision, StringComparer.Ordinal))
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.BadRequest, ValidationBody(Field("approvalId", "required"), Field("decision", "APPROVE, REJECT, or DEFER")), false, false));
        }

        var operationKey = $"{SchemaRegistryDagConstants.FlowSlug}:approval:{CurrentTenant.TenantId}:{approvalId}:{idempotencyKey ?? "no-key"}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:approval:{approvalId}";
        if (!ApprovalLocks.TryAdd(lockKey, 0))
        {
            return Failure<SchemaRegistryDagOutcome>("CONCURRENT_UPDATE", "Concurrent schema approval detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var approval = await _documents.GetAsync(SchemaRegistryDagConstants.ApprovalQueue, approvalId, cancellationToken).ConfigureAwait(false);
            if (approval.IsFailure || approval.Data is null || !IsTenantRecord(approval.Data.Body))
            {
                return Failure<SchemaRegistryDagOutcome>("NOT_FOUND", "Approval item was not found");
            }

            var schemaType = ReadString(approval.Data.Body, "schema_type") ?? string.Empty;
            var version = ReadString(approval.Data.Body, "version") ?? string.Empty;
            var schemaId = SchemaDocumentId(schemaType, version);
            var status = decision switch
            {
                "APPROVE" => "PUBLISHED",
                "REJECT" => "REJECTED",
                _ => "DEFERRED"
            };

            var updatedApproval = new Dictionary<string, object?>(approval.Data.Body)
            {
                ["status"] = status,
                ["decision"] = decision,
                ["decided_by"] = ReadString(request, "actorId") ?? "system",
                ["decided_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var storedApproval = await _documents.UpsertAsync(SchemaRegistryDagConstants.ApprovalQueue, approvalId, updatedApproval, approval.Data.Version, cancellationToken).ConfigureAwait(false);
            if (storedApproval.IsFailure)
            {
                return Failure<SchemaRegistryDagOutcome>(storedApproval.Error?.Code ?? "CONCURRENT_UPDATE", storedApproval.Error?.Message ?? "Approval changed during decision", storedApproval.Error?.Details);
            }

            if (decision == "APPROVE")
            {
                await PublishSchemaBodyAsync(schemaId, approval.Data.Body, null, cancellationToken).ConfigureAwait(false);
            }

            var response = new Dictionary<string, object?>
            {
                ["approvalId"] = approvalId,
                ["schemaType"] = schemaType,
                ["version"] = version,
                ["status"] = status,
                ["decision"] = decision
            };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, response, true, decision == "APPROVE"));
        }
        finally
        {
            ApprovalLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<SchemaCollection>> SearchSchemasAsync(
        IReadOnlyDictionary<string, object?> query,
        CancellationToken cancellationToken = default)
    {
        var filter = TenantFilter();
        if (!string.IsNullOrWhiteSpace(ReadString(query, "schemaType")))
        {
            filter["schema_type"] = ReadString(query, "schemaType")!.Trim();
        }
        if (!string.IsNullOrWhiteSpace(ReadString(query, "status")))
        {
            filter["status"] = ReadString(query, "status")!.Trim().ToUpperInvariant();
        }
        else
        {
            filter["deprecated"] = false;
        }

        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, filter, cancellationToken).ConfigureAwait(false);
        if (schemas.IsFailure)
        {
            return Failure<SchemaCollection>(schemas.Error?.Code ?? "SCHEMA_SEARCH_FAILED", schemas.Error?.Message ?? "Schema search failed");
        }

        var rows = (schemas.Data ?? [])
            .Where(row => IsTenantRecord(row.Body))
            .Select(row => PublicSchemaBody(row.Body, row.Version))
            .OrderBy(row => ReadString(row, "schemaType"), StringComparer.Ordinal)
            .ThenBy(row => ReadString(row, "version"), StringComparer.Ordinal)
            .ToList();
        return Success(new SchemaCollection(rows, rows.Count));
    }

    public async Task<DataProcessResult<DagRenderSnapshot>> RenderDagAsync(
        string format = "json",
        CancellationToken cancellationToken = default)
    {
        format = string.Equals(format, "mermaid", StringComparison.OrdinalIgnoreCase) ? "mermaid" : "json";
        var cacheId = $"dag-render-{format}";
        var cached = await _documents.GetAsync(SchemaRegistryDagConstants.DagRenderCache, cacheId, cancellationToken).ConfigureAwait(false);
        if (cached.Data is not null && IsTenantRecord(cached.Data.Body))
        {
            return Success(new DagRenderSnapshot(
                format,
                ReadString(cached.Data.Body, "content") ?? string.Empty,
                ReadInt(cached.Data.Body, "node_count") ?? 0,
                ReadInt(cached.Data.Body, "edge_count") ?? 0,
                true));
        }

        var topology = await BuildTopologyAsync(cancellationToken).ConfigureAwait(false);
        if (topology.IsFailure || topology.Data is null)
        {
            return Failure<DagRenderSnapshot>(topology.Error?.Code ?? "DAG_BUILD_FAILED", topology.Error?.Message ?? "DAG build failed");
        }

        var content = format == "mermaid" ? RenderMermaid(topology.Data) : RenderJsonLike(topology.Data);
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["format"] = format,
            ["content"] = content,
            ["node_count"] = topology.Data.NodeCount,
            ["edge_count"] = topology.Data.EdgeCount,
            ["cached_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SchemaRegistryDagConstants.DagRenderCache, cacheId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.DagRenderedEvent, cacheId, body), cancellationToken).ConfigureAwait(false);
        return Success(new DagRenderSnapshot(format, content, topology.Data.NodeCount, topology.Data.EdgeCount, false));
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> DeprecateSchemaAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var schemaType = (ReadString(request, "schemaType") ?? string.Empty).Trim();
        var version = NormalizeVersion(ReadString(request, "version") ?? string.Empty);
        if (string.IsNullOrWhiteSpace(schemaType) || string.IsNullOrWhiteSpace(version))
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.BadRequest, ValidationBody(Field("schemaType", "required"), Field("version", "required")), false, false));
        }

        var schemaId = SchemaDocumentId(schemaType, version);
        var operationKey = $"{SchemaRegistryDagConstants.FlowSlug}:deprecate:{CurrentTenant.TenantId}:{schemaId}:{idempotencyKey ?? "no-key"}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, reservation.Data.CachedResponse, true, true));
        }

        var lockKey = $"{CurrentTenant.TenantId}:deprecate:{schemaId}";
        if (!DeprecationLocks.TryAdd(lockKey, 0))
        {
            return Failure<SchemaRegistryDagOutcome>("CONCURRENT_UPDATE", "Concurrent schema deprecation detected");
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var schema = await _documents.GetAsync(SchemaRegistryDagConstants.Schemas, schemaId, cancellationToken).ConfigureAwait(false);
            if (schema.IsFailure || schema.Data is null || !IsTenantRecord(schema.Data.Body))
            {
                return Failure<SchemaRegistryDagOutcome>("NOT_FOUND", "Schema version was not found");
            }

            var ttlMs = await ResolveIntAsync("schema_registry_deprecation_ttl_ms", 604800000, cancellationToken).ConfigureAwait(false);
            var updated = new Dictionary<string, object?>(schema.Data.Body)
            {
                ["deprecated"] = true,
                ["active_until"] = DateTimeOffset.UtcNow.AddMilliseconds(ttlMs).ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(SchemaRegistryDagConstants.Schemas, schemaId, updated, ifMatch ?? schema.Data.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<SchemaRegistryDagOutcome>(stored.Error?.Code ?? "CONCURRENT_UPDATE", stored.Error?.Message ?? "Schema changed during deprecation", stored.Error?.Details);
            }

            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaDeprecatedEvent, schemaId, MinimalSchemaEvent(schemaType, version, "DEPRECATED", ReadString(updated, "change_type") ?? "UNKNOWN")), cancellationToken).ConfigureAwait(false);
            await BuildTopologyAsync(cancellationToken).ConfigureAwait(false);
            var response = new Dictionary<string, object?> { ["schemaType"] = schemaType, ["version"] = version, ["status"] = "DEPRECATED", ["versionPin"] = stored.Data?.Version ?? "1" };
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, response, true, true));
        }
        finally
        {
            DeprecationLocks.TryRemove(lockKey, out _);
        }
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> ValidateDocumentAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var schemaType = (ReadString(request, "schemaType") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(schemaType))
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.BadRequest, ValidationBody(Field("schemaType", "required")), false, false));
        }

        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType,
            ["status"] = "PUBLISHED",
            ["deprecated"] = false
        }, cancellationToken).ConfigureAwait(false);

        var active = (schemas.Data ?? [])
            .Where(row => IsTenantRecord(row.Body))
            .OrderByDescending(row => ParseVersion(ReadString(row.Body, "version") ?? "0.0.0"))
            .FirstOrDefault();
        if (active is null)
        {
            return Failure<SchemaRegistryDagOutcome>("NOT_FOUND", "Active schema version was not found");
        }

        var required = ReadStringArray(active.Body, "required_fields");
        var document = ReadObject(request, "document");
        var errors = required
            .Where(field => !document.ContainsKey(field) || document[field] is null || string.IsNullOrWhiteSpace(Convert.ToString(document[field], CultureInfo.InvariantCulture)))
            .Select(field => Field(field, "required"))
            .ToArray();

        if (errors.Length > 0)
        {
            var body = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["schema_type"] = schemaType,
                ["schema_version"] = ReadString(active.Body, "version"),
                ["errors"] = errors,
                ["recorded_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            await _documents.UpsertAsync(SchemaRegistryDagConstants.ValidationFailures, $"validation-{Guid.NewGuid():N}", body, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.ValidationFailedEvent, schemaType, body), cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.BadRequest, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = errors }, false, true));
        }

        return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, new Dictionary<string, object?> { ["schemaType"] = schemaType, ["valid"] = true, ["schemaVersion"] = ReadString(active.Body, "version") }, false, false));
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> RunMigrationAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var schemaType = (ReadString(request, "schemaType") ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(schemaType))
        {
            return Success(new SchemaRegistryDagOutcome(StatusCodes.BadRequest, ValidationBody(Field("schemaType", "required")), false, false));
        }

        var runId = $"migration-{CurrentTenant.TenantId}-{schemaType}-{idempotencyKey ?? Guid.NewGuid().ToString("n")}";
        var run = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType,
            ["status"] = ReadBool(request, "dryRun") ? "DRY_RUN" : "RUNNING",
            ["page_size"] = 100,
            ["started_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SchemaRegistryDagConstants.MigrationRuns, runId, run, null, cancellationToken).ConfigureAwait(false);

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            run["status"] = "RETRY_RETAINED";
            await _documents.UpsertAsync(SchemaRegistryDagConstants.MigrationRuns, runId, run, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.TimeoutRetainedEvent, runId, run), cancellationToken).ConfigureAwait(false);
            return Success(new SchemaRegistryDagOutcome(StatusCodes.GatewayTimeout, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["runId"] = runId, ["retryAfterMs"] = 5000 }, true, true));
        }

        run["status"] = ReadBool(request, "dryRun") ? "DRY_RUN_COMPLETE" : "COMPLETED";
        await _documents.UpsertAsync(SchemaRegistryDagConstants.MigrationRuns, runId, run, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.MigrationRunRecordedEvent, runId, run), cancellationToken).ConfigureAwait(false);
        return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, new Dictionary<string, object?> { ["runId"] = runId, ["status"] = run["status"], ["pageSize"] = 100 }, true, true));
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> AnalyzeQualityAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var schemaType = (ReadString(request, "schemaType") ?? string.Empty).Trim();
        var score = Math.Clamp(ReadDouble(request, "score") ?? 0.90d, 0d, 1d);
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType,
            ["score"] = score,
            ["analyzed_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SchemaRegistryDagConstants.QualityScores, $"quality-{schemaType}", body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.QualityAnalyzedEvent, schemaType, new Dictionary<string, object?> { ["schemaType"] = schemaType, ["score"] = score }), cancellationToken).ConfigureAwait(false);
        return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, new Dictionary<string, object?> { ["schemaType"] = schemaType, ["score"] = score }, true, true));
    }

    public async Task<DataProcessResult<SchemaRegistryDagOutcome>> ExportSchemasAsync(
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken = default)
    {
        var chunkSize = Math.Clamp(ReadInt(request, "chunkSize") ?? 100, 1, 500);
        var auditId = $"export-{Guid.NewGuid():N}";
        var audit = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["chunk_size"] = chunkSize,
            ["metadata_header"] = true,
            ["started_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SchemaRegistryDagConstants.ExportAudits, auditId, audit, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.ExportStartedEvent, auditId, audit), cancellationToken).ConfigureAwait(false);
        return Success(new SchemaRegistryDagOutcome(StatusCodes.Ok, new Dictionary<string, object?> { ["exportId"] = auditId, ["chunkSize"] = chunkSize, ["metadataHeader"] = true }, true, true));
    }

    private async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> PublishSchemaBodyAsync(
        string schemaId,
        IReadOnlyDictionary<string, object?> schemaBody,
        string? expectedVersion,
        CancellationToken cancellationToken)
    {
        var schemaType = ReadString(schemaBody, "schema_type") ?? string.Empty;
        var version = ReadString(schemaBody, "version") ?? string.Empty;
        var body = new Dictionary<string, object?>(schemaBody)
        {
            ["status"] = "PUBLISHED",
            ["deprecated"] = false,
            ["active_until"] = null,
            ["published_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(SchemaRegistryDagConstants.Schemas, schemaId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<IReadOnlyDictionary<string, object?>>(stored.Error?.Code ?? "CONCURRENT_UPDATE", stored.Error?.Message ?? "Schema changed during publish", stored.Error?.Details);
        }

        await _documents.UpsertAsync(SchemaRegistryDagConstants.SchemaHistory, $"history-{schemaId}-{Guid.NewGuid():N}", new Dictionary<string, object?>(body)
        {
            ["history_action"] = "PUBLISHED",
            ["version_pin"] = stored.Data?.Version ?? "1"
        }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.SchemaPublishedEvent, schemaId, MinimalSchemaEvent(schemaType, version, "PUBLISHED", ReadString(body, "change_type") ?? "UNKNOWN")), cancellationToken).ConfigureAwait(false);
        await BuildTopologyAsync(cancellationToken).ConfigureAwait(false);
        return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?> { ["versionPin"] = stored.Data?.Version ?? "1" });
    }

    private async Task<DataProcessResult<TopologySnapshot>> BuildTopologyAsync(CancellationToken cancellationToken)
    {
        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["status"] = "PUBLISHED",
            ["deprecated"] = false
        }, cancellationToken).ConfigureAwait(false);
        if (schemas.IsFailure)
        {
            return Failure<TopologySnapshot>(schemas.Error?.Code ?? "DAG_BUILD_FAILED", schemas.Error?.Message ?? "DAG build failed");
        }

        var rows = (schemas.Data ?? []).Where(row => IsTenantRecord(row.Body)).ToList();
        var nodes = rows
            .Select(row => ReadString(row.Body, "schema_type") ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var edges = rows
            .SelectMany(row =>
            {
                var source = ReadString(row.Body, "schema_type") ?? string.Empty;
                return ReadStringArray(row.Body, "dependencies").Select(target => (Source: source, Target: target));
            })
            .Where(edge => !string.IsNullOrWhiteSpace(edge.Source) && !string.IsNullOrWhiteSpace(edge.Target))
            .Distinct()
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["nodes"] = nodes,
            ["edges"] = edges.Select(edge => $"{edge.Source}->{edge.Target}").ToArray(),
            ["node_count"] = nodes.Length,
            ["edge_count"] = edges.Length,
            ["rebuilt_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(SchemaRegistryDagConstants.DagTopology, "active", body, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(SchemaRegistryDagConstants.DagRenderCache, "dag-render-json", new Dictionary<string, object?>(body)
        {
            ["format"] = "json",
            ["content"] = RenderJsonLike(new TopologySnapshot(nodes, edges))
        }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(SchemaRegistryDagConstants.DagRebuiltEvent, "active", new Dictionary<string, object?>
        {
            ["nodeCount"] = nodes.Length,
            ["edgeCount"] = edges.Length
        }), cancellationToken).ConfigureAwait(false);

        return Success(new TopologySnapshot(nodes, edges));
    }

    private async Task<SchemaRegistryDagOutcome?> ValidateVersionMonotonicAsync(string schemaType, string version, CancellationToken cancellationToken)
    {
        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType
        }, cancellationToken).ConfigureAwait(false);
        var candidate = ParseVersion(version);
        var latest = (schemas.Data ?? [])
            .Where(row => IsTenantRecord(row.Body))
            .Select(row => ParseVersion(ReadString(row.Body, "version") ?? "0.0.0"))
            .DefaultIfEmpty(new Version(0, 0, 0))
            .Max();
        if (candidate.CompareTo(latest) <= 0)
        {
            return new SchemaRegistryDagOutcome(StatusCodes.Conflict, new Dictionary<string, object?>
            {
                ["code"] = "VERSION_NOT_MONOTONIC",
                ["schemaType"] = schemaType,
                ["submittedVersion"] = version,
                ["currentVersion"] = latest?.ToString() ?? "0.0.0"
            }, false, false);
        }

        return null;
    }

    private async Task<SchemaRegistryDagOutcome?> ValidateCycleFreeAsync(string schemaType, IReadOnlyList<string> dependencies, CancellationToken cancellationToken)
    {
        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["status"] = "PUBLISHED",
            ["deprecated"] = false
        }, cancellationToken).ConfigureAwait(false);
        var graph = (schemas.Data ?? [])
            .Where(row => IsTenantRecord(row.Body))
            .GroupBy(row => ReadString(row.Body, "schema_type") ?? string.Empty, StringComparer.Ordinal)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.SelectMany(row => ReadStringArray(row.Body, "dependencies")).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
        graph[schemaType] = dependencies;

        var cycle = DetectCyclePath(graph);
        if (cycle.Count > 0)
        {
            return new SchemaRegistryDagOutcome(StatusCodes.Conflict, new Dictionary<string, object?>
            {
                ["code"] = "DAG_CYCLE_DETECTED",
                ["schemaType"] = schemaType,
                ["path"] = cycle,
                ["states"] = new[] { "WHITE", "GRAY", "BLACK" }
            }, false, false);
        }

        return null;
    }

    private async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> ClassifyCompatibilityAsync(
        string schemaType,
        string changeType,
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken)
    {
        var schemas = await _documents.SearchAsync(SchemaRegistryDagConstants.Schemas, new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType,
            ["status"] = "PUBLISHED",
            ["deprecated"] = false
        }, cancellationToken).ConfigureAwait(false);
        var previous = (schemas.Data ?? []).Where(row => IsTenantRecord(row.Body)).OrderByDescending(row => ParseVersion(ReadString(row.Body, "version") ?? "0.0.0")).FirstOrDefault();
        var removedFields = ReadStringArray(request, "removedFields");
        var changedTypes = ReadStringArray(request, "changedFieldTypes");
        if (previous is not null && (removedFields.Count > 0 || changedTypes.Count > 0) && !string.Equals(changeType, "BREAKING", StringComparison.Ordinal))
        {
            return Failure<IReadOnlyDictionary<string, object?>>("BREAKING_CHANGE_REQUIRES_APPROVAL", "Removed fields or type changes must be declared as BREAKING", new Dictionary<string, object?>
            {
                ["removedFields"] = removedFields,
                ["changedFieldTypes"] = changedTypes
            });
        }

        return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>
        {
            ["classification"] = previous is null ? "INITIAL" : changeType,
            ["previousVersion"] = previous is null ? null : ReadString(previous.Body, "version")
        });
    }

    private Dictionary<string, object?> CreateSchemaBody(
        IReadOnlyDictionary<string, object?> request,
        string schemaType,
        string version,
        string changeType,
        IReadOnlyList<string> dependencies,
        string status)
    {
        var jsonSchema = ReadObject(request, "jsonSchema");
        var required = ReadStringArray(jsonSchema, "required");
        return new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["schema_type"] = schemaType,
            ["version"] = version,
            ["status"] = status,
            ["change_type"] = changeType,
            ["dependencies"] = dependencies.ToArray(),
            ["json_schema"] = jsonSchema,
            ["required_fields"] = required.ToArray(),
            ["actor_id"] = ReadString(request, "actorId") ?? "system",
            ["deprecated"] = false,
            ["registered_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private SchemaRegistryDagOutcome? ValidateRegistrationRequest(IReadOnlyDictionary<string, object?> request)
    {
        var details = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "schemaType"))) details.Add(Field("schemaType", "required"));
        if (string.IsNullOrWhiteSpace(ReadString(request, "version"))) details.Add(Field("version", "required"));
        if (!string.IsNullOrWhiteSpace(ReadString(request, "version")) && ParseVersion(ReadString(request, "version")!) == new Version(0, 0, 0)) details.Add(Field("version", "semver required"));
        if (ReadObject(request, "jsonSchema").Count == 0) details.Add(Field("jsonSchema", "required"));
        var changeType = NormalizeChangeType(ReadString(request, "changeType"));
        if (!new[] { "ADDITIVE", "COMPATIBLE", "BREAKING" }.Contains(changeType, StringComparer.Ordinal)) details.Add(Field("changeType", "ADDITIVE, COMPATIBLE, or BREAKING"));
        if (request.ContainsKey("tenantId") || request.ContainsKey("tenant_id"))
        {
            details.Add(Field("tenantId", "tenant scope is read from TenantContext only"));
        }

        return details.Count == 0
            ? null
            : new SchemaRegistryDagOutcome(StatusCodes.BadRequest, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false, false);
    }

    private async Task<int> ResolveIntAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var result = await _freedom.GetAsync($"{key}.{CurrentTenant.TenantId}", cancellationToken).ConfigureAwait(false);
        return result.IsSuccess &&
            result.Data?.Value is not null &&
            int.TryParse(Convert.ToString(result.Data.Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{SchemaRegistryDagConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = SchemaRegistryDagConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private Dictionary<string, object?> MinimalSchemaEvent(string schemaType, string version, string status, string changeType) => new()
    {
        ["schemaType"] = schemaType,
        ["version"] = version,
        ["status"] = status,
        ["changeType"] = changeType
    };

    private Dictionary<string, object?> PublicSchemaBody(IReadOnlyDictionary<string, object?> body, string versionPin) => new()
    {
        ["schemaType"] = ReadString(body, "schema_type"),
        ["version"] = ReadString(body, "version"),
        ["status"] = ReadString(body, "status"),
        ["changeType"] = ReadString(body, "change_type"),
        ["dependencies"] = ReadStringArray(body, "dependencies"),
        ["versionPin"] = versionPin,
        ["deprecated"] = ReadBool(body, "deprecated")
    };

    private Dictionary<string, object?> TenantFilter() => new() { ["tenant_id"] = CurrentTenant.TenantId };

    private bool IsTenantRecord(IReadOnlyDictionary<string, object?> body) =>
        string.Equals(ReadString(body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal);

    private static string SchemaDocumentId(string schemaType, string version) => $"{schemaType}:{version}".ToLowerInvariant();
    private static string ApprovalDocumentId(string schemaType, string version) => $"approval:{SchemaDocumentId(schemaType, version)}";
    private static string NormalizeVersion(string version) => version.Trim().TrimStart('v', 'V');
    private static string NormalizeChangeType(string? changeType) => string.IsNullOrWhiteSpace(changeType) ? "ADDITIVE" : changeType.Trim().ToUpperInvariant();

    private static Version ParseVersion(string version)
    {
        var normalized = NormalizeVersion(version);
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3)
        {
            return new Version(0, 0, 0);
        }

        return int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            int.TryParse(parts.Length == 3 ? parts[2] : "0", out var patch)
                ? new Version(major, minor, patch)
                : new Version(0, 0, 0);
    }

    private static IReadOnlyList<string> DetectCyclePath(IReadOnlyDictionary<string, IReadOnlyList<string>> graph)
    {
        var color = new Dictionary<string, string>(StringComparer.Ordinal);
        var stack = new Stack<string>();

        foreach (var node in graph.Keys.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!color.ContainsKey(node) && Visit(node))
            {
                return stack.Reverse().ToArray();
            }
        }

        return Array.Empty<string>();

        bool Visit(string node)
        {
            color[node] = "GRAY";
            stack.Push(node);
            foreach (var next in graph.TryGetValue(node, out var dependencies) ? dependencies : Array.Empty<string>())
            {
                if (!color.TryGetValue(next, out var state))
                {
                    if (Visit(next))
                    {
                        return true;
                    }
                }
                else if (state == "GRAY")
                {
                    stack.Push(next);
                    return true;
                }
            }

            stack.Pop();
            color[node] = "BLACK";
            return false;
        }
    }

    private static string RenderMermaid(TopologySnapshot topology)
    {
        if (topology.NodeCount == 0)
        {
            return "graph TD\n  EMPTY[No active schemas]";
        }

        var lines = new List<string> { "graph TD" };
        foreach (var node in topology.Nodes)
        {
            lines.Add($"  {SafeNode(node)}[{node}]");
        }
        foreach (var edge in topology.Edges)
        {
            lines.Add($"  {SafeNode(edge.Source)} --> {SafeNode(edge.Target)}");
        }
        return string.Join('\n', lines);
    }

    private static string RenderJsonLike(TopologySnapshot topology)
    {
        var edges = topology.Edges.Select(edge => $"{edge.Source}->{edge.Target}");
        return $"nodes={string.Join(',', topology.Nodes)};edges={string.Join(',', edges)}";
    }

    private static string SafeNode(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }
        return builder.Length == 0 ? "node" : builder.ToString();
    }

    private static Dictionary<string, object?> ValidationBody(params Dictionary<string, object?>[] fields) => new()
    {
        ["code"] = "VALIDATION_FAILED",
        ["details"] = fields
    };

    private static Dictionary<string, object?> FieldDetails(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };
    private static Dictionary<string, object?> Field(string field, string reason) => new() { ["field"] = field, ["reason"] = reason };

    private static string? ReadString(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var result) && result;

    private static int? ReadInt(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static double? ReadDouble(IReadOnlyDictionary<string, object?> values, string key)
        => values.TryGetValue(key, out var value) && value is not null && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is IEnumerable<string> strings)
        {
            return strings.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        }

        if (value is IEnumerable<object?> objects && value is not string)
        {
            return objects.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var scalar = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(scalar) ? Array.Empty<string>() : new[] { scalar.Trim() };
    }

    private static IReadOnlyDictionary<string, object?> ReadObject(IReadOnlyDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>();
        }

        if (value is IReadOnlyDictionary<string, object?> readOnly)
        {
            return readOnly;
        }

        if (value is IDictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary);
        }

        if (value is IDictionary<string, object> objectDictionary)
        {
            return objectDictionary.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }

        return new Dictionary<string, object?>();
    }

    private sealed record TopologySnapshot(IReadOnlyList<string> Nodes, IReadOnlyList<(string Source, string Target)> Edges)
    {
        public int NodeCount => Nodes.Count;
        public int EdgeCount => Edges.Count;
    }

    private static class StatusCodes
    {
        public const int Ok = 200;
        public const int Created = 201;
        public const int Accepted = 202;
        public const int BadRequest = 400;
        public const int Conflict = 409;
        public const int GatewayTimeout = 504;
    }
}
