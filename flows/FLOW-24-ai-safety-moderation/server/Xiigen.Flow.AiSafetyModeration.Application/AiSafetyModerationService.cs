using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.AiSafetyModeration.Application;

public sealed class AiSafetyModerationService : MicroserviceBase<AiSafetyModerationService>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);
    private static readonly string[] TenantPayloadKeys = ["tenantId", "tenant_id", "ownerTenantId", "owner_tenant_id"];
    private static readonly string[] ClientScoreKeys = ["score", "clientScore", "grade", "percentage", "scorePercent"];
    private static readonly string[] ClientTimezoneKeys = ["timezone", "timeZone", "clientTimezone", "browserTimezone"];
    private static readonly string[] DirectCalendarSdkKeys = ["providerSdk", "directSdk", "directGoogleCalendarSdk", "microsoftGraphSdk", "calendarClient"];
    private static readonly string[] UnsafeTerms = ["unsafe", "self-harm", "bypass safety", "violent"];
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;

    public AiSafetyModerationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<AiSafetyModerationService> logger)
        : base(new ServiceDescriptor("ai-safety-moderation-service", "AI Safety Moderation", AiSafetyModerationConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> EvaluateConsentAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "studentId", "consentType");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var op = OperationKey("consent", idempotencyKey!);
        var replay = await ReserveAsync(op, cancellationToken).ConfigureAwait(false);
        if (replay is not null) return Success(replay);

        var studentId = ReadString(request, "studentId");
        var consentType = ReadString(request, "consentType");
        var outcome = NormalizeConsentOutcome(request);
        var body = new Dictionary<string, object?>
        {
            ["gateType"] = "CONSENT_GATE",
            ["studentId"] = studentId,
            ["consentType"] = consentType,
            ["outcome"] = outcome,
            ["allowed"] = outcome == "GRANTED",
            ["downstreamBlocked"] = outcome != "GRANTED",
            ["tenantId"] = CurrentTenant.TenantId,
            ["knowledgeScope"] = "PRIVATE",
            ["writtenAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.ConsentGateCollection, ConsentGateId(studentId, consentType), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await AuditAsync("CONSENT_GATE_WRITTEN", studentId, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.consent.gate-written", studentId, body), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["studentId"] = studentId, ["consentType"] = consentType, ["outcome"] = outcome, ["downstreamBlocked"] = outcome != "GRANTED", ["version"] = saved.Data!.Version };
        await _idempotency.MarkSucceededAsync(op, response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> ComposeLessonAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "studentId", "lessonId", "prompt");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var studentId = ReadString(request, "studentId");
        var lessonId = ReadString(request, "lessonId");
        var consent = await HasGrantedConsentAsync(studentId, cancellationToken).ConfigureAwait(false);
        if (!consent) return Success(Outcome(403, new Dictionary<string, object?> { ["code"] = "CONSENT_GATE_CLOSED", ["studentId"] = studentId, ["failClosed"] = true }, false));
        var content = $"{ReadString(request, "prompt").Trim()} :: adapted lesson for {studentId}";
        if (UnsafeTerms.Any(term => content.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            await AuditAsync("SAFETY_GATE_REJECTED", lessonId, cancellationToken).ConfigureAwait(false);
            return Success(Outcome(422, new Dictionary<string, object?> { ["code"] = "SAFETY_REJECTED", ["lessonId"] = lessonId, ["published"] = false }, false));
        }

        var token = IssueSafetyToken(lessonId, studentId);
        var tokenBody = new Dictionary<string, object?>
        {
            ["tokenId"] = token.TokenId,
            ["lessonId"] = token.LessonId,
            ["studentId"] = token.StudentId,
            ["safetyReviewId"] = token.SafetyReviewId,
            ["issuedAt"] = token.IssuedAt,
            ["expiresAt"] = token.ExpiresAt,
            ["tenantId"] = CurrentTenant.TenantId
        };
        var lessonBody = new Dictionary<string, object?>
        {
            ["lessonId"] = lessonId,
            ["studentId"] = studentId,
            ["content"] = content,
            ["safetyGateToken"] = token.TokenId,
            ["status"] = "PUBLISHED",
            ["source_flow"] = AiSafetyModerationConstants.FlowId,
            ["order"] = new[] { "compose", "safety_gate", "publish" },
            ["tenantId"] = CurrentTenant.TenantId,
            ["knowledgeScope"] = "PRIVATE"
        };
        await _documents.UpsertAsync(AiSafetyModerationConstants.SafetyTokenCollection, ScopedId(token.TokenId), tokenBody, null, cancellationToken).ConfigureAwait(false);
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.LessonCollection, ScopedId(lessonId), lessonBody, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.lesson.published", lessonId, lessonBody), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["lessonId"] = lessonId, ["status"] = "PUBLISHED", ["safetyGateToken"] = token.TokenId, ["order"] = new[] { "compose", "safety_gate", "publish" } };
        await _idempotency.MarkSucceededAsync(OperationKey("lesson", idempotencyKey!), response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> GradeQuizAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (ClientScoreKeys.Any(request.ContainsKey)) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "CLIENT_SCORE_REJECTED", ["serverRecomputeRequired"] = true }, false));
        var errors = ValidateRequired(request, "studentId", "quizId", "answers", "answerKey");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var studentId = ReadString(request, "studentId");
        var quizId = ReadString(request, "quizId");
        var answers = ReadMap(request, "answers");
        var answerKey = ReadMap(request, "answerKey");
        var total = answerKey.Count;
        var correct = answerKey.Count(pair => answers.TryGetValue(pair.Key, out var answer) && ValuesEqual(answer, pair.Value));
        var scorePercent = total == 0 ? 0 : Math.Round(correct * 100m / total, 2);
        var attemptId = ScopedId($"{quizId}:{Hash(idempotencyKey!)}");
        var attempt = new Dictionary<string, object?>
        {
            ["attemptId"] = attemptId,
            ["studentId"] = studentId,
            ["quizId"] = quizId,
            ["correct"] = correct,
            ["total"] = total,
            ["scorePercent"] = scorePercent,
            ["scoreSource"] = "SERVER_RECOMPUTED",
            ["tenantId"] = CurrentTenant.TenantId,
            ["source_flow"] = AiSafetyModerationConstants.FlowId
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.QuizAttemptCollection, attemptId, attempt, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        var ledger = new Dictionary<string, object?>
        {
            ["ledgerId"] = ScopedId($"ledger:{Guid.NewGuid():N}"),
            ["studentId"] = studentId,
            ["quizId"] = quizId,
            ["points"] = correct * 10,
            ["appendOnly"] = true,
            ["source_flow"] = AiSafetyModerationConstants.FlowId,
            ["tenantId"] = CurrentTenant.TenantId
        };
        await _documents.UpsertAsync(AiSafetyModerationConstants.LedgerCollection, Convert.ToString(ledger["ledgerId"], CultureInfo.InvariantCulture)!, ledger, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.gamification.queued", quizId, ledger), cancellationToken).ConfigureAwait(false);
        var response = new Dictionary<string, object?> { ["attemptId"] = attemptId, ["scorePercent"] = scorePercent, ["correct"] = correct, ["total"] = total, ["gamificationDispatch"] = "queued_async" };
        await _idempotency.MarkSucceededAsync(OperationKey("quiz", idempotencyKey!), response, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, response, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> EvaluateStreakAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (ClientTimezoneKeys.Any(request.ContainsKey)) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "CLIENT_TIMEZONE_REJECTED", ["timezoneSource"] = "profile" }, false));
        var errors = ValidateRequired(request, "studentId", "profile");
        var profile = ReadMap(request, "profile");
        var timezone = ReadString(profile, "timezone");
        if (string.IsNullOrWhiteSpace(timezone)) errors.Add(FieldError("profile.timezone", "Profile timezone is required."));
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);

        var now = DateTimeOffset.UtcNow;
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timezone); }
        catch (TimeZoneNotFoundException) { return Success(ValidationFailed([FieldError("profile.timezone", "Unknown timezone.")])); }
        catch (InvalidTimeZoneException) { return Success(ValidationFailed([FieldError("profile.timezone", "Invalid timezone.")])); }

        var localDate = TimeZoneInfo.ConvertTime(now, zone).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var studentId = ReadString(request, "studentId");
        var body = new Dictionary<string, object?>
        {
            ["studentId"] = studentId,
            ["localDate"] = localDate,
            ["timezone"] = timezone,
            ["timezoneSource"] = "F982_PROFILE",
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.StreakCollection, ScopedId($"{studentId}:{localDate}"), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        return Success(Outcome(201, new Dictionary<string, object?> { ["studentId"] = studentId, ["localDate"] = localDate, ["timezoneSource"] = "F982_PROFILE" }, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> ScheduleLessonAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "studentId", "lessonId", "reminderAt");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var studentId = ReadString(request, "studentId");
        if (!await HasGrantedConsentAsync(studentId, cancellationToken).ConfigureAwait(false)) return Success(Outcome(403, new Dictionary<string, object?> { ["code"] = "CONSENT_GATE_CLOSED", ["failClosed"] = true }, false));
        var body = new Dictionary<string, object?>
        {
            ["studentId"] = studentId,
            ["lessonId"] = ReadString(request, "lessonId"),
            ["reminderAt"] = ReadString(request, "reminderAt"),
            ["reminderSource"] = "tutor-reminder",
            ["tenantId"] = CurrentTenant.TenantId,
            ["source_flow"] = AiSafetyModerationConstants.FlowId
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.ScheduleCollection, ScopedId($"{studentId}:{ReadString(request, "lessonId")}"), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.lesson.scheduled", ReadString(request, "lessonId"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, new Dictionary<string, object?> { ["status"] = "SCHEDULED", ["reminderSource"] = "tutor-reminder" }, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> DeliverDomainPackAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "studentId", "packKey");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (!ReadBool(request, "freedomConfigEnabled") || ReadMap(request, "packConfig").Count == 0)
        {
            return Success(Outcome(200, new Dictionary<string, object?> { ["status"] = "SKIPPED", ["skipped"] = true, ["reason"] = "FREEDOM_CONFIG_MISSING" }, false));
        }

        var packKey = ReadString(request, "packKey");
        var body = new Dictionary<string, object?> { ["studentId"] = ReadString(request, "studentId"), ["packKey"] = packKey, ["status"] = "DELIVERED", ["tenantId"] = CurrentTenant.TenantId };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.DomainPackCollection, ScopedId(packKey), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.domain-pack.delivered", packKey, body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, new Dictionary<string, object?> { ["status"] = "DELIVERED", ["packKey"] = packKey }, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> SyncCalendarAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        if (DirectCalendarSdkKeys.Any(request.ContainsKey)) return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "CALENDAR_SDK_DIRECT_IMPORT_REJECTED" }, false));
        var errors = ValidateRequired(request, "studentId", "lessonId", "connectorId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (RequireIdempotency(idempotencyKey) is { } missing) return Success(missing);
        var body = new Dictionary<string, object?>
        {
            ["studentId"] = ReadString(request, "studentId"),
            ["lessonId"] = ReadString(request, "lessonId"),
            ["connectorId"] = ReadString(request, "connectorId"),
            ["status"] = "SYNCED",
            ["source_flow"] = AiSafetyModerationConstants.FlowId,
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.CalendarSyncCollection, ScopedId($"{ReadString(request, "studentId")}:{ReadString(request, "lessonId")}"), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        await _outbox.AppendAsync(Envelope("xiigen.ai-safety.calendar.synced", ReadString(request, "lessonId"), body), cancellationToken).ConfigureAwait(false);
        return Success(Outcome(201, new Dictionary<string, object?> { ["status"] = "SYNCED", ["source_flow"] = AiSafetyModerationConstants.FlowId }, true, true));
    }

    public async Task<DataProcessResult<AiSafetyModerationOutcome>> TrackProgressAsync(IReadOnlyDictionary<string, object?> request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (RejectTenantPayload(request) is { } tenantError) return Success(tenantError);
        var errors = ValidateRequired(request, "studentId", "lessonId");
        if (errors.Count > 0) return Success(ValidationFailed(errors));
        if (!ReadBool(request, "freedomConfigEnabled")) return Success(Outcome(200, new Dictionary<string, object?> { ["status"] = "SKIPPED", ["reason"] = "FREEDOM_CONFIG_MISSING" }, false));
        if (string.IsNullOrWhiteSpace(ReadString(request, "safetyGateToken"))) return Success(Outcome(403, new Dictionary<string, object?> { ["code"] = "SAFETY_GATE_TOKEN_REQUIRED" }, false));
        var body = new Dictionary<string, object?>
        {
            ["studentId"] = ReadString(request, "studentId"),
            ["lessonId"] = ReadString(request, "lessonId"),
            ["progressPercent"] = ReadInt(request, "progressPercent"),
            ["safetyGateToken"] = ReadString(request, "safetyGateToken"),
            ["source_flow"] = AiSafetyModerationConstants.FlowId,
            ["tenantId"] = CurrentTenant.TenantId
        };
        var saved = await _documents.UpsertAsync(AiSafetyModerationConstants.ProgressCollection, ScopedId($"{ReadString(request, "studentId")}:{ReadString(request, "lessonId")}"), body, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failure<AiSafetyModerationOutcome>(saved.Error!.Code, saved.Error.Message, saved.Error.Details);
        return Success(Outcome(201, new Dictionary<string, object?> { ["status"] = "TRACKED", ["progressPercent"] = body["progressPercent"] }, true));
    }

    public DataProcessResult<AiSafetyModerationOutcome> ValidatePlanCoverage(IReadOnlyDictionary<string, object?> request)
    {
        var steps = ReadPlanSteps(request);
        var taskIds = steps.Select(step => step.TaskTypeId).ToHashSet(StringComparer.Ordinal);
        var missing = AiSafetyModerationConstants.RequiredTaskTypes.Where(task => !taskIds.Contains(task)).ToArray();
        var duplicate = steps.GroupBy(step => step.TaskTypeId, StringComparer.Ordinal).Any(group => group.Count() != 1);
        if (missing.Length > 0 || duplicate)
        {
            return Success(Outcome(400, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_FAILED", ["missing"] = missing, ["singleResponsibility"] = !duplicate }, false));
        }

        return Success(Outcome(200, new Dictionary<string, object?> { ["code"] = "PLAN_COVERAGE_OK", ["taskTypeCount"] = taskIds.Count, ["singleResponsibility"] = true, ["requiredTasks"] = AiSafetyModerationConstants.RequiredTaskTypes }, false));
    }

    public async Task<DataProcessResult<AiSafetyModerationDashboard>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var filter = new Dictionary<string, object?> { ["tenantId"] = CurrentTenant.TenantId };
        var consent = await _documents.SearchAsync(AiSafetyModerationConstants.ConsentGateCollection, filter, cancellationToken).ConfigureAwait(false);
        var lessons = await _documents.SearchAsync(AiSafetyModerationConstants.LessonCollection, filter, cancellationToken).ConfigureAwait(false);
        var attempts = await _documents.SearchAsync(AiSafetyModerationConstants.QuizAttemptCollection, filter, cancellationToken).ConfigureAwait(false);
        var progress = await _documents.SearchAsync(AiSafetyModerationConstants.ProgressCollection, filter, cancellationToken).ConfigureAwait(false);
        return Success(new AiSafetyModerationDashboard(
            (consent.Data ?? []).Select(record => record.Body).ToArray(),
            (lessons.Data ?? []).Select(record => record.Body).ToArray(),
            (attempts.Data ?? []).Select(record => record.Body).ToArray(),
            (progress.Data ?? []).Select(record => record.Body).ToArray(),
            new Dictionary<string, object?> { ["consentGateCount"] = consent.Data?.Count ?? 0, ["lessonCount"] = lessons.Data?.Count ?? 0, ["quizAttemptCount"] = attempts.Data?.Count ?? 0, ["progressCount"] = progress.Data?.Count ?? 0 }));
    }

    private async Task<AiSafetyModerationOutcome?> ReserveAsync(string op, CancellationToken cancellationToken)
    {
        var reservation = await _idempotency.TryReserveAsync(op, IdempotencyTtl, cancellationToken).ConfigureAwait(false);
        var data = reservation.Data;
        return data is { CachedResponse: not null } && string.Equals(data.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            ? Outcome(200, data.CachedResponse, false)
            : null;
    }

    private async Task<bool> HasGrantedConsentAsync(string studentId, CancellationToken cancellationToken)
    {
        var record = await _documents.GetAsync(AiSafetyModerationConstants.ConsentGateCollection, ConsentGateId(studentId, "learning"), cancellationToken).ConfigureAwait(false);
        if (record.Data is null) return false;
        return string.Equals(ReadString(record.Data.Body, "outcome"), "GRANTED", StringComparison.Ordinal) && ReadBool(record.Data.Body, "allowed");
    }

    private SafetyGateToken IssueSafetyToken(string lessonId, string studentId)
    {
        var reviewId = $"review-{Guid.NewGuid():N}";
        var issuedAt = DateTimeOffset.UtcNow;
        return new SafetyGateToken($"sgt-{Hash($"{CurrentTenant.TenantId}:{lessonId}:{studentId}:{reviewId}")}", lessonId, studentId, reviewId, issuedAt.ToString("O", CultureInfo.InvariantCulture), issuedAt.AddHours(8).ToString("O", CultureInfo.InvariantCulture));
    }

    private async Task AuditAsync(string action, string subject, CancellationToken cancellationToken)
    {
        await _documents.UpsertAsync(AiSafetyModerationConstants.AuditCollection, ScopedId($"{action}:{subject}:{Guid.NewGuid():N}"), new Dictionary<string, object?> { ["action"] = action, ["subject"] = subject, ["tenantId"] = CurrentTenant.TenantId, ["knowledgeScope"] = "PRIVATE" }, null, cancellationToken).ConfigureAwait(false);
    }

    private CloudEventEnvelope Envelope(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(Guid.NewGuid().ToString("n"), $"/xiigen/{AiSafetyModerationConstants.FlowSlug}", type, "1.0", DateTimeOffset.UtcNow, subject, "application/json", data, new Dictionary<string, string> { ["xiigentenantid"] = CurrentTenant.TenantId, ["correlationid"] = CurrentCorrelationId });

    private string ScopedId(string id) => $"{CurrentTenant.TenantId}:{id}";
    private string ConsentGateId(string studentId, string consentType) => ScopedId($"consent:{studentId}:{consentType}");
    private static AiSafetyModerationOutcome Outcome(int status, IReadOnlyDictionary<string, object?> body, bool stateWritten, bool outboxRetained = false) => new(status, body, stateWritten, outboxRetained);
    private static AiSafetyModerationOutcome ValidationFailed(IReadOnlyList<IReadOnlyDictionary<string, object?>> details) => Outcome(400, new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED", ["details"] = details }, false);
    private static IReadOnlyDictionary<string, object?> FieldError(string field, string reason) => new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason };
    private static string OperationKey(string prefix, string key) => $"{AiSafetyModerationConstants.FlowId}:{prefix}:{key}";
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..32];
    private AiSafetyModerationOutcome? RequireIdempotency(string? key) => string.IsNullOrWhiteSpace(key) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "IDEMPOTENCY_KEY_REQUIRED" }, false) : null;
    private AiSafetyModerationOutcome? RejectTenantPayload(IReadOnlyDictionary<string, object?> request) => TenantPayloadKeys.Any(request.ContainsKey) ? Outcome(400, new Dictionary<string, object?> { ["code"] = "TENANT_SCOPE_PAYLOAD_REJECTED" }, false) : null;
    private static List<IReadOnlyDictionary<string, object?>> ValidateRequired(IReadOnlyDictionary<string, object?> request, params string[] fields) => fields.Where(field => string.IsNullOrWhiteSpace(ReadString(request, field))).Select(field => FieldError(field, $"{field} is required.")).ToList();
    private static string NormalizeConsentOutcome(IReadOnlyDictionary<string, object?> request)
    {
        var status = ReadString(request, "status");
        if (string.IsNullOrWhiteSpace(status) && request.ContainsKey("consented")) status = ReadBool(request, "consented") ? "GRANTED" : "DENIED";
        status = status.ToUpperInvariant();
        return status is "GRANTED" or "DENIED" or "PENDING" or "WITHDRAWN" ? status : "PENDING";
    }

    private static bool ValuesEqual(object? left, object? right) => string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    private static IReadOnlyDictionary<string, object?> ReadMap(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return new Dictionary<string, object?>();
        if (value is IReadOnlyDictionary<string, object?> map) return map;
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object) return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()) ?? new Dictionary<string, object?>();
        return new Dictionary<string, object?>();
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return string.Empty;
        return value is JsonElement element ? element.ToString() : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return false;
        return value is JsonElement element ? element.ValueKind == JsonValueKind.True : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> request, string key)
    {
        if (!request.TryGetValue(key, out var value) || value is null) return 0;
        return value is JsonElement element ? element.GetInt32() : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<AiSafetyPlanStep> ReadPlanSteps(IReadOnlyDictionary<string, object?> request)
    {
        if (!request.TryGetValue("planSteps", out var value) || value is null) return [];
        if (value is IEnumerable<AiSafetyPlanStep> typed) return typed.ToArray();
        if (value is IEnumerable<IReadOnlyDictionary<string, object?>> maps) return maps.Select(map => new AiSafetyPlanStep(ReadString(map, "id"), ReadString(map, "taskTypeId"), ReadString(map, "name"), ReadString(map, "responsibility"), ReadMap(map, "config"))).ToArray();
        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array) return JsonSerializer.Deserialize<List<AiSafetyPlanStep>>(element.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return [];
    }
}
