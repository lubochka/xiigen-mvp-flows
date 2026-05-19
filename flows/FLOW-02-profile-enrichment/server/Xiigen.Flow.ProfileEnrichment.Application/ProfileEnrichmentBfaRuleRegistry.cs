namespace Xiigen.Flow.ProfileEnrichment.Application;

public static class ProfileEnrichmentBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-02-1", "DUAL_WRITE_CONSTRAINT", "Profile enrichment writes a private full profile and a global matching record with four match-safe fields."),
        Rule("CF-02-2", "MACHINE_CONSTANT", "Matching timeout is a success mode: partialResults true is returned in a successful response, never as a failure."),
        Rule("CF-02-3", "EVENT_NAME_COLLISION", "PersonalizationCompleted is a MACHINE string literal and FLOW-02 never emits FLOW-01's OnboardingCompleted event."),
        Rule("CF-02-4", "SCOPE_ISOLATION", "Every profile, matching, personalization, and outbox operation is isolated by tenant context."),
        Rule("CF-854", "CONCURRENCY_CONSTRAINT", "Concurrent profile-enrichment write for the same resource allows one winner and returns CONCURRENT_UPDATE to the loser."),
        Rule("CF-855", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached response without duplicate profile, matching, feed, or outbox side effects."),
        Rule("CF-856", "ORDERING_CONSTRAINT", "Boundary validation rejects empty, null, or zero primary input and leaves no partial write."),
        Rule("CF-857", "DNA8_ORDERING", "Downstream fabric timeout returns 504 retry hint while retaining an outbox record for deduplicated retry.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            ProfileEnrichmentConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
