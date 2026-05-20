namespace Xiigen.Flow.TransactionalEventParticipation.Application;

public static class TransactionalEventParticipationConstants
{
    public const string FlowId = "FLOW-09";
    public const string FlowSlug = "transactional-event-participation";
    public const string DynamicEndpoint = "/api/dynamic/transactional-event-participation";

    public const string SeatReservations = "xiigen-transactional-event-seat-reservations";
    public const string Payments = "xiigen-transactional-event-payments";
    public const string Tickets = "xiigen-transactional-event-tickets";
    public const string Refunds = "xiigen-transactional-event-refunds";
    public const string Waitlist = "xiigen-transactional-event-waitlist";
    public const string TokenRedemptions = "xiigen-transactional-event-token-redemptions";
    public const string Analytics = "xiigen-transactional-event-analytics";
    public const string ComplianceQueue = "xiigen-transactional-event-compliance-queue";
    public const string GroupBookings = "xiigen-transactional-event-group-bookings";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-09-1",
        "CF-09-2",
        "CF-09-3",
        "CF-09-4",
        "CF-09-5",
        "CF-09-6",
        "CF-09-7",
        "CF-903",
        "CF-904",
        "CF-905",
        "CF-906",
        "CF-907",
        "CF-908",
        "CF-909",
        "CF-910",
        "CF-911",
        "CF-912"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "business_partner",
        "event_organiser",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public const string ParticipationRequestedEvent = "xiigen.transactional-event.participation-requested.v1";
    public const string EligibilityPassedEvent = "xiigen.transactional-event.eligibility-passed.v1";
    public const string EligibilityFailedEvent = "xiigen.transactional-event.eligibility-failed.v1";
    public const string InventoryReservedEvent = "xiigen.transactional-event.inventory-reserved.v1";
    public const string InventoryExhaustedEvent = "xiigen.transactional-event.inventory-exhausted.v1";
    public const string FraudCheckFailedEvent = "xiigen.transactional-event.fraud-check-failed.v1";
    public const string PaymentClearedEvent = "xiigen.transactional-event.payment-cleared.v1";
    public const string PaymentFailedEvent = "xiigen.transactional-event.payment-failed.v1";
    public const string TicketIssuedEvent = "xiigen.transactional-event.ticket-issued.v1";
    public const string TokenIssuedEvent = "xiigen.transactional-event.token-issued.v1";
    public const string TokenRedeemedEvent = "xiigen.transactional-event.token-redeemed.v1";
    public const string RefundProcessedEvent = "xiigen.transactional-event.refund-processed.v1";
    public const string RefundFailedEvent = "xiigen.transactional-event.refund-failed.v1";
    public const string ComplianceReviewRequestedEvent = "xiigen.transactional-event.compliance-review-requested.v1";
    public const string AnalyticsRecordedEvent = "xiigen.transactional-event.analytics-recorded.v1";
    public const string TimeoutRetainedEvent = "xiigen.transactional-event.timeout-retained.v1";
    public const string WaitlistJoinedEvent = "xiigen.transactional-event.waitlist-joined.v1";
}
