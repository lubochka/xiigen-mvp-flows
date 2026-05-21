using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.VisualFlowEngine.Application;

public sealed class VisualFlowEngineService : MicroserviceBase<VisualFlowEngineService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RegistrationLockTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan InjectionLockTtl = TimeSpan.FromMinutes(10);
    private static readonly string[] ImmutableStates = ["PUBLISHED", "ACTIVE"];
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

    public VisualFlowEngineService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<VisualFlowEngineService> logger)
        : base(
            new ServiceDescriptor("visual-flow-engine-service", "Visual Flow Engine", VisualFlowEngineConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<VisualFlowEngineOutcome>> SaveCanvasAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "flowId");
        var nodes = ReadNodes(request);
        var edges = ReadEdges(request);
        validation.AddRange(ValidateCanvas(nodes, edges));
        if (validation.Count > 0)
        {
            return Success(new VisualFlowEngineOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var flowId = ReadString(request, "flowId")!.Trim();
        var operationKey = $"{VisualFlowEngineConstants.FlowSlug}:canvas:{CurrentTenant.TenantId}:{flowId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutBody = ErrorBody("TIMEOUT", "Canvas write timed out before completion.", new Dictionary<string, object?>
            {
                ["retryAfterMs"] = 5000,
                ["flowId"] = flowId
            });
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.TimeoutRetainedEvent, flowId, timeoutBody), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Canvas write timed out", cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(504, timeoutBody, false, true));
        }

        var gate = GetResourceLock($"canvas:{CurrentTenant.TenantId}:{flowId}");
        if (!gate.Wait(0))
        {
            return Failure<VisualFlowEngineOutcome>("CONCURRENT_UPDATE", "Concurrent canvas write detected.", new Dictionary<string, object?> { ["currentVersion"] = "locked" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var current = await _documents.GetAsync(VisualFlowEngineConstants.FlowCanvases, flowId, cancellationToken).ConfigureAwait(false);
            if (current.IsFailure)
            {
                return Failure<VisualFlowEngineOutcome>(current.Error!.Code, current.Error.Message, current.Error.Details);
            }

            if (current.Data is not null && !string.Equals(ReadString(current.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
            {
                var rejected = ErrorBody("BOLA_VIOLATION", "Tenant does not own this visual flow canvas.");
                await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.FlowCanvasUpdateFailedEvent, flowId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new VisualFlowEngineOutcome(403, rejected, true, true));
            }

            var currentStatus = current.Data is null ? "DRAFT" : ReadString(current.Data.Body, "status") ?? "DRAFT";
            if (ImmutableStates.Contains(currentStatus, StringComparer.Ordinal))
            {
                var rejected = ErrorBody("FLOW_IMMUTABLE", "Published flows cannot be modified.", new Dictionary<string, object?> { ["status"] = currentStatus });
                await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.FlowImmutableRejectedEvent, flowId, rejected), cancellationToken).ConfigureAwait(false);
                return Success(new VisualFlowEngineOutcome(409, rejected, true, true));
            }

            if (current.Data is not null && !string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, current.Data.Version, StringComparison.Ordinal))
            {
                return Failure<VisualFlowEngineOutcome>("CONCURRENT_UPDATE", "Canvas version does not match.", new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            }

            var body = WithTenant(new Dictionary<string, object?>
            {
                ["flow_id"] = flowId,
                ["title"] = ReadString(request, "title") ?? "Visual flow draft",
                ["status"] = "DRAFT",
                ["mode"] = ReadString(request, "mode") ?? "Design",
                ["nodes"] = nodes.Select(NodeToDictionary).ToArray(),
                ["edges"] = edges.Select(EdgeToDictionary).ToArray(),
                ["updated_at"] = Now(),
                ["knowledge_scope"] = "PRIVATE"
            });
            var stored = await _documents.UpsertAsync(VisualFlowEngineConstants.FlowCanvases, flowId, body, current.Data?.Version, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<VisualFlowEngineOutcome>("CONCURRENT_UPDATE", "Concurrent canvas write detected.", new Dictionary<string, object?> { ["currentVersion"] = current.Data?.Version ?? "missing" });
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = flowId,
                ["flowId"] = flowId,
                ["status"] = "DRAFT",
                ["version"] = stored.Data!.Version,
                ["nodeCount"] = nodes.Count,
                ["edgeCount"] = edges.Count
            };
            await StoreAuditAsync(VisualFlowEngineConstants.CanvasAudits, $"canvas:{flowId}:{Guid.NewGuid():N}", "CANVAS_UPDATED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.FlowCanvasUpdatedEvent, flowId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(201, response, true, true));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DataProcessResult<VisualFlowEngineOutcome>> PublishFlowAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "flowId");
        if (validation.Count > 0)
        {
            return Success(new VisualFlowEngineOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var flowId = ReadString(request, "flowId")!.Trim();
        var operationKey = $"{VisualFlowEngineConstants.FlowSlug}:publish:{CurrentTenant.TenantId}:{flowId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var current = await _documents.GetAsync(VisualFlowEngineConstants.FlowCanvases, flowId, cancellationToken).ConfigureAwait(false);
        if (current.Data is null)
        {
            return Success(new VisualFlowEngineOutcome(404, ErrorBody("FLOW_NOT_FOUND", "Flow canvas was not found."), false, false));
        }
        if (!string.Equals(ReadString(current.Data.Body, "tenant_id"), CurrentTenant.TenantId, StringComparison.Ordinal))
        {
            return Success(new VisualFlowEngineOutcome(403, ErrorBody("BOLA_VIOLATION", "Tenant does not own this visual flow canvas."), false, false));
        }

        var nodes = ReadNodes(current.Data.Body);
        var edges = ReadEdges(current.Data.Body);
        var cycle = DetectCycle(nodes, edges);
        if (cycle is not null)
        {
            var body = ErrorBody("CYCLE_DETECTED", "This flow has a cycle. Break it before deploying.", new Dictionary<string, object?> { ["cycleNodes"] = cycle });
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.CycleDetectedEvent, flowId, body), cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(409, body, true, true));
        }

        var mismatch = FindTypeMismatch(edges);
        if (mismatch is not null)
        {
            var body = ErrorBody("TYPE_MISMATCH", "An edge connects incompatible node types.", EdgeToDictionary(mismatch));
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.TypeMismatchEvent, flowId, body), cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(409, body, true, true));
        }

        var expectedVersion = ReadString(request, "expectedVersion") ?? ifMatch;
        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(expectedVersion, current.Data.Version, StringComparison.Ordinal))
        {
            var body = ErrorBody("CONCURRENT_UPDATE", "Flow version does not match.", new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.FlowPublicationConflictEvent, flowId, body), cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(409, body, true, true));
        }

        var publishedAt = Now();
        var bodyToStore = WithTenant(new Dictionary<string, object?>(current.Data.Body)
        {
            ["status"] = "PUBLISHED",
            ["mode"] = "Active",
            ["published_at"] = publishedAt,
            ["updated_at"] = publishedAt
        });
        var stored = await _documents.UpsertAsync(VisualFlowEngineConstants.FlowCanvases, flowId, bodyToStore, current.Data.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<VisualFlowEngineOutcome>("CONCURRENT_UPDATE", "Concurrent publication detected.", new Dictionary<string, object?> { ["currentVersion"] = current.Data.Version });
        }

        var response = new Dictionary<string, object?>
        {
            ["id"] = flowId,
            ["flowId"] = flowId,
            ["status"] = "PUBLISHED",
            ["version"] = stored.Data!.Version,
            ["publishedAt"] = publishedAt
        };
        await StoreAuditAsync(VisualFlowEngineConstants.PublicationAudits, $"publish:{flowId}:{Guid.NewGuid():N}", "FLOW_PUBLISHED", response, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.FlowPublishedEvent, flowId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new VisualFlowEngineOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<VisualFlowEngineOutcome>> RegisterNodeTypeAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "nodeTypeId", "category");
        var category = ReadString(request, "category") ?? string.Empty;
        if (!VisualFlowEngineConstants.NodeCategories.Contains(category, StringComparer.Ordinal))
        {
            validation.Add(FieldError("category", "Category must be trigger, action, gate, transform, or output."));
        }
        if (validation.Count > 0)
        {
            return Success(new VisualFlowEngineOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var nodeTypeId = ReadString(request, "nodeTypeId")!.Trim();
        var operationKey = $"{VisualFlowEngineConstants.FlowSlug}:node-type:{CurrentTenant.TenantId}:{nodeTypeId}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var lockKey = $"node-type-reg-lock:{nodeTypeId}";
        var existingLock = await _documents.GetAsync(VisualFlowEngineConstants.NodeTypeLocks, lockKey, cancellationToken).ConfigureAwait(false);
        if (existingLock.Data is not null && string.Equals(ReadString(existingLock.Data.Body, "status"), "HELD", StringComparison.Ordinal))
        {
            var rejected = ErrorBody("NODE_TYPE_ALREADY_EXISTS", "Node type registration lock is already held.");
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.NodeTypeAlreadyExistsEvent, nodeTypeId, rejected), cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(409, rejected, true, true));
        }

        await _documents.UpsertAsync(VisualFlowEngineConstants.NodeTypeLocks, lockKey, WithTenant(new Dictionary<string, object?>
        {
            ["lock_key"] = lockKey,
            ["node_type_id"] = nodeTypeId,
            ["status"] = "HELD",
            ["locked_at"] = Now(),
            ["expires_at"] = DateTimeOffset.UtcNow.Add(RegistrationLockTtl).ToString("O", CultureInfo.InvariantCulture)
        }), existingLock.Data?.Version, cancellationToken).ConfigureAwait(false);

        try
        {
            if (ReadBool(request, "simulateCapabilityWriteFailure"))
            {
                throw new InvalidOperationException("Simulated capability index write failure.");
            }

            var registeredAt = Now();
            await _documents.UpsertAsync(VisualFlowEngineConstants.NodeTypes, nodeTypeId, WithTenant(new Dictionary<string, object?>
            {
                ["node_type_id"] = nodeTypeId,
                ["category"] = category,
                ["schema"] = ReadDictionary(request, "schema"),
                ["capabilities"] = ReadStringArray(request, "capabilities"),
                ["registered_at"] = registeredAt,
                ["knowledge_scope"] = "GLOBAL"
            }), null, cancellationToken).ConfigureAwait(false);

            await _documents.UpsertAsync(VisualFlowEngineConstants.NodeCapabilities, $"capability:{nodeTypeId}", WithTenant(new Dictionary<string, object?>
            {
                ["node_type_id"] = nodeTypeId,
                ["capabilities"] = ReadStringArray(request, "capabilities"),
                ["indexed_at"] = registeredAt,
                ["knowledge_scope"] = "GLOBAL"
            }), null, cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["id"] = nodeTypeId,
                ["nodeTypeId"] = nodeTypeId,
                ["status"] = "REGISTERED",
                ["category"] = category,
                ["registeredAt"] = registeredAt
            };
            await StoreAuditAsync(VisualFlowEngineConstants.NodeTypeAudits, $"node-type:{nodeTypeId}:{Guid.NewGuid():N}", "NODE_TYPE_REGISTERED", response, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.NodeTypeRegisteredEvent, nodeTypeId, response), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(201, response, true, true));
        }
        catch (Exception ex)
        {
            await _documents.UpsertAsync(VisualFlowEngineConstants.NodeTypeLocks, lockKey, WithTenant(new Dictionary<string, object?>
            {
                ["lock_key"] = lockKey,
                ["node_type_id"] = nodeTypeId,
                ["status"] = "RELEASED_AFTER_FAILURE",
                ["released_at"] = Now(),
                ["reason"] = ex.Message
            }), null, cancellationToken).ConfigureAwait(false);
            var failure = ErrorBody("NODE_TYPE_WRITE_FAILED", "Node type registration dual-write failed.", new Dictionary<string, object?> { ["reason"] = ex.Message });
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.NodeTypeRegistrationFailedEvent, nodeTypeId, failure), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "NODE_TYPE_WRITE_FAILED", ex.Message, cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(500, failure, true, true));
        }
    }

    public async Task<DataProcessResult<VisualFlowEngineOutcome>> InjectCodeAsync(
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "nodeId", "version", "codeHash", "targetPath");
        if (request.ContainsKey("sourceCode") || request.ContainsKey("rawSource") || request.ContainsKey("promptText"))
        {
            validation.Add(FieldError("sourceCode", "Generated source, raw source, and prompt text are never accepted in request or event payloads; use codeHash only."));
        }
        if (validation.Count > 0)
        {
            return Success(new VisualFlowEngineOutcome(400, ValidationBody(validation), false, false));
        }

        if (RequireIdempotency(idempotencyKey) is { } idempotencyError)
        {
            return Success(idempotencyError);
        }

        var nodeId = ReadString(request, "nodeId")!.Trim();
        var version = ReadString(request, "version")!.Trim();
        var operationKey = $"{VisualFlowEngineConstants.FlowSlug}:inject:{CurrentTenant.TenantId}:{nodeId}:{version}:{idempotencyKey}";
        if (await ReplayIfDuplicateAsync(operationKey, idempotencyKey, cancellationToken).ConfigureAwait(false) is { } replay)
        {
            return Success(replay);
        }

        var lockKey = $"injection-version-lock:{nodeId}:{version}";
        var existingLock = await _documents.GetAsync(VisualFlowEngineConstants.InjectionLocks, lockKey, cancellationToken).ConfigureAwait(false);
        if (existingLock.Data is not null)
        {
            var rejected = ErrorBody("INJECTION_CONFLICT", "This node version is already locked for injection.");
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.InjectionConflictEvent, nodeId, rejected), cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(409, rejected, true, true));
        }

        await _documents.UpsertAsync(VisualFlowEngineConstants.InjectionLocks, lockKey, WithTenant(new Dictionary<string, object?>
        {
            ["lock_key"] = lockKey,
            ["node_id"] = nodeId,
            ["version"] = version,
            ["status"] = "HELD",
            ["locked_at"] = Now(),
            ["expires_at"] = DateTimeOffset.UtcNow.Add(InjectionLockTtl).ToString("O", CultureInfo.InvariantCulture)
        }), null, cancellationToken).ConfigureAwait(false);

        await _documents.UpsertAsync(VisualFlowEngineConstants.InjectionAudits, $"pre:{nodeId}:{version}:{Guid.NewGuid():N}", WithTenant(new Dictionary<string, object?>
        {
            ["node_id"] = nodeId,
            ["version"] = version,
            ["phase"] = "PRE_WRITE",
            ["rollback_pointer"] = ReadString(request, "rollbackPointer") ?? $"rollback:{nodeId}:{version}",
            ["code_hash"] = ReadString(request, "codeHash"),
            ["created_at"] = Now(),
            ["knowledge_scope"] = "PRIVATE"
        }), null, cancellationToken).ConfigureAwait(false);

        if (ReadBool(request, "simulateInjectionFailure"))
        {
            var failed = ErrorBody("INJECTION_FAILED", "Code injection failed after rollback audit was written.");
            await _documents.UpsertAsync(VisualFlowEngineConstants.InjectionAudits, $"failed:{nodeId}:{version}:{Guid.NewGuid():N}", WithTenant(new Dictionary<string, object?>
            {
                ["node_id"] = nodeId,
                ["version"] = version,
                ["phase"] = "FAILED",
                ["reason"] = "simulated",
                ["created_at"] = Now()
            }), null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.InjectionFailedEvent, nodeId, failed), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "INJECTION_FAILED", "Simulated injection failure", cancellationToken).ConfigureAwait(false);
            return Success(new VisualFlowEngineOutcome(500, failed, true, true));
        }

        var injectedAt = Now();
        var resultBody = WithTenant(new Dictionary<string, object?>
        {
            ["node_id"] = nodeId,
            ["version"] = version,
            ["target_path"] = ReadString(request, "targetPath"),
            ["code_hash"] = ReadString(request, "codeHash"),
            ["status"] = "INJECTED",
            ["injected_at"] = injectedAt,
            ["knowledge_scope"] = "PRIVATE"
        });
        await _documents.UpsertAsync(VisualFlowEngineConstants.InjectionResults, $"{nodeId}:{version}", resultBody, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(VisualFlowEngineConstants.InjectionAudits, $"complete:{nodeId}:{version}:{Guid.NewGuid():N}", WithTenant(new Dictionary<string, object?>
        {
            ["node_id"] = nodeId,
            ["version"] = version,
            ["phase"] = "COMPLETE",
            ["result_hash"] = Hash(JsonSerializer.Serialize(resultBody)),
            ["created_at"] = injectedAt
        }), null, cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["id"] = $"{nodeId}:{version}",
            ["nodeId"] = nodeId,
            ["version"] = version,
            ["status"] = "INJECTED",
            ["codeHash"] = ReadString(request, "codeHash"),
            ["injectedAt"] = injectedAt
        };
        await _outbox.AppendAsync(CreateEvent(VisualFlowEngineConstants.CodeInjectedEvent, nodeId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new VisualFlowEngineOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<VisualFlowEngineOutcome>> RunTestAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        if (RejectsTenantScope(request) is { } tenantScopeError)
        {
            return Success(tenantScopeError);
        }

        var validation = ValidateRequired(request, "flowId");
        if (validation.Count > 0)
        {
            return Success(new VisualFlowEngineOutcome(400, ValidationBody(validation), false, false));
        }

        var flowId = ReadString(request, "flowId")!.Trim();
        var canvas = await _documents.GetAsync(VisualFlowEngineConstants.FlowCanvases, flowId, cancellationToken).ConfigureAwait(false);
        if (canvas.Data is null)
        {
            return Success(new VisualFlowEngineOutcome(404, ErrorBody("FLOW_NOT_FOUND", "Flow canvas was not found."), false, false));
        }

        var nodes = ReadNodes(canvas.Data.Body);
        var runId = $"run-{Hash($"{CurrentTenant.TenantId}:{flowId}:{Now()}")[..16]}";
        var trace = nodes.Select((node, index) => new Dictionary<string, object?>
        {
            ["nodeId"] = node.Id,
            ["order"] = index + 1,
            ["status"] = "SUCCESS",
            ["inputType"] = node.InputType,
            ["outputType"] = node.OutputType
        }).ToArray();
        var body = WithTenant(new Dictionary<string, object?>
        {
            ["run_id"] = runId,
            ["flow_id"] = flowId,
            ["status"] = "SUCCESS",
            ["trace"] = trace,
            ["created_at"] = Now()
        });
        await _documents.UpsertAsync(VisualFlowEngineConstants.TestRuns, runId, body, null, cancellationToken).ConfigureAwait(false);
        return Success(new VisualFlowEngineOutcome(200, new Dictionary<string, object?>
        {
            ["id"] = runId,
            ["runId"] = runId,
            ["flowId"] = flowId,
            ["status"] = "SUCCESS",
            ["trace"] = trace
        }, true, false));
    }

    public async Task<DataProcessResult<VisualFlowEngineDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var canvases = await SearchTenantAsync(VisualFlowEngineConstants.FlowCanvases, cancellationToken).ConfigureAwait(false);
        var nodeTypes = await SearchTenantAsync(VisualFlowEngineConstants.NodeTypes, cancellationToken).ConfigureAwait(false);
        var injections = await SearchTenantAsync(VisualFlowEngineConstants.InjectionResults, cancellationToken).ConfigureAwait(false);
        var runs = await SearchTenantAsync(VisualFlowEngineConstants.TestRuns, cancellationToken).ConfigureAwait(false);
        var canvasBodies = (canvases.Data ?? []).Select(record => record.Body).ToArray();
        var nodeTypeBodies = (nodeTypes.Data ?? []).Select(record => record.Body).ToArray();
        var injectionBodies = (injections.Data ?? []).Select(record => record.Body).ToArray();
        var runBodies = (runs.Data ?? []).Select(record => record.Body).ToArray();
        _ = _freedom;
        return Success(new VisualFlowEngineDashboard(
            canvasBodies,
            nodeTypeBodies,
            injectionBodies,
            runBodies,
            new Dictionary<string, object?>
            {
                ["draftFlows"] = canvasBodies.Count(body => string.Equals(ReadString(body, "status"), "DRAFT", StringComparison.Ordinal)),
                ["publishedFlows"] = canvasBodies.Count(body => string.Equals(ReadString(body, "status"), "PUBLISHED", StringComparison.Ordinal)),
                ["registeredNodeTypes"] = nodeTypeBodies.Length,
                ["successfulTestRuns"] = runBodies.Count(body => string.Equals(ReadString(body, "status"), "SUCCESS", StringComparison.Ordinal))
            }));
    }

    private SemaphoreSlim GetResourceLock(string key) => _resourceLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private async Task<VisualFlowEngineOutcome?> ReplayIfDuplicateAsync(string operationKey, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (RequireIdempotency(idempotencyKey) is { } error)
        {
            return error;
        }

        var reservation = await _idempotency.TryReserveAsync(operationKey, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return new VisualFlowEngineOutcome(500, ErrorBody(reservation.Error!.Code, reservation.Error.Message), false, false);
        }

        return reservation.FromCache && reservation.Data?.CachedResponse is not null
            ? new VisualFlowEngineOutcome(200, reservation.Data.CachedResponse, true, false)
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
            VisualFlowEngineConstants.FlowId,
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
                ["xiigenflowid"] = VisualFlowEngineConstants.FlowId
            });

    private static List<Dictionary<string, object?>> ValidateCanvas(IReadOnlyList<FlowNode> nodes, IReadOnlyList<FlowEdge> edges)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (nodes.Count == 0)
        {
            errors.Add(FieldError("nodes", "At least one node is required."));
            return errors;
        }

        foreach (var taskType in VisualFlowEngineConstants.RequiredTaskTypes)
        {
            if (!nodes.Any(node => string.Equals(node.TaskTypeId, taskType, StringComparison.Ordinal)))
            {
                errors.Add(FieldError("nodes", $"Every FLOW-18 task type must have a plan node. Missing {taskType}."));
            }
        }

        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add(FieldError("node.id", "Node id is required."));
            }
            if (string.IsNullOrWhiteSpace(node.NodeTypeId))
            {
                errors.Add(FieldError("node.nodeTypeId", "Node type id is required."));
            }
            if (!VisualFlowEngineConstants.NodeCategories.Contains(node.Category, StringComparer.Ordinal))
            {
                errors.Add(FieldError("node.category", "Node category is unknown."));
            }
            if (node.Config.TryGetValue("providerSdkImport", out var providerImport) && IsTruthy(providerImport))
            {
                errors.Add(FieldError("node.config.providerSdkImport", "Plan steps must use fabric interfaces and cannot import provider SDKs."));
            }
            if (node.Config.TryGetValue("entitySpecificController", out var controller) && IsTruthy(controller))
            {
                errors.Add(FieldError("node.config.entitySpecificController", "Entity-specific controllers are forbidden."));
            }
            if (!string.Equals(ReadString(node.Config, "returns"), "DataProcessResult", StringComparison.Ordinal))
            {
                errors.Add(FieldError("node.config.returns", "Every step must return DataProcessResult."));
            }
        }

        foreach (var edge in edges)
        {
            if (!nodes.Any(node => node.Id == edge.From))
            {
                errors.Add(FieldError("edges.from", $"Unknown source node {edge.From}."));
            }
            if (!nodes.Any(node => node.Id == edge.To))
            {
                errors.Add(FieldError("edges.to", $"Unknown target node {edge.To}."));
            }
        }

        return errors;
    }

    private static string[]? DetectCycle(IReadOnlyList<FlowNode> nodes, IReadOnlyList<FlowEdge> edges)
    {
        var colors = nodes.ToDictionary(node => node.Id, _ => "WHITE", StringComparer.Ordinal);
        var adjacency = nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            if (adjacency.TryGetValue(edge.From, out var list))
            {
                list.Add(edge.To);
            }
        }

        var stack = new Stack<string>();
        bool Dfs(string node)
        {
            colors[node] = "GRAY";
            stack.Push(node);
            foreach (var next in adjacency[node])
            {
                if (!colors.ContainsKey(next))
                {
                    continue;
                }
                if (colors[next] == "GRAY")
                {
                    stack.Push(next);
                    return true;
                }
                if (colors[next] == "WHITE" && Dfs(next))
                {
                    return true;
                }
            }
            _ = stack.Pop();
            colors[node] = "BLACK";
            return false;
        }

        foreach (var node in nodes.Select(node => node.Id))
        {
            if (colors[node] == "WHITE" && Dfs(node))
            {
                return stack.Reverse().Distinct(StringComparer.Ordinal).ToArray();
            }
        }

        return null;
    }

    private static FlowEdge? FindTypeMismatch(IReadOnlyList<FlowEdge> edges) =>
        edges.FirstOrDefault(edge =>
            !string.IsNullOrWhiteSpace(edge.SourceOutputType) &&
            !string.IsNullOrWhiteSpace(edge.TargetInputType) &&
            !string.Equals(edge.SourceOutputType, edge.TargetInputType, StringComparison.Ordinal));

    private static List<FlowNode> ReadNodes(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "nodes").Select(item => new FlowNode(
            ReadString(item, "id") ?? string.Empty,
            ReadString(item, "nodeTypeId") ?? ReadString(item, "node_type_id") ?? string.Empty,
            ReadString(item, "category") ?? string.Empty,
            ReadString(item, "taskTypeId") ?? ReadString(item, "task_type_id") ?? string.Empty,
            ReadString(item, "inputType") ?? ReadString(item, "input_type") ?? "json",
            ReadString(item, "outputType") ?? ReadString(item, "output_type") ?? "json",
            ReadDictionary(item, "config"))).ToList();

    private static List<FlowEdge> ReadEdges(IReadOnlyDictionary<string, object?> body) =>
        ReadArray(body, "edges").Select(item => new FlowEdge(
            ReadString(item, "from") ?? string.Empty,
            ReadString(item, "to") ?? string.Empty,
            ReadString(item, "sourceOutputType") ?? ReadString(item, "source_output_type") ?? "json",
            ReadString(item, "targetInputType") ?? ReadString(item, "target_input_type") ?? "json")).ToList();

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
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => ReadJsonObject(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ReadJsonValue).ToArray(),
        _ => value.ToString()
    };

    private static string[] ReadStringArray(IReadOnlyDictionary<string, object?> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }
        if (value is string[] array)
        {
            return array;
        }
        if (value is IEnumerable<string> strings)
        {
            return strings.ToArray();
        }
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(text => text.Length > 0).ToArray();
        }
        return [];
    }

    private static IReadOnlyDictionary<string, object?> NodeToDictionary(FlowNode node) => new Dictionary<string, object?>
    {
        ["id"] = node.Id,
        ["nodeTypeId"] = node.NodeTypeId,
        ["category"] = node.Category,
        ["taskTypeId"] = node.TaskTypeId,
        ["inputType"] = node.InputType,
        ["outputType"] = node.OutputType,
        ["config"] = node.Config
    };

    private static IReadOnlyDictionary<string, object?> EdgeToDictionary(FlowEdge edge) => new Dictionary<string, object?>
    {
        ["from"] = edge.From,
        ["to"] = edge.To,
        ["sourceOutputType"] = edge.SourceOutputType,
        ["targetInputType"] = edge.TargetInputType
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

    private static VisualFlowEngineOutcome? RequireIdempotency(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? new VisualFlowEngineOutcome(400, ErrorBody("IDEMPOTENCY_KEY_REQUIRED", "X-Idempotency-Key is required."), false, false)
            : null;

    private static VisualFlowEngineOutcome? RejectsTenantScope(IReadOnlyDictionary<string, object?> request)
    {
        var forbidden = request.Keys.FirstOrDefault(key => ForbiddenPayloadKeys.Any(forbiddenKey => string.Equals(key, forbiddenKey, StringComparison.OrdinalIgnoreCase)));
        return forbidden is null
            ? null
            : new VisualFlowEngineOutcome(400, ErrorBody("TENANT_SCOPE_FORBIDDEN", $"{forbidden} is resolved from trusted platform context, never request body."), false, false);
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
