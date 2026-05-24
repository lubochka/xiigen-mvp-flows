using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.AdaptiveRagDeepResearch.Application;

public sealed class AdaptiveRagDeepResearchService : MicroserviceBase<AdaptiveRagDeepResearchService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public AdaptiveRagDeepResearchService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<AdaptiveRagDeepResearchService> logger)
        : base(new ServiceDescriptor("adaptive-rag-deep-research-service", "Adaptive RAG Deep Research", AdaptiveRagDeepResearchConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<AdaptiveRagDeepResearchOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var rules = ReadRules(request);
        if (rules.Count == 0) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_REQUIRED" }, false));
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = AdaptiveRagDeepResearchConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid, ["requiredCount"] = AdaptiveRagDeepResearchConstants.RequiredBfaRules.Length }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["requiredCount"] = AdaptiveRagDeepResearchConstants.RequiredBfaRules.Length, ["coverage"] = "CF-601-CF-626+CF-1007-CF-1010" }, false));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> SubmitResearchAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "researchGoal", "query", "retrievalMode", "traceId");
        if (ReadInt(request, "budgetCapTokens") <= 0) errors.Add(FieldError("budgetCapTokens", "budgetCapTokens must be greater than zero."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout"))
        {
            await _outbox.AppendAsync(Envelope("adaptive-rag.timeout-retained", ScopedId($"timeout:{Hash(idempotencyKey!)}"), new Dictionary<string, object?> { ["retryAfterMs"] = 5000, ["tenantId"] = CurrentTenant.TenantId }), cancellationToken).ConfigureAwait(false);
            return Success(Outcome(504, new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1010" }, false, true));
        }

        if (ContainsTenantInjection(ReadString(request, "query"))) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_INJECTION_REJECTED", ["ruleId"] = "CF-604" }, false));
        if (ReadBool(request, "allowSilentTruncation")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "BUDGET_EXCEEDED", ["ruleId"] = "CF-622" }, false));
        if (ReadInt(request, "selfReflectionTokenCount") > 0 && ReadInt(request, "selfReflectionTokenCount") > ReadInt(request, "retrievedTokenCount")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "SELF_REFLECTION_EXPANDS_CONTEXT", ["ruleId"] = "CF-617" }, false));
        if (string.Equals(ReadString(request, "hallucinationChecker"), "single-model", StringComparison.OrdinalIgnoreCase)) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "MULTI_MODEL_DISPATCHER_REQUIRED", ["ruleId"] = "CF-612" }, false));

        var operation = OperationKey("research", idempotencyKey!);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);
        var runId = ScopedId($"research:{Hash(ReadString(request, "researchGoal") + ":" + ReadString(request, "traceId"))}");
        var existing = await GetTenantDocumentAsync(AdaptiveRagDeepResearchConstants.ResearchRunCollection, runId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["ruleId"] = "CF-1007" }, false));
        }

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["researchRunId"] = runId,
            ["researchGoal"] = ReadString(request, "researchGoal"),
            ["query"] = ReadString(request, "query"),
            ["retrievalMode"] = ReadString(request, "retrievalMode"),
            ["traceId"] = ReadString(request, "traceId"),
            ["budgetCapTokens"] = ReadInt(request, "budgetCapTokens"),
            ["tenantId"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["indexVersion"] = ReadString(request, "indexVersion", "v1"),
            ["rollbackTarget"] = ReadString(request, "rollbackTarget", "v0"),
            ["searchNamespace"] = $"arag::{CurrentTenant.TenantId}",
            ["hybridFusionShape"] = "compatible",
            ["storedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.ResearchRunCollection, runId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("RESEARCH_RUN_STORED", runId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("adaptive-rag.research.requested", runId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> UpdateRoutingPolicyAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "policyId", "experimentId", "userId", "updateMode");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (string.Equals(ReadString(request, "updateMode"), "inline", StringComparison.OrdinalIgnoreCase)) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "POLICY_UPDATE_MUST_BE_QUEUED", ["ruleId"] = "CF-608" }, false));
        if (ReadBool(request, "overrideExplicitAiConfig")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "EXPLICIT_AI_CONFIG_OVERRIDE_REJECTED", ["ruleId"] = "CF-607" }, false));
        if (ReadBool(request, "armStructureChanged") && !ReadBool(request, "migrationPlanApproved")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "BANDIT_ARM_MIGRATION_REQUIRED", ["ruleId"] = "CF-624" }, false));
        if (ReadInt(request, "feedbackRunCount") < 100 && ReadInt(request, "windowHours") < 24) return Success(Outcome(202, new Dictionary<string, object?> { ["code"] = "FEEDBACK_WINDOW_OPEN", ["ruleId"] = "CF-626", ["minimumRuns"] = 100, ["minimumHours"] = 24 }, false));

        var allocationKey = Hash($"{CurrentTenant.TenantId}:{ReadString(request, "userId")}:{ReadString(request, "experimentId")}");
        var policyId = ScopedId($"policy:{Hash(ReadString(request, "policyId"))}");
        var body = new Dictionary<string, object?> { ["policyId"] = policyId, ["experimentId"] = ReadString(request, "experimentId"), ["allocationKey"] = allocationKey, ["status"] = "queued", ["tenantId"] = CurrentTenant.TenantId, ["feedbackRunCount"] = ReadInt(request, "feedbackRunCount"), ["windowHours"] = ReadInt(request, "windowHours") };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.RoutingPolicyCollection, policyId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("adaptive-rag.routing-policy.update-requested", policyId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("routing", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> PromotePromptAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "promptId", "versionId", "targetStage");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "mutateActiveVersion")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "PROMPT_VERSION_IMMUTABLE", ["ruleId"] = "CF-609" }, false));
        if (!ReadBool(request, "qualityGatePassed")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "QUALITY_GATE_REQUIRED", ["ruleId"] = "CF-610" }, false));
        if (ReadDecimal(request, "qualityRegressionPercent") > 3m) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "RAG_ASSET_REGRESSION", ["ruleId"] = "CF-615" }, false));
        if (!IsValidPromotion(ReadString(request, "currentStage"), ReadString(request, "targetStage"))) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "PROMOTION_LADDER_SKIP_REJECTED", ["ruleId"] = "CF-620" }, false));

        var promotionId = ScopedId($"prompt:{Hash(ReadString(request, "promptId") + ":" + ReadString(request, "versionId"))}");
        var body = new Dictionary<string, object?> { ["promotionId"] = promotionId, ["promptId"] = ReadString(request, "promptId"), ["versionId"] = ReadString(request, "versionId"), ["targetStage"] = ReadString(request, "targetStage"), ["status"] = "approved", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.PromptPromotionCollection, promotionId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("adaptive-rag.prompt.promoted", promotionId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("prompt", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> EditKnowledgeGraphAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "graphEditId", "editType");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (!ReadBool(request, "gateApproved")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "GRAPH_EDIT_GATE_REQUIRED", ["ruleId"] = "CF-616" }, false));
        if (ReadBool(request, "synchronousCommunityRebuild")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "COMMUNITY_REBUILD_ASYNC_ONLY", ["ruleId"] = "CF-605" }, false));
        if (ReadBool(request, "hardCodedMaxHops")) return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "FREEDOM_GRAPH_CONFIG_REQUIRED", ["ruleId"] = "CF-623" }, false));

        var graphEditId = ScopedId($"graph:{Hash(ReadString(request, "graphEditId"))}");
        var body = new Dictionary<string, object?> { ["graphEditId"] = graphEditId, ["editType"] = ReadString(request, "editType"), ["communityRebuildMode"] = "queued", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.GraphEditCollection, graphEditId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("adaptive-rag.graph.edit-requested", graphEditId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("graph", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> SubmitFeedbackAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "traceId", "feedbackType", "score");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var traceId = ReadString(request, "traceId");
        var runs = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.ResearchRunCollection, new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId, ["traceId"] = traceId }, cancellationToken).ConfigureAwait(false);
        if ((runs.Data?.Count ?? 0) == 0) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "TRACE_NOT_FOUND", ["ruleId"] = "CF-611" }, false));

        var feedbackId = ScopedId($"feedback:{Hash(traceId + ":" + idempotencyKey)}");
        var body = new Dictionary<string, object?> { ["feedbackId"] = feedbackId, ["traceId"] = traceId, ["feedbackType"] = ReadString(request, "feedbackType"), ["score"] = ReadString(request, "score"), ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.FeedbackCollection, feedbackId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("adaptive-rag.feedback.received", feedbackId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("feedback", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> RunStressTestsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var submitted = ReadStressResults(request);
        var ids = submitted.Select(test => test.StressTestId).ToArray();
        var missingStressTests = AdaptiveRagDeepResearchConstants.RequiredStressTests.Where(test => !ids.Contains(test, StringComparer.Ordinal)).ToArray();
        var failing = submitted.Where(test => !string.Equals(test.Status, "PASS", StringComparison.OrdinalIgnoreCase)).Select(test => test.StressTestId).ToArray();
        if (missingStressTests.Length > 0 || failing.Length > 0) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "STRESS_MATRIX_FAILED", ["missing"] = missingStressTests, ["failing"] = failing }, false));
        var runId = ScopedId($"stress:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?> { ["stressRunId"] = runId, ["status"] = "PASS", ["stressTestCount"] = submitted.Count, ["coverage"] = "ST-369-ST-389", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.StressCollection, runId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AdaptiveRagDeepResearchOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("adaptive-rag.stress.completed", runId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("stress", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<AdaptiveRagDeepResearchOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = AdaptiveRagDeepResearchConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        var firstStepIsRouteDecision = steps.Count > 0 && string.Equals(steps[0].TaskTypeId, "T441", StringComparison.Ordinal);
        if (missing.Length > 0 || duplicate || !firstStepIsRouteDecision) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate, ["t441First"] = firstStepIsRouteDecision, ["ruleId"] = "CF-601" }, false));
        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["t441First"] = true, ["requiredTasks"] = AdaptiveRagDeepResearchConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<AdaptiveRagDeepResearchDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var research = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.ResearchRunCollection, filter, cancellationToken).ConfigureAwait(false);
        var routing = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.RoutingPolicyCollection, filter, cancellationToken).ConfigureAwait(false);
        var prompts = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.PromptPromotionCollection, filter, cancellationToken).ConfigureAwait(false);
        var graph = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.GraphEditCollection, filter, cancellationToken).ConfigureAwait(false);
        var feedback = await _documents.SearchAsync(AdaptiveRagDeepResearchConstants.FeedbackCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new AdaptiveRagDeepResearchDashboard(
            (research.Data ?? []).Select(record => record.Body).ToArray(),
            (routing.Data ?? []).Select(record => record.Body).ToArray(),
            (prompts.Data ?? []).Select(record => record.Body).ToArray(),
            (graph.Data ?? []).Select(record => record.Body).ToArray(),
            (feedback.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["ruleCount"] = AdaptiveRagDeepResearchConstants.RequiredBfaRules.Length, ["stressTestCount"] = AdaptiveRagDeepResearchConstants.RequiredStressTests.Length, ["researchRuns"] = research.Data?.Count ?? 0, ["routingPolicies"] = routing.Data?.Count ?? 0, ["feedbackItems"] = feedback.Data?.Count ?? 0 }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantId"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<AdaptiveRagDeepResearchOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(AdaptiveRagDeepResearchConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantId"] = CurrentTenant.TenantId, ["auditMode"] = "APPEND_ONLY" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{AdaptiveRagDeepResearchConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static AdaptiveRagDeepResearchOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static AdaptiveRagDeepResearchOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1009" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{AdaptiveRagDeepResearchConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private AdaptiveRagDeepResearchOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private AdaptiveRagDeepResearchOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static bool ContainsTenantInjection(string value) => value.Contains("tenantId:", StringComparison.OrdinalIgnoreCase) || value.Contains("other-tenant", StringComparison.OrdinalIgnoreCase);
    private static bool IsValidPromotion(string currentStage, string targetStage)
    {
        var ladder = new[] { "GENERATED", "INJECTED", "MINIMAL", "CORE" };
        var current = Array.FindIndex(ladder, stage => string.Equals(stage, currentStage, StringComparison.OrdinalIgnoreCase));
        var target = Array.FindIndex(ladder, stage => string.Equals(stage, targetStage, StringComparison.OrdinalIgnoreCase));
        return current >= 0 && target == current + 1;
    }

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;
    private static decimal ReadDecimal(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    private static IReadOnlyList<AdaptiveRagPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<AdaptiveRagPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<AdaptiveRagRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<AdaptiveRagRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<AdaptiveRagStressResult> ReadStressResults(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("stressResults", out var value) && value is not null ? JsonSerializer.Deserialize<AdaptiveRagStressResult[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
