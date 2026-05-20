namespace Xiigen.Flow.TransactionalEventParticipation.Application;

public static class TransactionalEventParticipationBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-09-1", "SEAT_BEFORE_PAYMENT", "Reserve a TTL seat hold before payment capture begins."),
        Rule("CF-09-2", "FAIL_OPEN", "Fraud service outage allows purchase to proceed and emits an audit event."),
        Rule("CF-09-3", "COMPLIANCE_ESCALATION_ON_EXHAUSTION", "Refund retry exhaustion emits RefundFailed and pushes manual review."),
        Rule("CF-09-4", "ALL_OR_NOTHING_GROUP", "Group ticket coordination succeeds for every member or rolls back the whole group."),
        Rule("CF-09-5", "INLINE_PURE", "Fee calculation returns a FeeBreakdown only and writes no records."),
        Rule("CF-09-6", "PLATFORM_ONLY_QR", "QR token generation and validation are platform-only boundaries."),
        Rule("CF-09-7", "SCOPE_ISOLATION", "All private payment, ticket, refund, waitlist, and redemption records carry tenant scope."),
        Rule("CF-903", "P10_CONCURRENCY", "Concurrent participation mutations split into one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-904", "P10_IDEMPOTENCY", "Repeated calls with one idempotency key replay the cached participation response."),
        Rule("CF-905", "P10_VALIDATION", "Blank or invalid participation fields return VALIDATION_FAILED without partial writes."),
        Rule("CF-906", "P10_TIMEOUT", "Downstream timeout returns 504 while retaining the outbox recovery record."),
        Rule("CF-907", "P10_CONCURRENCY_DUPLICATE", "Duplicate concurrency coverage uses the same tenant-scoped event seat key."),
        Rule("CF-908", "P10_IDEMPOTENCY_DUPLICATE", "Duplicate idempotency coverage stores one ticket and one payment."),
        Rule("CF-909", "P10_VALIDATION_DUPLICATE", "Zero amount is valid for free events; negative amount is rejected."),
        Rule("CF-910", "P10_TIMEOUT_DUPLICATE", "Timeout duplicate coverage keeps recovery state and no ticket."),
        Rule("CF-911", "P10_CONCURRENCY_THIRD", "Third concurrency coverage protects cross-tenant seat isolation."),
        Rule("CF-912", "P10_IDEMPOTENCY_THIRD", "Third idempotency coverage guarantees three retries return one cached result.")
    ];

    private static BfaRuleDefinition Rule(string id, string type, string description) => new(
        id,
        TransactionalEventParticipationConstants.FlowId,
        type,
        description,
        "BUILD_FAILURE",
        "FLOW_SCOPED",
        "GLOBAL",
        "MASTER_TENANT_ID");
}
