namespace Xiigen.Flow.ReviewsReputation.Application;

public static class ReviewsReputationBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-10-1", "ELIGIBILITY_FIRST", "Eligibility from attendance or purchase confirmation is checked before validation, idempotency, audit, or review writes."),
        Rule("CF-10-2", "THREE_PATH_MODERATION", "Moderation has exactly PASS, REJECT, and UNCERTAIN paths; uncertain reviews enter human review and are never auto-rejected."),
        Rule("CF-10-3", "PUBLISHED_ONLY_REPUTATION", "Reputation aggregates use published reviews only, apply recency weights, clamp the score, and recalculate after retraction."),
        Rule("CF-10-4", "OWNER_RESPONSE_BOUNDARY", "Owner responses require ownership, allow one response per review, permit revision after content-policy rejection, and never update reputation."),
        Rule("CF-913", "P10_CONCURRENCY", "Concurrent review submissions split into one accepted winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-914", "P10_IDEMPOTENCY", "Repeated submissions with one idempotency key replay the cached review response without duplicate side effects."),
        Rule("CF-915", "P10_VALIDATION", "Boundary validation returns field-level details and writes no review, audit, outbox, or idempotency success state."),
        Rule("CF-916", "P10_TIMEOUT", "A simulated fabric timeout returns 504 and retains an outbox recovery record without partial review state."),
        Rule("CF-917", "P10_SECONDARY_CONCURRENCY", "Concurrent owner response or retraction mutations split into one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-918", "P10_SECONDARY_IDEMPOTENCY", "Repeated score recomputation with one key returns the cached aggregate and writes one score version.")
    ];

    private static BfaRuleDefinition Rule(string id, string type, string description) => new(
        id,
        ReviewsReputationConstants.FlowId,
        type,
        description,
        "BUILD_FAILURE",
        "FLOW_SCOPED",
        "GLOBAL",
        "MASTER_TENANT_ID");
}
