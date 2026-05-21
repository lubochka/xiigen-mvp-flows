namespace Xiigen.Flow.MarketplacePayments.Application;

public static class MarketplacePaymentsBfaRuleRegistry
{
    public static IReadOnlyList<MarketplacePaymentsBfaRule> Rules { get; } =
    [
        Rule("CF-16-1", "ORDERING_CONSTRAINT", "Checkout validates buyer ownership before lock, takes a SETNX cart lock, reserves inventory with optimistic concurrency, audits, then emits checkout reserved."),
        Rule("CF-16-2", "PAYMENT_ATOMICITY", "Payment capture is idempotent, platform and seller splits sum to the charged total, PII is scrubbed, audit is append-only, and escrow is held before order confirmation."),
        Rule("CF-16-3", "ESCROW_STATE_MACHINE", "Escrow transitions stay in HELD, RELEASED, DISPUTED, REFUNDED states; compensation runs refund before inventory restore; payout stores only vault references."),
        Rule("CF-16-4", "SCOPE_ISOLATION", "All checkout, payment, escrow, payout, audit, refund, and dashboard reads use tenant context rather than request body tenant ids."),
        Rule("CF-262", "TWO_ACTOR_CONSTRAINT", "Buyer dispute synchronously freezes seller payouts before the dispute event is emitted."),
        Rule("CF-265", "TWO_ACTOR_NOTIFICATION", "Payout hold notification is written synchronously in the same tenant-scoped operation that freezes seller payout release."),
        Rule("CF-268", "READ_ONLY_SOCIAL_SIGNAL", "Marketplace payments cannot import or mutate social graph services; review and social signals remain read-only external inputs."),
        Rule("CF-949", "CONCURRENCY_CONSTRAINT", "Concurrent checkout writes use optimistic concurrency; one request wins and another receives CONCURRENT_UPDATE."),
        Rule("CF-950", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached response and creates no duplicate charge, escrow, audit, or event."),
        Rule("CF-951", "VALIDATION_CONSTRAINT", "Empty, null, or zero primary checkout fields return field-level validation errors with no partial write."),
        Rule("CF-952", "DNA8_ORDERING", "Fabric timeout returns TIMEOUT and retains a recovery outbox record for retry.")
    ];

    private static MarketplacePaymentsBfaRule Rule(string id, string type, string description) =>
        new(id, MarketplacePaymentsConstants.FlowId, type, description, "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID");
}
