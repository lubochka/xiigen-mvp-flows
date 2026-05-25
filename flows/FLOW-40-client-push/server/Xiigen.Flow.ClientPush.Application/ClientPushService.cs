using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.ClientPush.Application;

public sealed class ClientPushService : MicroserviceBase<ClientPushService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "connectionTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IClientPushConnectionPool _pool;

    public ClientPushService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IClientPushConnectionPool pool,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<ClientPushService> logger)
        : base(new ServiceDescriptor("client-push-service", "Client Push", ClientPushConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _pool = pool;
    }

    public DataProcessResult<ClientPushOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = ClientPushConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-797,CF-798,CF-1062-CF-1065" }, false));
    }

    public DataProcessResult<ClientPushOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = ClientPushConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = ClientPushConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<ClientPushOutcome>> RegisterConnectionAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "correlationId", "activeFlowState");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("client-push.connection.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);
        if (ReadBool(request, "expiredCorrelation")) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "EXPIRED_CORRELATION_ID", ["ruleId"] = "CF-1064" }, false));

        var correlationId = ReadString(request, "correlationId");
        var expectedKey = $"connection:{correlationId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1063" }, false));
        }

        var documentId = ScopedId($"connection:{correlationId}");
        var existing = await GetTenantDocumentAsync(ClientPushConstants.ConnectionsCollection, documentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1062" }, false));
        }

        var operation = OperationKey("connection", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var activeFlowState = ReadObject(request, "activeFlowState");
        var registered = _pool.RegisterConnection(correlationId, activeFlowState);
        if (registered.IsFailure) return Failure<ClientPushOutcome>(registered.Error!.Code, registered.Error.Message, registered.Error.Details);

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = documentId,
            ["correlationId"] = correlationId,
            ["registered"] = true,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["connectionInfo"] = ToDictionary(registered.Data!),
            ["activeFlowState"] = activeFlowState,
            ["version"] = nextVersion,
            ["registeredAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var saved = await _documents.UpsertAsync(ClientPushConstants.ConnectionsCollection, documentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<ClientPushOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        var envelope = Envelope("client-push.connection-registered", documentId, body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<ClientPushOutcome>> BridgeEventAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        _ = expectedVersion;
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "correlationId", "eventId", "eventType", "data");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("client-push.bridge.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var targetDomain = ReadString(request, "targetDomain");
        if (!string.IsNullOrWhiteSpace(targetDomain) && ClientPushConstants.ProhibitedEmitDomains.Any(domain => targetDomain.StartsWith(domain, StringComparison.Ordinal)))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PROHIBITED_EMIT_DOMAIN", ["targetDomain"] = targetDomain, ["ruleId"] = "CF-797" }, false));
        }

        var correlationId = ReadString(request, "correlationId");
        var eventId = ReadString(request, "eventId");
        var expectedKey = $"bridge:{correlationId}:{eventId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1063" }, false));
        }

        var crossTenantProbe = _pool.HasCorrelationOutsideCurrentTenant(correlationId);
        if (crossTenantProbe.Data)
        {
            return Success(Outcome(403, new Dictionary<string, object?> { ["code"] = "CROSS_TENANT_DELIVERY_ATTEMPT", ["ruleId"] = "CF-798" }, false));
        }

        var operation = OperationKey("bridge", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var data = ReadObject(request, "data");
        var envelope = Envelope(ReadString(request, "eventType"), correlationId, data, eventId);
        var active = _pool.GetActiveConnections().Data ?? [];
        var target = active.Any(conn => string.Equals(conn.CorrelationId, correlationId, StringComparison.Ordinal));
        if (!target)
        {
            var discarded = new Dictionary<string, object?> { ["delivered"] = false, ["recipientCount"] = 0, ["discarded"] = true, ["reason"] = "connection_not_found_no_retry" };
            await _idempotency.MarkSucceededAsync(operation, discarded, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(200, discarded, false));
        }

        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        var delivered = _pool.PushEvent(correlationId, new ClientPushSseEvent(ReadString(request, "eventType"), data, eventId));
        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"delivery:{eventId}"),
            ["correlationId"] = correlationId,
            ["eventId"] = eventId,
            ["eventType"] = ReadString(request, "eventType"),
            ["delivered"] = delivered.Data,
            ["recipientCount"] = delivered.Data ? 1 : 0,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = 1,
            ["deliveredAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(ClientPushConstants.DeliveryAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<ClientPushOutcome>> RunKeepaliveAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var runId = ReadString(request, "runId", "default");
        var expectedKey = $"keepalive:{runId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1063" }, false));
        }

        var interval = ReadInt(request, "keepaliveIntervalMs", ClientPushConstants.DefaultKeepaliveIntervalMs);
        var threshold = ReadInt(request, "droppedThresholdMs", ClientPushConstants.DefaultDroppedThresholdMs);
        var errors = new List<IReadOnlyDictionary<string, object?>>();
        if (interval <= 0) errors.Add(FieldError("keepaliveIntervalMs", "keepaliveIntervalMs must be greater than zero and come from FREEDOM config."));
        if (threshold <= 0) errors.Add(FieldError("droppedThresholdMs", "droppedThresholdMs must be greater than zero and come from FREEDOM config."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("client-push.keepalive.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("keepalive", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var active = _pool.GetActiveConnections().Data ?? [];
        var now = DateTimeOffset.UtcNow;
        var pinged = 0;
        var cleaned = 0;
        foreach (var connection in active)
        {
            if (now - connection.LastAcknowledgedAt > TimeSpan.FromMilliseconds(threshold))
            {
                _pool.CloseConnection(connection.CorrelationId);
                cleaned++;
            }
            else
            {
                var delivered = _pool.PushEvent(connection.CorrelationId, new ClientPushSseEvent("keepalive", new Dictionary<string, object?> { ["timestamp"] = now.ToString("O", CultureInfo.InvariantCulture) }, null));
                if (delivered.Data) pinged++;
            }
        }

        var body = new Dictionary<string, object?>
        {
            ["id"] = ScopedId($"keepalive:{runId}"),
            ["pinged"] = pinged,
            ["cleaned"] = cleaned,
            ["keepaliveIntervalMs"] = interval,
            ["droppedThresholdMs"] = threshold,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = 1,
            ["checkedAt"] = now.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(ClientPushConstants.KeepaliveAuditCollection, ReadString(body, "id"), body, null, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true));
    }

    public async Task<DataProcessResult<ClientPushDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var connections = await _documents.SearchAsync(ClientPushConstants.ConnectionsCollection, filter, cancellationToken).ConfigureAwait(false);
        var deliveries = await _documents.SearchAsync(ClientPushConstants.DeliveryAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        var keepalive = await _documents.SearchAsync(ClientPushConstants.KeepaliveAuditCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new ClientPushDashboard(
            (connections.Data ?? []).Select(record => record.Body).ToArray(),
            (deliveries.Data ?? []).Select(record => record.Body).ToArray(),
            (keepalive.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["connectionCount"] = connections.Data?.Count ?? 0, ["deliveryCount"] = deliveries.Data?.Count ?? 0, ["keepaliveRunCount"] = keepalive.Data?.Count ?? 0, ["requiredRuleCount"] = ClientPushConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<ClientPushOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<ClientPushOutcome>> TimeoutAsync(string type, string idempotencyKey, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1065", ["tenantScope"] = CurrentTenant.TenantId };
        var envelope = Envelope(type, ScopedId($"timeout:{Hash(idempotencyKey)}"), body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, body, false, true));
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data, string? eventId = null) => new(eventId ?? Guid.NewGuid().ToString("n"), $"/xiigen/{ClientPushConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static ClientPushOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static ClientPushOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1064" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{ClientPushConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private ClientPushOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1063" }, false) : null;
    private ClientPushOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || request[field] is null || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key, bool fallback = false) => source.TryGetValue(key, out var value) ? bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed : fallback;
    private static IReadOnlyDictionary<string, object?> ReadObject(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? (JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []) : [];
    private static IReadOnlyDictionary<string, object?> ToDictionary(ClientPushConnectionInfo connection) => new Dictionary<string, object?> { ["tenantId"] = connection.TenantId, ["correlationId"] = connection.CorrelationId, ["connectedAt"] = connection.ConnectedAt.ToString("O", CultureInfo.InvariantCulture), ["lastAcknowledgedAt"] = connection.LastAcknowledgedAt.ToString("O", CultureInfo.InvariantCulture) };
    private static IReadOnlyList<ClientPushRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<ClientPushRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<ClientPushPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<ClientPushPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
