namespace Xiigen.Flow.EventManagement.Application;

public static class EventManagementBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-03-1", "DNA8_ORDERING", "EventCreated must be stored before emitted: the event document is committed before the outbox event is appended."),
        Rule("CF-03-2", "MACHINE_CONSTANT", "capacity null means unlimited registrations; capacity zero means closed/waitlist only; no falsy capacity checks."),
        Rule("CF-03-3", "ORDERING_CONSTRAINT", "Content safety must complete and pass before any promotion record or PromotionPublished event is emitted."),
        Rule("CF-03-4", "SCOPE_ISOLATION", "Every event, registration, promotion, analytics, and outbox operation is isolated by tenant context."),
        Rule("CF-858", "CONCURRENCY_CONSTRAINT", "Concurrent create writes for the same event resource allow one winner and return CONCURRENT_UPDATE to the loser."),
        Rule("CF-859", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached event creation response without duplicate documents or outbox side effects."),
        Rule("CF-860", "ORDERING_CONSTRAINT", "Boundary validation rejects missing or invalid primary event fields and leaves no partial write."),
        Rule("CF-861", "DNA8_ORDERING", "Downstream fabric timeout returns 504 retry hint while retaining an outbox record for deduplicated retry."),
        Rule("CF-862", "CONCURRENCY_CONSTRAINT", "Concurrent attendee capacity writes allow one winner and return CONCURRENT_UPDATE to the loser.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            EventManagementConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}

