using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.CompletionGamification.Application;

public sealed class CompletionGamificationService : MicroserviceBase<CompletionGamificationService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightCompletions = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public CompletionGamificationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<CompletionGamificationService> logger)
        : base(
            new ServiceDescriptor("T44-T46", nameof(CompletionGamificationService), CompletionGamificationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<CompletionGamificationOutcome>> SubmitCompletionAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<CompletionGamificationOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var validation = ValidateCompletionRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var lessonId = ReadString(request, "lessonId")!.Trim();
        var learnerId = ReadString(request, "learnerId")!.Trim();
        var scorePercent = ReadScorePercent(request);
        var offsetMinutes = ReadNullableInt(request, "userTimezoneOffset")!.Value;
        var operationKey = $"{CompletionGamificationConstants.FlowSlug}:completion:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<CompletionGamificationOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new CompletionGamificationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["lessonId"] = lessonId,
                ["learnerId"] = learnerId,
                ["retryAfterMs"] = 5000
            };
            var retained = await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.TimeoutRetainedEvent, lessonId, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<CompletionGamificationOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<CompletionGamificationOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        var resourceKey = $"{CurrentTenant.TenantId}:{lessonId}:{learnerId}";
        if (!InFlightCompletions.TryAdd(resourceKey, 1))
        {
            await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Concurrent completion detected", cancellationToken).ConfigureAwait(false);
            return Failure<CompletionGamificationOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent completion detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var existing = await FindCompletionAsync(lessonId, learnerId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Completion already recorded", cancellationToken).ConfigureAwait(false);
                return Failure<CompletionGamificationOutcome>(
                    "CONCURRENT_UPDATE",
                    "Completion already recorded",
                    new Dictionary<string, object?> { ["currentVersion"] = existing.Version, ["reason"] = "duplicate_completion" });
            }

            var now = DateTimeOffset.UtcNow;
            var localDate = now.AddMinutes(offsetMinutes).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var completionId = $"completion-{CurrentTenant.TenantId}-{lessonId}-{learnerId}";
            var completionBody = new Dictionary<string, object?>
            {
                ["id"] = completionId,
                ["completion_id"] = completionId,
                ["lesson_id"] = lessonId,
                ["learner_id"] = learnerId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["score_percent"] = scorePercent,
                ["user_timezone_offset"] = offsetMinutes,
                ["local_date"] = localDate,
                ["status"] = "COMPLETION_RECORDED",
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now.ToString("O", CultureInfo.InvariantCulture)
            };

            var completionStored = await _documents.UpsertAsync(
                CompletionGamificationConstants.Completions,
                completionId,
                completionBody,
                ifMatch,
                cancellationToken).ConfigureAwait(false);
            if (completionStored.IsFailure)
            {
                await _idempotency.MarkFailedAsync(operationKey, completionStored.Error?.Code ?? "COMPLETION_STORE_FAILED", completionStored.Error?.Message ?? "Failed to store completion", cancellationToken).ConfigureAwait(false);
                return Failure<CompletionGamificationOutcome>(
                    completionStored.Error?.Code ?? "COMPLETION_STORE_FAILED",
                    completionStored.Error?.Message ?? "Failed to store completion",
                    completionStored.Error?.Details);
            }

            await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.CompletionRecordedEvent, lessonId, CompletionResponse(completionStored.Data!)), cancellationToken).ConfigureAwait(false);

            var points = await CalculateServerSidePointsAsync(scorePercent, request, cancellationToken).ConfigureAwait(false);
            var priorTotal = await SumLedgerPointsAsync(learnerId, cancellationToken).ConfigureAwait(false);
            var earnedPoints = points + ReadNullableInt(request, "peerBonusPoints").GetValueOrDefault(0);
            var newTotal = priorTotal + earnedPoints;
            var previousLevel = LevelForTotal(priorTotal);
            var currentLevel = LevelForTotal(newTotal);
            var ledgerId = $"ledger-{CurrentTenant.TenantId}-{lessonId}-{learnerId}";
            var ledgerBody = new Dictionary<string, object?>
            {
                ["id"] = ledgerId,
                ["ledger_id"] = ledgerId,
                ["completion_id"] = completionId,
                ["lesson_id"] = lessonId,
                ["learner_id"] = learnerId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["earned_points"] = earnedPoints,
                ["total_points"] = newTotal,
                ["level"] = currentLevel,
                ["source"] = "server-calculated",
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now.ToString("O", CultureInfo.InvariantCulture)
            };
            var ledgerStored = await _documents.UpsertAsync(CompletionGamificationConstants.PointLedger, ledgerId, ledgerBody, null, cancellationToken).ConfigureAwait(false);
            if (ledgerStored.IsFailure)
            {
                return Failure<CompletionGamificationOutcome>(ledgerStored.Error?.Code ?? "LEDGER_STORE_FAILED", ledgerStored.Error?.Message ?? "Failed to store point ledger");
            }

            await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.PointsAwardedEvent, lessonId, ledgerStored.Data!.Body), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.LedgerUpdatedEvent, lessonId, ledgerStored.Data.Body), cancellationToken).ConfigureAwait(false);

            var streak = await UpdateStreakAsync(learnerId, localDate, cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.StreakUpdatedEvent, lessonId, streak), cancellationToken).ConfigureAwait(false);

            var achievements = await UnlockAchievementsAsync(
                lessonId,
                learnerId,
                scorePercent,
                currentLevel,
                previousLevel,
                streak,
                cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.LevelUpCheckedEvent, lessonId, new Dictionary<string, object?> { ["learnerId"] = learnerId, ["level"] = currentLevel }), cancellationToken).ConfigureAwait(false);

            foreach (var achievement in achievements)
            {
                await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.AchievementUnlockedEvent, lessonId, achievement), cancellationToken).ConfigureAwait(false);
            }

            var social = await GateSocialShareAsync(lessonId, learnerId, request, achievements, cancellationToken).ConfigureAwait(false);
            var terminal = await CompleteLearningFlowAsync(lessonId, learnerId, completionId, newTotal, currentLevel, cancellationToken).ConfigureAwait(false);

            var response = new Dictionary<string, object?>
            {
                ["id"] = completionId,
                ["completionId"] = completionId,
                ["lessonId"] = lessonId,
                ["learnerId"] = learnerId,
                ["status"] = "COMPLETED",
                ["earnedPoints"] = earnedPoints,
                ["totalPoints"] = newTotal,
                ["level"] = currentLevel,
                ["currentStreak"] = streak["current_streak"],
                ["localDate"] = localDate,
                ["achievements"] = achievements,
                ["socialShareAllowed"] = social["social_share_allowed"],
                ["socialShareId"] = social.TryGetValue("social_share_id", out var shareId) ? shareId : null,
                ["learningCompletionId"] = terminal["learning_completion_id"],
                ["version"] = completionStored.Data!.Version
            };

            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new CompletionGamificationOutcome(201, response, true, true));
        }
        finally
        {
            InFlightCompletions.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<CompletionGamificationDashboard>> GetDashboardAsync(
        string learnerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(learnerId))
        {
            return Failure<CompletionGamificationDashboard>("VALIDATION_FAILED", "learnerId is required");
        }

        var tenantFilter = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["learner_id"] = learnerId.Trim()
        };
        var completions = await _documents.SearchAsync(CompletionGamificationConstants.Completions, tenantFilter, cancellationToken).ConfigureAwait(false);
        var achievements = await _documents.SearchAsync(CompletionGamificationConstants.Achievements, tenantFilter, cancellationToken).ConfigureAwait(false);
        var totalPoints = await SumLedgerPointsAsync(learnerId.Trim(), cancellationToken).ConfigureAwait(false);
        var streak = await GetStreakAsync(learnerId.Trim(), cancellationToken).ConfigureAwait(false);

        return Success(new CompletionGamificationDashboard(
            learnerId.Trim(),
            totalPoints,
            LevelForTotal(totalPoints),
            ReadNullableInt(streak?.Body, "current_streak").GetValueOrDefault(0),
            completions.Data?.Count ?? 0,
            achievements.Data?.Count ?? 0,
            (completions.Data ?? []).Select(record => CompletionResponse(record)).ToArray(),
            (achievements.Data ?? []).Select(record => new Dictionary<string, object?>(record.Body)).ToArray()));
    }

    public async Task<DataProcessResult<CompletionGamificationStatus>> GetCompletionStatusAsync(
        string lessonId,
        string learnerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lessonId) || string.IsNullOrWhiteSpace(learnerId))
        {
            return Failure<CompletionGamificationStatus>("VALIDATION_FAILED", "lessonId and learnerId are required");
        }

        var completion = await FindCompletionAsync(lessonId.Trim(), learnerId.Trim(), cancellationToken).ConfigureAwait(false);
        if (completion is null)
        {
            return Success(new CompletionGamificationStatus(lessonId.Trim(), learnerId.Trim(), "NOT_STARTED", 0, 0, 1, 0, string.Empty, false));
        }

        var ledger = await FindLedgerAsync(lessonId.Trim(), learnerId.Trim(), cancellationToken).ConfigureAwait(false);
        var streak = await GetStreakAsync(learnerId.Trim(), cancellationToken).ConfigureAwait(false);
        var social = await _documents.SearchAsync(
            CompletionGamificationConstants.SocialShares,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["lesson_id"] = lessonId.Trim(), ["learner_id"] = learnerId.Trim() },
            cancellationToken).ConfigureAwait(false);

        var totalPoints = ReadNullableInt(ledger?.Body, "total_points").GetValueOrDefault(0);
        return Success(new CompletionGamificationStatus(
            lessonId.Trim(),
            learnerId.Trim(),
            "COMPLETED",
            ReadNullableInt(ledger?.Body, "earned_points").GetValueOrDefault(0),
            totalPoints,
            ReadNullableInt(ledger?.Body, "level").GetValueOrDefault(LevelForTotal(totalPoints)),
            ReadNullableInt(streak?.Body, "current_streak").GetValueOrDefault(0),
            ReadString(completion.Body, "local_date") ?? string.Empty,
            (social.Data?.Count ?? 0) > 0));
    }

    public async Task<DataProcessResult<CompletionGamificationOutcome>> SubmitSocialShareAsync(
        string lessonId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<CompletionGamificationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var learnerId = ReadString(request, "learnerId");
        if (string.IsNullOrWhiteSpace(lessonId) || string.IsNullOrWhiteSpace(learnerId))
        {
            return Failure<CompletionGamificationOutcome>("VALIDATION_FAILED", "Social share requires lessonId and learnerId");
        }

        var operationKey = $"{CompletionGamificationConstants.FlowSlug}:share:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new CompletionGamificationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var achievement = await _documents.SearchAsync(
            CompletionGamificationConstants.Achievements,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["lesson_id"] = lessonId.Trim(), ["learner_id"] = learnerId.Trim() },
            cancellationToken).ConfigureAwait(false);
        if ((achievement.Data?.Count ?? 0) == 0 || !ReadBool(request, "shareConsent") || ReadBool(request, "privateMode"))
        {
            return Failure<CompletionGamificationOutcome>("SOCIAL_SHARE_REJECTED", "Social sharing requires an unlocked achievement and explicit consent");
        }

        var payload = await StoreSocialShareAsync(lessonId.Trim(), learnerId.Trim(), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, payload, cancellationToken).ConfigureAwait(false);
        return Success(new CompletionGamificationOutcome(201, payload, true, true));
    }

    public async Task<DataProcessResult<CompletionGamificationOutcome>> SubmitPeerGradeAsync(
        string lessonId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<CompletionGamificationOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var learnerId = ReadString(request, "learnerId");
        var graderId = ReadString(request, "graderId");
        var grade = ReadNullableInt(request, "grade");
        if (string.IsNullOrWhiteSpace(lessonId) || string.IsNullOrWhiteSpace(learnerId) || string.IsNullOrWhiteSpace(graderId) || grade is null or < 1 or > 5)
        {
            return Failure<CompletionGamificationOutcome>("VALIDATION_FAILED", "Peer grade requires learnerId, graderId, and grade from 1 to 5");
        }

        var operationKey = $"{CompletionGamificationConstants.FlowSlug}:grade:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new CompletionGamificationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var gradeId = $"grade-{CurrentTenant.TenantId}-{lessonId.Trim()}-{learnerId.Trim()}-{graderId.Trim()}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = gradeId,
            ["grade_id"] = gradeId,
            ["lesson_id"] = lessonId.Trim(),
            ["learner_id"] = learnerId.Trim(),
            ["grader_id"] = graderId.Trim(),
            ["tenant_id"] = CurrentTenant.TenantId,
            ["grade"] = grade.Value,
            ["comment"] = ReadString(request, "comment") ?? string.Empty,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(CompletionGamificationConstants.PeerGrades, gradeId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<CompletionGamificationOutcome>(stored.Error?.Code ?? "PEER_GRADE_STORE_FAILED", stored.Error?.Message ?? "Failed to store peer grade");
        }

        var response = new Dictionary<string, object?>(stored.Data!.Body);
        await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.PeerGradeRecordedEvent, lessonId.Trim(), response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new CompletionGamificationOutcome(201, response, true, true));
    }

    private DataProcessResult<CompletionGamificationOutcome>? ValidateCompletionRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "lessonId")))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "lessonId", ["reason"] = "required" });
        }

        if (string.IsNullOrWhiteSpace(ReadString(request, "learnerId")))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "learnerId", ["reason"] = "required" });
        }

        var score = ReadScorePercent(request);
        if (score < 0 || score > 100)
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "questionnaireResult.scorePercent", ["reason"] = "must be 0..100" });
        }

        if (ReadNullableInt(request, "userTimezoneOffset") is null)
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "userTimezoneOffset", ["reason"] = "required" });
        }

        if (request.ContainsKey("earnedPoints"))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "earnedPoints", ["reason"] = "server_calculated_only" });
        }

        return errors.Count == 0
            ? null
            : Failure<CompletionGamificationOutcome>("VALIDATION_FAILED", "Completion request failed validation", new Dictionary<string, object?> { ["details"] = errors });
    }

    private async Task<int> CalculateServerSidePointsAsync(
        int scorePercent,
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken)
    {
        var pointsPerPercent = (int)await GetFreedomNumberAsync("flow05_points_per_score_percent", 2, cancellationToken).ConfigureAwait(false);
        var perfectBonus = (int)await GetFreedomNumberAsync("flow05_perfect_score_bonus_points", 25, cancellationToken).ConfigureAwait(false);
        var completionBonus = (int)await GetFreedomNumberAsync("flow05_completion_bonus_points", 10, cancellationToken).ConfigureAwait(false);
        var points = scorePercent * pointsPerPercent + completionBonus;
        if (scorePercent >= 100 || ReadBool(request, "perfectScore"))
        {
            points += perfectBonus;
        }

        return Math.Max(0, points);
    }

    private async Task<IReadOnlyDictionary<string, object?>> UpdateStreakAsync(
        string learnerId,
        string localDate,
        CancellationToken cancellationToken)
    {
        var existing = await GetStreakAsync(learnerId, cancellationToken).ConfigureAwait(false);
        var priorDate = ReadString(existing?.Body, "last_local_date");
        var priorStreak = ReadNullableInt(existing?.Body, "current_streak").GetValueOrDefault(0);
        var current = priorDate switch
        {
            null => 1,
            var value when string.Equals(value, localDate, StringComparison.Ordinal) => priorStreak,
            var value when IsYesterday(value, localDate) => priorStreak + 1,
            _ => 1
        };
        var streakId = $"streak-{CurrentTenant.TenantId}-{learnerId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = streakId,
            ["streak_id"] = streakId,
            ["learner_id"] = learnerId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["current_streak"] = current,
            ["last_local_date"] = localDate,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(CompletionGamificationConstants.Streaks, streakId, body, existing?.Version, cancellationToken).ConfigureAwait(false);
        return body;
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> UnlockAchievementsAsync(
        string lessonId,
        string learnerId,
        int scorePercent,
        int currentLevel,
        int previousLevel,
        IReadOnlyDictionary<string, object?> streak,
        CancellationToken cancellationToken)
    {
        var existing = await _documents.SearchAsync(
            CompletionGamificationConstants.Achievements,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["learner_id"] = learnerId },
            cancellationToken).ConfigureAwait(false);
        var owned = (existing.Data ?? []).Select(record => ReadString(record.Body, "achievement_code")).Where(code => code is not null).ToHashSet(StringComparer.Ordinal);
        var candidates = new List<(string Code, string Title)>();
        if (!owned.Contains("first_completion"))
        {
            candidates.Add(("first_completion", "First lesson completed"));
        }

        if (scorePercent >= 90 && !owned.Contains("high_score"))
        {
            candidates.Add(("high_score", "High score"));
        }

        if (currentLevel > previousLevel)
        {
            candidates.Add(($"level_{currentLevel}", $"Level {currentLevel} reached"));
        }

        if (ReadNullableInt(streak, "current_streak").GetValueOrDefault(0) >= 3 && !owned.Contains("three_day_streak"))
        {
            candidates.Add(("three_day_streak", "Three day streak"));
        }

        var unlocked = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var candidate in candidates)
        {
            var id = $"achievement-{CurrentTenant.TenantId}-{learnerId}-{candidate.Code}";
            var body = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["achievement_id"] = id,
                ["achievement_code"] = candidate.Code,
                ["achievement_title"] = candidate.Title,
                ["lesson_id"] = lessonId,
                ["learner_id"] = learnerId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["unlocked_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            var stored = await _documents.UpsertAsync(CompletionGamificationConstants.Achievements, id, body, null, cancellationToken).ConfigureAwait(false);
            if (stored.IsSuccess)
            {
                unlocked.Add(new Dictionary<string, object?>(stored.Data!.Body));
            }
        }

        return unlocked;
    }

    private async Task<IReadOnlyDictionary<string, object?>> GateSocialShareAsync(
        string lessonId,
        string learnerId,
        IReadOnlyDictionary<string, object?> request,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> achievements,
        CancellationToken cancellationToken)
    {
        if (achievements.Count == 0 || !ReadBool(request, "shareConsent") || ReadBool(request, "privateMode"))
        {
            return new Dictionary<string, object?>
            {
                ["social_share_allowed"] = false,
                ["reason"] = achievements.Count == 0 ? "NO_ACHIEVEMENT" : "PRIVACY_GATE"
            };
        }

        return await StoreSocialShareAsync(lessonId, learnerId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<string, object?>> StoreSocialShareAsync(
        string lessonId,
        string learnerId,
        CancellationToken cancellationToken)
    {
        var shareId = $"share-{CurrentTenant.TenantId}-{lessonId}-{learnerId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = shareId,
            ["social_share_id"] = shareId,
            ["lesson_id"] = lessonId,
            ["learner_id"] = learnerId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["social_share_allowed"] = true,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(CompletionGamificationConstants.SocialShares, shareId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.SocialGatePassedEvent, lessonId, body), cancellationToken).ConfigureAwait(false);
        return body;
    }

    private async Task<IReadOnlyDictionary<string, object?>> CompleteLearningFlowAsync(
        string lessonId,
        string learnerId,
        string completionId,
        int totalPoints,
        int level,
        CancellationToken cancellationToken)
    {
        var learningCompletionId = $"learning-completion-{CurrentTenant.TenantId}-{lessonId}-{learnerId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = learningCompletionId,
            ["learning_completion_id"] = learningCompletionId,
            ["completion_id"] = completionId,
            ["lesson_id"] = lessonId,
            ["learner_id"] = learnerId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["total_points"] = totalPoints,
            ["level"] = level,
            ["status"] = "ALL_GAMIFICATION_COMPLETE",
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(CompletionGamificationConstants.LearningCompletions, learningCompletionId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(CompletionGamificationConstants.FlowCompletedEvent, lessonId, body), cancellationToken).ConfigureAwait(false);
        return body;
    }

    private async Task<DocumentRecord?> FindCompletionAsync(string lessonId, string learnerId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            CompletionGamificationConstants.Completions,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["lesson_id"] = lessonId, ["learner_id"] = learnerId },
            cancellationToken).ConfigureAwait(false);
        return result.Data?.FirstOrDefault();
    }

    private async Task<DocumentRecord?> FindLedgerAsync(string lessonId, string learnerId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            CompletionGamificationConstants.PointLedger,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["lesson_id"] = lessonId, ["learner_id"] = learnerId },
            cancellationToken).ConfigureAwait(false);
        return result.Data?.FirstOrDefault();
    }

    private async Task<DocumentRecord?> GetStreakAsync(string learnerId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            CompletionGamificationConstants.Streaks,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["learner_id"] = learnerId },
            cancellationToken).ConfigureAwait(false);
        return result.Data?.FirstOrDefault();
    }

    private async Task<int> SumLedgerPointsAsync(string learnerId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            CompletionGamificationConstants.PointLedger,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["learner_id"] = learnerId },
            cancellationToken).ConfigureAwait(false);
        return (result.Data ?? []).Sum(record => ReadNullableInt(record.Body, "earned_points").GetValueOrDefault(0));
    }

    private static Dictionary<string, object?> CompletionResponse(DocumentRecord record) =>
        new()
        {
            ["completionId"] = record.Id,
            ["lessonId"] = ReadString(record.Body, "lesson_id"),
            ["learnerId"] = ReadString(record.Body, "learner_id"),
            ["scorePercent"] = ReadNullableInt(record.Body, "score_percent"),
            ["localDate"] = ReadString(record.Body, "local_date"),
            ["version"] = record.Version
        };

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{CompletionGamificationConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = CompletionGamificationConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private async Task<double> GetFreedomNumberAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value.Data?.Value is int integer)
        {
            return integer;
        }

        if (value.Data?.Value is long number)
        {
            return number;
        }

        if (value.Data?.Value is double floating)
        {
            return floating;
        }

        if (value.Data?.Value is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static int ReadScorePercent(IReadOnlyDictionary<string, object?> request)
    {
        var direct = ReadNullableInt(request, "scorePercent");
        if (direct is not null)
        {
            return direct.Value;
        }

        if (request.TryGetValue("questionnaireResult", out var nested) && nested is IReadOnlyDictionary<string, object?> dictionary)
        {
            return ReadNullableInt(dictionary, "scorePercent").GetValueOrDefault(-1);
        }

        if (nested is Dictionary<string, object?> mutable)
        {
            return ReadNullableInt(mutable, "scorePercent").GetValueOrDefault(-1);
        }

        return -1;
    }

    private static int LevelForTotal(int totalPoints) => Math.Max(1, totalPoints / 500 + 1);

    private static bool IsYesterday(string priorLocalDate, string currentLocalDate) =>
        DateTime.TryParseExact(priorLocalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var prior) &&
        DateTime.TryParseExact(currentLocalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var current) &&
        prior.Date == current.Date.AddDays(-1);

    private static string? ReadString(IReadOnlyDictionary<string, object?>? values, string key)
    {
        if (values is null || !values.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            string text => text,
            Guid guid => guid.ToString("D"),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static int? ReadNullableInt(IReadOnlyDictionary<string, object?>? values, string key)
    {
        if (values is null || !values.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int integer => integer,
            long number => checked((int)number),
            double floating => (int)Math.Round(floating),
            decimal money => (int)Math.Round(money),
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) && value switch
        {
            bool flag => flag,
            string text => bool.TryParse(text, out var parsed) && parsed,
            _ => false
        };
}
