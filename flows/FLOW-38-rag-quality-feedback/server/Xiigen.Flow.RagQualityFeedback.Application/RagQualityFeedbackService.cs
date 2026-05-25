using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.RagQualityFeedback.Application;

public sealed class RagQualityFeedbackService : MicroserviceBase<RagQualityFeedbackService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "feedbackTenantId", "patternTenantId"];
    private static readonly string[] Outcomes = ["SUCCESS_WITHIN_BUDGET", "WASTED_CYCLE", "ESCALATION_REQUIRED"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public RagQualityFeedbackService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<RagQualityFeedbackService> logger)
        : base(new ServiceDescriptor("rag-quality-feedback-service", "RAG Quality Feedback", RagQualityFeedbackConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<RagQualityFeedbackOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = RagQualityFeedbackConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-1054-CF-1057" }, false));
    }

    public async Task<DataProcessResult<RagQualityFeedbackOutcome>> RecordFeedbackAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);

        var errors = ValidateRequired(request, "cycleId", "patternId", "retrievalId", "outcome");
        var outcome = ReadString(request, "outcome");
        if (!Outcomes.Contains(outcome, StringComparer.Ordinal)) errors.Add(FieldError("outcome", "outcome must be SUCCESS_WITHIN_BUDGET, WASTED_CYCLE, or ESCALATION_REQUIRED."));
        var previousScore = ReadDecimal(request, "previousQualityScore", decimal.MinValue);
        var af9Score = ReadDecimal(request, "af9Score", decimal.MinValue);
        if (previousScore < RagQualityFeedbackConstants.ScoreFloor || previousScore > RagQualityFeedbackConstants.ScoreCeiling) errors.Add(FieldError("previousQualityScore", "previousQualityScore must be between 0 and 1."));
        if (af9Score <= 0m || af9Score > 1m) errors.Add(FieldError("af9Score", "af9Score must be greater than 0 and no more than 1."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var cycleId = ReadString(request, "cycleId");
        var patternId = ReadString(request, "patternId");
        var expectedKey = $"{cycleId}:{patternId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1055" }, false));
        }

        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync(idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var documentId = ScopedId($"quality:{cycleId}:{patternId}");
        var existing = await GetTenantDocumentAsync(RagQualityFeedbackConstants.QualityUpdateCollection, documentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1054" }, false));
        }

        var operation = OperationKey("quality", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        if (!ReadBool(request, "hasDpoTriple", true))
        {
            var noUpdateBody = new Dictionary<string, object?>
            {
                ["code"] = "NO_OUTCOME_NO_UPDATE",
                ["cycleId"] = cycleId,
                ["patternId"] = patternId,
                ["stateWritten"] = false
            };
            await _idempotency.MarkSucceededAsync(operation, noUpdateBody, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(200, noUpdateBody, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var delta = outcome == "SUCCESS_WITHIN_BUDGET" ? RagQualityFeedbackConstants.DeltaMagnitude : outcome == "WASTED_CYCLE" ? -RagQualityFeedbackConstants.DeltaMagnitude : 0m;
        var newScore = Clamp(previousScore + delta);
        var updatedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var extractedRules = BuildDistilledRules(request, cycleId, outcome, updatedAt);

        var body = new Dictionary<string, object?>
        {
            ["id"] = documentId,
            ["cycleId"] = cycleId,
            ["patternId"] = patternId,
            ["retrievalId"] = ReadString(request, "retrievalId"),
            ["outcome"] = outcome,
            ["previousQualityScore"] = previousScore,
            ["delta"] = delta,
            ["newQualityScore"] = newScore,
            ["af9Score"] = af9Score,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["updatedAt"] = updatedAt,
            ["extractedRuleCount"] = extractedRules.Count
        };

        var saved = await _documents.UpsertAsync(RagQualityFeedbackConstants.QualityUpdateCollection, documentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<RagQualityFeedbackOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        await _documents.UpsertAsync(RagQualityFeedbackConstants.CycleOutcomeCollection, ScopedId($"cycle:{cycleId}"), new Dictionary<string, object?> { ["cycleId"] = cycleId, ["patternId"] = patternId, ["retrievalId"] = ReadString(request, "retrievalId"), ["outcome"] = outcome, ["af9Score"] = af9Score, ["tenantScope"] = CurrentTenant.TenantId, ["version"] = nextVersion, ["updatedAt"] = updatedAt }, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(RagQualityFeedbackConstants.RetrievalOutcomeCollection, ScopedId($"retrieval:{ReadString(request, "retrievalId")}:{patternId}"), new Dictionary<string, object?> { ["cycleId"] = cycleId, ["patternId"] = patternId, ["retrievalId"] = ReadString(request, "retrievalId"), ["outcome"] = outcome, ["previousQualityScore"] = previousScore, ["newQualityScore"] = newScore, ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(RagQualityFeedbackConstants.BatchRecordCollection, ScopedId($"batch:{cycleId}"), new Dictionary<string, object?> { ["cycleId"] = cycleId, ["updatedCount"] = outcome == "ESCALATION_REQUIRED" ? 0 : 1, ["skippedCount"] = 0, ["clampedCount"] = newScore == previousScore + delta ? 0 : 1, ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        if (outcome == "ESCALATION_REQUIRED")
        {
            await _documents.UpsertAsync(RagQualityFeedbackConstants.CapabilityGapCollection, ScopedId($"gap:{cycleId}:{patternId}"), new Dictionary<string, object?> { ["cycleId"] = cycleId, ["patternId"] = patternId, ["sourceFlow"] = RagQualityFeedbackConstants.FlowId, ["gapSignal"] = "retrieval-escalation-required", ["tenantScope"] = CurrentTenant.TenantId }, null, cancellationToken).ConfigureAwait(false);
        }

        foreach (var rule in extractedRules)
        {
            await _documents.UpsertAsync(RagQualityFeedbackConstants.DistilledRulesCollection, ScopedId(ReadString(rule, "ruleId")), rule, null, cancellationToken).ConfigureAwait(false);
        }

        await _outbox.AppendAsync(Envelope("rag-quality-feedback.quality-updated", documentId, body), cancellationToken).ConfigureAwait(false);
        await AuditAsync("RAG_QUALITY_FEEDBACK_RECORDED", documentId, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public DataProcessResult<RagQualityFeedbackOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = RagQualityFeedbackConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = RagQualityFeedbackConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<RagQualityFeedbackDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var cycleOutcomes = await _documents.SearchAsync(RagQualityFeedbackConstants.CycleOutcomeCollection, filter, cancellationToken).ConfigureAwait(false);
        var qualityUpdates = await _documents.SearchAsync(RagQualityFeedbackConstants.QualityUpdateCollection, filter, cancellationToken).ConfigureAwait(false);
        var batches = await _documents.SearchAsync(RagQualityFeedbackConstants.BatchRecordCollection, filter, cancellationToken).ConfigureAwait(false);
        var distilledRules = await _documents.SearchAsync(RagQualityFeedbackConstants.DistilledRulesCollection, filter, cancellationToken).ConfigureAwait(false);
        var gaps = await _documents.SearchAsync(RagQualityFeedbackConstants.CapabilityGapCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new RagQualityFeedbackDashboard(
            (cycleOutcomes.Data ?? []).Select(record => record.Body).ToArray(),
            (qualityUpdates.Data ?? []).Select(record => record.Body).ToArray(),
            (batches.Data ?? []).Select(record => record.Body).ToArray(),
            (distilledRules.Data ?? []).Select(record => record.Body).ToArray(),
            (gaps.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["cycleOutcomeCount"] = cycleOutcomes.Data?.Count ?? 0, ["qualityUpdateCount"] = qualityUpdates.Data?.Count ?? 0, ["distilledRuleCount"] = distilledRules.Data?.Count ?? 0, ["gapSignalCount"] = gaps.Data?.Count ?? 0, ["requiredRuleCount"] = RagQualityFeedbackConstants.RequiredBfaRules.Length }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<RagQualityFeedbackOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<RagQualityFeedbackOutcome>> TimeoutAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        await _outbox.AppendAsync(Envelope("rag-quality-feedback.timeout-retained", ScopedId($"timeout:{Hash(idempotencyKey)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantScope"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1057" }, false, true));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(RagQualityFeedbackConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantScope"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildDistilledRules(IReadOnlyDictionary<string, object?> request, string cycleId, string outcome, string updatedAt)
    {
        if (outcome != "SUCCESS_WITHIN_BUDGET") return [];
        var candidate = ReadString(request, "distilledRuleCandidate");
        var confidence = ReadDecimal(request, "distilledRuleConfidence", 0m);
        if (string.IsNullOrWhiteSpace(candidate) || confidence < RagQualityFeedbackConstants.DistilledRuleMinConfidence) return [];

        return
        [
            new Dictionary<string, object?>
            {
                ["ruleId"] = $"DR-{cycleId}-1",
                ["cycleId"] = cycleId,
                ["flowId"] = RagQualityFeedbackConstants.FlowId,
                ["ruleText"] = candidate,
                ["confidence"] = confidence,
                ["tenantScope"] = CurrentTenant.TenantId,
                ["extractedAt"] = updatedAt
            }
        ];
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{RagQualityFeedbackConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static RagQualityFeedbackOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static RagQualityFeedbackOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1056" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{RagQualityFeedbackConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private static decimal Clamp(decimal value) => Math.Min(RagQualityFeedbackConstants.ScoreCeiling, Math.Max(RagQualityFeedbackConstants.ScoreFloor, value));
    private RagQualityFeedbackOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1055" }, false) : null;
    private RagQualityFeedbackOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static decimal ReadDecimal(IReadOnlyDictionary<string, object?> source, string key, decimal fallback = 0m) => source.TryGetValue(key, out var value) && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key, bool fallback = false) => source.TryGetValue(key, out var value) ? bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed : fallback;
    private static IReadOnlyList<RagQualityFeedbackRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<RagQualityFeedbackRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<RagQualityFeedbackPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<RagQualityFeedbackPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
