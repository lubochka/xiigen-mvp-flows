using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.MetaArbitrationEngine.Application;

public sealed class MetaArbitrationEngineService : MicroserviceBase<MetaArbitrationEngineService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "conflictTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public MetaArbitrationEngineService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MetaArbitrationEngineService> logger)
        : base(new ServiceDescriptor("meta-arbitration-engine-service", "Meta Arbitration Engine", MetaArbitrationEngineConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<MetaArbitrationEngineOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = MetaArbitrationEngineConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1031-CF-1034" }, false));
    }

    public async Task<DataProcessResult<MetaArbitrationEngineOutcome>> ResolveConflictAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "conflictId", "roundId", "policySet", "primaryVerdict", "counterVerdict");
        if (ReadInt(request, "arbiterCount") <= 0) errors.Add(FieldError("arbiterCount", "arbiterCount must be greater than zero."));
        if (ReadInt(request, "policyWeight") <= 0) errors.Add(FieldError("policyWeight", "policyWeight must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("meta-arbitration-engine.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("decision", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var decisionId = ScopedId($"decision:{ReadString(request, "conflictId")}:{ReadString(request, "roundId")}");
        var existing = await GetTenantDocumentAsync(MetaArbitrationEngineConstants.DecisionCollection, decisionId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1031" }, false));
        }

        var escalationReasons = ReadStringArray(request, "escalationReasons");
        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var decision = DetermineDecision(ReadString(request, "primaryVerdict"), ReadString(request, "counterVerdict"), escalationReasons);
        var body = new Dictionary<string, object?>
        {
            ["id"] = decisionId,
            ["conflictId"] = ReadString(request, "conflictId"),
            ["roundId"] = ReadString(request, "roundId"),
            ["policySet"] = ReadString(request, "policySet"),
            ["primaryVerdict"] = ReadString(request, "primaryVerdict"),
            ["counterVerdict"] = ReadString(request, "counterVerdict"),
            ["arbiterCount"] = ReadInt(request, "arbiterCount"),
            ["policyWeight"] = ReadInt(request, "policyWeight"),
            ["decision"] = decision,
            ["escalationReasons"] = escalationReasons,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(MetaArbitrationEngineConstants.DecisionCollection, decisionId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaArbitrationEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(MetaArbitrationEngineConstants.ConflictCollection, ScopedId($"conflict:{ReadString(request, "conflictId")}"), new Dictionary<string, object?> { ["conflictId"] = ReadString(request, "conflictId"), ["roundId"] = ReadString(request, "roundId"), ["decision"] = decision, ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(MetaArbitrationEngineConstants.PanelCollection, ScopedId($"panel:{Hash(ReadString(request, "policySet"))}"), new Dictionary<string, object?> { ["policySet"] = ReadString(request, "policySet"), ["arbiterCount"] = ReadInt(request, "arbiterCount"), ["policyWeight"] = ReadInt(request, "policyWeight"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        if (decision is "ESCALATE" or "HALT")
        {
            var briefing = new Dictionary<string, object?> { ["conflictId"] = ReadString(request, "conflictId"), ["roundId"] = ReadString(request, "roundId"), ["decision"] = decision, ["options"] = escalationReasons.Count == 0 ? ["operator_review"] : escalationReasons, ["tenantScope"] = CurrentTenant.TenantId };
            await _documents.UpsertAsync(MetaArbitrationEngineConstants.BriefingCollection, ScopedId($"briefing:{ReadString(request, "conflictId")}:{ReadString(request, "roundId")}"), briefing, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("meta-arbitration-engine.escalation-briefing-ready", decisionId, briefing), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _outbox.AppendAsync(Envelope("meta-arbitration-engine.round-decision-ready", decisionId, body), cancellationToken).ConfigureAwait(false);
        }

        await AuditAsync("META_ARBITRATION_DECISION_APPENDED", decisionId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public DataProcessResult<MetaArbitrationEngineOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = MetaArbitrationEngineConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = MetaArbitrationEngineConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<MetaArbitrationEngineDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var decisions = await _documents.SearchAsync(MetaArbitrationEngineConstants.DecisionCollection, filter, cancellationToken).ConfigureAwait(false);
        var conflicts = await _documents.SearchAsync(MetaArbitrationEngineConstants.ConflictCollection, filter, cancellationToken).ConfigureAwait(false);
        var briefings = await _documents.SearchAsync(MetaArbitrationEngineConstants.BriefingCollection, filter, cancellationToken).ConfigureAwait(false);
        var panels = await _documents.SearchAsync(MetaArbitrationEngineConstants.PanelCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new MetaArbitrationEngineDashboard(
            (decisions.Data ?? []).Select(record => record.Body).ToArray(),
            (conflicts.Data ?? []).Select(record => record.Body).ToArray(),
            (briefings.Data ?? []).Select(record => record.Body).ToArray(),
            (panels.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["decisionCount"] = decisions.Data?.Count ?? 0, ["conflictCount"] = conflicts.Data?.Count ?? 0, ["briefingCount"] = briefings.Data?.Count ?? 0, ["panelCount"] = panels.Data?.Count ?? 0, ["requiredRuleCount"] = MetaArbitrationEngineConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<MetaArbitrationEngineOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<MetaArbitrationEngineOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1034" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(MetaArbitrationEngineConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{MetaArbitrationEngineConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static MetaArbitrationEngineOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static MetaArbitrationEngineOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1033" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{MetaArbitrationEngineConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private static string DetermineDecision(string primary, string counter, IReadOnlyList<string> escalationReasons) => escalationReasons.Count > 0 ? "ESCALATE" : string.Equals(primary, counter, StringComparison.OrdinalIgnoreCase) ? "ACCEPT" : "HALT";
    private MetaArbitrationEngineOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1032" }, false) : null;
    private MetaArbitrationEngineOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<MetaArbitrationRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<MetaArbitrationRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<MetaArbitrationPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<MetaArbitrationPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
