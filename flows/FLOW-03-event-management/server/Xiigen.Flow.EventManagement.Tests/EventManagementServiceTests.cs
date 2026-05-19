using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.EventManagement.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.EventManagement.Tests;

public sealed class EventManagementServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = EventManagementBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(EventManagementConstants.RuleIds, ids);
        Assert.All(EventManagementBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(EventManagementConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0301_CreateEvent_StoresEventBeforeEventCreatedOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var result = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Annual Tech Summit"),
            ifMatch: null,
            idempotencyKey: "create-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);

        var events = await fixture.Documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["title"] = "Annual Tech Summit" });
        var stored = Assert.Single(events.Data ?? []);
        Assert.Equal("GLOBAL", stored.Body["knowledge_scope"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == EventManagementConstants.EventCreatedEvent);
        Assert.True(stored.UpdatedAt <= outbox.Data!.First(record => record.Event.Type == EventManagementConstants.EventCreatedEvent).CreatedAt);
    }

    [Fact]
    public async Task CF0302_CapacityNullMeansUnlimitedAndZeroMeansWaitlistOnly()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var unlimited = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Unlimited Workshop", capacity: null),
            ifMatch: null,
            idempotencyKey: "unlimited");
        var closed = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Closed Briefing", capacity: 0),
            ifMatch: null,
            idempotencyKey: "closed");

        Assert.True(unlimited.IsSuccess, unlimited.Error?.Message);
        Assert.True(closed.IsSuccess, closed.Error?.Message);

        var unlimitedRegistration = await fixture.EventManagement.RegisterAsync(
            (string)unlimited.Data!.Body["eventId"]!,
            new Dictionary<string, object?> { ["attendeeId"] = "attendee-1" },
            "reg-unlimited");
        var closedRegistration = await fixture.EventManagement.RegisterAsync(
            (string)closed.Data!.Body["eventId"]!,
            new Dictionary<string, object?> { ["attendeeId"] = "attendee-2" },
            "reg-closed");

        Assert.Equal("CONFIRMED", unlimitedRegistration.Data?.Body["status"]);
        Assert.Equal("WAITLISTED", closedRegistration.Data?.Body["status"]);
    }

    [Fact]
    public async Task CF0303_ContentSafetyFailureRejectsPromotionWithoutSystemFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var eventResult = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Sensitive Roundtable", promotionEnabled: false),
            ifMatch: null,
            idempotencyKey: "promo-source");
        var eventId = (string)eventResult.Data!.Body["eventId"]!;

        var promotion = await fixture.EventManagement.PromoteEventAsync(
            eventId,
            new Dictionary<string, object?> { ["organizerId"] = "org-001", ["contentSafetyStatus"] = "fail" });

        Assert.True(promotion.IsSuccess, promotion.Error?.Message);
        Assert.Equal(200, promotion.Data?.HttpStatus);
        Assert.False((bool)promotion.Data!.Body["promoted"]!);

        var promotions = await fixture.Documents.SearchAsync(
            EventManagementConstants.Promotions,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["event_id"] = eventId });
        Assert.Empty(promotions.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == EventManagementConstants.PromotionRejectedEvent);
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type == EventManagementConstants.PromotionPublishedEvent);
    }

    [Fact]
    public async Task CF0304_EventsAndReadsAreTenantScoped()
    {
        var fixture = FlowFixture.Create();

        using (fixture.PushContext("tenant-a", "event_organiser"))
        {
            var created = await fixture.EventManagement.CreateEventAsync(
                EventRequest("Shared Name"),
                ifMatch: null,
                idempotencyKey: "tenant-a");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        using (fixture.PushContext("tenant-b", "event_organiser"))
        {
            var listBefore = await fixture.EventManagement.ListEventsAsync();
            Assert.Empty(listBefore.Data ?? []);

            var created = await fixture.EventManagement.CreateEventAsync(
                EventRequest("Shared Name"),
                ifMatch: null,
                idempotencyKey: "tenant-b");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        var tenantA = await fixture.Documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = "tenant-a" });
        var tenantB = await fixture.Documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = "tenant-b" });
        Assert.Single(tenantA.Data ?? []);
        Assert.Single(tenantB.Data ?? []);
    }

    [Fact]
    public async Task CF858_ConcurrentCreateSameResource_AllowsOneWinnerAndRejectsOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var first = fixture.EventManagement.CreateEventAsync(
            EventRequest("Race Summit", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-a");
        var second = fixture.EventManagement.CreateEventAsync(
            EventRequest("Race Summit", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF859_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var first = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Replay Summit"),
            ifMatch: null,
            idempotencyKey: "same-key");
        var second = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Replay Summit"),
            ifMatch: null,
            idempotencyKey: "same-key");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["eventId"], second.Data?.Body["eventId"]);

        var events = await fixture.Documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["title"] = "Replay Summit" });
        Assert.Single(events.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == EventManagementConstants.EventCreatedEvent);
    }

    [Fact]
    public async Task CF860_BoundaryValidationRejectsMissingPrimaryFieldAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var result = await fixture.EventManagement.CreateEventAsync(
            new Dictionary<string, object?> { ["title"] = "", ["organizerId"] = "org-001", ["startDate"] = FutureDate() },
            ifMatch: null,
            idempotencyKey: "invalid");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);

        var events = await fixture.Documents.SearchAsync(
            EventManagementConstants.Events,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(events.Data ?? []);
    }

    [Fact]
    public async Task CF861_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var result = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Timeout Summit", simulateFabricTimeout: true),
            ifMatch: null,
            idempotencyKey: "timeout-1");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == EventManagementConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var created = await fixture.EventManagement.CreateEventAsync(
            EventRequest("One Seat Session", capacity: 1),
            ifMatch: null,
            idempotencyKey: "one-seat");
        var eventId = (string)created.Data!.Body["eventId"]!;

        var first = fixture.EventManagement.RegisterAsync(
            eventId,
            new Dictionary<string, object?> { ["attendeeId"] = "attendee-a", ["simulateSlowWrite"] = true },
            "reg-a");
        var second = fixture.EventManagement.RegisterAsync(
            eventId,
            new Dictionary<string, object?> { ["attendeeId"] = "attendee-b", ["simulateSlowWrite"] = true },
            "reg-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task ProductSpec_MatchingWeightsFeedPlacementAndNotificationChannels_AreApplied()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var result = await fixture.EventManagement.CreateEventAsync(
            EventRequest(
                "Strong Audience Summit",
                matchingCriteria: new Dictionary<string, object?>
                {
                    ["industry"] = "technology",
                    ["interestRelevance"] = 1,
                    ["locationProximity"] = 1,
                    ["timeAvailability"] = 1,
                    ["priceSensitivity"] = 1,
                    ["socialConnections"] = 1,
                    ["distanceKm"] = 10
                },
                pushEnabled: true,
                urgent: true),
            ifMatch: null,
            idempotencyKey: "strong-audience");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains("audience:strong", (IReadOnlyList<string>)result.Data!.Body["audienceSegments"]!);
        Assert.Contains("top-3", (IReadOnlyList<string>)result.Data.Body["feedPlacements"]!);
        Assert.Contains("sms", (IReadOnlyList<string>)result.Data.Body["notificationChannels"]!);
    }

    [Fact]
    public async Task ProductSpec_ZeroAudienceFallsBackToRegionAndIndustry()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var result = await fixture.EventManagement.CreateEventAsync(
            EventRequest(
                "Fallback Audience",
                matchingCriteria: new Dictionary<string, object?> { ["region"] = "north", ["industry"] = "education" }),
            ifMatch: null,
            idempotencyKey: "fallback");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains("region:north", (IReadOnlyList<string>)result.Data!.Body["audienceSegments"]!);
        Assert.Contains("industry:education", (IReadOnlyList<string>)result.Data.Body["audienceSegments"]!);
    }

    [Fact]
    public async Task AnalyticsCounter_IsTtlWindowedAndBestEffort()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var created = await fixture.EventManagement.CreateEventAsync(
            EventRequest("Analytics Session"),
            ifMatch: null,
            idempotencyKey: "analytics-source");
        var eventId = (string)created.Data!.Body["eventId"]!;

        var analytics = await fixture.EventManagement.TrackAnalyticsAsync(eventId, "campaign_engagement");

        Assert.True(analytics.IsSuccess, analytics.Error?.Message);
        Assert.True(analytics.Data!.Tracked);
        Assert.False(string.IsNullOrWhiteSpace(analytics.Data.ExpiresAt));
    }

    private static Dictionary<string, object?> EventRequest(
        string title,
        int? capacity = 100,
        bool promotionEnabled = true,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false,
        IReadOnlyDictionary<string, object?>? matchingCriteria = null,
        bool pushEnabled = false,
        bool urgent = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["organizerId"] = "org-001",
            ["startDate"] = FutureDate(),
            ["capacity"] = capacity,
            ["promotionEnabled"] = promotionEnabled,
            ["contentSafetyStatus"] = "pass",
            ["matchingCriteria"] = matchingCriteria ?? new Dictionary<string, object?>
            {
                ["industry"] = "technology",
                ["interestRelevance"] = 0.8,
                ["locationProximity"] = 0.7,
                ["timeAvailability"] = 0.6,
                ["priceSensitivity"] = 0.5,
                ["socialConnections"] = 0.5,
                ["distanceKm"] = 20
            },
            ["pushEnabled"] = pushEnabled,
            ["urgent"] = urgent
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

    private static string FutureDate() => DateTimeOffset.UtcNow.AddDays(30).ToString("O");

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            EventManagementService eventManagement,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            EventManagement = eventManagement;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public EventManagementService EventManagement { get; }
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
            var eventManagement = new EventManagementService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<EventManagementService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                freedom,
                eventManagement,
                tenantAccessor,
                correlationAccessor);
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
