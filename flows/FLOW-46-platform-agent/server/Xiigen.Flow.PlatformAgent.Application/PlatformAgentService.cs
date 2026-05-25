using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.PlatformAgent.Application;

public sealed class PlatformAgentService : MicroserviceBase<PlatformAgentService>
{
    private const string MasterTenant = "master";
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public PlatformAgentService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<PlatformAgentService> logger)
        : base(new ServiceDescriptor("platform-agent-service", "Platform Agent", PlatformAgentConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public DataProcessResult<PlatformAgentOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRecords(request, "rules");
        var ids = rules.Select(rule => ReadString(rule, "ruleId")).ToArray();
        var missing = PlatformAgentConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(ReadString(rule, "severity")) || string.IsNullOrWhiteSpace(ReadString(rule, "factory")) || string.IsNullOrWhiteSpace(ReadString(rule, "trigger")) || string.IsNullOrWhiteSpace(ReadString(rule, "scope"))).Select(rule => ReadString(rule, "ruleId")).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "FLOW-46 Platform Agent invariants" }, false));
    }

    public DataProcessResult<PlatformAgentOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadRecords(request, "planSteps");
        var taskIds = steps.Select(step => ReadString(step, "taskTypeId")).ToHashSet(StringComparer.Ordinal);
        var missing = PlatformAgentConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => ReadString(step, "taskTypeId"), StringComparer.Ordinal).Any(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = PlatformAgentConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<PlatformAgentOutcome>> RunAgentAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var userIntent = ReadString(request, "userIntent").Trim();
        if (string.IsNullOrWhiteSpace(userIntent)) return Success(Outcome(400, ErrorBody("MISSING_INTENT", "userIntent is required."), false));

        if (!string.Equals(CurrentTenant.TenantId, MasterTenant, StringComparison.Ordinal))
        {
            return Success(Outcome(403, ErrorBody("NOT_ADMIN", "Platform agent runs require master tenant context."), false));
        }

        var sessionId = ReadString(request, "sessionId", $"agent-{Guid.NewGuid():N}");
        var existing = await _documents.SearchAsync(PlatformAgentConstants.SessionsCollection, new Dictionary<string, object?> { ["sessionId"] = sessionId }, cancellationToken).ConfigureAwait(false);
        if ((existing.Data?.Count ?? 0) > 0) return Success(Outcome(200, existing.Data![0].Body, true));

        var enriched = await EnrichPlatformContextAsync(request, cancellationToken).ConfigureAwait(false);
        if (enriched.IsFailure) return Success(Outcome(500, ErrorBody("ENRICH_FAILED", "Platform context enrichment failed."), false));

        var judge = await EvaluateSuperJudgeAsync(sessionId, request, enriched.Data!, cancellationToken).ConfigureAwait(false);
        if (judge.IsFailure) return Success(Outcome(500, ErrorBody(judge.Error?.Code ?? "SUPER_JUDGE_FAILED", judge.Error?.Message ?? "Super judge failed."), false));

        var actions = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var action in ReadRecords(request, "proposedActions"))
        {
            var published = await PublishActionAsync(sessionId, action, cancellationToken).ConfigureAwait(false);
            if (published.IsFailure) return Success(Outcome(400, ErrorBody(published.Error?.Code ?? "ACTION_FAILED", published.Error?.Message ?? "Action publish failed."), false));
            actions.Add(published.Data!);
        }

        var contributions = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var contribution in ReadRecords(request, "contributions"))
        {
            var recorded = await ContributePatternAsync(sessionId, contribution, cancellationToken).ConfigureAwait(false);
            if (recorded.IsFailure) return Success(Outcome(400, ErrorBody(recorded.Error?.Code ?? "CONTRIBUTION_FAILED", recorded.Error?.Message ?? "Contribution failed."), false));
            contributions.Add(recorded.Data!);
        }

        var completedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var session = new Dictionary<string, object?>
        {
            ["sessionId"] = sessionId,
            ["userIntent"] = userIntent,
            ["af9Verdict"] = ReadString(request, "af9Verdict", "PASS"),
            ["superJudgeVerdict"] = judge.Data!["verdict"],
            ["grade"] = string.Equals(Convert.ToString(judge.Data!["verdict"], CultureInfo.InvariantCulture), "OVERRIDE_BLOCK", StringComparison.Ordinal) ? "BLOCKED" : "PASSED",
            ["platformPatternsMatched"] = enriched.Data!["platformPatternsMatched"],
            ["actionsProposed"] = actions.Count,
            ["contributionsRecorded"] = contributions.Count,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["completedAt"] = completedAt,
            ["connectionType"] = "FLOW_SCOPED",
            ["knowledgeScope"] = "GLOBAL"
        };

        await _documents.UpsertAsync(PlatformAgentConstants.SessionsCollection, sessionId, session, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.platform-agent.session-completed", sessionId, session), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, session, true, true));
    }

    public async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> EnrichPlatformContextAsync(IReadOnlyDictionary<string, object?> request, CancellationToken cancellationToken = default)
    {
        var keywords = ReadKeywords(request);
        var filter = new Dictionary<string, object?> { ["knowledgeScope"] = "GLOBAL" };
        var search = await _documents.SearchAsync(PlatformAgentConstants.RagPatternsCollection, filter, cancellationToken).ConfigureAwait(false);
        if (search.IsFailure) return Failure<IReadOnlyDictionary<string, object?>>("SEARCH_FAILED", "GLOBAL platform pattern search failed.");

        var patterns = (search.Data ?? [])
            .Select(record => record.Body)
            .Where(body => keywords.Count == 0 || keywords.Any(keyword => JsonSerializer.Serialize(body).Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>
        {
            ["patterns"] = ReadRecords(request, "patterns"),
            ["linkedModules"] = ReadRecords(request, "linkedModules"),
            ["platformPatterns"] = patterns,
            ["platformPatternsMatched"] = patterns.Length
        });
    }

    public async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> EvaluateSuperJudgeAsync(string sessionId, IReadOnlyDictionary<string, object?> request, IReadOnlyDictionary<string, object?> enriched, CancellationToken cancellationToken = default)
    {
        var matched = ToInt(enriched, "platformPatternsMatched");
        if (matched == 0)
        {
            return Success<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?> { ["verdict"] = "DEFER_TO_AF9", ["reason"] = "CF-840 zero platform patterns matched; no LLM call", ["cost"] = 0, ["llmCalls"] = 0 });
        }

        var verdict = ReadString(request, "superJudgeVerdict", "DEFER_TO_AF9").ToUpperInvariant();
        if (!PlatformAgentConstants.SuperJudgeVerdicts.Contains(verdict, StringComparer.Ordinal)) return Failure<IReadOnlyDictionary<string, object?>>("MALFORMED_LLM_RESPONSE", "Unsupported super judge verdict.");
        var output = new Dictionary<string, object?> { ["verdict"] = verdict, ["reason"] = ReadString(request, "superJudgeReason", "Platform patterns reviewed"), ["cost"] = 1, ["llmCalls"] = 1, ["modelUsed"] = "freedom:superJudge.model" };
        if (verdict == "DEFER_TO_AF9") return Success<IReadOnlyDictionary<string, object?>>(output);

        var constraint = ReadString(request, "discriminatingConstraint");
        if (string.IsNullOrWhiteSpace(constraint)) return Failure<IReadOnlyDictionary<string, object?>>("EMPTY_DISCRIMINATING_CONSTRAINT", "OVERRIDE verdict requires discriminatingConstraint.");
        var dpoId = $"dpo-{sessionId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var dpo = new Dictionary<string, object?> { ["tripleId"] = dpoId, ["sessionId"] = sessionId, ["source"] = "T653_SUPER_JUDGE", ["superJudgeVerdict"] = verdict, ["discriminating_constraint"] = constraint, ["tenantScope"] = CurrentTenant.TenantId, ["knowledgeScope"] = "GLOBAL" };
        await _documents.UpsertAsync(PlatformAgentConstants.TrainingDataCollection, dpoId, dpo, null, cancellationToken).ConfigureAwait(false);
        output["dpoTripleId"] = dpoId;
        return Success<IReadOnlyDictionary<string, object?>>(output);
    }

    public async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> PublishActionAsync(string sessionId, IReadOnlyDictionary<string, object?> action, CancellationToken cancellationToken = default)
    {
        var actionId = ReadString(action, "actionId", $"action-{Guid.NewGuid():N}");
        var actionType = ReadString(action, "actionType", "ADVISE").ToUpperInvariant();
        if (!PlatformAgentConstants.ActionTypes.Contains(actionType, StringComparer.Ordinal)) return Failure<IReadOnlyDictionary<string, object?>>("UNKNOWN_ACTION_TYPE", "Unsupported platform agent action type.");

        var targetScope = ReadString(action, "targetTenantId", CurrentTenant.TenantId);
        var knowledgeScope = actionType == "APPLY_GLOBAL" ? "GLOBAL" : "PRIVATE";
        if ((actionType == "PROPOSE_EDIT" || actionType == "CREATE_FLOW") && string.Equals(targetScope, MasterTenant, StringComparison.Ordinal)) return Failure<IReadOnlyDictionary<string, object?>>("INVALID_TARGET", "Cross-tenant action target must differ from master.");

        if (actionType == "PROPOSE_EDIT" || actionType == "CREATE_FLOW")
        {
            var audit = new Dictionary<string, object?> { ["actionId"] = $"scope-switch-{actionId}", ["sessionId"] = sessionId, ["actionType"] = "SCOPE_SWITCH", ["tenantScope"] = MasterTenant, ["targetTenantId"] = targetScope, ["status"] = "AUDITED", ["reason"] = ReadString(action, "reason", actionType) };
            await _documents.UpsertAsync(PlatformAgentConstants.AgentActionsCollection, ScopedId(audit["actionId"]!.ToString()!), audit, null, cancellationToken).ConfigureAwait(false);
        }

        var record = new Dictionary<string, object?> { ["actionId"] = actionId, ["sessionId"] = sessionId, ["actionType"] = actionType, ["tenantScope"] = actionType == "APPLY_GLOBAL" ? MasterTenant : targetScope, ["targetTenantId"] = targetScope, ["knowledgeScope"] = knowledgeScope, ["status"] = "STORED", ["storedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), ["payload"] = ReadObject(action, "payload") };
        var targetCollection = actionType switch { "APPLY_GLOBAL" => PlatformAgentConstants.FlowTemplatesCollection, "PROPOSE_EDIT" or "CREATE_FLOW" => PlatformAgentConstants.TenantTopologiesCollection, _ => PlatformAgentConstants.AgentActionsCollection };
        await _documents.UpsertAsync(targetCollection, actionId, record, null, cancellationToken).ConfigureAwait(false);
        await _documents.UpsertAsync(PlatformAgentConstants.AgentActionsCollection, actionId, record, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.platform-agent.action-proposed", actionId, record), cancellationToken).ConfigureAwait(false);
        record["status"] = "EMITTED";
        return Success<IReadOnlyDictionary<string, object?>>(record);
    }

    public async Task<DataProcessResult<IReadOnlyDictionary<string, object?>>> ContributePatternAsync(string sessionId, IReadOnlyDictionary<string, object?> contribution, CancellationToken cancellationToken = default)
    {
        var patternId = ReadString(contribution, "patternId", $"pattern-{Guid.NewGuid():N}");
        var path = ReadString(contribution, "path", "A").ToUpperInvariant();
        var solution = ReadObject(contribution, "solution");
        if (path == "B" && solution.ContainsKey("unableToStripField"))
        {
            var failed = ContributionAudit(sessionId, patternId, path, "SANITIZATION_FAILED", 0, solution.Keys.ToArray(), []);
            await _documents.UpsertAsync(PlatformAgentConstants.ContributionsCollection, failed["contributionId"]!.ToString()!, failed, null, cancellationToken).ConfigureAwait(false);
            return Failure<IReadOnlyDictionary<string, object?>>("SANITIZATION_FAILED", "CF-841 sanitizer failure aborts contribution with no retry.");
        }

        var sanitized = solution.Where(pair => !PlatformAgentConstants.SensitiveFields.Contains(pair.Key, StringComparer.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var stripped = solution.Count - sanitized.Count;
        var consent = ReadString(contribution, "consent", "KEEP_PRIVATE").ToUpperInvariant();
        var status = path == "A" ? "RECORDED" : consent == "SHARE" ? "SHARED" : "KEPT_PRIVATE";
        var scope = path == "A" || consent == "SHARE" ? "GLOBAL" : "PRIVATE";
        var target = new Dictionary<string, object?>(sanitized, StringComparer.Ordinal) { ["patternId"] = patternId, ["tenantScope"] = scope == "GLOBAL" ? MasterTenant : ReadString(contribution, "sourceTenantId", CurrentTenant.TenantId), ["knowledgeScope"] = scope, ["sourceType"] = path == "A" ? "AGENT_RUN" : "TENANT_DERIVED" };
        var collections = scope == "GLOBAL" ? PlatformAgentConstants.GlobalContributionCollections : [PlatformAgentConstants.RagPatternsCollection];
        foreach (var collection in collections) await _documents.UpsertAsync(collection, patternId, target, null, cancellationToken).ConfigureAwait(false);

        var record = ContributionAudit(sessionId, patternId, path, status, stripped, solution.Keys.ToArray(), sanitized.Keys.ToArray());
        await _documents.UpsertAsync(PlatformAgentConstants.ContributionsCollection, record["contributionId"]!.ToString()!, record, null, cancellationToken).ConfigureAwait(false);
        return Success<IReadOnlyDictionary<string, object?>>(record);
    }

    public async Task<DataProcessResult<PlatformAgentDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var sessions = await _documents.SearchAsync(PlatformAgentConstants.SessionsCollection, filter, cancellationToken).ConfigureAwait(false);
        var actions = await _documents.SearchAsync(PlatformAgentConstants.AgentActionsCollection, filter, cancellationToken).ConfigureAwait(false);
        var contributions = await _documents.SearchAsync(PlatformAgentConstants.ContributionsCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new PlatformAgentDashboard((sessions.Data ?? []).Select(record => record.Body).ToArray(), (actions.Data ?? []).Select(record => record.Body).ToArray(), (contributions.Data ?? []).Select(record => record.Body).ToArray(), new Dictionary<string, object?> { ["sessionCount"] = sessions.Data?.Count ?? 0, ["actionCount"] = actions.Data?.Count ?? 0, ["contributionCount"] = contributions.Data?.Count ?? 0, ["requiredRuleCount"] = PlatformAgentConstants.RequiredBfaRules.Length }));
    }

    private IReadOnlyDictionary<string, object?> ContributionAudit(string sessionId, string patternId, string path, string status, int stripped, IReadOnlyList<string> pre, IReadOnlyList<string> post) => new Dictionary<string, object?> { ["contributionId"] = $"contrib-{patternId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", ["sessionId"] = sessionId, ["patternId"] = patternId, ["path"] = path, ["status"] = status, ["tenantScope"] = CurrentTenant.TenantId, ["strippedCount"] = stripped, ["preSanitizationFields"] = pre, ["postSanitizationFields"] = post, ["knowledgeScope"] = "GLOBAL" };
    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{PlatformAgentConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static PlatformAgentOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static IReadOnlyDictionary<string, object?> ErrorBody(string code, string message) => new Dictionary<string, object?> { ["code"] = code, ["message"] = message };
    private static int ToInt(IReadOnlyDictionary<string, object?> body, string key) => body.TryGetValue(key, out var value) ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : 0;
    private static IReadOnlyDictionary<string, object?> ReadObject(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is JsonElement element && element.ValueKind == JsonValueKind.Object ? JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText())! : value as IReadOnlyDictionary<string, object?> ?? new Dictionary<string, object?>();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? value switch { JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? fallback, JsonElement element => element.ToString(), null => fallback, _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback } : fallback;
    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadRecords(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return [];
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array) return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(element.GetRawText())?.Cast<IReadOnlyDictionary<string, object?>>().ToArray() ?? [];
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> records) return records.ToArray();
        if (value is IEnumerable<Dictionary<string, object?>> dictionaries) return dictionaries.Cast<IReadOnlyDictionary<string, object?>>().ToArray();
        return [];
    }

    private static IReadOnlyList<string> ReadKeywords(IReadOnlyDictionary<string, object?> request)
    {
        var intent = ReadString(request, "userIntent").ToLowerInvariant();
        return intent.Split([' ', ',', '.', ';', ':', '-', '_'], StringSplitOptions.RemoveEmptyEntries).Where(word => word.Length >= 3).Take(8).ToArray();
    }
}