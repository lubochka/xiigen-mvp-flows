using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.TransactionalEventParticipation.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.TransactionalEventParticipation.Tests;

public sealed class TransactionalEventParticipationServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = TransactionalEventParticipationBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(TransactionalEventParticipationConstants.RuleIds));
        Assert.Equal(17, TransactionalEventParticipationBfaRuleRegistry.Rules.Count);
        Assert.All(TransactionalEventParticipationBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(TransactionalEventParticipationConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0901_SeatReservationIsHeldBeforePaymentTicketAndToken()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.StartParticipationAsync(Booking("event-a", "buyer-a"), null, "seat-before-payment");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        var reservations = await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.SeatReservations, Tenant("acme-corp"));
        var payments = await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Payments, Tenant("acme-corp"));
        var tickets = await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, Tenant("acme-corp"));
        Assert.Single(reservations.Data ?? []);
        Assert.Single(payments.Data ?? []);
        Assert.Single(tickets.Data ?? []);
        var eventTypes = (await fixture.Outbox.ListAsync()).Data?.Select(record => record.Event.Type).ToList() ?? [];
        Assert.True(eventTypes.IndexOf(TransactionalEventParticipationConstants.InventoryReservedEvent) < eventTypes.IndexOf(TransactionalEventParticipationConstants.PaymentClearedEvent));
        Assert.True(eventTypes.IndexOf(TransactionalEventParticipationConstants.PaymentClearedEvent) < eventTypes.IndexOf(TransactionalEventParticipationConstants.TicketIssuedEvent));
    }

    [Fact]
    public async Task CF0902_FraudServiceUnavailableFailsOpenAndAuditsDecision()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Booking("event-fraud", "buyer-fraud");
        request["simulateFraudServiceDown"] = true;

        var result = await fixture.Service.StartParticipationAsync(request, null, "fraud-open-key");

        Assert.Equal(201, result.Data?.HttpStatus);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record =>
            record.Event.Type == TransactionalEventParticipationConstants.FraudCheckFailedEvent &&
            Equals(record.Event.Data["proceed"], true));
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == TransactionalEventParticipationConstants.TicketIssuedEvent);
    }

    [Fact]
    public async Task CF0903_RefundRetryExhaustionRoutesComplianceReview()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var booking = await fixture.Service.StartParticipationAsync(Booking("event-refund", "buyer-refund"), null, "refund-booking-key");
        var ticketId = Convert.ToString(booking.Data?.Body["ticketId"])!;

        var refund = await fixture.Service.RequestRefundAsync(new Dictionary<string, object?>
        {
            ["ticketId"] = ticketId,
            ["attempt"] = 3,
            ["maxAttempts"] = 3,
            ["reason"] = "processor_timeout"
        }, "refund-exhaust-key");

        Assert.Equal(201, refund.Data?.HttpStatus);
        var compliance = await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.ComplianceQueue, Tenant("acme-corp"));
        Assert.Single(compliance.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == TransactionalEventParticipationConstants.RefundFailedEvent);
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == TransactionalEventParticipationConstants.ComplianceReviewRequestedEvent);
    }

    [Fact]
    public async Task CF0904_GroupTicketFailureRollsBackAllParticipants()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Booking("event-group", "buyer-group");
        request["groupMembers"] = new[] { "buyer-a", "buyer-b", "buyer-c" };
        request["simulateGroupMemberFailure"] = true;

        var result = await fixture.Service.StartParticipationAsync(request, null, "group-rollback-key");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("GROUP_BOOKING_ROLLED_BACK", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Payments, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.GroupBookings, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF0905_FeeCalculationIsInlineAndDoesNotWriteSeparateDocuments()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.StartParticipationAsync(Booking("event-fee", "buyer-fee"), null, "fee-key");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.True(((IReadOnlyDictionary<string, object?>)result.Data!.Body["fee"]!).ContainsKey("serviceFee"));
        var outbox = await fixture.Outbox.ListAsync();
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type.Contains("fee", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CF0906_PlatformOnlyQrTokenAcceptsIssuedTokenAndRejectsCustomToken()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "event_organiser");
        var booking = await fixture.Service.StartParticipationAsync(Booking("event-qr", "buyer-qr"), null, "qr-key");
        var qrToken = Convert.ToString(booking.Data?.Body["qrToken"])!;

        var rejected = await fixture.Service.RedeemTokenAsync(new Dictionary<string, object?> { ["qrToken"] = "custom-token:abc" });
        var accepted = await fixture.Service.RedeemTokenAsync(new Dictionary<string, object?> { ["qrToken"] = qrToken });

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("INVALID_TOKEN", rejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
    }

    [Fact]
    public async Task CF0907_CrossTenantTicketSearchDoesNotLeak()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_user"))
        {
            await fixture.Service.StartParticipationAsync(Booking("event-tenant", "buyer-tenant"), null, "tenant-a-key");
        }

        using (fixture.PushContext("northwind", "tenant_user"))
        {
            var tickets = await fixture.Service.GetTicketsAsync();
            Assert.True(tickets.IsSuccess, tickets.Error?.Message);
            Assert.Empty(tickets.Data?.Tickets ?? []);
        }
    }

    [Fact]
    public async Task CF903_OptimisticConcurrency_AllowsOneSeatWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var first = fixture.Service.StartParticipationAsync(Booking("event-race", "buyer-race-a", "A1", simulateSlowWrite: true), "v1", "race-a");
        var second = fixture.Service.StartParticipationAsync(Booking("event-race", "buyer-race-b", "A1", simulateSlowWrite: true), "v1", "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF904_ThreeRetriesWithSameIdempotencyKey_ReturnCachedTicket()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.StartParticipationAsync(Booking("event-retry", "buyer-retry"), null, "retry-key");
        var second = await fixture.Service.StartParticipationAsync(Booking("event-retry", "buyer-retry"), null, "retry-key");
        var third = await fixture.Service.StartParticipationAsync(Booking("event-retry", "buyer-retry"), null, "retry-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(200, third.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["ticketId"], second.Data?.Body["ticketId"]);
        Assert.Equal(second.Data?.Body["ticketId"], third.Data?.Body["ticketId"]);
    }

    [Fact]
    public async Task CF905_BoundaryValidationRejectsMissingEventAndUserWithoutState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.StartParticipationAsync(new Dictionary<string, object?> { ["amount"] = 100 }, null, "invalid-key");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.SeatReservations, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF906_FabricTimeoutReturns504AndRetainsOutboxRecovery()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Booking("event-timeout", "buyer-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.StartParticipationAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == TransactionalEventParticipationConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF907_ConcurrencyDuplicateCoverage_UsesTenantScopedSeatKey()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("northwind", "tenant_user");
        var first = fixture.Service.StartParticipationAsync(Booking("event-race-2", "buyer-a", "B1", simulateSlowWrite: true), "v1", "race-2-a");
        var second = fixture.Service.StartParticipationAsync(Booking("event-race-2", "buyer-b", "B1", simulateSlowWrite: true), "v1", "race-2-b");

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF908_IdempotencyDuplicateCoverage_StoresOneTicketAndPayment()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        await fixture.Service.StartParticipationAsync(Booking("event-one-row", "buyer-one-row"), null, "one-row-key");
        await fixture.Service.StartParticipationAsync(Booking("event-one-row", "buyer-one-row"), null, "one-row-key");

        Assert.Single((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Payments, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF909_ZeroAmountIsAllowedButNegativeAmountFails()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var free = Booking("event-free", "buyer-free");
        free["amount"] = 0;
        var negative = Booking("event-negative", "buyer-negative");
        negative["amount"] = -1;

        var freeResult = await fixture.Service.StartParticipationAsync(free, null, "free-key");
        var negativeResult = await fixture.Service.StartParticipationAsync(negative, null, "negative-key");

        Assert.Equal(201, freeResult.Data?.HttpStatus);
        Assert.Equal(400, negativeResult.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", negativeResult.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF910_TimeoutDuplicateCoverage_DoesNotStoreTicket()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var timeout = Booking("event-timeout-repeat", "buyer-timeout-repeat");
        timeout["simulateFabricTimeout"] = true;

        await fixture.Service.StartParticipationAsync(timeout, null, "timeout-repeat-key");
        await fixture.Service.StartParticipationAsync(timeout, null, "timeout-repeat-key");

        Assert.Empty((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Tickets, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF911_SameSeatIdAcrossTenantsIsAllowed()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_user"))
        {
            var first = await fixture.Service.StartParticipationAsync(Booking("event-shared-seat", "buyer-a", "S1"), null, "tenant-a-seat");
            Assert.Equal(201, first.Data?.HttpStatus);
        }

        using (fixture.PushContext("northwind", "tenant_user"))
        {
            var second = await fixture.Service.StartParticipationAsync(Booking("event-shared-seat", "buyer-b", "S1"), null, "tenant-b-seat");
            Assert.Equal(201, second.Data?.HttpStatus);
        }
    }

    [Fact]
    public async Task CF912_ThreeRefundRetriesWithSameKey_ReturnCachedEscalation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.RequestRefundAsync(new Dictionary<string, object?> { ["ticketId"] = "ticket-cache", ["attempt"] = 3, ["maxAttempts"] = 3 }, "refund-cache-key");
        var second = await fixture.Service.RequestRefundAsync(new Dictionary<string, object?> { ["ticketId"] = "ticket-cache", ["attempt"] = 3, ["maxAttempts"] = 3 }, "refund-cache-key");
        var third = await fixture.Service.RequestRefundAsync(new Dictionary<string, object?> { ["ticketId"] = "ticket-cache", ["attempt"] = 3, ["maxAttempts"] = 3 }, "refund-cache-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(200, third.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["refundId"], second.Data?.Body["refundId"]);
        Assert.Equal(second.Data?.Body["refundId"], third.Data?.Body["refundId"]);
    }

    [Fact]
    public async Task Analytics_AggregatesCountsWithoutParticipantIdentifiers()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Booking("event-analytics", "buyer-analytics");
        request["connectionsViewed"] = 10;
        request["connectionsRequested"] = 4;
        request["participantIds"] = new[] { "buyer-analytics" };

        await fixture.Service.StartParticipationAsync(request, null, "analytics-key");
        var analytics = await fixture.Service.GetAnalyticsAsync();

        Assert.True(analytics.IsSuccess, analytics.Error?.Message);
        Assert.Equal(1, analytics.Data?.TicketCount);
        Assert.False(analytics.Data?.ParticipantIdsStoredInAggregate);
    }

    [Fact]
    public async Task Waitlist_WhenInventoryIsGone_CanStorePendingEntry()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var waitlist = await fixture.Service.JoinWaitlistAsync(new Dictionary<string, object?> { ["eventId"] = "event-waitlist", ["userId"] = "buyer-waitlist" }, "waitlist-key");

        Assert.Equal(201, waitlist.Data?.HttpStatus);
        Assert.Equal("PENDING", waitlist.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(TransactionalEventParticipationConstants.Waitlist, Tenant("acme-corp"))).Data ?? []);
    }

    private static Dictionary<string, object?> Booking(
        string eventId,
        string userId,
        string seatId = "general",
        bool simulateSlowWrite = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["participationId"] = $"part-{eventId}-{userId}",
            ["eventId"] = eventId,
            ["userId"] = userId,
            ["seatId"] = seatId,
            ["amount"] = 125,
            ["currency"] = "USD",
            ["seats"] = 1,
            ["eligible"] = true,
            ["capacityAvailable"] = 4,
            ["paymentMethod"] = "card",
            ["ticketType"] = "general"
        };

        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        return request;
    }

    private static Dictionary<string, object?> Tenant(string tenantId) => new() { ["tenant_id"] = tenantId };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            TransactionalEventParticipationService service,
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
        public TransactionalEventParticipationService Service { get; }
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
            var service = new TransactionalEventParticipationService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<TransactionalEventParticipationService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
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
