
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.FriendRequestSocialFeed.Application;

public sealed class FriendRequestSocialFeedService : MicroserviceBase<FriendRequestSocialFeedService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightRequests = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> InFlightResponses = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public FriendRequestSocialFeedService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<FriendRequestSocialFeedService> logger)
        : base(
            new ServiceDescriptor("FLOW-07", nameof(FriendRequestSocialFeedService), FriendRequestSocialFeedConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<FriendRequestSocialFeedOutcome>> SubmitFriendRequestAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<FriendRequestSocialFeedOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var validation = ValidateFriendRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var senderUserId = ReadString(request, "senderUserId")!.Trim();
        var recipientUserId = ReadString(request, "recipientUserId")!.Trim();
        var requestId = (ReadString(request, "requestId") ?? $"request-{senderUserId}-{recipientUserId}").Trim();
        var operationKey = $"{FriendRequestSocialFeedConstants.FlowSlug}:request:{CurrentTenant.TenantId}:{idempotencyKey}";

        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<FriendRequestSocialFeedOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new FriendRequestSocialFeedOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["requestId"] = requestId,
                ["senderUserId"] = senderUserId,
                ["recipientUserId"] = recipientUserId,
                ["code"] = "TIMEOUT",
                ["retryAfterMs"] = 5000
            };
            await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.TimeoutRetainedEvent, requestId, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timed out", cancellationToken).ConfigureAwait(false);
            return Success(new FriendRequestSocialFeedOutcome(504, timeoutPayload, false, true));
        }

        var concurrencyKey = DirectionIndependentConnectionId(senderUserId, recipientUserId);
        if (!InFlightRequests.TryAdd(concurrencyKey, 0))
        {
            return Failure<FriendRequestSocialFeedOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent friend request mutation detected",
                new Dictionary<string, object?> { ["currentVersion"] = ifMatch ?? "unknown", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(80, cancellationToken).ConfigureAwait(false);
            }

            var privacy = await CheckPrivacyInlineAsync(recipientUserId, senderUserId, "friend_request", request, cancellationToken).ConfigureAwait(false);
            if (privacy.IsFailure)
            {
                return Failure<FriendRequestSocialFeedOutcome>(privacy.Error!.Code, privacy.Error.Message, privacy.Error.Details);
            }

            if (privacy.Data is false)
            {
                var blocked = new Dictionary<string, object?>
                {
                    ["requestId"] = requestId,
                    ["status"] = "BLOCKED",
                    ["reason"] = "request_could_not_be_sent"
                };
                await _idempotency.MarkSucceededAsync(operationKey, blocked, cancellationToken).ConfigureAwait(false);
                return Success(new FriendRequestSocialFeedOutcome(200, blocked, false, false));
            }

            var senderRequests = await _documents.SearchAsync(
                FriendRequestSocialFeedConstants.FriendRequests,
                new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["sender_user_id"] = senderUserId },
                cancellationToken).ConfigureAwait(false);
            if ((senderRequests.Data?.Count ?? 0) >= 20)
            {
                return Failure<FriendRequestSocialFeedOutcome>(
                    "RATE_LIMITED",
                    "Friend request daily limit reached",
                    new Dictionary<string, object?> { ["limit"] = 20 });
            }

            var existing = await FindRequestAsync(senderUserId, recipientUserId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                var existingBody = RequestResponse(existing, 200, existingRecord: true);
                await _idempotency.MarkSucceededAsync(operationKey, existingBody, cancellationToken).ConfigureAwait(false);
                return Success(new FriendRequestSocialFeedOutcome(200, existingBody, true, true));
            }
            var mutual = await FindRequestAsync(recipientUserId, senderUserId, cancellationToken).ConfigureAwait(false);
            if (mutual is not null && string.Equals(ReadString(mutual.Body, "status"), "PENDING", StringComparison.Ordinal))
            {
                var accepted = await AcceptCoreAsync(
                    mutual,
                    recipientUserId,
                    requestId,
                    ReadDouble(request, "baseMatchScore") ?? 0.65,
                    request,
                    cancellationToken).ConfigureAwait(false);
                if (accepted.IsSuccess)
                {
                    var acceptedResponseBody = new Dictionary<string, object?>(accepted.Data!.Body)
                    {
                        ["status"] = "AUTO_ACCEPTED"
                    };
                    await _idempotency.MarkSucceededAsync(operationKey, acceptedResponseBody, cancellationToken).ConfigureAwait(false);
                    return Success(new FriendRequestSocialFeedOutcome(accepted.Data.HttpStatus, acceptedResponseBody, accepted.Data.StateStored, accepted.Data.OutboxRetained));
                }

                return accepted;
            }

            var sentAt = DateTimeOffset.UtcNow;
            var body = new Dictionary<string, object?>
            {
                ["tenant_id"] = CurrentTenant.TenantId,
                ["request_id"] = requestId,
                ["connection_id"] = concurrencyKey,
                ["sender_user_id"] = senderUserId,
                ["recipient_user_id"] = recipientUserId,
                ["message"] = ReadString(request, "message"),
                ["status"] = "PENDING",
                ["base_match"] = Clamp(ReadDouble(request, "baseMatchScore") ?? 0.25),
                ["source"] = ReadString(request, "source") ?? "direct",
                ["sent_at"] = sentAt,
                ["knowledge_scope"] = "PRIVATE"
            };

            var stored = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FriendRequests, requestId, body, ifMatch, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure)
            {
                return Failure<FriendRequestSocialFeedOutcome>(stored.Error?.Code ?? "STORE_FAILED", stored.Error?.Message ?? "Failed to store friend request");
            }

            var response = RequestResponse(stored.Data!, 201);
            await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.RequestRequestedEvent, requestId, response), cancellationToken).ConfigureAwait(false);
            await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.RequestSentEvent, requestId, response), cancellationToken).ConfigureAwait(false);
            await StoreNotificationAsync(requestId, recipientUserId, "friend_request", cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
            return Success(new FriendRequestSocialFeedOutcome(201, response, true, true));
        }
        finally
        {
            InFlightRequests.TryRemove(concurrencyKey, out _);
        }
    }

    public async Task<DataProcessResult<FriendRequestSocialFeedOutcome>> RespondToFriendRequestAsync(
        string requestId,
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<FriendRequestSocialFeedOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        if (IsMissing(requestId) || IsMissing(ReadString(request, "actorUserId")))
        {
            return ValidationFailure("requestId", "requestId and actorUserId are required");
        }

        var operationKey = $"{FriendRequestSocialFeedConstants.FlowSlug}:respond:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new FriendRequestSocialFeedOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var storedRequest = await _documents.GetAsync(FriendRequestSocialFeedConstants.FriendRequests, requestId.Trim(), cancellationToken).ConfigureAwait(false);
        if (storedRequest.IsFailure || storedRequest.Data is null)
        {
            return Failure<FriendRequestSocialFeedOutcome>("NOT_FOUND", "Friend request was not found");
        }

        var decision = (ReadString(request, "decision") ?? "ACCEPT").Trim().ToUpperInvariant();
        if (decision is not "ACCEPT" and not "DECLINE")
        {
            return ValidationFailure("decision", "decision must be ACCEPT or DECLINE");
        }

        var concurrencyKey = $"respond:{CurrentTenant.TenantId}:{requestId.Trim()}";
        if (!InFlightResponses.TryAdd(concurrencyKey, 0))
        {
            return Failure<FriendRequestSocialFeedOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent friend request response detected",
                new Dictionary<string, object?> { ["currentVersion"] = storedRequest.Data.Version, ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(80, cancellationToken).ConfigureAwait(false);
            }

            if (ReadBool(request, "simulateFabricTimeout"))
            {
                var timeoutPayload = new Dictionary<string, object?>
                {
                    ["requestId"] = requestId.Trim(),
                    ["code"] = "TIMEOUT",
                    ["retryAfterMs"] = 5000
                };
                await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.TimeoutRetainedEvent, requestId.Trim(), timeoutPayload), cancellationToken).ConfigureAwait(false);
                return Success(new FriendRequestSocialFeedOutcome(504, timeoutPayload, false, true));
            }

            if (decision == "DECLINE")
            {
                var rejected = new Dictionary<string, object?>(storedRequest.Data.Body)
                {
                    ["status"] = "DECLINED",
                    ["responded_by"] = ReadString(request, "actorUserId")!.Trim(),
                    ["responded_at"] = DateTimeOffset.UtcNow
                };
                var updated = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FriendRequests, requestId.Trim(), rejected, ifMatch, cancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return Failure<FriendRequestSocialFeedOutcome>(updated.Error?.Code ?? "STORE_FAILED", updated.Error?.Message ?? "Failed to decline request");
                }

                var response = RequestResponse(updated.Data!, 200);
                await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.RequestDeclinedEvent, requestId.Trim(), response), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
                return Success(new FriendRequestSocialFeedOutcome(200, response, true, true));
            }

            var accepted = await AcceptCoreAsync(
                storedRequest.Data,
                ReadString(request, "actorUserId")!.Trim(),
                requestId.Trim(),
                ReadDouble(request, "baseMatchScore") ?? ReadDouble(storedRequest.Data.Body, "base_match") ?? 0.25,
                request,
                cancellationToken).ConfigureAwait(false);
            if (accepted.IsSuccess)
            {
                await _idempotency.MarkSucceededAsync(operationKey, accepted.Data!.Body, cancellationToken).ConfigureAwait(false);
            }

            return accepted;
        }
        finally
        {
            InFlightResponses.TryRemove(concurrencyKey, out _);
        }
    }
    public async Task<DataProcessResult<FriendRequestSocialFeedFeed>> GetFeedAsync(
        string connectionId,
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        if (IsMissing(connectionId) || IsMissing(viewerUserId))
        {
            return DataProcessResult<FriendRequestSocialFeedFeed>.Failure("VALIDATION_FAILED", "connectionId and viewerUserId are required");
        }

        var items = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.FeedItems,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["connection_id"] = connectionId.Trim(), ["viewer_user_id"] = viewerUserId.Trim() },
            cancellationToken).ConfigureAwait(false);
        if (items.IsFailure)
        {
            return DataProcessResult<FriendRequestSocialFeedFeed>.Failure(items.Error?.Code ?? "STORE_FAILED", items.Error?.Message ?? "Feed lookup failed");
        }

        var list = (items.Data ?? [])
            .OrderBy(record => ReadDouble(record.Body, "position") ?? 999)
            .Select(record => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["feedItemId"] = record.Id,
                ["connectionId"] = ReadString(record.Body, "connection_id"),
                ["viewerUserId"] = ReadString(record.Body, "viewer_user_id"),
                ["sourceUserId"] = ReadString(record.Body, "source_user_id"),
                ["title"] = ReadString(record.Body, "title"),
                ["score"] = ReadDouble(record.Body, "score") ?? 0,
                ["zone"] = ReadString(record.Body, "zone"),
                ["version"] = record.Version
            })
            .ToList();

        var strength = list.Count == 0 ? 0 : list.Max(item => ReadDouble(item, "score") ?? 0);
        return DataProcessResult<FriendRequestSocialFeedFeed>.Success(new FriendRequestSocialFeedFeed(connectionId.Trim(), viewerUserId.Trim(), strength, list));
    }

    public async Task<DataProcessResult<FriendRequestSocialFeedAnalytics>> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var connections = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.Connections,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId },
            cancellationToken).ConfigureAwait(false);
        var requests = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.FriendRequests,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["status"] = "PENDING" },
            cancellationToken).ConfigureAwait(false);

        var strengths = (connections.Data ?? [])
            .Select(record => ReadDouble(record.Body, "connection_strength") ?? 0)
            .ToList();
        var uniqueConnectionCount = (connections.Data ?? [])
            .Select(record => ReadString(record.Body, "connection_id"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Count();
        var buckets = new Dictionary<string, int>
        {
            ["strong"] = strengths.Count(value => value >= 0.8),
            ["medium"] = strengths.Count(value => value >= 0.5 && value < 0.8),
            ["weak"] = strengths.Count(value => value >= 0.2 && value < 0.5),
            ["dormant"] = strengths.Count(value => value < 0.2)
        };

        var analyticsId = $"analytics-{CurrentTenant.TenantId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["connection_count"] = uniqueConnectionCount,
            ["pending_request_count"] = requests.Data?.Count ?? 0,
            ["average_strength"] = strengths.Count == 0 ? 0 : strengths.Average(),
            ["buckets"] = buckets,
            ["knowledge_scope"] = "GLOBAL"
        };
        await _documents.UpsertAsync(FriendRequestSocialFeedConstants.Analytics, analyticsId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.AnalyticsRecordedEvent, analyticsId, body), cancellationToken).ConfigureAwait(false);

        return DataProcessResult<FriendRequestSocialFeedAnalytics>.Success(new FriendRequestSocialFeedAnalytics(uniqueConnectionCount, requests.Data?.Count ?? 0, strengths.Count == 0 ? 0 : strengths.Average(), buckets));
    }

    private async Task<DataProcessResult<FriendRequestSocialFeedOutcome>> AcceptCoreAsync(
        DocumentRecord requestRecord,
        string actorUserId,
        string requestId,
        double baseMatchScore,
        IReadOnlyDictionary<string, object?> options,
        CancellationToken cancellationToken)
    {
        var senderUserId = ReadString(requestRecord.Body, "sender_user_id") ?? string.Empty;
        var recipientUserId = ReadString(requestRecord.Body, "recipient_user_id") ?? string.Empty;
        var connectionId = ReadString(requestRecord.Body, "connection_id") ?? DirectionIndependentConnectionId(senderUserId, recipientUserId);
        var establishedAt = DateTimeOffset.UtcNow;
        var finalStrength = CalculateConnectionStrength(baseMatchScore, options);

        var existing = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.Connections,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["connection_id"] = connectionId },
            cancellationToken).ConfigureAwait(false);
        if ((existing.Data?.Count ?? 0) >= 2)
        {
            var existingResponse = new Dictionary<string, object?>
            {
                ["requestId"] = requestId,
                ["connectionId"] = connectionId,
                ["senderUserId"] = senderUserId,
                ["recipientUserId"] = recipientUserId,
                ["status"] = "ACCEPTED",
                ["connectionStrength"] = finalStrength,
                ["existingRecord"] = true
            };
            return Success(new FriendRequestSocialFeedOutcome(200, existingResponse, true, true));
        }

        var acceptedBody = new Dictionary<string, object?>(requestRecord.Body)
        {
            ["status"] = "ACCEPTED",
            ["responded_by"] = actorUserId,
            ["accepted_at"] = establishedAt,
            ["connection_strength"] = finalStrength
        };
        var requestUpdate = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FriendRequests, requestRecord.Id, acceptedBody, null, cancellationToken).ConfigureAwait(false);
        if (requestUpdate.IsFailure)
        {
            return Failure<FriendRequestSocialFeedOutcome>(requestUpdate.Error?.Code ?? "STORE_FAILED", requestUpdate.Error?.Message ?? "Failed to accept request");
        }

        var sortedIds = SortPair(senderUserId, recipientUserId);
        var edgeA = ConnectionBody(connectionId, sortedIds[0], sortedIds[1], requestId, finalStrength, establishedAt);
        var edgeB = ConnectionBody(connectionId, sortedIds[1], sortedIds[0], requestId, finalStrength, establishedAt);
        var storeA = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.Connections, $"{connectionId}:{sortedIds[0]}:{sortedIds[1]}", edgeA, null, cancellationToken).ConfigureAwait(false);
        var storeB = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.Connections, $"{connectionId}:{sortedIds[1]}:{sortedIds[0]}", edgeB, null, cancellationToken).ConfigureAwait(false);
        if (storeA.IsFailure || storeB.IsFailure)
        {
            return Failure<FriendRequestSocialFeedOutcome>("STORE_FAILED", "Failed to store both bidirectional connection edges");
        }

        var response = new Dictionary<string, object?>
        {
            ["requestId"] = requestId,
            ["connectionId"] = connectionId,
            ["senderUserId"] = senderUserId,
            ["recipientUserId"] = recipientUserId,
            ["status"] = "ACCEPTED",
            ["connectionStrength"] = finalStrength,
            ["postLimit"] = PostLimit(finalStrength),
            ["zone"] = FeedZone(finalStrength),
            ["version"] = requestUpdate.Data!.Version
        };
        await PopulateHistoricalFeedAsync(connectionId, senderUserId, recipientUserId, finalStrength, options, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.RequestAcceptedEvent, requestId, response), cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.ConnectionEstablishedEvent, connectionId, response), cancellationToken).ConfigureAwait(false);
        await StoreNotificationAsync(requestId, senderUserId, "friend_request_accepted", cancellationToken).ConfigureAwait(false);
        return Success(new FriendRequestSocialFeedOutcome(201, response, true, true));
    }

    private async Task PopulateHistoricalFeedAsync(
        string connectionId,
        string userA,
        string userB,
        double strength,
        IReadOnlyDictionary<string, object?> options,
        CancellationToken cancellationToken)
    {
        var limit = PostLimit(strength);
        var zone = FeedZone(strength);
        var users = new[] { userA, userB };
        foreach (var viewer in users)
        {
            var source = viewer == userA ? userB : userA;
            for (var i = 0; i < limit; i++)
            {
                var score = ReadBool(options, "forceZeroScore") && i == 0 ? 0 : Clamp(strength - (i * 0.01));
                var feedItemId = $"feed-{connectionId}-{viewer}-{i + 1:00}";
                var item = new Dictionary<string, object?>
                {
                    ["tenant_id"] = CurrentTenant.TenantId,
                    ["feed_item_id"] = feedItemId,
                    ["connection_id"] = connectionId,
                    ["viewer_user_id"] = viewer,
                    ["source_user_id"] = source,
                    ["title"] = $"Historical post {i + 1} from {source}",
                    ["score"] = score,
                    ["position"] = i + 1,
                    ["zone"] = zone,
                    ["knowledge_scope"] = "PRIVATE"
                };
                var stored = await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FeedItems, feedItemId, item, null, cancellationToken).ConfigureAwait(false);
                if (stored.IsSuccess)
                {
                    await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.FeedItemGeneratedEvent, feedItemId, item), cancellationToken).ConfigureAwait(false);
                    await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FeedScores, $"score-{feedItemId}", item, null, cancellationToken).ConfigureAwait(false);
                    await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.FeedItemScoredEvent, feedItemId, item), cancellationToken).ConfigureAwait(false);
                    var allowed = await CheckPrivacyInlineAsync(viewer, source, "feed_delivery", options, cancellationToken).ConfigureAwait(false);
                    if (allowed.IsSuccess && allowed.Data)
                    {
                        var delivery = new Dictionary<string, object?>(item)
                        {
                            ["delivered_at"] = DateTimeOffset.UtcNow
                        };
                        await _documents.UpsertAsync(FriendRequestSocialFeedConstants.FeedDeliveries, $"delivery-{feedItemId}", delivery, null, cancellationToken).ConfigureAwait(false);
                        await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.FeedDeliveredEvent, feedItemId, delivery), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    private async Task<DataProcessResult<bool>> CheckPrivacyInlineAsync(
        string targetUserId,
        string requesterUserId,
        string action,
        IReadOnlyDictionary<string, object?> request,
        CancellationToken cancellationToken)
    {
        if (ReadBool(request, "recipientPrivacyBlocked") || ReadBool(request, "senderBlocked"))
        {
            return DataProcessResult<bool>.Success(false);
        }

        var rules = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.PrivacyRules,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["target_user_id"] = targetUserId, ["requester_user_id"] = requesterUserId, ["action"] = action },
            cancellationToken).ConfigureAwait(false);
        if (rules.IsFailure)
        {
            return DataProcessResult<bool>.Failure(rules.Error?.Code ?? "PRIVACY_CHECK_FAILED", rules.Error?.Message ?? "Privacy lookup failed");
        }

        var blocked = (rules.Data ?? []).Any(rule => string.Equals(ReadString(rule.Body, "effect"), "BLOCK", StringComparison.Ordinal));
        return DataProcessResult<bool>.Success(!blocked);
    }

    private async Task StoreNotificationAsync(string subjectId, string userId, string kind, CancellationToken cancellationToken)
    {
        var notificationId = $"notification-{subjectId}-{userId}-{kind}";
        var body = new Dictionary<string, object?>
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["notification_id"] = notificationId,
            ["subject_id"] = subjectId,
            ["user_id"] = userId,
            ["kind"] = kind,
            ["created_at"] = DateTimeOffset.UtcNow,
            ["knowledge_scope"] = "PRIVATE"
        };
        await _documents.UpsertAsync(FriendRequestSocialFeedConstants.Notifications, notificationId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(FriendRequestSocialFeedConstants.NotificationSentEvent, notificationId, body), cancellationToken).ConfigureAwait(false);
    }

    private async Task<DocumentRecord?> FindRequestAsync(string senderUserId, string recipientUserId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            FriendRequestSocialFeedConstants.FriendRequests,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["sender_user_id"] = senderUserId, ["recipient_user_id"] = recipientUserId },
            cancellationToken).ConfigureAwait(false);
        return result.Data?.FirstOrDefault();
    }

    private Dictionary<string, object?> ConnectionBody(string connectionId, string fromUserId, string toUserId, string requestId, double strength, DateTimeOffset establishedAt) =>
        new()
        {
            ["tenant_id"] = CurrentTenant.TenantId,
            ["connection_id"] = connectionId,
            ["from_user_id"] = fromUserId,
            ["to_user_id"] = toUserId,
            ["request_id"] = requestId,
            ["connection_strength"] = strength,
            ["established_at"] = establishedAt,
            ["knowledge_scope"] = "PRIVATE"
        };
    private DataProcessResult<FriendRequestSocialFeedOutcome>? ValidateFriendRequest(IReadOnlyDictionary<string, object?> request)
    {
        var sender = ReadString(request, "senderUserId");
        var recipient = ReadString(request, "recipientUserId");
        if (IsMissing(sender))
        {
            return ValidationFailure("senderUserId", "senderUserId is required");
        }

        if (IsMissing(recipient))
        {
            return ValidationFailure("recipientUserId", "recipientUserId is required");
        }

        if (string.Equals(sender!.Trim(), recipient!.Trim(), StringComparison.Ordinal))
        {
            return ValidationFailure("recipientUserId", "sender and recipient must be different users");
        }

        return null;
    }

    private static DataProcessResult<FriendRequestSocialFeedOutcome> ValidationFailure(string field, string reason) =>
        DataProcessResult<FriendRequestSocialFeedOutcome>.Success(new FriendRequestSocialFeedOutcome(
            400,
            new Dictionary<string, object?>
            {
                ["code"] = "VALIDATION_FAILED",
                ["details"] = new[] { new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason } }
            },
            false,
            false));

    private static Dictionary<string, object?> RequestResponse(DocumentRecord record, int statusCode, bool existingRecord = false)
    {
        var body = new Dictionary<string, object?>
        {
            ["id"] = record.Id,
            ["requestId"] = ReadString(record.Body, "request_id"),
            ["connectionId"] = ReadString(record.Body, "connection_id"),
            ["senderUserId"] = ReadString(record.Body, "sender_user_id"),
            ["recipientUserId"] = ReadString(record.Body, "recipient_user_id"),
            ["status"] = ReadString(record.Body, "status"),
            ["version"] = record.Version,
            ["httpStatus"] = statusCode
        };
        if (existingRecord)
        {
            body["existingRecord"] = true;
        }

        return body;
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{FriendRequestSocialFeedConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = FriendRequestSocialFeedConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static double CalculateConnectionStrength(double baseMatchScore, IReadOnlyDictionary<string, object?> values)
    {
        var group = ReadDouble(values, "groupWeight") ?? 0.5;
        var events = ReadDouble(values, "eventWeight") ?? 0.5;
        var purchase = ReadDouble(values, "purchaseWeight") ?? 0.5;
        var questionnaire = ReadDouble(values, "questionnaireWeight") ?? 0.5;
        var ml = Math.Min(0.2, Math.Max(-0.2, ReadDouble(values, "mlAdjustment") ?? 0));
        return Clamp((Clamp(baseMatchScore) * 0.25) + (Clamp(group) * 0.20) + (Clamp(events) * 0.20) + (Clamp(purchase) * 0.15) + (Clamp(questionnaire) * 0.20) + ml);
    }

    private static int PostLimit(double strength) => strength switch
    {
        > 0.8 => 20,
        >= 0.5 => 10,
        _ => 5
    };

    private static string FeedZone(double strength) => strength switch
    {
        > 0.8 => "top-20-percent",
        >= 0.5 => "middle-40-percent",
        _ => "bottom-40-percent"
    };

    private string DirectionIndependentConnectionId(string userA, string userB)
    {
        var sorted = SortPair(userA, userB);
        return $"conn-{CurrentTenant.TenantId}-{sorted[0]}-{sorted[1]}";
    }

    private static string[] SortPair(string userA, string userB) =>
        string.CompareOrdinal(userA, userB) <= 0 ? [userA, userB] : [userB, userA];

    private static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "0", StringComparison.Ordinal);

    private static double Clamp(double value) => Math.Min(1, Math.Max(0, value));

    private static string? ReadString(IReadOnlyDictionary<string, object?>? values, string key)
    {
        if (values is null || !values.TryGetValue(key, out var value) || value is null)
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

    private static double? ReadDouble(IReadOnlyDictionary<string, object?>? values, string key)
    {
        if (values is null || !values.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int integer => integer,
            long number => number,
            double floating => floating,
            decimal money => (double)money,
            string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
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
