namespace Xiigen.Flow.UserGroupsCommunities.Application;

public static class UserGroupsCommunitiesBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-06-1", "ACCESS_CONTROL_BEFORE_CONTENT", "Content seeding is triggered only after MembershipActivated; GroupJoinRequested never seeds group feed content."),
        Rule("CF-06-2", "TIER_FROM_SUBSCRIPTION", "Membership tier is resolved server-side from subscription records or FREEDOM defaults, never from join request payload."),
        Rule("CF-06-3", "MEMBERSHIP_SETNX_IDEMPOTENCY", "Composite tenant/user/group duplicate membership requests return the existing membership response rather than ALREADY_MEMBER failure."),
        Rule("CF-06-4", "SCOPE_ISOLATION", "Membership, approval, feed, tier change, access log, analytics, and outbox records are tenant-private."),
        Rule("CF-878", "CONCURRENCY_CONSTRAINT", "Concurrent membership mutations for the same tenant/user/group allow one winner and return CONCURRENT_UPDATE with currentVersion to the loser."),
        Rule("CF-879", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns HTTP 200 cached response without duplicate documents or outbox side effects."),
        Rule("CF-880", "VALIDATION_CONSTRAINT", "Empty, null, or zero primary fields return VALIDATION_FAILED and leave no partial membership write."),
        Rule("CF-881", "DNA8_ORDERING", "Timeout or downstream fabric failure returns 504 TIMEOUT with retryAfterMs while retaining an outbox row for retry."),
        Rule("CF-882", "CONCURRENCY_CONSTRAINT", "Duplicate concurrent approval and join paths use the same tenant/user/group concurrency guard."),
        Rule("CF-883", "IDEMPOTENCY_CONSTRAINT", "Join, approval, tier, and leave operations deduplicate by tenant-scoped operation key.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            UserGroupsCommunitiesConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
