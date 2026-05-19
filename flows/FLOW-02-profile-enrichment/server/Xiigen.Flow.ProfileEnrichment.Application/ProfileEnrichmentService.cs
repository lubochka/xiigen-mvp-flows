using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.ProfileEnrichment.Application;

public sealed class ProfileEnrichmentService : MicroserviceBase<ProfileEnrichmentService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightResources = new(StringComparer.Ordinal);
    private static readonly string[] TrendingFallback = ["trending-001", "trending-002", "trending-003"];

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public ProfileEnrichmentService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<ProfileEnrichmentService> logger)
        : base(
            new ServiceDescriptor("T50-T52", nameof(ProfileEnrichmentService), ProfileEnrichmentConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<ProfileEnrichmentOutcome>> SubmitQuestionnaireAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<ProfileEnrichmentOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var userId = ReadString(request, "userId");
        var industry = ReadString(request, "industry");
        var stage = ReadString(request, "businessStage") ?? ReadString(request, "stage") ?? "startup";
        var location = ReadString(request, "location") ?? "remote";
        var teamSize = ReadString(request, "teamSize") ?? "small";

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(industry))
        {
            return Failure<ProfileEnrichmentOutcome>(
                "VALIDATION_FAILED",
                "Profile enrichment input validation failed",
                new Dictionary<string, object?>
                {
                    ["details"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["field"] = string.IsNullOrWhiteSpace(userId) ? "userId" : "industry",
                            ["reason"] = "required"
                        }
                    }
                });
        }

        var operationKey = $"{ProfileEnrichmentConstants.FlowSlug}:submit:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<ProfileEnrichmentOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new ProfileEnrichmentOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var retained = await _outbox.AppendAsync(
                CreateEvent("xiigen.profile-enrichment.timeout-retained.v1", userId, new Dictionary<string, object?>
                {
                    ["userId"] = userId,
                    ["retryAfterMs"] = 5000
                }),
                cancellationToken).ConfigureAwait(false);

            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<ProfileEnrichmentOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<ProfileEnrichmentOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        var resourceKey = $"{CurrentTenant.TenantId}:{userId}";
        if (!InFlightResources.TryAdd(resourceKey, 1))
        {
            return Failure<ProfileEnrichmentOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent profile enrichment update detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var profileId = $"profile-{CurrentTenant.TenantId}-{userId}";
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var profileStored = await StorePrivateProfileAsync(profileId, userId, industry, stage, location, teamSize, now, ifMatch, cancellationToken).ConfigureAwait(false);
            if (profileStored.IsFailure)
            {
                return Failure<ProfileEnrichmentOutcome>(
                    profileStored.Error?.Code ?? "PROFILE_STORE_FAILED",
                    profileStored.Error?.Message ?? "Failed to store private profile",
                    profileStored.Error?.Details);
            }

            var matchingProfileId = $"match-{profileId}";
            var matchingStored = await StoreGlobalMatchingProfileAsync(matchingProfileId, userId, industry, stage, location, teamSize, now, cancellationToken).ConfigureAwait(false);
            if (matchingStored.IsFailure)
            {
                return Failure<ProfileEnrichmentOutcome>("MATCHING_PROFILE_STORE_FAILED", "Failed to store global matching profile");
            }

            var analytics = await StoreAnalyticsSegmentAsync(userId, industry, now, cancellationToken).ConfigureAwait(false);
            var learning = await StoreLearningProgramAsync(userId, profileId, industry, now, cancellationToken).ConfigureAwait(false);
            if (analytics.IsFailure || learning.IsFailure)
            {
                return Failure<ProfileEnrichmentOutcome>("FAN_IN_FAILED", "Profile enrichment fan-in failed");
            }

            var matching = await ScoreMatchingAsync(userId, profileId, ReadBool(request, "simulateMatchingTimeout"), cancellationToken).ConfigureAwait(false);
            if (matching.IsFailure || matching.Data is null)
            {
                return Failure<ProfileEnrichmentOutcome>(matching.Error?.Code ?? "MATCHING_FAILED", matching.Error?.Message ?? "Matching convergence failed");
            }

            var personalization = await BuildPersonalizationAsync(userId, profileId, matching.Data, cancellationToken).ConfigureAwait(false);
            if (personalization.IsFailure || personalization.Data is null)
            {
                return Failure<ProfileEnrichmentOutcome>(personalization.Error?.Code ?? "PERSONALIZATION_FAILED", personalization.Error?.Message ?? "Personalization failed");
            }

            var response = new Dictionary<string, object?>
            {
                ["id"] = profileId,
                ["profileId"] = profileId,
                ["version"] = profileStored.Data!.Version,
                ["matchingProfileId"] = matchingProfileId,
                ["matching"] = matching.Data.MatchedBusinessIds,
                ["suggestions"] = matching.Data.Suggestions,
                ["partialResults"] = matching.Data.PartialResults,
                ["feedId"] = personalization.Data.FeedId,
                ["signalsUsed"] = personalization.Data.SignalsUsed,
                ["degraded"] = personalization.Data.Degraded,
                ["completionEventName"] = ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent
            };

            await _outbox.AppendAsync(CreateEvent(ProfileEnrichmentConstants.EnrichmentDataReadyEvent, profileId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(
                CreateEvent(matching.Data.PartialResults ? ProfileEnrichmentConstants.NeedsMoreDataEvent : ProfileEnrichmentConstants.MatchingConvergedEvent, profileId, response),
                cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(ProfileEnrichmentConstants.CompletedEvent, profileId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent, profileId, response), cancellationToken).ConfigureAwait(false);

            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new ProfileEnrichmentOutcome(201, response, true, true));
        }
        finally
        {
            InFlightResources.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<ProfileEnrichmentReadModel>> GetMatchingAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var match = await LatestAsync(ProfileEnrichmentConstants.BusinessMatches, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);
        var suggestions = await LatestAsync(ProfileEnrichmentConstants.ConnectionSuggestions, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);

        return Success(new ProfileEnrichmentReadModel(
            ReadString(match?.Body, "profile_id") ?? string.Empty,
            userId,
            ReadStringArray(match?.Body, "matched_business_ids"),
            ReadStringArray(suggestions?.Body, "suggestions"),
            ReadBool(match?.Body, "partial_results"),
            0,
            false,
            [],
            ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent));
    }

    public async Task<DataProcessResult<ProfileEnrichmentReadModel>> GetPersonalizationAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var feed = await LatestAsync(ProfileEnrichmentConstants.PersonalizationFeeds, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);

        return Success(new ProfileEnrichmentReadModel(
            ReadString(feed?.Body, "profile_id") ?? string.Empty,
            userId,
            [],
            [],
            false,
            ReadInt(feed?.Body, "signals_used"),
            ReadBool(feed?.Body, "degraded"),
            ReadStringArray(feed?.Body, "content_items"),
            ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent));
    }

    private async Task<DataProcessResult<DocumentRecord>> StorePrivateProfileAsync(
        string profileId,
        string userId,
        string industry,
        string stage,
        string location,
        string teamSize,
        string now,
        string? ifMatch,
        CancellationToken cancellationToken)
    {
        var questionnaire = new Dictionary<string, object?>
        {
            ["industry"] = industry,
            ["businessStage"] = stage,
            ["location"] = location,
            ["teamSize"] = teamSize,
            ["submittedAt"] = now
        };
        return await _documents.UpsertAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            profileId,
            new Dictionary<string, object?>
            {
                ["id"] = profileId,
                ["profile_id"] = profileId,
                ["user_id"] = userId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["questionnaire"] = questionnaire,
                ["industry_code"] = industry,
                ["business_stage"] = stage,
                ["location_proximity"] = location,
                ["team_size_tier"] = teamSize,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now
            },
            ifMatch,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<DataProcessResult<DocumentRecord>> StoreGlobalMatchingProfileAsync(
        string matchingProfileId,
        string userId,
        string industry,
        string stage,
        string location,
        string teamSize,
        string now,
        CancellationToken cancellationToken) =>
        await _documents.UpsertAsync(
            ProfileEnrichmentConstants.MatchingProfiles,
            matchingProfileId,
            new Dictionary<string, object?>
            {
                ["id"] = matchingProfileId,
                ["matching_profile_id"] = matchingProfileId,
                ["user_id"] = userId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["industry_code"] = industry,
                ["business_stage"] = stage,
                ["location_proximity"] = location,
                ["team_size_tier"] = teamSize,
                ["knowledge_scope"] = "GLOBAL",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now
            },
            expectedVersion: null,
            cancellationToken).ConfigureAwait(false);

    private async Task<DataProcessResult> StoreAnalyticsSegmentAsync(string userId, string industry, string now, CancellationToken cancellationToken)
    {
        var segment = industry.Trim().ToLowerInvariant() switch
        {
            "tech" or "technology" => "TECH_STARTUP",
            "finance" => "FINTECH",
            "health" or "healthcare" => "HEALTHTECH",
            "retail" => "ECOMMERCE",
            "education" => "EDTECH",
            _ => "GENERAL"
        };
        var segmentId = $"segment-{CurrentTenant.TenantId}-{userId}";
        var result = await _documents.UpsertAsync(
            ProfileEnrichmentConstants.AnalyticsSegments,
            segmentId,
            new Dictionary<string, object?>
            {
                ["segment_id"] = segmentId,
                ["user_id"] = userId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["segment"] = segment,
                ["confidence"] = segment == "GENERAL" ? 0.5 : 0.85,
                ["degraded"] = false,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now
            },
            expectedVersion: null,
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? DataProcessResult.Success() : DataProcessResult.Failure(result.Error!.Code, result.Error.Message);
    }

    private async Task<DataProcessResult> StoreLearningProgramAsync(string userId, string profileId, string industry, string now, CancellationToken cancellationToken)
    {
        var module = industry.Trim().ToLowerInvariant() switch
        {
            "tech" or "technology" => "MODULE_TECH_FOUNDATIONS",
            "finance" => "MODULE_FINTECH_BASICS",
            "health" or "healthcare" => "MODULE_HEALTH_COMPLIANCE",
            "retail" => "MODULE_ECOMMERCE_SETUP",
            "education" => "MODULE_EDTECH_LAUNCH",
            _ => "MODULE_INTRO"
        };
        var programId = $"program-{CurrentTenant.TenantId}-{userId}";
        var result = await _documents.UpsertAsync(
            ProfileEnrichmentConstants.LearningPrograms,
            programId,
            new Dictionary<string, object?>
            {
                ["program_id"] = programId,
                ["profile_id"] = profileId,
                ["user_id"] = userId,
                ["tenant_id"] = CurrentTenant.TenantId,
                ["module_type"] = module,
                ["degraded"] = false,
                ["knowledge_scope"] = "PRIVATE",
                ["connection_type"] = "FLOW_SCOPED",
                ["created_at"] = now
            },
            expectedVersion: null,
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? DataProcessResult.Success() : DataProcessResult.Failure(result.Error!.Code, result.Error.Message);
    }

    private async Task<DataProcessResult<MatchingResult>> ScoreMatchingAsync(string userId, string profileId, bool forcePartial, CancellationToken cancellationToken)
    {
        var threshold = await GetFreedomNumberAsync("flow02_matching_convergence_threshold", 0.7, cancellationToken).ConfigureAwait(false);
        var profiles = await _documents.SearchAsync(
            ProfileEnrichmentConstants.MatchingProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["knowledge_scope"] = "GLOBAL" },
            cancellationToken).ConfigureAwait(false);
        if (profiles.IsFailure)
        {
            return Failure<MatchingResult>("MATCHING_READ_FAILED", "Could not read matching profiles");
        }

        var matchedIds = (profiles.Data ?? [])
            .Select(record => ReadString(record.Body, "matching_profile_id"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var partial = forcePartial || matchedIds.Length < 2;
        var topScore = partial ? Math.Min(threshold, 0.66) : 0.94;
        var scoringId = $"score-{CurrentTenant.TenantId}-{userId}";
        var matchDoc = new Dictionary<string, object?>
        {
            ["scoring_id"] = scoringId,
            ["profile_id"] = profileId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["matched_business_ids"] = matchedIds,
            ["partial_results"] = partial,
            ["top_score"] = topScore,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(ProfileEnrichmentConstants.BusinessMatches, scoringId, matchDoc, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<MatchingResult>("MATCHING_STORE_FAILED", "Could not store matching result");
        }

        var suggestionId = $"suggestions-{CurrentTenant.TenantId}-{userId}";
        var suggestions = matchedIds.Take(3).ToArray();
        var suggestionDoc = new Dictionary<string, object?>
        {
            ["suggestion_id"] = suggestionId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["suggestions"] = suggestions,
            ["partial_results"] = partial,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var suggestionStored = await _documents.UpsertAsync(ProfileEnrichmentConstants.ConnectionSuggestions, suggestionId, suggestionDoc, null, cancellationToken).ConfigureAwait(false);
        if (suggestionStored.IsFailure)
        {
            return Failure<MatchingResult>("SUGGESTION_STORE_FAILED", "Could not store connection suggestions");
        }

        return Success(new MatchingResult(scoringId, matchedIds, suggestions, partial, topScore));
    }

    private async Task<DataProcessResult<PersonalizationResult>> BuildPersonalizationAsync(string userId, string profileId, MatchingResult matching, CancellationToken cancellationToken)
    {
        var signals = 0;
        var items = new List<string>();
        var profile = await LatestAsync(ProfileEnrichmentConstants.BusinessProfiles, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);
        var segment = await LatestAsync(ProfileEnrichmentConstants.AnalyticsSegments, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);
        var program = await LatestAsync(ProfileEnrichmentConstants.LearningPrograms, TenantUserFilter(userId), cancellationToken).ConfigureAwait(false);

        AddSignalItem(ReadString(profile?.Body, "industry_code"), "industry-content", items, ref signals);
        AddSignalItem(ReadString(segment?.Body, "segment"), "segment-content", items, ref signals);
        AddSignalItem(ReadString(program?.Body, "module_type"), "curriculum-content", items, ref signals);
        AddSignalItem(matching.MatchedBusinessIds.FirstOrDefault(), "match-content", items, ref signals);

        if (items.Count == 0)
        {
            items.AddRange(TrendingFallback);
        }

        var degraded = signals < 4;
        var feedId = $"feed-{CurrentTenant.TenantId}-{userId}";
        var feedDoc = new Dictionary<string, object?>
        {
            ["feed_id"] = feedId,
            ["profile_id"] = profileId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["content_items"] = items.ToArray(),
            ["signals_used"] = signals,
            ["degraded"] = degraded,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(ProfileEnrichmentConstants.PersonalizationFeeds, feedId, feedDoc, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<PersonalizationResult>("FEED_STORE_FAILED", "Could not store personalized feed");
        }

        var completionId = $"completion-{CurrentTenant.TenantId}-{userId}";
        var completionDoc = new Dictionary<string, object?>
        {
            ["completion_id"] = completionId,
            ["profile_id"] = profileId,
            ["feed_id"] = feedId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["completion_event_name"] = ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var completionStored = await _documents.UpsertAsync(ProfileEnrichmentConstants.PersonalizationCompletions, completionId, completionDoc, null, cancellationToken).ConfigureAwait(false);
        if (completionStored.IsFailure)
        {
            return Failure<PersonalizationResult>("COMPLETION_STORE_FAILED", "Could not store personalization completion");
        }

        return Success(new PersonalizationResult(feedId, items, signals, degraded));
    }

    private async Task<DocumentRecord?> LatestAsync(string collection, IReadOnlyDictionary<string, object?> filter, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(collection, filter, cancellationToken).ConfigureAwait(false);
        return result.Data?.OrderByDescending(record => record.UpdatedAt).FirstOrDefault();
    }

    private IReadOnlyDictionary<string, object?> TenantUserFilter(string userId) =>
        new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["user_id"] = userId };

    private async Task<double> GetFreedomNumberAsync(string key, double fallback, CancellationToken cancellationToken)
    {
        var value = await _freedom.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value.IsSuccess && value.Data?.Value is not null && double.TryParse(Convert.ToString(value.Data.Value, CultureInfo.InvariantCulture), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            "xiigen://flow/profile-enrichment",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = ProfileEnrichmentConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static void AddSignalItem(string? value, string prefix, List<string> items, ref int signals)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        signals++;
        items.Add($"{prefix}-{value}");
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static int ReadInt(IReadOnlyDictionary<string, object?>? source, string key) =>
        source is not null && source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : 0;

    private static IReadOnlyList<string> ReadStringArray(IReadOnlyDictionary<string, object?>? source, string key)
    {
        if (source is null || !source.TryGetValue(key, out var value))
        {
            return [];
        }

        return value switch
        {
            string[] strings => strings,
            IEnumerable<string> strings => strings.ToArray(),
            object?[] objects => objects.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToArray(),
            _ => []
        };
    }

    private sealed record MatchingResult(
        string ScoringId,
        IReadOnlyList<string> MatchedBusinessIds,
        IReadOnlyList<string> Suggestions,
        bool PartialResults,
        double TopScore);

    private sealed record PersonalizationResult(
        string FeedId,
        IReadOnlyList<string> ContentItems,
        int SignalsUsed,
        bool Degraded);
}
