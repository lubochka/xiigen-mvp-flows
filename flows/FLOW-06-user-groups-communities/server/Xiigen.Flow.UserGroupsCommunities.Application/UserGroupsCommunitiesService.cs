using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserGroupsCommunities.Application;

public sealed class UserGroupsCommunitiesService : MicroserviceBase<UserGroupsCommunitiesService>
{
    private static readonly ConcurrentDictionary<string, byte> InFlightMemberships = new(StringComparer.Ordinal);

    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFreedomConfigService _freedom;

    public UserGroupsCommunitiesService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFreedomConfigService freedom,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<UserGroupsCommunitiesService> logger)
        : base(
            new ServiceDescriptor("FLOW-06", nameof(UserGroupsCommunitiesService), UserGroupsCommunitiesConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _freedom = freedom;
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesOutcome>> SubmitMembershipAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<UserGroupsCommunitiesOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var validation = ValidateMembershipRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var groupId = ReadString(request, "groupId")!.Trim();
        var userId = ReadString(request, "userId")!.Trim();
        var action = (ReadString(request, "action") ?? "JOIN").Trim().ToUpperInvariant();
        var groupType = (ReadString(request, "groupType") ?? "OPEN").Trim().ToUpperInvariant();
        var operationKey = $"{UserGroupsCommunitiesConstants.FlowSlug}:membership:{CurrentTenant.TenantId}:{idempotencyKey}";

        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<UserGroupsCommunitiesOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new UserGroupsCommunitiesOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutPayload = new Dictionary<string, object?>
            {
                ["groupId"] = groupId,
                ["userId"] = userId,
                ["retryAfterMs"] = 5000
            };
            var retained = await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.TimeoutRetainedEvent, groupId, timeoutPayload), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (retained.IsFailure)
            {
                return Failure<UserGroupsCommunitiesOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<UserGroupsCommunitiesOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        var resourceKey = $"{CurrentTenant.TenantId}:{groupId}:{userId}";
        if (!InFlightMemberships.TryAdd(resourceKey, 1))
        {
            await _idempotency.MarkFailedAsync(operationKey, "CONCURRENT_UPDATE", "Concurrent membership mutation detected", cancellationToken).ConfigureAwait(false);
            return Failure<UserGroupsCommunitiesOutcome>(
                "CONCURRENT_UPDATE",
                "Concurrent membership mutation detected",
                new Dictionary<string, object?> { ["currentVersion"] = "pending", ["reason"] = "concurrent_update" });
        }

        try
        {
            if (ReadBool(request, "simulateSlowWrite"))
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var existing = await FindMembershipAsync(groupId, userId, cancellationToken).ConfigureAwait(false);
            if (action == "JOIN" && existing is not null && IsActive(existing))
            {
                var existingResponse = MembershipResponse(existing);
                existingResponse["existingRecord"] = true;
                await _idempotency.MarkSucceededAsync(operationKey, existingResponse, cancellationToken).ConfigureAwait(false);
                return Success(new UserGroupsCommunitiesOutcome(200, existingResponse, true, false));
            }

            if (action == "LEAVE")
            {
                var leaveResult = await LeaveMembershipAsync(groupId, userId, existing, operationKey, cancellationToken).ConfigureAwait(false);
                return leaveResult;
            }

            await EnsureGroupAsync(groupId, groupType, cancellationToken).ConfigureAwait(false);
            var isInviteOnly = string.Equals(groupType, "INVITE_ONLY", StringComparison.Ordinal);
            var invitationId = ReadString(request, "invitationId");
            if (isInviteOnly && string.IsNullOrWhiteSpace(invitationId))
            {
                var joinRequest = await StoreJoinRequestAsync(groupId, userId, "PENDING_APPROVAL", cancellationToken).ConfigureAwait(false);
                var response = new Dictionary<string, object?>
                {
                    ["requestId"] = ReadString(joinRequest, "request_id"),
                    ["groupId"] = groupId,
                    ["userId"] = userId,
                    ["status"] = "PENDING_APPROVAL",
                    ["tier"] = "pending",
                    ["feedAvailable"] = false,
                    ["existingRecord"] = false
                };
                await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.GroupJoinRequestedEvent, groupId, response), cancellationToken).ConfigureAwait(false);
                await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
                return Success(new UserGroupsCommunitiesOutcome(201, response, true, true));
            }

            var tier = await ResolveMembershipTierAsync(userId, groupId, cancellationToken).ConfigureAwait(false);
            var membership = await StoreMembershipAsync(groupId, userId, tier, "ACTIVE", ifMatch, cancellationToken).ConfigureAwait(false);
            if (membership.IsFailure)
            {
                await _idempotency.MarkFailedAsync(operationKey, membership.Error?.Code ?? "MEMBERSHIP_STORE_FAILED", membership.Error?.Message ?? "Failed to store membership", cancellationToken).ConfigureAwait(false);
                return Failure<UserGroupsCommunitiesOutcome>(
                    membership.Error?.Code ?? "MEMBERSHIP_STORE_FAILED",
                    membership.Error?.Message ?? "Failed to store membership",
                    membership.Error?.Details);
            }

            var responseBody = MembershipResponse(membership.Data!);
            await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.MembershipAcceptedEvent, groupId, responseBody), cancellationToken).ConfigureAwait(false);
            await PopulateFeedAsync(groupId, userId, DefaultContentItems(), cancellationToken).ConfigureAwait(false);
            await UpdateAnalyticsAsync(groupId, userId, "membership_activated", cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, responseBody, cancellationToken).ConfigureAwait(false);
            return Success(new UserGroupsCommunitiesOutcome(201, responseBody, true, true));
        }
        finally
        {
            InFlightMemberships.TryRemove(resourceKey, out _);
        }
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesDiscovery>> DiscoverGroupsAsync(
        string? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureGroupAsync("community-open-builders", "OPEN", cancellationToken).ConfigureAwait(false);
        await EnsureGroupAsync("community-invite-founders", "INVITE_ONLY", cancellationToken).ConfigureAwait(false);

        var result = await _documents.SearchAsync(
            UserGroupsCommunitiesConstants.Groups,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId },
            cancellationToken).ConfigureAwait(false);

        var visible = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var record in result.Data ?? [])
        {
            if (await CanViewGroupAsync(record, viewerUserId, cancellationToken).ConfigureAwait(false))
            {
                visible.Add(GroupResponse(record));
            }
        }

        return Success(new UserGroupsCommunitiesDiscovery(
            visible,
            visible.Where(group => string.Equals(ReadString(group, "groupType"), "OPEN", StringComparison.Ordinal)).Take(3).ToArray(),
            CurrentTenant.Roles.FirstOrDefault() ?? "anonymous"));
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesMembershipStatus>> GetMembershipStatusAsync(
        string groupId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId))
        {
            return Failure<UserGroupsCommunitiesMembershipStatus>("VALIDATION_FAILED", "groupId and userId are required");
        }

        var membership = await FindMembershipAsync(groupId.Trim(), userId.Trim(), cancellationToken).ConfigureAwait(false);
        var pending = await CountPendingApprovalsAsync(groupId.Trim(), cancellationToken).ConfigureAwait(false);
        if (membership is null)
        {
            return Success(new UserGroupsCommunitiesMembershipStatus(groupId.Trim(), userId.Trim(), "NOT_MEMBER", "none", false, false, pending, string.Empty));
        }

        var status = ReadString(membership.Body, "status") ?? "UNKNOWN";
        var tier = ReadString(membership.Body, "tier") ?? "member";
        return Success(new UserGroupsCommunitiesMembershipStatus(
            groupId.Trim(),
            userId.Trim(),
            status,
            tier,
            string.Equals(status, "ACTIVE", StringComparison.Ordinal),
            string.Equals(status, "ACTIVE", StringComparison.Ordinal),
            pending,
            membership.Version));
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesOutcome>> ReviewJoinRequestAsync(
        string groupId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<UserGroupsCommunitiesOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var userId = ReadString(request, "userId");
        var decision = (ReadString(request, "decision") ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId) || decision is not ("APPROVE" or "REJECT"))
        {
            return Failure<UserGroupsCommunitiesOutcome>("VALIDATION_FAILED", "Approval requires groupId, userId, and APPROVE or REJECT decision");
        }

        var operationKey = $"{UserGroupsCommunitiesConstants.FlowSlug}:approval:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new UserGroupsCommunitiesOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var requestBody = await StoreJoinRequestAsync(groupId.Trim(), userId.Trim(), decision == "APPROVE" ? "APPROVED" : "REJECTED", cancellationToken).ConfigureAwait(false);
        if (decision == "REJECT")
        {
            var rejected = new Dictionary<string, object?>(requestBody)
            {
                ["status"] = "REJECTED",
                ["feedAvailable"] = false
            };
            await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.MembershipRejectedEvent, groupId.Trim(), rejected), cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkSucceededAsync(operationKey, rejected, cancellationToken).ConfigureAwait(false);
            return Success(new UserGroupsCommunitiesOutcome(201, rejected, true, true));
        }

        var tier = await ResolveMembershipTierAsync(userId.Trim(), groupId.Trim(), cancellationToken).ConfigureAwait(false);
        var stored = await StoreMembershipAsync(groupId.Trim(), userId.Trim(), tier, "ACTIVE", null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            await _idempotency.MarkFailedAsync(operationKey, stored.Error?.Code ?? "APPROVAL_FAILED", stored.Error?.Message ?? "Approval failed", cancellationToken).ConfigureAwait(false);
            return Failure<UserGroupsCommunitiesOutcome>(stored.Error?.Code ?? "APPROVAL_FAILED", stored.Error?.Message ?? "Approval failed", stored.Error?.Details);
        }

        var response = MembershipResponse(stored.Data!);
        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.MembershipAcceptedEvent, groupId.Trim(), response), cancellationToken).ConfigureAwait(false);
        await PopulateFeedAsync(groupId.Trim(), userId.Trim(), DefaultContentItems(), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new UserGroupsCommunitiesOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesOutcome>> UpdateTierAsync(
        string groupId,
        IReadOnlyDictionary<string, object?> request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<UserGroupsCommunitiesOutcome>("BAD_REQUEST", "X-Idempotency-Key header is required");
        }

        var targetUserId = ReadString(request, "targetUserId");
        var requestedByUserId = ReadString(request, "requestedByUserId");
        var targetTier = NormalizeTier(ReadString(request, "targetTier"));
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(targetUserId) || string.IsNullOrWhiteSpace(requestedByUserId) || targetTier is null)
        {
            return Failure<UserGroupsCommunitiesOutcome>("VALIDATION_FAILED", "Tier update requires groupId, targetUserId, requestedByUserId, and targetTier");
        }

        if (string.Equals(targetUserId.Trim(), requestedByUserId.Trim(), StringComparison.Ordinal))
        {
            return Failure<UserGroupsCommunitiesOutcome>("TIER_CHANGE_REJECTED", "Members cannot promote themselves");
        }

        var requester = await FindMembershipAsync(groupId.Trim(), requestedByUserId.Trim(), cancellationToken).ConfigureAwait(false);
        if (targetTier == "admin" && !string.Equals(ReadString(requester?.Body, "tier"), "admin", StringComparison.Ordinal))
        {
            return Failure<UserGroupsCommunitiesOutcome>("TIER_CHANGE_REJECTED", "Admin tier assignment requires an existing group admin");
        }

        var operationKey = $"{UserGroupsCommunitiesConstants.FlowSlug}:tier:{CurrentTenant.TenantId}:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromHours(24), cancellationToken).ConfigureAwait(false);
        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new UserGroupsCommunitiesOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        var stored = await StoreMembershipAsync(groupId.Trim(), targetUserId.Trim(), targetTier, "ACTIVE", null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<UserGroupsCommunitiesOutcome>(stored.Error?.Code ?? "TIER_STORE_FAILED", stored.Error?.Message ?? "Tier update failed", stored.Error?.Details);
        }

        var response = MembershipResponse(stored.Data!);
        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.TierAssignedEvent, groupId.Trim(), response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new UserGroupsCommunitiesOutcome(201, response, true, true));
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesFeed>> GetFeedAsync(
        string groupId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId))
        {
            return Failure<UserGroupsCommunitiesFeed>("VALIDATION_FAILED", "groupId and userId are required");
        }

        var membership = await FindMembershipAsync(groupId.Trim(), userId.Trim(), cancellationToken).ConfigureAwait(false);
        await LogAccessAsync(groupId.Trim(), userId.Trim(), membership is not null && IsActive(membership), cancellationToken).ConfigureAwait(false);
        if (membership is null || !IsActive(membership))
        {
            return Failure<UserGroupsCommunitiesFeed>("ACCESS_DENIED", "Active membership is required before feed content is available");
        }

        var feed = await PopulateFeedAsync(groupId.Trim(), userId.Trim(), DefaultContentItems(), cancellationToken).ConfigureAwait(false);
        if (feed.IsFailure)
        {
            return Failure<UserGroupsCommunitiesFeed>(feed.Error?.Code ?? "FEED_FAILED", feed.Error?.Message ?? "Feed population failed", feed.Error?.Details);
        }

        var tier = ReadString(membership.Body, "tier") ?? "member";
        var items = (feed.Data?.Body.TryGetValue("items", out var rawItems) == true && rawItems is IReadOnlyList<IReadOnlyDictionary<string, object?>> typedItems)
            ? typedItems
            : [];
        return Success(new UserGroupsCommunitiesFeed(groupId.Trim(), userId.Trim(), tier, false, items));
    }

    public async Task<DataProcessResult<UserGroupsCommunitiesOutcome>> PopulateFeedAsync(
        string groupId,
        string userId,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> contentItems,
        CancellationToken cancellationToken = default)
    {
        if (contentItems.Count == 0)
        {
            return Success(new UserGroupsCommunitiesOutcome(
                200,
                new Dictionary<string, object?>
                {
                    ["groupId"] = groupId,
                    ["userId"] = userId,
                    ["status"] = "SKIPPED_EMPTY_CONTENT",
                    ["items"] = Array.Empty<IReadOnlyDictionary<string, object?>>()
                },
                false,
                false));
        }

        var weights = await GetFeedWeightsAsync(cancellationToken).ConfigureAwait(false);
        var items = contentItems
            .Select((item, index) =>
            {
                var engagement = Clamp(ReadDouble(item, "engagementScore").GetValueOrDefault(0.5));
                var freshness = Clamp(ReadDouble(item, "freshnessScore").GetValueOrDefault(0.5));
                var tierFit = Clamp(ReadDouble(item, "tierFitScore").GetValueOrDefault(0.5));
                var score = engagement * weights.Engagement + freshness * weights.Freshness + tierFit * weights.TierFit;
                return new Dictionary<string, object?>
                {
                    ["contentId"] = ReadString(item, "contentId") ?? $"content-{index + 1}",
                    ["title"] = ReadString(item, "title") ?? $"Community update {index + 1}",
                    ["engagementScore"] = engagement,
                    ["freshnessScore"] = freshness,
                    ["tierFitScore"] = tierFit,
                    ["rankingScore"] = Math.Round(score, 3)
                };
            })
            .OrderByDescending(item => item["rankingScore"])
            .Cast<IReadOnlyDictionary<string, object?>>()
            .ToArray();

        var feedId = $"feed-{CurrentTenant.TenantId}-{groupId}-{userId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = feedId,
            ["feed_id"] = feedId,
            ["group_id"] = groupId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["items"] = items,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        var stored = await _documents.UpsertAsync(UserGroupsCommunitiesConstants.Feeds, feedId, body, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<UserGroupsCommunitiesOutcome>(stored.Error?.Code ?? "FEED_STORE_FAILED", stored.Error?.Message ?? "Failed to store feed", stored.Error?.Details);
        }

        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.FeedPopulatedEvent, groupId, body), cancellationToken).ConfigureAwait(false);
        return Success(new UserGroupsCommunitiesOutcome(201, body, true, true));
    }

    private DataProcessResult<UserGroupsCommunitiesOutcome>? ValidateMembershipRequest(IReadOnlyDictionary<string, object?> request)
    {
        var errors = new List<Dictionary<string, object?>>();
        if (string.IsNullOrWhiteSpace(ReadString(request, "groupId")))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "groupId", ["reason"] = "required" });
        }

        if (string.IsNullOrWhiteSpace(ReadString(request, "userId")))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "userId", ["reason"] = "required" });
        }

        var action = (ReadString(request, "action") ?? "JOIN").Trim().ToUpperInvariant();
        if (action is not ("JOIN" or "LEAVE"))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "action", ["reason"] = "must_be_JOIN_or_LEAVE" });
        }

        var groupType = (ReadString(request, "groupType") ?? "OPEN").Trim().ToUpperInvariant();
        if (groupType is not ("OPEN" or "INVITE_ONLY"))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "groupType", ["reason"] = "must_be_OPEN_or_INVITE_ONLY" });
        }

        if (request.ContainsKey("membershipTier"))
        {
            errors.Add(new Dictionary<string, object?> { ["field"] = "membershipTier", ["reason"] = "server_resolved_only" });
        }

        return errors.Count == 0
            ? null
            : Failure<UserGroupsCommunitiesOutcome>("VALIDATION_FAILED", "Membership request failed validation", new Dictionary<string, object?> { ["details"] = errors });
    }

    private async Task<DataProcessResult<UserGroupsCommunitiesOutcome>> LeaveMembershipAsync(
        string groupId,
        string userId,
        DocumentRecord? existing,
        string operationKey,
        CancellationToken cancellationToken)
    {
        var tier = ReadString(existing?.Body, "tier") ?? "member";
        var stored = await StoreMembershipAsync(groupId, userId, tier, "LEFT", existing?.Version, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return Failure<UserGroupsCommunitiesOutcome>(stored.Error?.Code ?? "LEAVE_FAILED", stored.Error?.Message ?? "Failed to leave group", stored.Error?.Details);
        }

        var response = MembershipResponse(stored.Data!);
        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.MembershipLeftEvent, groupId, response), cancellationToken).ConfigureAwait(false);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(new UserGroupsCommunitiesOutcome(201, response, true, true));
    }

    private async Task<DataProcessResult<DocumentRecord>> StoreMembershipAsync(
        string groupId,
        string userId,
        string tier,
        string status,
        string? ifMatch,
        CancellationToken cancellationToken)
    {
        var membershipId = $"membership-{CurrentTenant.TenantId}-{groupId}-{userId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = membershipId,
            ["membership_id"] = membershipId,
            ["group_id"] = groupId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["tier"] = tier,
            ["status"] = status,
            ["active"] = string.Equals(status, "ACTIVE", StringComparison.Ordinal),
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        return await _documents.UpsertAsync(UserGroupsCommunitiesConstants.Memberships, membershipId, body, ifMatch, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<string, object?>> StoreJoinRequestAsync(
        string groupId,
        string userId,
        string status,
        CancellationToken cancellationToken)
    {
        var requestId = $"join-{CurrentTenant.TenantId}-{groupId}-{userId}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = requestId,
            ["request_id"] = requestId,
            ["group_id"] = groupId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["status"] = status,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(UserGroupsCommunitiesConstants.JoinRequests, requestId, body, null, cancellationToken).ConfigureAwait(false);
        return body;
    }

    private async Task EnsureGroupAsync(string groupId, string groupType, CancellationToken cancellationToken)
    {
        var existing = await _documents.SearchAsync(
            UserGroupsCommunitiesConstants.Groups,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["group_id"] = groupId },
            cancellationToken).ConfigureAwait(false);
        if ((existing.Data?.Count ?? 0) > 0)
        {
            return;
        }

        var group = new Dictionary<string, object?>
        {
            ["id"] = $"group-{CurrentTenant.TenantId}-{groupId}",
            ["group_id"] = groupId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["name"] = ToTitle(groupId),
            ["group_type"] = groupType,
            ["visibility"] = string.Equals(groupType, "INVITE_ONLY", StringComparison.Ordinal) ? "invite_only" : "public",
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(UserGroupsCommunitiesConstants.Groups, (string)group["id"]!, group, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> CanViewGroupAsync(DocumentRecord group, string? viewerUserId, CancellationToken cancellationToken)
    {
        if (!string.Equals(ReadString(group.Body, "group_type"), "INVITE_ONLY", StringComparison.Ordinal))
        {
            await LogAccessAsync(ReadString(group.Body, "group_id") ?? group.Id, viewerUserId ?? "anonymous", true, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (string.IsNullOrWhiteSpace(viewerUserId))
        {
            await LogAccessAsync(ReadString(group.Body, "group_id") ?? group.Id, "anonymous", false, cancellationToken).ConfigureAwait(false);
            return false;
        }

        var membership = await FindMembershipAsync(ReadString(group.Body, "group_id") ?? group.Id, viewerUserId.Trim(), cancellationToken).ConfigureAwait(false);
        var allowed = membership is not null && IsActive(membership);
        await LogAccessAsync(ReadString(group.Body, "group_id") ?? group.Id, viewerUserId.Trim(), allowed, cancellationToken).ConfigureAwait(false);
        return allowed;
    }

    private async Task LogAccessAsync(string groupId, string userId, bool allowed, CancellationToken cancellationToken)
    {
        var accessId = $"access-{CurrentTenant.TenantId}-{groupId}-{userId}-{Guid.NewGuid():N}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = accessId,
            ["group_id"] = groupId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["allowed"] = allowed,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["created_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(UserGroupsCommunitiesConstants.AccessLogs, accessId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.AccessCheckInlineEvent, groupId, body), cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateAnalyticsAsync(string groupId, string userId, string metric, CancellationToken cancellationToken)
    {
        var analyticsId = $"analytics-{CurrentTenant.TenantId}-{groupId}-{metric}";
        var body = new Dictionary<string, object?>
        {
            ["id"] = analyticsId,
            ["group_id"] = groupId,
            ["user_id"] = userId,
            ["tenant_id"] = CurrentTenant.TenantId,
            ["metric"] = metric,
            ["knowledge_scope"] = "PRIVATE",
            ["connection_type"] = "FLOW_SCOPED",
            ["updated_at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
        await _documents.UpsertAsync(UserGroupsCommunitiesConstants.Analytics, analyticsId, body, null, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync(CreateEvent(UserGroupsCommunitiesConstants.AnalyticsUpdatedEvent, groupId, body), cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ResolveMembershipTierAsync(string userId, string groupId, CancellationToken cancellationToken)
    {
        var subscription = await _documents.SearchAsync(
            UserGroupsCommunitiesConstants.Subscriptions,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["user_id"] = userId, ["group_id"] = groupId },
            cancellationToken).ConfigureAwait(false);
        var tier = ReadString(subscription.Data?.FirstOrDefault()?.Body, "membership_tier");
        if (!string.IsNullOrWhiteSpace(tier))
        {
            return NormalizeTier(tier) ?? "member";
        }

        var fallback = await _freedom.GetAsync("flow06_default_membership_tier", cancellationToken).ConfigureAwait(false);
        return NormalizeTier(Convert.ToString(fallback.Data?.Value, CultureInfo.InvariantCulture)) ?? "member";
    }

    private async Task<(double Engagement, double Freshness, double TierFit)> GetFeedWeightsAsync(CancellationToken cancellationToken)
    {
        var config = await _freedom.GetAsync("flow06_group_feed_weights", cancellationToken).ConfigureAwait(false);
        if (config.Data?.Value is IReadOnlyDictionary<string, object?> values)
        {
            return (
                ReadDouble(values, "engagement").GetValueOrDefault(0.5),
                ReadDouble(values, "freshness").GetValueOrDefault(0.3),
                ReadDouble(values, "tierFit").GetValueOrDefault(0.2));
        }

        return (0.5, 0.3, 0.2);
    }

    private async Task<int> CountPendingApprovalsAsync(string groupId, CancellationToken cancellationToken)
    {
        var pending = await _documents.SearchAsync(
            UserGroupsCommunitiesConstants.JoinRequests,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["group_id"] = groupId, ["status"] = "PENDING_APPROVAL" },
            cancellationToken).ConfigureAwait(false);
        return pending.Data?.Count ?? 0;
    }

    private async Task<DocumentRecord?> FindMembershipAsync(string groupId, string userId, CancellationToken cancellationToken)
    {
        var result = await _documents.SearchAsync(
            UserGroupsCommunitiesConstants.Memberships,
            new Dictionary<string, object?> { ["tenant_id"] = CurrentTenant.TenantId, ["group_id"] = groupId, ["user_id"] = userId },
            cancellationToken).ConfigureAwait(false);
        return result.Data?.FirstOrDefault();
    }

    private static bool IsActive(DocumentRecord record) =>
        string.Equals(ReadString(record.Body, "status"), "ACTIVE", StringComparison.Ordinal);

    private static Dictionary<string, object?> MembershipResponse(DocumentRecord record) =>
        new()
        {
            ["membershipId"] = record.Id,
            ["groupId"] = ReadString(record.Body, "group_id"),
            ["userId"] = ReadString(record.Body, "user_id"),
            ["status"] = ReadString(record.Body, "status"),
            ["tier"] = ReadString(record.Body, "tier"),
            ["feedAvailable"] = IsActive(record),
            ["version"] = record.Version
        };

    private static Dictionary<string, object?> GroupResponse(DocumentRecord record) =>
        new()
        {
            ["groupId"] = ReadString(record.Body, "group_id"),
            ["name"] = ReadString(record.Body, "name"),
            ["groupType"] = ReadString(record.Body, "group_type"),
            ["visibility"] = ReadString(record.Body, "visibility"),
            ["version"] = record.Version
        };

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            $"/xiigen/flows/{UserGroupsCommunitiesConstants.FlowSlug}",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            new Dictionary<string, object?>(data),
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = UserGroupsCommunitiesConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> DefaultContentItems() =>
    [
        new Dictionary<string, object?> { ["contentId"] = "welcome-thread", ["title"] = "Welcome thread", ["engagementScore"] = 0.8, ["freshnessScore"] = 0.9, ["tierFitScore"] = 0.7 },
        new Dictionary<string, object?> { ["contentId"] = "weekly-brief", ["title"] = "Weekly group brief", ["engagementScore"] = 0.6, ["freshnessScore"] = 0.8, ["tierFitScore"] = 0.9 },
        new Dictionary<string, object?> { ["contentId"] = "mentor-room", ["title"] = "Mentor room", ["engagementScore"] = 0.9, ["freshnessScore"] = 0.5, ["tierFitScore"] = 0.8 }
    ];

    private static string? NormalizeTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
        {
            return null;
        }

        return tier.Trim().ToLowerInvariant() switch
        {
            "member" => "member",
            "premium" => "premium",
            "moderator" => "moderator",
            "admin" => "admin",
            _ => null
        };
    }

    private static double Clamp(double value) => Math.Min(1, Math.Max(0, value));

    private static string ToTitle(string value) =>
        string.Join(' ', value
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => string.Concat(char.ToUpperInvariant(part[0]), part[1..])));

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
