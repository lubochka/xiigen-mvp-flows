using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.OssCurriculum.Application;

public sealed class OssCurriculumService : MicroserviceBase<OssCurriculumService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id", "curriculumTenantId"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IOssCurriculumQueueBroker _queueBroker;

    public OssCurriculumService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IOssCurriculumQueueBroker queueBroker,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<OssCurriculumService> logger)
        : base(new ServiceDescriptor("oss-curriculum-service", "OSS Curriculum", OssCurriculumConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _queueBroker = queueBroker;
    }

    public DataProcessResult<OssCurriculumOutcome> ValidateRulePack(IReadOnlyDictionary<string, object?> request)
    {
        var rules = ReadRules(request);
        var ids = rules.Select(rule => rule.RuleId).ToArray();
        var missing = OssCurriculumConstants.RequiredBfaRules.Where(rule => !ids.Contains(rule, StringComparer.Ordinal)).ToArray();
        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var invalid = rules.Where(rule => string.IsNullOrWhiteSpace(rule.Severity) || string.IsNullOrWhiteSpace(rule.Factory) || string.IsNullOrWhiteSpace(rule.Trigger) || string.IsNullOrWhiteSpace(rule.Scope)).Select(rule => rule.RuleId).ToArray();
        if (rules.Count == 0 || missing.Length > 0 || duplicate.Length > 0 || invalid.Length > 0)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "RULE_PACK_INVALID", ["missing"] = missing, ["duplicate"] = duplicate, ["invalid"] = invalid }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "RULE_PACK_VALID", ["ruleCount"] = rules.Count, ["coverage"] = "CF-803,CF-804,CF-1058-CF-1061" }, false));
    }

    public DataProcessResult<OssCurriculumOutcome> ValidateTaskCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = OssCurriculumConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "TASK_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["requiredTasks"] = OssCurriculumConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<OssCurriculumOutcome>> AssignTierAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "dpoTripleId", "taskTypeId", "archetype", "status");
        if (!string.Equals(ReadString(request, "status"), "VALIDATED", StringComparison.Ordinal)) errors.Add(FieldError("status", "status must be VALIDATED for curriculum assignment."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("curriculum-tier.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var dpoTripleId = ReadString(request, "dpoTripleId");
        var taskTypeId = ReadString(request, "taskTypeId");
        var archetype = ReadString(request, "archetype").ToUpperInvariant();
        if (!OssCurriculumConstants.ArchetypeTierMap.TryGetValue(archetype, out var tier))
        {
            return Success(ValidationFailed([FieldError("archetype", $"archetype must be one of {string.Join(", ", OssCurriculumConstants.ArchetypeTierMap.Keys)}.")]));
        }

        var expectedKey = $"tier:{dpoTripleId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1059" }, false));
        }

        var documentId = ScopedId($"tier:{dpoTripleId}");
        var existing = await GetTenantDocumentAsync(OssCurriculumConstants.CurriculumTierCollection, documentId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
        {
            return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1058" }, false));
        }

        var operation = OperationKey("tier", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var nextVersion = existing is null ? 1 : ReadInt(existing, "version", 1) + 1;
        var body = new Dictionary<string, object?>
        {
            ["id"] = documentId,
            ["dpoTripleId"] = dpoTripleId,
            ["taskTypeId"] = taskTypeId,
            ["archetype"] = archetype,
            ["status"] = "VALIDATED",
            ["curriculumTier"] = tier,
            ["tierAssignedBeforeValidation"] = true,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = nextVersion,
            ["assignedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        var saved = await _documents.UpsertAsync(OssCurriculumConstants.CurriculumTierCollection, documentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<OssCurriculumOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);

        var envelope = Envelope("oss-curriculum.tier-assigned", documentId, body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _queueBroker.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(existing is null ? 201 : 200, body, true, true));
    }

    public async Task<DataProcessResult<OssCurriculumOutcome>> SubmitShadowRunsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "dpoTripleId", "winningNodeId", "cycleId", "curriculumTier");
        var tier = ReadInt(request, "curriculumTier", 0);
        if (tier <= 0) errors.Add(FieldError("curriculumTier", "curriculumTier must be a positive tier from the tier assigner."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("shadow-run.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var dpoTripleId = ReadString(request, "dpoTripleId");
        var cycleId = ReadString(request, "cycleId");
        var expectedKey = $"shadow:{dpoTripleId}:{cycleId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1059" }, false));
        }

        var models = ReadStringArray(request, "ossModels");
        if (models.Count == 0) models = OssCurriculumConstants.DefaultOssModels;
        if (models.Any(string.IsNullOrWhiteSpace)) return Success(ValidationFailed([FieldError("ossModels", "ossModels cannot contain empty model names.")]));

        var operation = OperationKey("shadow", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var shadowRunIds = new List<string>();
        foreach (var model in models)
        {
            var shadowRunId = ScopedId($"shadow:{dpoTripleId}:{model}:{cycleId}");
            var existing = await GetTenantDocumentAsync(OssCurriculumConstants.ShadowRunCollection, shadowRunId, cancellationToken).ConfigureAwait(false);
            if (existing is not null && !string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(ReadString(existing, "version"), expectedVersion, StringComparison.Ordinal))
            {
                return Success(Outcome(409, new Dictionary<string, object?> { ["code"] = "CONCURRENT_UPDATE", ["currentVersion"] = ReadInt(existing, "version", 1), ["reason"] = "concurrent_update", ["ruleId"] = "CF-1058" }, false));
            }

            var record = new Dictionary<string, object?>
            {
                ["id"] = shadowRunId,
                ["shadowRunId"] = shadowRunId,
                ["ossModel"] = model,
                ["cycleId"] = cycleId,
                ["dpoTripleId"] = dpoTripleId,
                ["winningNodeId"] = ReadString(request, "winningNodeId"),
                ["curriculumTier"] = tier,
                ["taskTypeId"] = ReadString(request, "taskTypeId", "T598-SHADOW-RUN-ORCHESTRATOR"),
                ["status"] = "PENDING",
                ["tenantScope"] = CurrentTenant.TenantId,
                ["version"] = existing is null ? 1 : ReadInt(existing, "version", 1) + 1,
                ["submittedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            await _documents.UpsertAsync(OssCurriculumConstants.ShadowRunCollection, shadowRunId, record, expectedVersion, cancellationToken).ConfigureAwait(false);
            shadowRunIds.Add(shadowRunId);
        }

        var body = new Dictionary<string, object?>
        {
            ["dpoTripleId"] = dpoTripleId,
            ["cycleId"] = cycleId,
            ["shadowRunIds"] = shadowRunIds,
            ["submittedModels"] = models,
            ["curriculumTier"] = tier,
            ["async"] = true
        };
        var envelope = Envelope("oss-curriculum.shadow-runs-submitted", ScopedId($"shadow-batch:{dpoTripleId}:{cycleId}"), body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _queueBroker.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<OssCurriculumOutcome>> CollectSignalsAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "shadowRunId", "ossModel", "cycleId", "grade", "ragContextSize", "graphContextSize");
        if (ReadDecimal(request, "grade", -1m) < 0m) errors.Add(FieldError("grade", "grade must be zero or greater."));
        if (ReadInt(request, "ragContextSize", -1) < 0) errors.Add(FieldError("ragContextSize", "ragContextSize must be zero or greater."));
        if (ReadInt(request, "graphContextSize", -1) < 0) errors.Add(FieldError("graphContextSize", "graphContextSize must be zero or greater."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var shadowRunId = ReadString(request, "shadowRunId");
        var expectedKey = $"signals:{shadowRunId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1059" }, false));
        }

        var operation = OperationKey("signals", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var ossModel = ReadString(request, "ossModel");
        var previous = (await _documents.SearchAsync(OssCurriculumConstants.ShadowRunCollection, new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId, ["ossModel"] = ossModel }, cancellationToken).ConfigureAwait(false)).Data ?? [];
        var grade = ReadDecimal(request, "grade", 0m);
        var prior = previous.Select(record => record.Body).Where(body => !string.Equals(ReadString(body, "shadowRunId"), shadowRunId, StringComparison.Ordinal) && body.ContainsKey("grade")).OrderByDescending(body => ReadString(body, "submittedAt")).FirstOrDefault();
        var trend = "STATIC";
        if (prior is not null)
        {
            var priorGrade = ReadDecimal(prior, "grade", grade);
            trend = grade > priorGrade + 0.01m ? "IMPROVING" : grade < priorGrade - 0.01m ? "DECLINING" : "STATIC";
        }

        var signals = new Dictionary<string, object?>
        {
            ["grade_trend"] = trend,
            ["rag_context_size"] = ReadInt(request, "ragContextSize"),
            ["graph_context_size"] = ReadInt(request, "graphContextSize")
        };
        if (signals.Count != 3 || !OssCurriculumConstants.LearningSignalKeys.All(signals.ContainsKey))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "SIGNAL_COUNT_MISMATCH", ["expected"] = OssCurriculumConstants.LearningSignalKeys }, false));
        }

        var signalRecordId = ScopedId($"signal:{shadowRunId}");
        var body = new Dictionary<string, object?>
        {
            ["id"] = signalRecordId,
            ["signalRecordId"] = signalRecordId,
            ["shadowRunId"] = shadowRunId,
            ["ossModel"] = ossModel,
            ["cycleId"] = ReadString(request, "cycleId"),
            ["grade"] = grade,
            ["signals"] = signals,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = 1,
            ["collectedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(OssCurriculumConstants.LearningSignalCollection, signalRecordId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        var envelope = Envelope("oss-curriculum.learning-signal-recorded", signalRecordId, body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _queueBroker.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<OssCurriculumOutcome>> TrackProgressAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "dpoTripleId", "ossModel", "station", "cycleId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        if (ReadBool(request, "simulateTimeout")) return await TimeoutAsync("progress.timeout", idempotencyKey!, cancellationToken).ConfigureAwait(false);

        var dpoTripleId = ReadString(request, "dpoTripleId");
        var ossModel = ReadString(request, "ossModel");
        var cycleId = ReadString(request, "cycleId");
        var expectedKey = $"progress:{dpoTripleId}:{ossModel}:{cycleId}";
        if (!string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_INVALID", ["expected"] = expectedKey, ["ruleId"] = "CF-1059" }, false));
        }

        var operation = OperationKey("progress", expectedKey);
        var replay = await ReserveAsync(operation, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var signals = (await _documents.SearchAsync(OssCurriculumConstants.LearningSignalCollection, new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId }, cancellationToken).ConfigureAwait(false)).Data?
            .Select(record => record.Body)
            .Where(body => string.Equals(ReadString(body, "ossModel"), ossModel, StringComparison.Ordinal))
            .OrderByDescending(body => ReadString(body, "collectedAt"))
            .ToArray() ?? [];
        var minCycles = ReadInt(request, "minCycles", OssCurriculumConstants.DefaultMinCycles);
        var status = "DEFERRED";
        string? recommendation = null;
        if (signals.Length >= minCycles)
        {
            var recent = signals.Take(OssCurriculumConstants.PlateauWindow).ToArray();
            var staticPlateau = recent.Length == OssCurriculumConstants.PlateauWindow && recent.All(signal => ReadSignalTrend(signal) == "STATIC");
            var improving = recent.Any(signal => ReadSignalTrend(signal) == "IMPROVING");
            status = staticPlateau ? "PLATEAU_DETECTED" : improving ? "IMPROVING" : "ACTIVE";
            recommendation = staticPlateau ? "PRE_SEEDING_RECOMMENDED" : null;
        }

        var documentId = ScopedId($"progress:{dpoTripleId}:{ossModel}:{cycleId}");
        var body = new Dictionary<string, object?>
        {
            ["id"] = documentId,
            ["dpoTripleId"] = dpoTripleId,
            ["ossModel"] = ossModel,
            ["station"] = ReadString(request, "station"),
            ["cycleId"] = cycleId,
            ["progressStatus"] = status,
            ["recommendation"] = recommendation,
            ["automaticActionTaken"] = false,
            ["tenantScope"] = CurrentTenant.TenantId,
            ["version"] = 1,
            ["checkedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(OssCurriculumConstants.ProgressCollection, documentId, body, expectedVersion, cancellationToken).ConfigureAwait(false);
        var envelope = Envelope("oss-curriculum.progress-checked", documentId, body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _queueBroker.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operation, body, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, body, true, true));
    }

    public async Task<DataProcessResult<OssCurriculumDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantScope"] = CurrentTenant.TenantId };
        var tiers = await _documents.SearchAsync(OssCurriculumConstants.CurriculumTierCollection, filter, cancellationToken).ConfigureAwait(false);
        var shadow = await _documents.SearchAsync(OssCurriculumConstants.ShadowRunCollection, filter, cancellationToken).ConfigureAwait(false);
        var signals = await _documents.SearchAsync(OssCurriculumConstants.LearningSignalCollection, filter, cancellationToken).ConfigureAwait(false);
        var progress = await _documents.SearchAsync(OssCurriculumConstants.ProgressCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new OssCurriculumDashboard(
            (tiers.Data ?? []).Select(record => record.Body).ToArray(),
            (shadow.Data ?? []).Select(record => record.Body).ToArray(),
            (signals.Data ?? []).Select(record => record.Body).ToArray(),
            (progress.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["tierCount"] = tiers.Data?.Count ?? 0, ["shadowRunCount"] = shadow.Data?.Count ?? 0, ["signalCount"] = signals.Data?.Count ?? 0, ["progressCheckCount"] = progress.Data?.Count ?? 0, ["requiredRuleCount"] = OssCurriculumConstants.RequiredBfaRules.Length } ));
    }

    private async Task<IReadOnlyDictionary<string, object?>?> GetTenantDocumentAsync(string collection, string id, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(collection, id, cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return null;
        return string.Equals(ReadString(record.Data.Body, "tenantScope"), CurrentTenant.TenantId, StringComparison.Ordinal) ? record.Data.Body : null;
    }

    private async Task<OssCurriculumOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase) ? Outcome(200, data.CachedResponse, false) : null;
    }

    private async Task<DataProcessResult<OssCurriculumOutcome>> TimeoutAsync(string type, string idempotencyKey, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?> { ["code"] = "TIMEOUT", ["retryAfterMs"] = 5000, ["outboxRetained"] = true, ["ruleId"] = "CF-1061", ["tenantScope"] = CurrentTenant.TenantId };
        var envelope = Envelope(type, ScopedId($"timeout:{Hash(idempotencyKey)}"), body);
        await _outbox.AppendAsync(envelope, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(504, body, false, true));
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) => new(Guid.NewGuid().ToString("n"), $"/xiigen/{OssCurriculumConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });
    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private static OssCurriculumOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static OssCurriculumOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details, ["ruleId"] = "CF-1060" }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{OssCurriculumConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private OssCurriculumOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED", ["ruleId"] = "CF-1059" }, false) : null;
    private OssCurriculumOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;

    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => !request.ContainsKey(field) || string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string ReadString(IReadOnlyDictionary<string, object?> source, string key, string fallback = "") => source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback : fallback;
    private static int ReadInt(IReadOnlyDictionary<string, object?> source, string key, int fallback = 0) => source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static decimal ReadDecimal(IReadOnlyDictionary<string, object?> source, string key, decimal fallback = 0m) => source.TryGetValue(key, out var value) && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key, bool fallback = false) => source.TryGetValue(key, out var value) ? bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed : fallback;
    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?> source, string key) => source.TryGetValue(key, out var value) && value is not null ? (JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []) : [];
    private static string ReadSignalTrend(IReadOnlyDictionary<string, object?> record)
    {
        if (!record.TryGetValue("signals", out var signals) || signals is null) return "";
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(signals), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return ReadString(payload, "grade_trend");
    }

    private static IReadOnlyList<OssCurriculumRuleSnapshot> ReadRules(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("rules", out var value) && value is not null ? JsonSerializer.Deserialize<OssCurriculumRuleSnapshot[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
    private static IReadOnlyList<OssCurriculumPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request) => request.TryGetValue("planSteps", out var value) && value is not null ? JsonSerializer.Deserialize<OssCurriculumPlanStep[]>(JsonSerializer.Serialize(value), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [] : [];
}
