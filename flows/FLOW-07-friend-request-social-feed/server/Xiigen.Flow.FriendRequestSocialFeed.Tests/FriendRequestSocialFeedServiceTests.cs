using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.FriendRequestSocialFeed.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.FriendRequestSocialFeed.Tests;

public sealed class FriendRequestSocialFeedServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = FriendRequestSocialFeedBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(FriendRequestSocialFeedConstants.RuleIds));
        Assert.Equal(15, FriendRequestSocialFeedBfaRuleRegistry.Rules.Count);
        Assert.All(FriendRequestSocialFeedBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(FriendRequestSocialFeedConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0701_PrivacyGatekeeper_BlocksInlineBeforeRequestStateOrOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.SeedPrivacyBlockAsync("recipient-a", "sender-a", "friend_request");

        var result = await fixture.Service.SubmitFriendRequestAsync(
            FriendRequest("request-private", "sender-a", "recipient-a"),
            null,
            "privacy-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("BLOCKED", result.Data?.Body["status"]);
        var requests = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FriendRequests, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(requests.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type == FriendRequestSocialFeedConstants.RequestSentEvent);
    }

    [Fact]
    public async Task CF0702_AcceptanceWritesBidirectionalGraphBeforeConnectionEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-graph", "alice", "bob"), null, "graph-submit");
        var accepted = await fixture.Service.RespondToFriendRequestAsync(
            "request-graph",
            ResponseRequest("bob", "ACCEPT", baseScore: 1, group: 1, events: 1, purchase: 1, questionnaire: 1),
            null,
            "graph-accept");

        Assert.True(accepted.IsSuccess, accepted.Error?.Message);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("ACCEPTED", accepted.Data?.Body["status"]);
        var connectionId = Convert.ToString(accepted.Data?.Body["connectionId"]);
        var edges = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.Connections, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["connection_id"] = connectionId });
        Assert.Equal(2, edges.Data?.Count ?? 0);
        Assert.Contains(edges.Data ?? [], edge => Convert.ToString(edge.Body["from_user_id"]) == "alice" && Convert.ToString(edge.Body["to_user_id"]) == "bob");
        Assert.Contains(edges.Data ?? [], edge => Convert.ToString(edge.Body["from_user_id"]) == "bob" && Convert.ToString(edge.Body["to_user_id"]) == "alice");
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == FriendRequestSocialFeedConstants.ConnectionEstablishedEvent);
    }

    [Fact]
    public async Task CF0703_ZeroFeedScore_IsStoredAndPublished()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-zero", "alice", "bob"), null, "zero-submit");
        var accepted = await fixture.Service.RespondToFriendRequestAsync(
            "request-zero",
            ResponseRequest("bob", "ACCEPT", baseScore: 1, group: 1, events: 1, purchase: 1, questionnaire: 1, forceZero: true),
            null,
            "zero-accept");

        Assert.Equal(201, accepted.Data?.HttpStatus);
        var scores = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FeedScores, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["score"] = 0.0 });
        Assert.NotEmpty(scores.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == FriendRequestSocialFeedConstants.FeedItemScoredEvent && Convert.ToDouble(record.Event.Data["score"]) == 0);
    }

    [Fact]
    public async Task CF0704_TenantPrivateScope_DoesNotLeakConnectionsOrFeedsAcrossTenants()
    {
        var fixture = FlowFixture.Create();

        using (fixture.PushContext("tenant-a", "tenant_user"))
        {
            await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-tenant", "same-a", "same-b"), null, "tenant-submit");
            await fixture.Service.RespondToFriendRequestAsync("request-tenant", ResponseRequest("same-b", "ACCEPT"), null, "tenant-accept");
        }

        using (fixture.PushContext("tenant-b", "tenant_user"))
        {
            var connections = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.Connections, new Dictionary<string, object?> { ["tenant_id"] = "tenant-b" });
            var feed = await fixture.Service.GetFeedAsync("conn-tenant-a-same-a-same-b", "same-a");
            Assert.Empty(connections.Data ?? []);
            Assert.True(feed.IsSuccess, feed.Error?.Message);
            Assert.Empty(feed.Data?.Items ?? []);
        }
    }

    [Fact]
    public async Task CF884_ConcurrentRequest_AllowsOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.Service.SubmitFriendRequestAsync(
            FriendRequest("request-race-a", "race-a", "race-b", simulateSlowWrite: true),
            null,
            "race-key-a");
        var second = fixture.Service.SubmitFriendRequestAsync(
            FriendRequest("request-race-b", "race-a", "race-b", simulateSlowWrite: true),
            null,
            "race-key-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF885_IdempotentRequestReplay_ReturnsCachedResponseWithoutDuplicateState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-replay", "alice", "bob"), null, "same-key");
        var second = await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-replay", "alice", "bob"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["requestId"], second.Data?.Body["requestId"]);
        var requests = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FriendRequests, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["sender_user_id"] = "alice", ["recipient_user_id"] = "bob" });
        Assert.Single(requests.Data ?? []);
    }

    [Fact]
    public async Task CF886_RequestValidation_RejectsMissingFieldsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.SubmitFriendRequestAsync(
            new Dictionary<string, object?> { ["requestId"] = "invalid", ["senderUserId"] = "", ["recipientUserId"] = "bob" },
            null,
            "invalid-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        var requests = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FriendRequests, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(requests.Data ?? []);
    }

    [Fact]
    public async Task CF887_RequestTimeout_ReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.SubmitFriendRequestAsync(
            FriendRequest("request-timeout", "alice", "bob", simulateFabricTimeout: true),
            null,
            "timeout-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.Equal("TIMEOUT", result.Data?.Body["code"]);
        Assert.Equal(5000, result.Data?.Body["retryAfterMs"]);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == FriendRequestSocialFeedConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF888_ResponseValidation_RejectsInvalidDecision()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-invalid-decision", "alice", "bob"), null, "decision-submit");

        var result = await fixture.Service.RespondToFriendRequestAsync(
            "request-invalid-decision",
            ResponseRequest("bob", "MAYBE"),
            null,
            "decision-invalid");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF889_ResponseIdempotency_ReplaysWithoutDuplicateConnectionsOrFeed()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-response-replay", "alice", "bob"), null, "response-submit");

        var first = await fixture.Service.RespondToFriendRequestAsync("request-response-replay", ResponseRequest("bob", "ACCEPT"), null, "response-same-key");
        var second = await fixture.Service.RespondToFriendRequestAsync("request-response-replay", ResponseRequest("bob", "ACCEPT"), null, "response-same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        var connections = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.Connections, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Equal(2, connections.Data?.Count ?? 0);
        var feedItems = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FeedItems, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Equal(20, feedItems.Data?.Count ?? 0);
    }

    [Fact]
    public async Task CF890_ResponseConcurrency_AllowsOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-response-race", "alice", "bob"), null, "response-race-submit");

        var first = fixture.Service.RespondToFriendRequestAsync("request-response-race", ResponseRequest("bob", "ACCEPT", simulateSlow: true), null, "response-race-a");
        var second = fixture.Service.RespondToFriendRequestAsync("request-response-race", ResponseRequest("bob", "ACCEPT", simulateSlow: true), null, "response-race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF891_ResponseTimeout_RetainsOutboxAndDoesNotCreateConnection()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-response-timeout", "alice", "bob"), null, "response-timeout-submit");

        var result = await fixture.Service.RespondToFriendRequestAsync(
            "request-response-timeout",
            ResponseRequest("bob", "ACCEPT", simulateTimeout: true),
            null,
            "response-timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        var connections = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.Connections, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(connections.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == FriendRequestSocialFeedConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF892_MutualPendingRequests_AutoAcceptWithoutSecondPendingRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-a-b", "alice", "bob"), null, "a-b");
        var result = await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-b-a", "bob", "alice"), null, "b-a");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("AUTO_ACCEPTED", result.Data?.Body["status"]);
        var requests = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.FriendRequests, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(requests.Data ?? []);
    }

    [Fact]
    public async Task CF893_FeedWindow_UsesConnectionStrengthBuckets()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var strong = await AcceptPairAsync(fixture, "strong", 1, 1, 1, 1, 1);
        var medium = await AcceptPairAsync(fixture, "medium", 0.55, 0.55, 0.55, 0.55, 0.55);
        var weak = await AcceptPairAsync(fixture, "weak", 0.1, 0.1, 0.1, 0.1, 0.1);

        Assert.Equal(20, (await fixture.Service.GetFeedAsync(strong, "strong-a")).Data?.Items.Count);
        Assert.Equal(10, (await fixture.Service.GetFeedAsync(medium, "medium-a")).Data?.Items.Count);
        Assert.Equal(5, (await fixture.Service.GetFeedAsync(weak, "weak-a")).Data?.Items.Count);
    }

    [Fact]
    public async Task CF894_Analytics_AggregatesTenantMetricsWithoutUserIdentifiers()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await AcceptPairAsync(fixture, "analytics", 1, 1, 1, 1, 1);

        var analytics = await fixture.Service.GetAnalyticsAsync();

        Assert.True(analytics.IsSuccess, analytics.Error?.Message);
        Assert.Equal(1, analytics.Data?.ConnectionCount);
        Assert.True(analytics.Data?.StrengthBuckets["strong"] >= 2);
        var records = await fixture.Documents.SearchAsync(FriendRequestSocialFeedConstants.Analytics, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        var body = Assert.Single(records.Data ?? []).Body;
        Assert.DoesNotContain("user_id", body.Keys);
        Assert.DoesNotContain("sender_user_id", body.Keys);
        Assert.DoesNotContain("recipient_user_id", body.Keys);
    }

    [Fact]
    public async Task ProductSpec_RateLimit_StopsTheTwentyFirstOutboundRequest()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        for (var index = 0; index < 20; index++)
        {
            var result = await fixture.Service.SubmitFriendRequestAsync(FriendRequest($"request-limit-{index}", "sender-limit", $"recipient-{index}"), null, $"limit-{index}");
            Assert.Equal(201, result.Data?.HttpStatus);
        }

        var limited = await fixture.Service.SubmitFriendRequestAsync(FriendRequest("request-limit-final", "sender-limit", "recipient-final"), null, "limit-final");

        Assert.True(limited.IsFailure);
        Assert.Equal("RATE_LIMITED", limited.Error?.Code);
    }

    private static async Task<string> AcceptPairAsync(
        FlowFixture fixture,
        string prefix,
        double baseScore,
        double group,
        double events,
        double purchase,
        double questionnaire)
    {
        await fixture.Service.SubmitFriendRequestAsync(FriendRequest($"request-{prefix}", $"{prefix}-a", $"{prefix}-b"), null, $"{prefix}-submit");
        var accepted = await fixture.Service.RespondToFriendRequestAsync(
            $"request-{prefix}",
            ResponseRequest($"{prefix}-b", "ACCEPT", baseScore, group, events, purchase, questionnaire),
            null,
            $"{prefix}-accept");

        return Convert.ToString(accepted.Data?.Body["connectionId"])!;
    }

    private static Dictionary<string, object?> FriendRequest(
        string requestId,
        string senderUserId,
        string recipientUserId,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["requestId"] = requestId,
            ["senderUserId"] = senderUserId,
            ["recipientUserId"] = recipientUserId,
            ["message"] = $"Connect {senderUserId} to {recipientUserId}",
            ["baseMatchScore"] = 0.65,
            ["source"] = "direct"
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

    private static Dictionary<string, object?> ResponseRequest(
        string actorUserId,
        string decision,
        double baseScore = 0.65,
        double group = 0.65,
        double events = 0.65,
        double purchase = 0.65,
        double questionnaire = 0.65,
        bool forceZero = false,
        bool simulateSlow = false,
        bool simulateTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["actorUserId"] = actorUserId,
            ["decision"] = decision,
            ["baseMatchScore"] = baseScore,
            ["groupWeight"] = group,
            ["eventWeight"] = events,
            ["purchaseWeight"] = purchase,
            ["questionnaireWeight"] = questionnaire
        };
        if (forceZero)
        {
            request["forceZeroScore"] = true;
        }

        if (simulateSlow)
        {
            request["simulateSlowWrite"] = true;
        }

        if (simulateTimeout)
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
            FriendRequestSocialFeedService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public FriendRequestSocialFeedService Service { get; }
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
            var service = new FriendRequestSocialFeedService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<FriendRequestSocialFeedService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
        }

        public async Task SeedPrivacyBlockAsync(string targetUserId, string requesterUserId, string action)
        {
            var id = $"privacy-{targetUserId}-{requesterUserId}-{action}";
            await Documents.UpsertAsync(
                FriendRequestSocialFeedConstants.PrivacyRules,
                id,
                new Dictionary<string, object?>
                {
                    ["tenant_id"] = "acme-corp",
                    ["target_user_id"] = targetUserId,
                    ["requester_user_id"] = requesterUserId,
                    ["action"] = action,
                    ["effect"] = "BLOCK"
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
