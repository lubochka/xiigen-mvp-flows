using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignSystemGovernance.Application;

public sealed class DesignSystemGovernanceService : MicroserviceBase<DesignSystemGovernanceService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "designTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public DesignSystemGovernanceService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DesignSystemGovernanceService> logger)
        : base(new ServiceDescriptor("design-system-governance-service", "Design System Governance", DesignSystemGovernanceConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<DesignSystemGovernanceOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = DesignSystemGovernanceConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1050-CF-1053" }, false));
    }

    public async Task<DataProcessResult<DesignSystemGovernanceOutcome>> RegisterGovernanceAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "designSystemId", "stackFamily", "tokenSetName", "genesisFrame", "targetStack");
        if (ReadInt(request, "compatibilityScore") <= 0) errors.Add(FieldError("compatibilityScore", "compatibilityScore must be greater than zero."));
        if (ReadInt(request, "designDebtScore") < 0) errors.Add(FieldError("designDebtScore", "designDebtScore cannot be negative."));
        if (ReadInt(request, "tokenConsistencyScore") <= 0) errors.Add(FieldError("tokenConsistencyScore", "tokenConsistencyScore must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("design-system-governance.timeout-retained", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var operation = OperationKey("governance", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var designSystemId = ReadString(request, "designSystemId");
        var documentId = ScopedId($"design-system:{designSystemId}");
        var existing = await GetTenantDocumentAsync(DesignSystemGovernanceConstants.ClassificationCollection, documentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1050" }, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var classification = Classify(ReadInt(request, "compatibilityScore"), ReadInt(request, "designDebtScore"), ReadInt(request, "tokenConsistencyScore"));
        var body = new Dictionary<string, object?>
        {
            ["id"] = documentId,
            ["designSystemId"] = designSystemId,
            ["stackFamily"] = ReadString(request, "stackFamily"),
            ["tokenSetName"] = ReadString(request, "tokenSetName"),
            ["genesisFrame"] = ReadString(request, "genesisFrame"),
            ["targetStack"] = ReadString(request, "targetStack"),
            ["compatibilityScore"] = ReadInt(request, "compatibilityScore"),
            ["designDebtScore"] = ReadInt(request, "designDebtScore"),
            ["tokenConsistencyScore"] = ReadInt(request, "tokenConsistencyScore"),
            ["classification"] = classification,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var saved = await _documents.UpsertAsync(DesignSystemGovernanceConstants.ClassificationCollection, documentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<DesignSystemGovernanceOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(DesignSystemGovernanceConstants.PromptCollection, ScopedId($"prompt:{designSystemId}:{nextVersion}"), new Dictionary<string, object?> { ["designSystemId"] = designSystemId, ["genesisFrame"] = ReadString(request, "genesisFrame"), ["excludedIncompatibilities"] = ReadStringArray(request, "excludedIncompatibilities"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(DesignSystemGovernanceConstants.DebtCollection, ScopedId($"debt:{designSystemId}:{nextVersion}"), new Dictionary<string, object?> { ["designSystemId"] = designSystemId, ["designDebtScore"] = ReadInt(request, "designDebtScore"), ["tokenConsistencyScore"] = ReadInt(request, "tokenConsistencyScore"), ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(DesignSystemGovernanceConstants.CompatibilityCollection, ScopedId($"compatibility:{designSystemId}:{ReadString(request, "targetStack")}"), new Dictionary<string, object?> { ["designSystemId"] = designSystemId, ["targetStack"] = ReadString(request, "targetStack"), ["classification"] = classification, ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("design-system-governance.classification-ready", documentId, body), cancellationToken).ConfigureAwait(false);
        await AuditAsync("DESIGN_SYSTEM_CLASSIFICATION_APPENDED", documentId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public DataProcessResult<DesignSystemGovernanceOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = DesignSystemGovernanceConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = DesignSystemGovernanceConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<DesignSystemGovernanceDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var classifications = await _documents.SearchAsync(DesignSystemGovernanceConstants.ClassificationCollection, filter, cancellationToken).ConfigureAwait(false);
        var prompts = await _documents.SearchAsync(DesignSystemGovernanceConstants.PromptCollection, filter, cancellationToken).ConfigureAwait(false);
        var debts = await _documents.SearchAsync(DesignSystemGovernanceConstants.DebtCollection, filter, cancellationToken).ConfigureAwait(false);
        var compatibility = await _documents.SearchAsync(DesignSystemGovernanceConstants.CompatibilityCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new DesignSystemGovernanceDashboard(
            (classifications.Data ?? []).Select(record => record.Body).ToArray(),
            (prompts.Data ?? []).Select(record => record.Body).ToArray(),
            (debts.Data ?? []).Select(record => record.Body).ToArray(),
            (compatibility.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["classificationCount"] = classifications.Data?.Count ?? 0, ["promptCount"] = prompts.Data?.Count ?? 0, ["debtReportCount"] = debts.Data?.Count ?? 0, ["compatibilityReportCount"] = compatibility.Data?.Count ?? 0, ["requiredRuleCount"] = DesignSystemGovernanceConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<DesignSystemGovernanceOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<DesignSystemGovernanceOutcome>> TimeoutAsync(string eventType, string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope(eventType, ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1053" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(DesignSystemGovernanceConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{DesignSystemGovernanceConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static DesignSystemGovernanceOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static DesignSystemGovernanceOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1052" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{DesignSystemGovernanceConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private static string Classify(int compatibility, int debt, int consistency) => compatibility >= 80 && consistency >= 80 && debt <= 40 ? "APPROVED" : compatibility >= 60 && consistency >= 60 ? "NEEDS_REVIEW" : "BLOCKED";
    private DesignSystemGovernanceOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1051" }, false) : null;
    private DesignSystemGovernanceOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<DesignSystemGovernanceRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<DesignSystemGovernanceRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<DesignSystemGovernancePlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<DesignSystemGovernancePlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
