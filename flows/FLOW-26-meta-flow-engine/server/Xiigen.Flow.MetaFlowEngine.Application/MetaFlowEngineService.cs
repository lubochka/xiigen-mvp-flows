using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.MetaFlowEngine.Application;

public sealed class MetaFlowEngineService : MicroserviceBase<MetaFlowEngineService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private static readonly string[] SupportedChangeTypes = ["CAPABILITY_GAP", "FLOW_DAG", "FACTORY_SPEC", "TASK_TYPE_CONTRACT", "CODE_BUNDLE"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public MetaFlowEngineService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<MetaFlowEngineService> logger)
        : base(new ServiceDescriptor("meta-flow-engine-service", "Self-Developing Meta-Flow Engine", MetaFlowEngineConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<MetaFlowEngineOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var snapshots = ReadRules(request);
        if (snapshots.Count == 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_REQUIRED" }, false));
        }

        var ids = snapshots.Select(rule => rule.RuleId).ToArray();
        var missing = MetaFlowEngineConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = snapshots
            .Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.SourceFlow) || string.IsNullOrWhiteSpace(rule.TargetFlow) || string.IsNullOrWhiteSpace(rule.Scope))
            .Select(rule => rule.RuleId)
            .ToArray();

        if (missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?>
            {
                ["code"] = "RULE_PACK_INVALID",
                ["missing"] = missing,
                ["duplicate"] = duplicate,
                ["invalid"] = invalid,
                ["requiredCount"] = MetaFlowEngineConstants.RequiredBfaRules.Length
            }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?>
        {
            ["code"] = "RULE_PACK_VALID",
            ["ruleCount"] = snapshots.Count,
            ["requiredCount"] = MetaFlowEngineConstants.RequiredBfaRules.Length,
            ["coverage"] = "CF-510-CF-544+CF-989-CF-995"
        }, false));
    }

    public async Task<DataProcessResult<MetaFlowEngineOutcome>> SubmitChangeAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "proposedChangeId", "changeType", "diffBlobRef", "sourceFlow");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var changeType = ReadString(request, "changeType").ToUpperInvariant();
        if (!SupportedChangeTypes.Contains(changeType, StringComparer.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "CHANGE_TYPE_INVALID", ["allowed"] = SupportedChangeTypes }, false));
        }

        var diffBlobRef = ReadString(request, "diffBlobRef");
        if (!diffBlobRef.StartsWith("sha256:", StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "DIFF_BLOB_REF_NOT_CONTENT_ADDRESSED", ["requiredPrefix"] = "sha256:" }, false));
        }

        var op = OperationKey("change", idempotencyKey!);
        var replay = await ReserveAsync(op, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var changeId = ReadString(request, "proposedChangeId");
        var normalizedHash = Hash(JsonSerializer.Serialize(request.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value)));
        var body = new Dictionary<string, object?>
        {
            ["proposedChangeId"] = changeId,
            ["changeType"] = changeType,
            ["sourceFlow"] = ReadString(request, "sourceFlow"),
            ["diffBlobRef"] = diffBlobRef,
            ["contentHash"] = normalizedHash,
            ["state"] = "INTAKE_RECEIVED",
            ["tenantScope"] = CurrentTenant.TenantId,
            ["tenantId"] = CurrentTenant.TenantId,
            ["source_flow"] = MetaFlowEngineConstants.FlowId,
            ["writtenAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(MetaFlowEngineConstants.ChangeCollection, ScopedId(changeId), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaFlowEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("SELF_BUILD_RUN_RECEIVED", changeId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.meta-flow.self-build.received", changeId, body), cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?> { ["proposedChangeId"] = changeId, ["contentHash"] = normalizedHash, ["state"] = "INTAKE_RECEIVED", ["version"] = saved.Data!.Version };
        await _idempotency.MarkSucceededAsync(op, response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<MetaFlowEngineOutcome>> ScanConflictsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "proposedChangeId", "changeType", "sourceFlow");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var rulePack = ValidateRulePack(new Dictionary<string, object?> { ["rules"] = request.TryGetValue("rules", out var rules) ? rules : Array.Empty<object>() });
        if (rulePack.Data?.HttpStatus != 200) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_MUST_PASS_BEFORE_SCAN", ["rulePack"] = rulePack.Data?.Body }, false));

        var conflicts = DetectConflicts(request);
        var severity = conflicts.Any(conflict => string.Equals(ReadString(conflict, "severity"), "CRITICAL", StringComparison.Ordinal)) ? "CRITICAL" : conflicts.Count > 0 ? "HIGH" : "NONE";
        var scanId = ScopedId($"scan:{ReadString(request, "proposedChangeId")}:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?>
        {
            ["scanId"] = scanId,
            ["proposedChangeId"] = ReadString(request, "proposedChangeId"),
            ["sourceFlow"] = ReadString(request, "sourceFlow"),
            ["conflicts"] = conflicts,
            ["compositeSeverity"] = severity,
            ["state"] = severity == "NONE" ? "SKIP_ARBITRATION" : "PENDING_RESOLUTION",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(MetaFlowEngineConstants.ConflictCollection, scanId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaFlowEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.meta-flow.gaps.scanned", scanId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("scan", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<MetaFlowEngineOutcome>> AnalyzeImpactAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "conflictScanId", "impactedFlows");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var impacted = ReadStringArray(request, "impactedFlows");
        var invalid = impacted.Where(flow => !flow.StartsWith("FLOW-", StringComparison.Ordinal)).ToArray();
        if (invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IMPACT_GRAPH_INVALID", ["invalidFlows"] = invalid }, false));
        }

        var maxDepth = Math.Clamp(ReadInt(request, "maxTraversalDepth", 5), 1, 5);
        var circular = impacted.GroupBy(flow => flow, StringComparer.Ordinal).Any(group => group.Count() > 1);
        var direct = impacted.Take(Math.Min(2, impacted.Length)).ToArray();
        var transitive = impacted.Skip(direct.Length).Take(maxDepth).ToArray();
        var impactId = ScopedId($"impact:{Hash(ReadString(request, "conflictScanId"))}:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?>
        {
            ["impactId"] = impactId,
            ["conflictScanId"] = ReadString(request, "conflictScanId"),
            ["direct"] = direct,
            ["transitive"] = transitive,
            ["totalUniqueFlows"] = impacted.Distinct(StringComparer.Ordinal).Count(),
            ["maxHopReached"] = Math.Min(maxDepth, impacted.Length),
            ["circularDependencyDetected"] = circular,
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(MetaFlowEngineConstants.ImpactCollection, impactId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaFlowEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.meta-flow.plan.analyzed", impactId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("impact", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<MetaFlowEngineOutcome>> RunStressTestsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var submitted = ReadStressResults(request);
        var ids = submitted.Select(test => test.StressTestId).ToArray();
        var missingStressTests = MetaFlowEngineConstants.RequiredStressTests.Where(test => !ids.Contains(test, StringComparer.Ordinal)).ToArray();
        var failing = submitted.Where(test => !string.Equals(test.Status, "PASS", StringComparison.OrdinalIgnoreCase)).Select(test => test.StressTestId).ToArray();
        if (missingStressTests.Length > 0 || failing.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "STRESS_MATRIX_FAILED", ["missing"] = missingStressTests, ["failing"] = failing }, false));
        }

        var runId = ScopedId($"stress:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?>
        {
            ["stressRunId"] = runId,
            ["status"] = "PASS",
            ["stressTestCount"] = submitted.Count,
            ["coverage"] = "ST-300-ST-324",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(MetaFlowEngineConstants.StressCollection, runId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaFlowEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.meta-flow.stress.completed", runId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("stress", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<MetaFlowEngineOutcome>> ApproveReleaseAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "proposedChangeId", "decision", "conflictScanId", "impactId", "stressRunId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var decision = ReadString(request, "decision").ToUpperInvariant();
        var rationale = ReadString(request, "rationale");
        if (decision is not ("REFACTOR_FLOWS" or "REJECT_CHANGE" or "COMPAT_MODE" or "FORCE_PROCEED"))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "INVALID_DECISION_VALUE" }, false));
        }

        if (decision == "FORCE_PROCEED" && rationale.Length < 50)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "FORCE_PROCEED_RATIONALE_REQUIRED", ["minLength"] = 50 }, false));
        }

        var evidence = await EvidenceExistsAsync(ReadString(request, "conflictScanId"), ReadString(request, "impactId"), ReadString(request, "stressRunId"), cancellationToken).ConfigureAwait(false);
        if (!evidence)
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "RELEASE_GATE_EVIDENCE_MISSING", ["failClosed"] = true }, false));
        }

        var gateId = ScopedId($"release:{ReadString(request, "proposedChangeId")}:{Hash(idempotencyKey!)}");
        var body = new Dictionary<string, object?>
        {
            ["releaseGateId"] = gateId,
            ["proposedChangeId"] = ReadString(request, "proposedChangeId"),
            ["decision"] = decision,
            ["rationale"] = rationale,
            ["status"] = decision == "REJECT_CHANGE" ? "REJECTED" : "APPROVED",
            ["auditMode"] = "APPEND_ONLY",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(MetaFlowEngineConstants.ReleaseGateCollection, gateId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<MetaFlowEngineOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("RELEASE_GATE_CLOSED", gateId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.meta-flow.promotion.approved", gateId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("release", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<MetaFlowEngineOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = MetaFlowEngineConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["requiredTasks"] = MetaFlowEngineConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<MetaFlowEngineDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var rules = await _documents.SearchAsync(MetaFlowEngineConstants.RulePackCollection, filter, cancellationToken).ConfigureAwait(false);
        var conflicts = await _documents.SearchAsync(MetaFlowEngineConstants.ConflictCollection, filter, cancellationToken).ConfigureAwait(false);
        var impacts = await _documents.SearchAsync(MetaFlowEngineConstants.ImpactCollection, filter, cancellationToken).ConfigureAwait(false);
        var stress = await _documents.SearchAsync(MetaFlowEngineConstants.StressCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new MetaFlowEngineDashboard(
            (rules.Data ?? []).Select(record => record.Body).ToArray(),
            (conflicts.Data ?? []).Select(record => record.Body).ToArray(),
            (impacts.Data ?? []).Select(record => record.Body).ToArray(),
            (stress.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?>
            {
                ["ruleCount"] = MetaFlowEngineConstants.RequiredBfaRules.Length,
                ["stressTestCount"] = MetaFlowEngineConstants.RequiredStressTests.Length,
                ["conflictCount"] = conflicts.Data?.Count ?? 0,
                ["impactReportCount"] = impacts.Data?.Count ?? 0
            }));
    }

    private async Task<bool> EvidenceExistsAsync(string conflictScanId, string impactId, string stressRunId, CancellationToken cancellationToken)
    {
        var conflict = await _documents.GetAsync(MetaFlowEngineConstants.ConflictCollection, conflictScanId, cancellationToken).ConfigureAwait(false);
        var impact = await _documents.GetAsync(MetaFlowEngineConstants.ImpactCollection, impactId, cancellationToken).ConfigureAwait(false);
        var stress = await _documents.GetAsync(MetaFlowEngineConstants.StressCollection, stressRunId, cancellationToken).ConfigureAwait(false);
        return conflict.Data is not null && impact.Data is not null && stress.Data is not null;
    }

    private async Task<MetaFlowEngineOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            ? Outcome(200, data.CachedResponse, false)
            : null;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> DetectConflicts(IReadOnlyDictionary<string, object?> request)
    {
        var changeType = ReadString(request, "changeType").ToUpperInvariant();
        var removedRequiredFields = ReadStringArray(request, "removedRequiredFields");
        var removedEndpoints = ReadStringArray(request, "removedEndpoints");
        var conflicts = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var field in removedRequiredFields)
        {
            conflicts.Add(new Dictionary<string, object?> { ["ruleId"] = "CF-477", ["severity"] = "CRITICAL", ["subject"] = field, ["reason"] = "Required field removal breaks dependent flows." });
        }

        foreach (var endpoint in removedEndpoints)
        {
            conflicts.Add(new Dictionary<string, object?> { ["ruleId"] = "CF-477", ["severity"] = "CRITICAL", ["subject"] = endpoint, ["reason"] = "Endpoint removal breaks registered API consumers." });
        }

        if (changeType == "FLOW_DAG" && ReadBool(request, "transitionRemoved"))
        {
            conflicts.Add(new Dictionary<string, object?> { ["ruleId"] = "CF-478", ["severity"] = "CRITICAL", ["subject"] = "transition_removed", ["reason"] = "Referenced state transition was removed." });
        }

        if (ReadBool(request, "aiCriticalWithoutEvidence"))
        {
            conflicts.Add(new Dictionary<string, object?> { ["ruleId"] = "CF-481", ["severity"] = "LOW", ["subject"] = "semantic_analysis", ["reason"] = "AI severity downgraded because evidence links are missing." });
        }

        return conflicts;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(MetaFlowEngineConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?>
        {
            ["action"] = action,
            ["subject"] = subject,
            ["tenantId"] = CurrentTenant.TenantId,
            ["auditMode"] = "APPEND_ONLY"
        }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(Guid.NewGuid().ToString("n"), $"/xiigen/{MetaFlowEngineConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });

    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static MetaFlowEngineOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static MetaFlowEngineOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{MetaFlowEngineConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private MetaFlowEngineOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private MetaFlowEngineOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => string.IsNullOrWhiteSpace(ReadString(request, field)) && !request.ContainsKey(field)).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;

    private static IReadOnlyList<BfaGovernancePlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("planSteps", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<BfaGovernancePlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<BfaRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("rules", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<BfaRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<BfaStressResult> ReadStressResults(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("stressResults", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<BfaStressResult[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static string[] ReadStringArray(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return [];
        if (value is string text) return string.IsNullOrWhiteSpace(text) ? [] : [text];
        return JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}

