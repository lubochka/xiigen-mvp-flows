namespace Xiigen.Flow.FreelancerMarketplace.Application;

public static class FreelancerMarketplaceBfaRuleRegistry
{
    public static IReadOnlyList<FreelancerMarketplaceBfaRule> Rules { get; } =
    [
        Rule("CF-17-1", "ORDERING_CONSTRAINT", "T613 validates gig ownership before lock, takes an exclusive gig acceptance lock, validates bid status with optimistic concurrency, writes audit, then emits GigAccepted."),
        Rule("CF-17-2", "CONTRACT_IMMUTABILITY", "T614 rejects immutable contract field writes from compile-time constants, validates milestone sum, writes contract with optimistic concurrency, writes audit, then emits FreelancerContractActivated."),
        Rule("CF-17-3", "ESCROW_STATE_MACHINE", "T615 validates submitted delivery before release, uses status updates instead of delete, disputes move no funds, and compensation executes in LIFO order."),
        Rule("CF-17-4", "REVIEW_APPEND_ONLY", "T616 accepts only two review directions, rejects duplicate direction per engagement, writes reviews append-only, and excludes comments from platform audit."),
        Rule("CF-286", "TENANT_IDENTITY_PLANE", "Freelancer gig, contract, delivery, review, and audit records use the current TenantContext rather than request body tenant ids."),
        Rule("CF-287", "DWH_READ_ONLY", "FLOW-17 analytics exports are outbox events only; FLOW-17 never mutates FLOW-14 warehouse tables directly."),
        Rule("CF-288", "SEARCH_FABRIC_REUSE", "Freelancer search uses flow-local indexes and fabric interfaces; no direct search SDK or foreign index ownership."),
        Rule("CF-289", "NOTIFICATION_DEDUP", "Failure, dispute, and review notification events are emitted after durable audit and idempotency gates."),
        Rule("CF-290", "COMPLIANCE_AUDIT_SHARED", "Freelancer audit payloads are hashable, PII-aware, and compatible with platform compliance audit readers."),
        Rule("CF-291", "TENANT_SCOPE_ISOLATION", "Every FLOW-17 read and write includes tenant scope from platform context."),
        Rule("CF-953", "CONCURRENCY_CONSTRAINT", "Concurrent writes to the same gig or contract use optimistic concurrency; one request wins and another receives CONCURRENT_UPDATE."),
        Rule("CF-954", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached response and creates no duplicate locks, contract writes, audits, or events."),
        Rule("CF-955", "VALIDATION_CONSTRAINT", "Empty, null, or zero primary fields return field-level validation errors with no partial write."),
        Rule("CF-956", "DNA8_TIMEOUT_RECOVERY", "Fabric timeout returns TIMEOUT, retains recovery outbox event, and leaves retryable state for the queue consumer.")
    ];

    private static FreelancerMarketplaceBfaRule Rule(string id, string type, string description) =>
        new(id, FreelancerMarketplaceConstants.FlowId, type, description, "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID");
}
