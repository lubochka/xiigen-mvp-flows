namespace Xiigen.Flow.AdsPlatform.Application;

public static class AdsPlatformBfaRuleRegistry
{
    public static readonly IReadOnlyList<AdsPlatformBfaRule> Rules =
    [
        Rule("CF-20-1", "ConsentGateEnforcer must run as unconditional order-1 check before auction or billing side effects."),
        Rule("CF-20-2", "AuctionBidProcessor must acquire a SETNX-equivalent bid lock and adjust budget atomically."),
        Rule("CF-20-3", "FraudPreBillingValidator must run before billing and restore budget on fraud rejection with zero raw payment data persisted."),
        Rule("CF-20-4", "PoliticalContentReviewer must use dual-gate score convergence and route ambiguous or divergent content to human review."),
        Rule("CF-965", "Concurrent writes to the same ad-auction resource must produce one accepted response and one CONCURRENT_UPDATE conflict."),
        Rule("CF-966", "Repeated X-Idempotency-Key submissions must replay the cached response without duplicate state or outbox effects."),
        Rule("CF-967", "Empty, null, or zero primary inputs must return field-level validation errors and leave no partial write."),
        Rule("CF-968", "Downstream fabric timeout must return TIMEOUT, retain recovery outbox intent, and avoid partial business state.")
    ];

    private static AdsPlatformBfaRule Rule(string id, string description) =>
        new(
            id,
            AdsPlatformConstants.FlowId,
            "functional-coverage",
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
