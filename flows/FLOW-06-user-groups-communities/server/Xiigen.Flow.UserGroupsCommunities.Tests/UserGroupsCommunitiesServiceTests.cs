using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.UserGroupsCommunities.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.UserGroupsCommunities.Tests;

public sealed class UserGroupsCommunitiesServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = UserGroupsCommunitiesBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(UserGroupsCommunitiesConstants.RuleIds));
        Assert.Equal(10, UserGroupsCommunitiesBfaRuleRegistry.Rules.Count);
        Assert.All(UserGroupsCommunitiesBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(UserGroupsCommunitiesConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0601_InviteOnlyJoinRequests_DoNotSeedFeedBeforeApproval()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var pending = await fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("founders-circle", "user-pending", groupType: "INVITE_ONLY"),
            null,
            "pending-join");

        Assert.True(pending.IsSuccess, pending.Error?.Message);
        Assert.Equal("PENDING_APPROVAL", pending.Data?.Body["status"]);
        Assert.False((bool)pending.Data!.Body["feedAvailable"]!);

        var feeds = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Feeds, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(feeds.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == UserGroupsCommunitiesConstants.GroupJoinRequestedEvent);
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type == UserGroupsCommunitiesConstants.FeedPopulatedEvent);
    }

    [Fact]
    public async Task CF0602_MembershipTier_IsServerSideAndPayloadTierIsRejected()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.SeedSubscriptionAsync("builders", "user-premium", "premium");

        var accepted = await fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("builders", "user-premium"),
            null,
            "premium-join");
        var rejectedPayload = MembershipRequest("builders", "user-injected");
        rejectedPayload["membershipTier"] = "admin";
        var rejected = await fixture.UserGroupsCommunities.SubmitMembershipAsync(rejectedPayload, null, "tier-injected");

        Assert.True(accepted.IsSuccess, accepted.Error?.Message);
        Assert.Equal("premium", accepted.Data?.Body["tier"]);
        Assert.True(rejected.IsFailure);
        Assert.Equal("VALIDATION_FAILED", rejected.Error?.Code);
    }

    [Fact]
    public async Task CF0603_DuplicateMembershipWithDifferentKey_ReturnsExistingRecordNotFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("builders", "user-existing"), null, "join-a");
        var second = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("builders", "user-existing"), null, "join-b");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(true, second.Data?.Body["existingRecord"]);
        var memberships = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["group_id"] = "builders", ["user_id"] = "user-existing" });
        Assert.Single(memberships.Data ?? []);
    }

    [Fact]
    public async Task CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped()
    {
        var fixture = FlowFixture.Create();

        using (fixture.PushContext("tenant-a", "tenant_user"))
        {
            var created = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("shared-group", "same-user"), null, "tenant-a-join");
            Assert.True(created.IsSuccess, created.Error?.Message);
            var feed = await fixture.UserGroupsCommunities.GetFeedAsync("shared-group", "same-user");
            Assert.True(feed.IsSuccess, feed.Error?.Message);
        }

        using (fixture.PushContext("tenant-b", "tenant_user"))
        {
            var status = await fixture.UserGroupsCommunities.GetMembershipStatusAsync("shared-group", "same-user");
            Assert.Equal("NOT_MEMBER", status.Data?.Status);
            var denied = await fixture.UserGroupsCommunities.GetFeedAsync("shared-group", "same-user");
            Assert.True(denied.IsFailure);
            Assert.Equal("ACCESS_DENIED", denied.Error?.Code);
        }

        var tenantA = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "tenant-a" });
        var tenantB = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "tenant-b" });
        Assert.Single(tenantA.Data ?? []);
        Assert.Empty(tenantB.Data ?? []);
    }

    [Fact]
    public async Task CF878_ConcurrentJoin_AllowsOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("race-group", "race-user", simulateSlowWrite: true),
            null,
            "race-a");
        var second = fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("race-group", "race-user", simulateSlowWrite: true),
            null,
            "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF879_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("replay-group", "replay-user"), null, "same-key");
        var second = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("replay-group", "replay-user"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["membershipId"], second.Data?.Body["membershipId"]);

        var memberships = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["group_id"] = "replay-group", ["user_id"] = "replay-user" });
        Assert.Single(memberships.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == UserGroupsCommunitiesConstants.MembershipAcceptedEvent);
    }

    [Fact]
    public async Task CF880_ValidationRejectsMissingPrimaryFieldAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.UserGroupsCommunities.SubmitMembershipAsync(
            new Dictionary<string, object?> { ["groupId"] = string.Empty, ["userId"] = "user-invalid" },
            null,
            "invalid");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
        var memberships = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(memberships.Data ?? []);
    }

    [Fact]
    public async Task CF881_TimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("timeout-group", "timeout-user", simulateFabricTimeout: true),
            null,
            "timeout-key");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);
        var memberships = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(memberships.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == UserGroupsCommunitiesConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF882_ApprovalConcurrency_UsesSameMembershipGuard()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("approval-race", "race-user", simulateSlowWrite: true),
            null,
            "approval-race-a");
        var second = fixture.UserGroupsCommunities.SubmitMembershipAsync(
            MembershipRequest("approval-race", "race-user", simulateSlowWrite: true),
            null,
            "approval-race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF883_ApprovalTierAndLeaveOperations_AreIdempotent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("idempotent-group", "admin-user"), null, "admin-join");
        await fixture.UserGroupsCommunities.UpdateTierAsync("idempotent-group", new Dictionary<string, object?> { ["targetUserId"] = "admin-user", ["requestedByUserId"] = "system-admin", ["targetTier"] = "admin" }, "admin-tier");
        var approvalA = await fixture.UserGroupsCommunities.ReviewJoinRequestAsync("idempotent-group", new Dictionary<string, object?> { ["userId"] = "approved-user", ["decision"] = "APPROVE" }, "approval-key");
        var approvalB = await fixture.UserGroupsCommunities.ReviewJoinRequestAsync("idempotent-group", new Dictionary<string, object?> { ["userId"] = "approved-user", ["decision"] = "APPROVE" }, "approval-key");
        var tierA = await fixture.UserGroupsCommunities.UpdateTierAsync("idempotent-group", new Dictionary<string, object?> { ["targetUserId"] = "approved-user", ["requestedByUserId"] = "admin-user", ["targetTier"] = "moderator" }, "tier-key");
        var tierB = await fixture.UserGroupsCommunities.UpdateTierAsync("idempotent-group", new Dictionary<string, object?> { ["targetUserId"] = "approved-user", ["requestedByUserId"] = "admin-user", ["targetTier"] = "moderator" }, "tier-key");

        Assert.Equal(201, approvalA.Data?.HttpStatus);
        Assert.Equal(200, approvalB.Data?.HttpStatus);
        Assert.Equal(201, tierA.Data?.HttpStatus);
        Assert.Equal(200, tierB.Data?.HttpStatus);
    }

    [Fact]
    public async Task ProductSpec_AdminApproval_ActivatesInviteOnlyMembershipAndThenSeedsFeed()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var pending = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("invite-group", "invite-user", groupType: "INVITE_ONLY"), null, "pending");
        var approved = await fixture.UserGroupsCommunities.ReviewJoinRequestAsync("invite-group", new Dictionary<string, object?> { ["userId"] = "invite-user", ["decision"] = "APPROVE" }, "approve");

        Assert.Equal("PENDING_APPROVAL", pending.Data?.Body["status"]);
        Assert.Equal("ACTIVE", approved.Data?.Body["status"]);
        var feed = await fixture.UserGroupsCommunities.GetFeedAsync("invite-group", "invite-user");
        Assert.True(feed.IsSuccess, feed.Error?.Message);
        Assert.NotEmpty(feed.Data?.Items ?? []);
    }

    [Fact]
    public async Task ProductSpec_TierManagement_BlocksSelfPromotionAndRequiresAdminForAdminTier()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("tier-group", "member-a"), null, "member-a");
        await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("tier-group", "member-b"), null, "member-b");
        var selfPromotion = await fixture.UserGroupsCommunities.UpdateTierAsync("tier-group", new Dictionary<string, object?> { ["targetUserId"] = "member-a", ["requestedByUserId"] = "member-a", ["targetTier"] = "admin" }, "self");
        var nonAdminPromotion = await fixture.UserGroupsCommunities.UpdateTierAsync("tier-group", new Dictionary<string, object?> { ["targetUserId"] = "member-b", ["requestedByUserId"] = "member-a", ["targetTier"] = "admin" }, "non-admin");

        Assert.True(selfPromotion.IsFailure);
        Assert.Equal("TIER_CHANGE_REJECTED", selfPromotion.Error?.Code);
        Assert.True(nonAdminPromotion.IsFailure);
        Assert.Equal("TIER_CHANGE_REJECTED", nonAdminPromotion.Error?.Code);
    }

    [Fact]
    public async Task ProductSpec_GroupFeed_SkipsEmptyContentAndClampsRankingScores()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var skipped = await fixture.UserGroupsCommunities.PopulateFeedAsync("feed-group", "feed-user", []);
        var populated = await fixture.UserGroupsCommunities.PopulateFeedAsync(
            "feed-group",
            "feed-user",
            [
                new Dictionary<string, object?> { ["contentId"] = "wild", ["title"] = "Wild scores", ["engagementScore"] = 3.0, ["freshnessScore"] = -2.0, ["tierFitScore"] = 0.5 }
            ]);

        Assert.Equal("SKIPPED_EMPTY_CONTENT", skipped.Data?.Body["status"]);
        Assert.Equal(201, populated.Data?.HttpStatus);
        var items = Assert.IsAssignableFrom<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(populated.Data?.Body["items"]);
        Assert.Equal(1.0, items[0]["engagementScore"]);
        Assert.Equal(0.0, items[0]["freshnessScore"]);
    }

    [Fact]
    public async Task ProductSpec_Discovery_HidesInviteOnlyGroupsFromAnonymousVisitors()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "anonymous");

        var discovery = await fixture.UserGroupsCommunities.DiscoverGroupsAsync(null);

        Assert.True(discovery.IsSuccess, discovery.Error?.Message);
        Assert.Contains(discovery.Data?.Groups ?? [], group => string.Equals(Convert.ToString(group["groupType"]), "OPEN", StringComparison.Ordinal));
        Assert.DoesNotContain(discovery.Data?.Groups ?? [], group => string.Equals(Convert.ToString(group["groupType"]), "INVITE_ONLY", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProductSpec_LeaveMembership_StoresStateBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");

        await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("leave-group", "leave-user"), null, "join-before-leave");
        var left = await fixture.UserGroupsCommunities.SubmitMembershipAsync(MembershipRequest("leave-group", "leave-user", action: "LEAVE"), null, "leave");

        Assert.Equal("LEFT", left.Data?.Body["status"]);
        var memberships = await fixture.Documents.SearchAsync(UserGroupsCommunitiesConstants.Memberships, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["group_id"] = "leave-group", ["user_id"] = "leave-user" });
        Assert.Equal("LEFT", memberships.Data?.Single().Body["status"]);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == UserGroupsCommunitiesConstants.MembershipLeftEvent);
    }

    private static Dictionary<string, object?> MembershipRequest(
        string groupId,
        string userId,
        string action = "JOIN",
        string groupType = "OPEN",
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["userId"] = userId,
            ["action"] = action,
            ["groupType"] = groupType
        };
        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        if (simulateFabricTimeout)
        {
            request["simulateFabricTimeout"] = true;
        }

        return request;
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            UserGroupsCommunitiesService userGroupsCommunities,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            UserGroupsCommunities = userGroupsCommunities;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public UserGroupsCommunitiesService UserGroupsCommunities { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var freedom = new InMemoryFreedomConfigService();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var userGroupsCommunities = new UserGroupsCommunitiesService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<UserGroupsCommunitiesService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                freedom,
                userGroupsCommunities,
                tenantAccessor,
                correlationAccessor);
        }

        public async Task SeedSubscriptionAsync(string groupId, string userId, string tier)
        {
            var id = $"subscription-{groupId}-{userId}";
            await Documents.UpsertAsync(
                UserGroupsCommunitiesConstants.Subscriptions,
                id,
                new Dictionary<string, object?>
                {
                    ["id"] = id,
                    ["tenant_id"] = "acme-corp",
                    ["group_id"] = groupId,
                    ["user_id"] = userId,
                    ["membership_tier"] = tier
                },
                null);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = $"corr-{Guid.NewGuid():N}";
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" ? null : "user-1",
                new HashSet<string> { role },
                correlation,
                "test",
                DateTimeOffset.UtcNow));
            var correlationScope = CorrelationContextAccessor.Push(new CorrelationContext(correlation, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(tenant, correlationScope);
        }

        private sealed class CompositeScope : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;

            public CompositeScope(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _second.Dispose();
                _first.Dispose();
            }
        }
    }
}
