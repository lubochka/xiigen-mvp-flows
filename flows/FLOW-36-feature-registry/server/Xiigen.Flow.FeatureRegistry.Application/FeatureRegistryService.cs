using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.FeatureRegistry.Application;

public sealed class FeatureRegistryService : MicroserviceBase<FeatureRegistryService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "featureTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public FeatureRegistryService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<FeatureRegistryService> logger)
        : base(new ServiceDescriptor("feature-registry-service", "Feature Registry", FeatureRegistryConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<FeatureRegistryOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = FeatureRegistryConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-808-CF-1049" }, false));
    }

    public async Task<DataProcessResult<FeatureRegistryOutcome>> RegisterFeatureAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "ftId", "capabilityName", "sourcePlatform", "targetPlatform", "classificationSource");
        if (ReadInt(request, "signalScore") <= 0) errors.Add(FieldError("signalScore", "signalScore must be greater than zero."));
        if (ReadInt(request, "costEstimate") < 0) errors.Add(FieldError("costEstimate", "costEstimate cannot be negative."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("feature-registry.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);
        if (request.ContainsKey("tenantPortingCandidateOverride")) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "MACHINE_CLASSIFICATION_LOCKED", ["ruleId"] = "CF-813" }, false));

        var operation = OperationKey("feature", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var ftId = ReadString(request, "ftId");
        var featureId = ScopedId($"feature:{ftId}");
        var existing = await GetTenantDocumentAsync(FeatureRegistryConstants.FeatureCollection, featureId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1035" }, false));
        }

        var portingCandidate = ReadBool(request, "portingCandidate");
        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var decision = DetermineDecision(portingCandidate, ReadInt(request, "signalScore"), ReadInt(request, "costEstimate"));
        var feature = new Dictionary<string, object?>
        {
            ["id"] = featureId,
            ["ftId"] = ftId,
            ["capabilityName"] = ReadString(request, "capabilityName"),
            ["sourcePlatform"] = ReadString(request, "sourcePlatform"),
            ["targetPlatform"] = ReadString(request, "targetPlatform"),
            ["classificationSource"] = ReadString(request, "classificationSource"),
            ["portingCandidate"] = portingCandidate,
            ["signalScore"] = ReadInt(request, "signalScore"),
            ["costEstimate"] = ReadInt(request, "costEstimate"),
            ["decision"] = decision,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var saved = await _documents.UpsertAsync(FeatureRegistryConstants.FeatureCollection, featureId, feature, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<FeatureRegistryOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(FeatureRegistryConstants.SignalCollection, ScopedId($"signal:{ftId}:{nextVersion}"), new Dictionary<string, object?> { ["ftId"] = ftId, ["signalScore"] = ReadInt(request, "signalScore"), ["costEstimate"] = ReadInt(request, "costEstimate"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        if (decision == "PROHIBITED")
        {
            await _outbox.AppendAsync(Envelope("feature-registry.porting-prohibited", featureId, feature), cancellationToken).ConfigureAwait(false);
            await AuditAsync("FEATURE_PORTING_PROHIBITED", featureId, cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operation, feature, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(existing is null ? 201 : 200, feature, true, true));
        }

        var adapter = new Dictionary<string, object?> { ["ftId"] = ftId, ["targetPlatform"] = ReadString(request, "targetPlatform"), ["adapterPlan"] = $"{ftId}:{ReadString(request, "targetPlatform")}", ["tenantScope"] = CurrentTenant.TenantId };
        var simulation = new Dictionary<string, object?> { ["ftId"] = ftId, ["decision"] = decision, ["simulationStatus"] = decision == "APPROVE" ? "PASS" : "DEFERRED", ["tenantScope"] = CurrentTenant.TenantId };
        await _documents.UpsertAsync(FeatureRegistryConstants.AdapterCollection, ScopedId($"adapter:{ftId}:{ReadString(request, "targetPlatform")}"), adapter, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(FeatureRegistryConstants.SimulationCollection, ScopedId($"simulation:{ftId}:{nextVersion}"), simulation, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("feature-registry.porting-decision-ready", featureId, feature), cancellationToken).ConfigureAwait(false);
        await AuditAsync("FEATURE_PORTING_DECISION_APPENDED", featureId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, feature, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, feature, true, true));
    }

    public DataProcessResult<FeatureRegistryOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = FeatureRegistryConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = FeatureRegistryConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<FeatureRegistryDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var features = await _documents.SearchAsync(FeatureRegistryConstants.FeatureCollection, filter, cancellationToken).ConfigureAwait(false);
        var signals = await _documents.SearchAsync(FeatureRegistryConstants.SignalCollection, filter, cancellationToken).ConfigureAwait(false);
        var adapters = await _documents.SearchAsync(FeatureRegistryConstants.AdapterCollection, filter, cancellationToken).ConfigureAwait(false);
        var simulations = await _documents.SearchAsync(FeatureRegistryConstants.SimulationCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new FeatureRegistryDashboard(
            (features.Data ?? []).Select(record => record.Body).ToArray(),
            (signals.Data ?? []).Select(record => record.Body).ToArray(),
            (adapters.Data ?? []).Select(record => record.Body).ToArray(),
            (simulations.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["featureCount"] = features.Data?.Count ?? 0, ["signalCount"] = signals.Data?.Count ?? 0, ["adapterCount"] = adapters.Data?.Count ?? 0, ["simulationCount"] = simulations.Data?.Count ?? 0, ["requiredRuleCount"] = FeatureRegistryConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<FeatureRegistryOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<FeatureRegistryOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1038" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(FeatureRegistryConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{FeatureRegistryConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static FeatureRegistryOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static FeatureRegistryOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1037" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{FeatureRegistryConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private static string DetermineDecision(bool portingCandidate, int signalScore, int costEstimate) => !portingCandidate ? "PROHIBITED" : signalScore >= 75 && costEstimate <= 60 ? "APPROVE" : signalScore >= 50 ? "DEFER" : "BLOCK";
    private FeatureRegistryOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1036" }, false) : null;
    private FeatureRegistryOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<FeatureRegistryRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<FeatureRegistryRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<FeatureRegistryPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<FeatureRegistryPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
