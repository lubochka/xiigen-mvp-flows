namespace Xiigen.Flow.SubscriptionBilling.Application;

public static class SubscriptionBillingBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRule> Rules =
    [
        Rule("CF-12-1", "CANCELLED_STATUS_VOIDING", "Cancelled or paused subscriptions emit InvoiceVoided and never attempt payment capture."),
        Rule("CF-12-2", "INTEGER_CENTS_PRICE", "Plan prices must be positive integer cents; floating-point or zero prices are rejected before state mutation."),
        Rule("CF-12-3", "MRR_NORMALIZATION", "Recurring revenue analytics normalize monthly and annual plans through the approved deterministic formula and dunning schedule."),
        Rule("CF-12-4", "PAYMENT_TOKEN_VAULT_REFERENCE", "Subscription activation stores only payment method references and never persists raw payment tokens."),
        Rule("CF-929", "P10_PRIMARY_CONCURRENCY", "Concurrent plan publication for the same plan allows one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-930", "P10_PRIMARY_IDEMPOTENCY", "Repeated plan publication with one idempotency key replays the cached response and creates no duplicate side effects."),
        Rule("CF-931", "P10_PRIMARY_VALIDATION", "Malformed plan publication returns field-level validation errors and writes no partial plan, audit, or event state."),
        Rule("CF-932", "P10_PRIMARY_TIMEOUT", "A downstream write timeout returns 504 with retryAfterMs and retains a recovery outbox record without plan publication."),
        Rule("CF-933", "P10_SECONDARY_CONCURRENCY", "Concurrent invoice charging for the same subscription period allows one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-934", "P10_SECONDARY_IDEMPOTENCY", "Repeated invoice charging with one idempotency key replays the cached invoice response and does not double-charge.")
    ];

    private static BfaRule Rule(string id, string type, string description) =>
        new(
            SubscriptionBillingConstants.FlowId,
            id,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
