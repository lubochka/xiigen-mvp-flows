using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.SystemInitiationBootstrap.Application;

public sealed class SystemInitiationBootstrapService : MicroserviceBase<SystemInitiationBootstrapService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "bootstrapTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public SystemInitiationBootstrapService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<SystemInitiationBootstrapService> logger)
        : base(new ServiceDescriptor("system-initiation-bootstrap-service", "System Initiation Bootstrap", SystemInitiationBootstrapConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<SystemInitiationBootstrapOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = SystemInitiationBootstrapConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1023-CF-1026" }, false));
    }

    public async Task<DataProcessResult<SystemInitiationBootstrapOutcome>> StartBootstrapAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "planBundleRef", "schemaRegistryRef", "factoryRegistryRef", "skillsIndexRef", "smokeFlowId");
        if (ReadInt(request, "requiredFactoryCount") <= 0) errors.Add(FieldError("requiredFactoryCount", "requiredFactoryCount must be greater than zero."));
        if (ReadInt(request, "coreFlowCount") <= 0) errors.Add(FieldError("coreFlowCount", "coreFlowCount must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("system-bootstrap.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("bootstrap", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var bootstrapId = ScopedId($"bootstrap:{ReadString(request, "planBundleRef")}");
        var existing = await GetTenantDocumentAsync(SystemInitiationBootstrapConstants.BootstrapCollection, bootstrapId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1023" }, false));
        }

        var missingFactories = ReadStringArray(request, "missingFactories");
        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = bootstrapId,
            ["planBundleRef"] = ReadString(request, "planBundleRef"),
            ["schemaRegistryRef"] = ReadString(request, "schemaRegistryRef"),
            ["factoryRegistryRef"] = ReadString(request, "factoryRegistryRef"),
            ["skillsIndexRef"] = ReadString(request, "skillsIndexRef"),
            ["smokeFlowId"] = ReadString(request, "smokeFlowId"),
            ["requiredFactoryCount"] = ReadInt(request, "requiredFactoryCount"),
            ["coreFlowCount"] = ReadInt(request, "coreFlowCount"),
            ["missingFactories"] = missingFactories,
            ["bootstrapStatus"] = missingFactories.Count == 0 ? "ready_to_compile" : "capability_gaps_detected",
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(SystemInitiationBootstrapConstants.BootstrapCollection, bootstrapId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<SystemInitiationBootstrapOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(SystemInitiationBootstrapConstants.RegistryCollection, ScopedId($"registry:{Hash(ReadString(request, "planBundleRef"))}"), new Dictionary<string, object?> { ["planBundleRef"] = ReadString(request, "planBundleRef"), ["schemaRegistryRef"] = ReadString(request, "schemaRegistryRef"), ["factoryRegistryRef"] = ReadString(request, "factoryRegistryRef"), ["skillsIndexRef"] = ReadString(request, "skillsIndexRef"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        if (missingFactories.Count > 0)
        {
            await _documents.UpsertAsync(SystemInitiationBootstrapConstants.GapCollection, ScopedId($"gap:{Hash(string.Join('|', missingFactories))}"), new Dictionary<string, object?> { ["missingFactories"] = missingFactories, ["eventType"] = "CapabilityGapDetected", ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("system-bootstrap.capability-gap-detected", bootstrapId, body), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _documents.UpsertAsync(SystemInitiationBootstrapConstants.SmokeCollection, ScopedId($"smoke:{ReadString(request, "smokeFlowId")}"), new Dictionary<string, object?> { ["smokeFlowId"] = ReadString(request, "smokeFlowId"), ["result"] = "scheduled", ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(Envelope("system-bootstrap.core-flows-ready", bootstrapId, body), cancellationToken).ConfigureAwait(false);
        }

        await AuditAsync("SYSTEM_BOOTSTRAP_STARTED", bootstrapId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public DataProcessResult<SystemInitiationBootstrapOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = SystemInitiationBootstrapConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = SystemInitiationBootstrapConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<SystemInitiationBootstrapDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var runs = await _documents.SearchAsync(SystemInitiationBootstrapConstants.BootstrapCollection, filter, cancellationToken).ConfigureAwait(false);
        var registries = await _documents.SearchAsync(SystemInitiationBootstrapConstants.RegistryCollection, filter, cancellationToken).ConfigureAwait(false);
        var gaps = await _documents.SearchAsync(SystemInitiationBootstrapConstants.GapCollection, filter, cancellationToken).ConfigureAwait(false);
        var smoke = await _documents.SearchAsync(SystemInitiationBootstrapConstants.SmokeCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new SystemInitiationBootstrapDashboard(
            (runs.Data ?? []).Select(record => record.Body).ToArray(),
            (registries.Data ?? []).Select(record => record.Body).ToArray(),
            (gaps.Data ?? []).Select(record => record.Body).ToArray(),
            (smoke.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["bootstrapRunCount"] = runs.Data?.Count ?? 0, ["registryCount"] = registries.Data?.Count ?? 0, ["gapCount"] = gaps.Data?.Count ?? 0, ["smokeRunCount"] = smoke.Data?.Count ?? 0, ["requiredRuleCount"] = SystemInitiationBootstrapConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<SystemInitiationBootstrapOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<SystemInitiationBootstrapOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1026" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(SystemInitiationBootstrapConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{SystemInitiationBootstrapConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static SystemInitiationBootstrapOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static SystemInitiationBootstrapOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1025" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{SystemInitiationBootstrapConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private SystemInitiationBootstrapOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1024" }, false) : null;
    private SystemInitiationBootstrapOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<SystemBootstrapRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<SystemBootstrapRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<SystemBootstrapPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<SystemBootstrapPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
