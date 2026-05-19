using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.EventAttendance.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.EventAttendance.Tests;

public sealed class EventAttendanceServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = EventAttendanceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(EventAttendanceConstants.RuleIds, ids);
        Assert.Equal(8, EventAttendanceBfaRuleRegistry.Rules.Count);
        Assert.All(EventAttendanceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(EventAttendanceConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0401_RsvpCreation_StoresSingleRsvpBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-atomic", "attendee-1", capacity: 5),
            ifMatch: null,
            idempotencyKey: "atomic-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);

        var rsvps = await fixture.Documents.SearchAsync(
            EventAttendanceConstants.Rsvps,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["event_id"] = "evt-atomic", ["attendee_id"] = "attendee-1" });
        var stored = Assert.Single(rsvps.Data ?? []);
        Assert.Equal("CONFIRMED", stored.Body["status"]);

        var outbox = await fixture.Outbox.ListAsync();
        var confirmed = Assert.Single(outbox.Data ?? [], record => record.Event.Type == EventAttendanceConstants.RsvpConfirmedEvent);
        Assert.True(stored.UpdatedAt <= confirmed.CreatedAt);
    }

    [Fact]
    public async Task CF0402_WaitlistPromotion_UsesFifoJoinTimestamp()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-fifo", "confirmed", capacity: 1), null, "fifo-1");
        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-fifo", "wait-a", capacity: 1), null, "fifo-2");
        await Task.Delay(5);
        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-fifo", "wait-b", capacity: 1), null, "fifo-3");

        var cancelled = await fixture.EventAttendance.CancelRsvpAsync(
            "evt-fifo",
            new Dictionary<string, object?> { ["attendeeId"] = "confirmed" },
            "cancel-fifo");

        Assert.True(cancelled.IsSuccess, cancelled.Error?.Message);

        var promoted = await fixture.EventAttendance.GetAttendanceStatusAsync("evt-fifo", "wait-a");
        var stillWaiting = await fixture.EventAttendance.GetAttendanceStatusAsync("evt-fifo", "wait-b");

        Assert.Equal("CONFIRMED", promoted.Data?.Status);
        Assert.Equal("WAITLISTED", stillWaiting.Data?.Status);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == EventAttendanceConstants.WaitlistPromotionCompletedEvent);
    }

    [Fact]
    public async Task CF0403_FeedbackWindow_OpensOnlyFromEventEnded()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        var rejected = await fixture.EventAttendance.OpenFeedbackWindowAsync(
            "evt-feedback",
            new Dictionary<string, object?> { ["triggerSource"] = "timer" });
        var accepted = await fixture.EventAttendance.OpenFeedbackWindowAsync(
            "evt-feedback",
            new Dictionary<string, object?> { ["triggerSource"] = "event.ended" });

        Assert.True(rejected.IsFailure);
        Assert.Equal("FEEDBACK_TRIGGER_REJECTED", rejected.Error?.Code);
        Assert.True(accepted.IsSuccess, accepted.Error?.Message);

        var windows = await fixture.Documents.SearchAsync(
            EventAttendanceConstants.FeedbackWindows,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["event_id"] = "evt-feedback" });
        Assert.Single(windows.Data ?? []);
    }

    [Fact]
    public async Task CF0404_AttendanceData_IsTenantScoped()
    {
        var fixture = FlowFixture.Create();

        using (fixture.PushContext("tenant-a", "tenant_user"))
        {
            var created = await fixture.EventAttendance.SubmitRsvpAsync(
                RsvpRequest("evt-shared", "same-attendee", capacity: 2),
                null,
                "tenant-a-rsvp");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        using (fixture.PushContext("tenant-b", "tenant_user"))
        {
            var status = await fixture.EventAttendance.GetAttendanceStatusAsync("evt-shared", "same-attendee");
            Assert.Equal("NONE", status.Data?.Status);

            var created = await fixture.EventAttendance.SubmitRsvpAsync(
                RsvpRequest("evt-shared", "same-attendee", capacity: 2),
                null,
                "tenant-b-rsvp");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        var tenantA = await fixture.Documents.SearchAsync(EventAttendanceConstants.Rsvps, new Dictionary<string, object?> { ["tenant_id"] = "tenant-a" });
        var tenantB = await fixture.Documents.SearchAsync(EventAttendanceConstants.Rsvps, new Dictionary<string, object?> { ["tenant_id"] = "tenant-b" });
        Assert.Single(tenantA.Data ?? []);
        Assert.Single(tenantB.Data ?? []);
    }

    [Fact]
    public async Task CF863_ConcurrentSameAttendeeRsvp_AllowsOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-race", "attendee-race", capacity: 5, simulateSlowWrite: true),
            null,
            "race-a");
        var second = fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-race", "attendee-race", capacity: 5, simulateSlowWrite: true),
            null,
            "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF864_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-replay", "attendee-replay", capacity: 5), null, "same-key");
        var second = await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-replay", "attendee-replay", capacity: 5), null, "same-key");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["rsvpId"], second.Data?.Body["rsvpId"]);

        var rsvps = await fixture.Documents.SearchAsync(
            EventAttendanceConstants.Rsvps,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["event_id"] = "evt-replay", ["attendee_id"] = "attendee-replay" });
        Assert.Single(rsvps.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == EventAttendanceConstants.RsvpConfirmedEvent);
    }

    [Fact]
    public async Task CF865_ValidationRejectsMissingPrimaryFieldAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.EventAttendance.SubmitRsvpAsync(
            new Dictionary<string, object?> { ["eventId"] = "evt-invalid", ["eventCapacity"] = 5 },
            null,
            "invalid");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);

        var rsvps = await fixture.Documents.SearchAsync(EventAttendanceConstants.Rsvps, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(rsvps.Data ?? []);
    }

    [Fact]
    public async Task CF866_TimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-timeout", "attendee-timeout", capacity: 5, simulateFabricTimeout: true),
            null,
            "timeout-1");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);

        var rsvps = await fixture.Documents.SearchAsync(EventAttendanceConstants.Rsvps, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(rsvps.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == EventAttendanceConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task ProductSpec_CapacityZeroRoutesWaitlistAsSuccessAndNullIsUnlimited()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var waitlisted = await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-zero", "attendee-w", capacity: 0), null, "zero");
        var unlimited = await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-null", "attendee-u", capacity: null), null, "null");

        Assert.True(waitlisted.IsSuccess, waitlisted.Error?.Message);
        Assert.True(unlimited.IsSuccess, unlimited.Error?.Message);
        Assert.Equal("WAITLISTED", waitlisted.Data?.Body["status"]);
        Assert.Equal("CONFIRMED", unlimited.Data?.Body["status"]);
    }

    [Fact]
    public async Task ProductSpec_PaidRsvpRequiresPaymentConfirmed()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var blocked = await fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-paid", "attendee-paid", capacity: 5, isPaid: true, paymentConfirmed: false),
            null,
            "paid-blocked");
        var accepted = await fixture.EventAttendance.SubmitRsvpAsync(
            RsvpRequest("evt-paid", "attendee-paid", capacity: 5, isPaid: true, paymentConfirmed: true),
            null,
            "paid-accepted");

        Assert.True(blocked.IsFailure);
        Assert.Equal("PAYMENT_REQUIRED", blocked.Error?.Code);
        Assert.True(accepted.IsSuccess, accepted.Error?.Message);
        Assert.Equal("CONFIRMED", accepted.Data?.Body["status"]);
    }

    [Fact]
    public async Task ProductSpec_CheckInRequiresConfirmedRsvpAndIsIdempotent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");

        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-checkin", "confirmed", capacity: 1), null, "rsvp-confirmed");
        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-checkin", "waiting", capacity: 1), null, "rsvp-waiting");

        var confirmed = await fixture.EventAttendance.CheckInAsync("evt-checkin", new Dictionary<string, object?> { ["attendeeId"] = "confirmed" }, "check-confirmed");
        var replay = await fixture.EventAttendance.CheckInAsync("evt-checkin", new Dictionary<string, object?> { ["attendeeId"] = "confirmed" }, "check-confirmed");
        var waiting = await fixture.EventAttendance.CheckInAsync("evt-checkin", new Dictionary<string, object?> { ["attendeeId"] = "waiting" }, "check-waiting");

        Assert.Equal(201, confirmed.Data?.HttpStatus);
        Assert.Equal(200, replay.Data?.HttpStatus);
        Assert.False((bool)waiting.Data!.Body["checkedIn"]!);
        Assert.Equal("NOT_CONFIRMED", waiting.Data.Body["reason"]);
    }

    [Fact]
    public async Task ProductSpec_CancellationPromotesEarliestWaitlistedAttendee()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-cancel", "confirmed", capacity: 1), null, "cancel-rsvp-1");
        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-cancel", "wait-a", capacity: 1), null, "cancel-rsvp-2");

        var result = await fixture.EventAttendance.CancelRsvpAsync("evt-cancel", new Dictionary<string, object?> { ["attendeeId"] = "confirmed" }, "cancel-main");
        var promoted = await fixture.EventAttendance.GetAttendanceStatusAsync("evt-cancel", "wait-a");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("wait-a", result.Data?.Body["promotedAttendeeId"]);
        Assert.Equal("CONFIRMED", promoted.Data?.Status);
    }

    [Fact]
    public async Task ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.EventAttendance.SubmitRsvpAsync(RsvpRequest("evt-feedback-flow", "attendee-feedback", capacity: 5), null, "feedback-rsvp");
        await fixture.EventAttendance.CheckInAsync("evt-feedback-flow", new Dictionary<string, object?> { ["attendeeId"] = "attendee-feedback" }, "feedback-checkin");
        var window = await fixture.EventAttendance.OpenFeedbackWindowAsync("evt-feedback-flow", new Dictionary<string, object?> { ["triggerSource"] = "event.ended" });
        var feedback = await fixture.EventAttendance.SubmitFeedbackAsync(
            "evt-feedback-flow",
            new Dictionary<string, object?> { ["attendeeId"] = "attendee-feedback", ["rating"] = 5, ["comment"] = "Useful session" },
            "feedback-submit");

        Assert.True(window.IsSuccess, window.Error?.Message);
        Assert.True(feedback.IsSuccess, feedback.Error?.Message);
        Assert.Equal(201, feedback.Data?.HttpStatus);
    }

    private static Dictionary<string, object?> RsvpRequest(
        string eventId,
        string attendeeId,
        int? capacity,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false,
        bool isPaid = false,
        bool paymentConfirmed = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["eventId"] = eventId,
            ["attendeeId"] = attendeeId,
            ["eventTitle"] = "Community product session",
            ["eventCapacity"] = capacity,
            ["eventStartsAtUtc"] = DateTimeOffset.UtcNow.AddDays(10).ToString("O"),
            ["eventEndsAtUtc"] = DateTimeOffset.UtcNow.AddDays(10).AddHours(2).ToString("O"),
            ["seedEvent"] = true,
            ["isPaidEvent"] = isPaid,
            ["paymentConfirmed"] = paymentConfirmed
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
            EventAttendanceService eventAttendance,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            EventAttendance = eventAttendance;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public EventAttendanceService EventAttendance { get; }
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
            var eventAttendance = new EventAttendanceService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<EventAttendanceService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                freedom,
                eventAttendance,
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
