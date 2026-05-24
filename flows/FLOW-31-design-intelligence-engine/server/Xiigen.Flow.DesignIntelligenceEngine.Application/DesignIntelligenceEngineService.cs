using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignIntelligenceEngine.Application;

public sealed class DesignIntelligenceEngineService : MicroserviceBase<DesignIntelligenceEngineService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public DesignIntelligenceEngineService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DesignIntelligenceEngineService> logger)
        : base(new ServiceDescriptor("design-intelligence-engine-service", "Design Intelligence Engine", DesignIntelligenceEngineConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<DesignIntelligenceEngineOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = DesignIntelligenceEngineConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid, ["requiredCount"] = DesignIntelligenceEngineConstants.RequiredBfaRules.Length }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1015-CF-1018" }, false));
    }

    public async Task<DataProcessResult<DesignIntelligenceEngineOutcome>> AnalyzeDesignAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "designFileKey", "screenName", "componentMap", "tokenSet", "requestedBy");
        if (ReadInt(request, "confidenceScore") <= 0) errors.Add(FieldError("confidenceScore", "confidenceScore must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout"))
        {
            await _outbox.AppendAsync(Envelope("design-intelligence.analysis.timeout-retained", ScopedId($"timeout:{Hash(idempotencyKey!)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
            return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1018" }, false, true));
        }

        var operation = OperationKey("analysis", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var analysisId = ScopedId($"analysis:{Hash(ReadString(request, "designFileKey") + ":" + ReadString(request, "screenName"))}");
        var existing = await GetTenantDocumentAsync(DesignIntelligenceEngineConstants.AnalysisCollection, analysisId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1015" }, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var tokenCount = CountObjectEntries(request, "tokenSet");
        var componentCount = CountObjectEntries(request, "componentMap");
        var body = new Dictionary<string, object?>
        {
            ["id"] = analysisId,
            ["designFileKey"] = ReadString(request, "designFileKey"),
            ["screenName"] = ReadString(request, "screenName"),
            ["requestedBy"] = ReadString(request, "requestedBy"),
            ["componentCount"] = componentCount,
            ["tokenCount"] = tokenCount,
            ["confidenceScore"] = ReadInt(request, "confidenceScore"),
            ["status"] = componentCount > 0 && tokenCount > 0 ? "analysis_ready" : "needs_review",
            ["sourceAlias"] = DesignIntelligenceEngineConstants.ImplementationAlias,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(DesignIntelligenceEngineConstants.AnalysisCollection, analysisId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<DesignIntelligenceEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("DESIGN_INTELLIGENCE_ANALYZED", analysisId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("design-intelligence.analysis.completed", analysisId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<DesignIntelligenceEngineOutcome>> ExtractTokensAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "designFileKey", "tokenSet", "extractionProfile");
        var tokenCount = CountObjectEntries(request, "tokenSet");
        if (tokenCount == 0) errors.Add(FieldError("tokenSet", "tokenSet must contain at least one design token."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("tokens", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var tokenRunId = ScopedId($"tokens:{Hash(ReadString(request, "designFileKey") + ":" + ReadString(request, "extractionProfile"))}");
        var body = new Dictionary<string, object?>
        {
            ["tokenRunId"] = tokenRunId,
            ["designFileKey"] = ReadString(request, "designFileKey"),
            ["extractionProfile"] = ReadString(request, "extractionProfile"),
            ["tokenCount"] = tokenCount,
            ["normalizedNamespace"] = $"figma31::{CurrentTenant.TenantId}::{ReadString(request, "designFileKey")}",
            ["tenantScope"] = CurrentTenant.TenantId,
            ["recordedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(DesignIntelligenceEngineConstants.TokenCollection, tokenRunId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<DesignIntelligenceEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("design-intelligence.tokens.extracted", tokenRunId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<DesignIntelligenceEngineOutcome>> ValidateComplianceAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "designFileKey", "componentMap", "ruleSet");
        if (CountObjectEntries(request, "componentMap") == 0) errors.Add(FieldError("componentMap", "componentMap must contain component references."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("compliance", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var complianceId = ScopedId($"compliance:{Hash(ReadString(request, "designFileKey") + ":" + ReadString(request, "ruleSet"))}");
        var body = new Dictionary<string, object?>
        {
            ["complianceId"] = complianceId,
            ["designFileKey"] = ReadString(request, "designFileKey"),
            ["ruleSet"] = ReadString(request, "ruleSet"),
            ["componentCount"] = CountObjectEntries(request, "componentMap"),
            ["governanceStatus"] = ReadBool(request, "forceConcern") ? "needs_review" : "approved",
            ["tenantScope"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(DesignIntelligenceEngineConstants.ComplianceCollection, complianceId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<DesignIntelligenceEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("design-intelligence.compliance.validated", complianceId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<DesignIntelligenceEngineOutcome>> PublishGovernanceAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "designFileKey", "reportName", "releaseChannel");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var operation = OperationKey("governance", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var reportId = ScopedId($"governance:{Hash(ReadString(request, "designFileKey") + ":" + ReadString(request, "reportName"))}");
        var body = new Dictionary<string, object?>
        {
            ["reportId"] = reportId,
            ["designFileKey"] = ReadString(request, "designFileKey"),
            ["reportName"] = ReadString(request, "reportName"),
            ["releaseChannel"] = ReadString(request, "releaseChannel"),
            ["state"] = "published",
            ["tenantScope"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(DesignIntelligenceEngineConstants.GovernanceCollection, reportId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<DesignIntelligenceEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("DESIGN_GOVERNANCE_PUBLISHED", reportId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("design-intelligence.governance.published", reportId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<DesignIntelligenceEngineOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = DesignIntelligenceEngineConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = DesignIntelligenceEngineConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<DesignIntelligenceEngineDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var analyses = await _documents.SearchAsync(DesignIntelligenceEngineConstants.AnalysisCollection, filter, cancellationToken).ConfigureAwait(false);
        var tokens = await _documents.SearchAsync(DesignIntelligenceEngineConstants.TokenCollection, filter, cancellationToken).ConfigureAwait(false);
        var compliance = await _documents.SearchAsync(DesignIntelligenceEngineConstants.ComplianceCollection, filter, cancellationToken).ConfigureAwait(false);
        var governance = await _documents.SearchAsync(DesignIntelligenceEngineConstants.GovernanceCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new DesignIntelligenceEngineDashboard(
            (analyses.Data ?? []).Select(record => record.Body).ToArray(),
            (tokens.Data ?? []).Select(record => record.Body).ToArray(),
            (compliance.Data ?? []).Select(record => record.Body).ToArray(),
            (governance.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["analysisCount"] = analyses.Data?.Count ?? 0, ["tokenRunCount"] = tokens.Data?.Count ?? 0, ["complianceCount"] = compliance.Data?.Count ?? 0, ["governanceReportCount"] = governance.Data?.Count ?? 0, ["requiredRuleCount"] = DesignIntelligenceEngineConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<DesignIntelligenceEngineOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(DesignIntelligenceEngineConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{DesignIntelligenceEngineConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static DesignIntelligenceEngineOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static DesignIntelligenceEngineOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1017" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{DesignIntelligenceEngineConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private DesignIntelligenceEngineOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1016" }, false) : null;
    private DesignIntelligenceEngineOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static int CountObjectEntries(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(value))?.Count ?? 0 : 0;
    private static IReadOnlyList<DesignIntelligenceRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<DesignIntelligenceRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<DesignIntelligencePlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<DesignIntelligencePlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
