namespace Xiigen.Flow.EventAttendance.Application;

public static class EventAttendanceBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-04-1", "DNA8_ORDERING", "RSVP creation is one atomic state write before any RSVP outbox event is appended."),
        Rule("CF-04-2", "ORDERING_CONSTRAINT", "Waitlist promotion uses FIFO join_timestamp order and conditional WAITLISTED to CONFIRMED update."),
        Rule("CF-04-3", "TRIGGER_CONSTRAINT", "Feedback windows open only from an event.ended CloudEvent trigger, never from timer, HTTP, or manual trigger."),
        Rule("CF-04-4", "SCOPE_ISOLATION", "RSVP, waitlist, check-in, feedback, event-source, and outbox operations are isolated by tenant context."),
        Rule("CF-863", "CONCURRENCY_CONSTRAINT", "Concurrent RSVP writes for the same event and attendee allow one winner and return CONCURRENT_UPDATE to the loser."),
        Rule("CF-864", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached RSVP response without duplicate documents or outbox side effects."),
        Rule("CF-865", "VALIDATION_CONSTRAINT", "Boundary validation rejects missing primary RSVP fields and leaves no partial write."),
        Rule("CF-866", "DNA8_ORDERING", "Downstream fabric timeout returns 504 retry hint while retaining an outbox record for deduplicated retry.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            EventAttendanceConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
