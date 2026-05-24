using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.TenantLifecycleManager.Application;

public sealed class TenantLifecycleManagerService : MicroserviceBase<TenantLifecycleManagerService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public TenantLifecycleManagerService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<TenantLifecycleManagerService> logger)
        : base(new ServiceDescriptor("tenant-lifecycle-manager-service", "Tenant Lifecycle Manager", TenantLifecycleManagerConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<TenantLifecycleManagerOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = TenantLifecycleManagerConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid, ["requiredCount"] = TenantLifecycleManagerConstants.RequiredBfaRules.Length }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1011-CF-1014" }, false));
    }

    public async Task<DataProcessResult<TenantLifecycleManagerOutcome>> ProvisionTenantAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "targetTenantKey", "planCode", "region", "requestedBy");
        if (ReadInt(request, "quotaUnits") <= 0) errors.Add(FieldError("quotaUnits", "quotaUnits must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout"))
        {
            await _outbox.AppendAsync(Envelope("tenant-lifecycle.provision.timeout-retained", ScopedId($"timeout:{Hash(idempotencyKey!)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantId"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
            return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1014" }, false, true));
        }

        var operation = OperationKey("provision", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var lifecycleId = ScopedId($"lifecycle:{Hash(ReadString(request, "targetTenantKey"))}");
        var existing = await GetTenantDocumentAsync(TenantLifecycleManagerConstants.LifecycleCollection, lifecycleId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1011" }, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = lifecycleId,
            ["targetTenantKey"] = ReadString(request, "targetTenantKey"),
            ["planCode"] = ReadString(request, "planCode"),
            ["region"] = ReadString(request, "region"),
            ["requestedBy"] = ReadString(request, "requestedBy"),
            ["quotaUnits"] = ReadInt(request, "quotaUnits"),
            ["state"] = existing is null ? "provisioning" : ReadString(existing, "state", "provisioning"),
            ["healthScore"] = existing is null ? 100 : ReadInt(existing, "healthScore", 100),
            ["tenantId"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(TenantLifecycleManagerConstants.LifecycleCollection, lifecycleId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<TenantLifecycleManagerOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("TENANT_LIFECYCLE_PROVISIONED", lifecycleId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("tenant-lifecycle.provision.requested", lifecycleId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<TenantLifecycleManagerOutcome>> RecordHealthAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "targetTenantKey", "probeId", "status");
        var healthScore = ReadInt(request, "healthScore");
        if (healthScore is < 0 or > 100) errors.Add(FieldError("healthScore", "healthScore must be between 0 and 100."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("health", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var healthId = ScopedId($"health:{Hash(ReadString(request, "targetTenantKey") + ":" + ReadString(request, "probeId"))}");
        var body = new Dictionary<string, object?>
        {
            ["healthId"] = healthId,
            ["targetTenantKey"] = ReadString(request, "targetTenantKey"),
            ["probeId"] = ReadString(request, "probeId"),
            ["status"] = ReadString(request, "status"),
            ["healthScore"] = healthScore,
            ["tenantId"] = CurrentTenant.TenantId,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(TenantLifecycleManagerConstants.HealthCollection, healthId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<TenantLifecycleManagerOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("tenant-lifecycle.health.recorded", healthId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<TenantLifecycleManagerOutcome>> ScheduleOffboardingAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "targetTenantKey", "reason", "effectiveDate");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("offboarding", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var offboardingId = ScopedId($"offboarding:{Hash(ReadString(request, "targetTenantKey") + ":" + ReadString(request, "effectiveDate"))}");
        var body = new Dictionary<string, object?>
        {
            ["offboardingId"] = offboardingId,
            ["targetTenantKey"] = ReadString(request, "targetTenantKey"),
            ["reason"] = ReadString(request, "reason"),
            ["effectiveDate"] = ReadString(request, "effectiveDate"),
            ["state"] = "offboarding",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(TenantLifecycleManagerConstants.OffboardingCollection, offboardingId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<TenantLifecycleManagerOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("TENANT_LIFECYCLE_OFFBOARDING_SCHEDULED", offboardingId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("tenant-lifecycle.offboarding.scheduled", offboardingId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<TenantLifecycleManagerOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = TenantLifecycleManagerConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = TenantLifecycleManagerConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<TenantLifecycleManagerDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var lifecycles = await _documents.SearchAsync(TenantLifecycleManagerConstants.LifecycleCollection, filter, cancellationToken).ConfigureAwait(false);
        var health = await _documents.SearchAsync(TenantLifecycleManagerConstants.HealthCollection, filter, cancellationToken).ConfigureAwait(false);
        var offboarding = await _documents.SearchAsync(TenantLifecycleManagerConstants.OffboardingCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new TenantLifecycleManagerDashboard(
            (lifecycles.Data ?? []).Select(record => record.Body).ToArray(),
            (health.Data ?? []).Select(record => record.Body).ToArray(),
            (offboarding.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["lifecycleCount"] = lifecycles.Data?.Count ?? 0, ["healthCheckCount"] = health.Data?.Count ?? 0, ["offboardingCount"] = offboarding.Data?.Count ?? 0, ["requiredRuleCount"] = TenantLifecycleManagerConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantId"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<TenantLifecycleManagerOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(TenantLifecycleManagerConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantId"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{TenantLifecycleManagerConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static TenantLifecycleManagerOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static TenantLifecycleManagerOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1013" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{TenantLifecycleManagerConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private TenantLifecycleManagerOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1012" }, false) : null;
    private TenantLifecycleManagerOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<TenantLifecycleRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<TenantLifecycleRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<TenantLifecyclePlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<TenantLifecyclePlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
