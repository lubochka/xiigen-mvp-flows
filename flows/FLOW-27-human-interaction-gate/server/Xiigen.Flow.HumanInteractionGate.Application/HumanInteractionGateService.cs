using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.HumanInteractionGate.Application;

public sealed class HumanInteractionGateService : MicroserviceBase<HumanInteractionGateService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public HumanInteractionGateService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<HumanInteractionGateService> logger)
        : base(new ServiceDescriptor("human-interaction-gate-service", "Human Interaction Gate", HumanInteractionGateConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public DataProcessResult<HumanInteractionGateOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var snapshots = ReadRules(request);
        if (snapshots.Count == 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_REQUIRED" }, false));
        }

        var ids = snapshots.Select(rule => rule.RuleId).ToArray();
        var missing = HumanInteractionGateConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = snapshots
            .Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope))
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
                ["requiredCount"] = HumanInteractionGateConstants.RequiredBfaRules.Length
            }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?>
        {
            ["code"] = "RULE_PACK_VALID",
            ["ruleCount"] = snapshots.Count,
            ["requiredCount"] = HumanInteractionGateConstants.RequiredBfaRules.Length,
            ["coverage"] = "CF-545-CF-565+CF-996-CF-1002"
        }, false));
    }

    public async Task<DataProcessResult<HumanInteractionGateOutcome>> OpenGateAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "runId", "nodeId", "title", "blockingPolicy", "assigneePolicy");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var assigneePolicy = ReadDictionary(request, "assigneePolicy");
        var policyType = ReadString(assigneePolicy, "type").ToLowerInvariant();
        var targets = ReadStringArray(assigneePolicy, "targets");
        if (targets.Any(target => target.Contains(':', StringComparison.Ordinal)))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "CROSS_TENANT_GROUP_REJECTED", ["ruleId"] = "CF-552" }, false));
        }

        var quorumNeeded = ReadInt(assigneePolicy, "needed", 0);
        if (policyType == "quorum" && quorumNeeded > targets.Length)
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "QUORUM_EXCEEDS_ELIGIBLE_USERS", ["needed"] = quorumNeeded, ["eligibleUsers"] = targets.Length }, false));
        }

        var op = OperationKey("open", idempotencyKey!);
        var replay = await ReserveAsync(op, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var existing = await FindOpenTaskAsync(ReadString(request, "runId"), ReadString(request, "nodeId"), cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            await _idempotency.MarkSucceededAsync(op, existing, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(200, existing, false));
        }

        var taskId = ScopedId($"task:{ReadString(request, "runId")}:{ReadString(request, "nodeId")}");
        var dueAt = DateTimeOffset.UtcNow.AddDays(ReadInt(request, "dueInDays", 7)).ToString("O", CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["taskId"] = taskId,
            ["runId"] = ReadString(request, "runId"),
            ["nodeId"] = ReadString(request, "nodeId"),
            ["title"] = ReadString(request, "title"),
            ["status"] = "waiting_for_user",
            ["blockingPolicy"] = ReadString(request, "blockingPolicy"),
            ["assigneePolicy"] = assigneePolicy,
            ["answers"] = Array.Empty<IReadOnlyDictionary<string, object?>>(),
            ["dueAt"] = dueAt,
            ["tenantId"] = CurrentTenant.TenantId,
            ["version"] = 1,
            ["source_flow"] = HumanInteractionGateConstants.FlowId
        };
        var saved = await _documents.UpsertAsync(HumanInteractionGateConstants.TaskCollection, taskId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<HumanInteractionGateOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("USER_TASK_CREATED", taskId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.human-gate.user-task.created", taskId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(op, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<HumanInteractionGateOutcome>> ClaimTaskAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "taskId", "claimantUserId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var task = await GetTenantDocumentAsync(HumanInteractionGateConstants.TaskCollection, ReadString(request, "taskId"), cancellationToken).ConfigureAwait(false);
        if (task is null) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "TASK_NOT_FOUND" }, false));
        if (!string.Equals(ReadString(task, "status"), "waiting_for_user", StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "TASK_NOT_CLAIMABLE", ["status"] = ReadString(task, "status") }, false));
        }

        var claimId = ScopedId($"claim:{Hash(ReadString(request, "taskId"))}:{ReadString(request, "claimantUserId")}");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(ReadInt(request, "ttlMinutes", 5)).ToString("O", CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["claimId"] = claimId,
            ["taskId"] = ReadString(request, "taskId"),
            ["claimantUserId"] = ReadString(request, "claimantUserId"),
            ["status"] = "active",
            ["expiresAt"] = expiresAt,
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(HumanInteractionGateConstants.ClaimCollection, claimId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<HumanInteractionGateOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.human-gate.task.claimed", claimId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("claim", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<HumanInteractionGateOutcome>> SubmitAnswerAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "taskId", "answererUserId", "decision");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var op = OperationKey("answer", idempotencyKey!);
        var replay = await ReserveAsync(op, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var task = await GetTenantDocumentAsync(HumanInteractionGateConstants.TaskCollection, ReadString(request, "taskId"), cancellationToken).ConfigureAwait(false);
        if (task is null) return Success(Outcome(404, new Dictionary<string, object?> { ["code"] = "TASK_NOT_FOUND" }, false));
        var status = ReadString(task, "status");
        if (status is "expired" or "skipped" or "cancelled")
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "GATE_EXPIRED", ["ruleId"] = "CF-558" }, false));
        }

        if (status == "answered")
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "ALREADY_ANSWERED" }, false));
        }

        var activeClaim = await ActiveClaimForAsync(ReadString(request, "taskId"), ReadString(request, "answererUserId"), cancellationToken).ConfigureAwait(false);
        if (request.ContainsKey("requireClaim") && activeClaim is null)
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CLAIM_REQUIRED_OR_EXPIRED", ["ruleId"] = "CF-548" }, false));
        }

        var answers = ReadAnswerList(task).ToList();
        if (answers.Any(answer => string.Equals(ReadString(answer, "answererUserId"), ReadString(request, "answererUserId"), StringComparison.Ordinal)))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "DUPLICATE_ANSWER" }, false));
        }

        answers.Add(new Dictionary<string, object?>
        {
            ["answererUserId"] = ReadString(request, "answererUserId"),
            ["decision"] = ReadString(request, "decision"),
            ["answeredAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        });

        var assigneePolicy = ReadDictionary(task, "assigneePolicy");
        var policyType = ReadString(assigneePolicy, "type").ToLowerInvariant();
        var targets = ReadStringArray(assigneePolicy, "targets");
        var needed = policyType switch
        {
            "allof" => targets.Length,
            "quorum" => ReadInt(assigneePolicy, "needed", 1),
            _ => 1
        };
        var reached = answers.Count >= needed;
        var updated = new Dictionary<string, object?>(task)
        {
            ["answers"] = answers,
            ["answeredCount"] = answers.Count,
            ["neededCount"] = needed,
            ["status"] = reached ? "answered" : "waiting_for_user",
            ["version"] = ReadInt(task, "version", 1) + 1
        };
        var saved = await _documents.UpsertAsync(HumanInteractionGateConstants.TaskCollection, ReadString(request, "taskId"), updated, ReadString(task, "version"), cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<HumanInteractionGateOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("USER_TASK_ANSWER_RECORDED", ReadString(request, "taskId"), cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope(reached ? "xiigen.human-gate.task.answered" : "xiigen.human-gate.task.partial-answer", ReadString(request, "taskId"), updated), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(op, updated, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, updated, true, true));
    }

    public async Task<DataProcessResult<HumanInteractionGateOutcome>> EvaluateCompletionAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "runId", "terminalNodes");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var openRequired = await CountOpenRequiredTasksAsync(ReadString(request, "runId"), cancellationToken).ConfigureAwait(false);
        var terminalNodes = ReadDictionaryArray(request, "terminalNodes");
        var failedRequired = terminalNodes.Where(node => ReadBool(node, "required") && !string.Equals(ReadString(node, "status"), "SUCCEEDED", StringComparison.Ordinal)).ToArray();
        var pendingJudge = ReadStringArray(request, "pendingJudgeGateIds");
        var ready = openRequired.Count == 0 && failedRequired.Length == 0 && pendingJudge.Length == 0;
        var body = new Dictionary<string, object?>
        {
            ["completionId"] = ScopedId($"completion:{ReadString(request, "runId")}:{Hash(idempotencyKey!)}"),
            ["runId"] = ReadString(request, "runId"),
            ["ready"] = ready,
            ["openRequiredTasks"] = openRequired,
            ["failedRequiredTerminalNodes"] = failedRequired.Select(node => ReadString(node, "nodeId")).ToArray(),
            ["pendingJudgeGateIds"] = pendingJudge,
            ["status"] = ready ? "READY" : "BLOCKED_ON_DEPENDENCY",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(HumanInteractionGateConstants.CompletionCollection, ReadString(body, "completionId"), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<HumanInteractionGateOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.human-gate.completion.evaluated", ReadString(body, "completionId"), body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("completion", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<HumanInteractionGateOutcome>> RunStressTestsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var submitted = ReadStressResults(request);
        var ids = submitted.Select(test => test.StressTestId).ToArray();
        var missingStressTests = HumanInteractionGateConstants.RequiredStressTests.Where(test => !ids.Contains(test, StringComparer.Ordinal)).ToArray();
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
            ["coverage"] = "ST-325-ST-345",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(HumanInteractionGateConstants.StressCollection, runId, body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<HumanInteractionGateOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.human-gate.stress.completed", runId, body), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(OperationKey("stress", idempotencyKey!), body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public DataProcessResult<HumanInteractionGateOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = HumanInteractionGateConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["requiredTasks"] = HumanInteractionGateConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<HumanInteractionGateDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var tasks = await _documents.SearchAsync(HumanInteractionGateConstants.TaskCollection, filter, cancellationToken).ConfigureAwait(false);
        var claims = await _documents.SearchAsync(HumanInteractionGateConstants.ClaimCollection, filter, cancellationToken).ConfigureAwait(false);
        var completions = await _documents.SearchAsync(HumanInteractionGateConstants.CompletionCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new HumanInteractionGateDashboard(
            (tasks.Data ?? []).Select(record => record.Body).ToArray(),
            (claims.Data ?? []).Select(record => record.Body).ToArray(),
            (completions.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?>
            {
                ["ruleCount"] = HumanInteractionGateConstants.RequiredBfaRules.Length,
                ["stressTestCount"] = HumanInteractionGateConstants.RequiredStressTests.Length,
                ["openTaskCount"] = tasks.Data?.Count(record => string.Equals(ReadString(record.Body, "status"), "waiting_for_user", StringComparison.Ordinal)) ?? 0,
                ["claimCount"] = claims.Data?.Count ?? 0
            }));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> FindOpenTaskAsync(string runId, string nodeId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(HumanInteractionGateConstants.TaskCollection, new Dictionary<string, object?>
        {
            ["tenantId"] = CurrentTenant.TenantId,
            ["runId"] = runId,
            ["nodeId"] = nodeId,
            ["status"] = "waiting_for_user"
        }, cancellationToken).ConfigureAwait(false);
        return records.Data?.FirstOrDefault()?.Body;
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantId"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<IReadOnlyDictionary<string, object?>?> ActiveClaimForAsync(string taskId, string userId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(HumanInteractionGateConstants.ClaimCollection, new Dictionary<string, object?>
        {
            ["tenantId"] = CurrentTenant.TenantId,
            ["taskId"] = taskId,
            ["claimantUserId"] = userId,
            ["status"] = "active"
        }, cancellationToken).ConfigureAwait(false);
        var active = records.Data?.FirstOrDefault()?.Body;
        if (active is null) return null;
        return DateTimeOffset.TryParse(ReadString(active, "expiresAt"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt) && expiresAt > DateTimeOffset.UtcNow ? active : null;
    }

    private async Task<IReadOnlyList<string>> CountOpenRequiredTasksAsync(string runId, CancellationToken cancellationToken)
    {
        var records = await _documents.SearchAsync(HumanInteractionGateConstants.TaskCollection, new Dictionary<string, object?>
        {
            ["tenantId"] = CurrentTenant.TenantId,
            ["runId"] = runId,
            ["status"] = "waiting_for_user",
            ["blockingPolicy"] = "blocking"
        }, cancellationToken).ConfigureAwait(false);
        return (records.Data ?? []).Select(record => ReadString(record.Body, "taskId")).ToArray();
    }

    private async Task<HumanInteractionGateOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            ? Outcome(200, data.CachedResponse, false)
            : null;
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(HumanInteractionGateConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?>
        {
            ["action"] = action,
            ["subject"] = subject,
            ["tenantId"] = CurrentTenant.TenantId,
            ["auditMode"] = "APPEND_ONLY"
        }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(Guid.NewGuid().ToString("n"), $"/xiigen/{HumanInteractionGateConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });

    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static HumanInteractionGateOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static HumanInteractionGateOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{HumanInteractionGateConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private HumanInteractionGateOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private HumanInteractionGateOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => string.IsNullOrWhiteSpace(ReadString(request, field)) && !request.ContainsKey(field)).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;

    private static IReadOnlyDictionary<string, object?> ReadDictionary(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return new Dictionary<string, object?>();
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Dictionary<string, object?>();
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadDictionaryArray(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return [];
        return JsonSerializer.Deserialize<Dictionary<string, object?>[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ReadAnswerList(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("answers", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<Dictionary<string, object?>[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<HumanGatePlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("planSteps", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<HumanGatePlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<HumanGateRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("rules", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<HumanGateRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static IReadOnlyList<HumanGateStressResult> ReadStressResults(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("stressResults", out var value) || value is null) return [];
        return JsonSerializer.Deserialize<HumanGateStressResult[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static string[] ReadStringArray(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null) return [];
        if (value is string text) return string.IsNullOrWhiteSpace(text) ? [] : [text];
        return JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}
