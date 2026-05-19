namespace Xiigen.Flow.FriendRequestSocialFeed.Application;

public static class FriendRequestSocialFeedBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-07-1", "INLINE_ONLY_SERVICE", "PrivacyGatekeeper is called inline by request, generation, and delivery paths; no queue-driven privacy checks are used."),
        Rule("CF-07-2", "BIDIRECTIONAL_ATOMIC_WRITE", "Accepted friend requests write A-to-B and B-to-A connection adjacency records under one connection id before any event is emitted."),
        Rule("CF-07-3", "SCORE_ZERO_PASSTHROUGH", "Feed scoring preserves score zero as a valid item score and still emits the scored event."),
        Rule("CF-07-4", "SCOPE_ISOLATION", "Request, connection, feed, notification, and analytics records are tenant-private; aggregate analytics carries no per-user identifiers."),
        Rule("CF-884", "CONCURRENCY_CONSTRAINT", "Concurrent friend request mutations for the same tenant/sender/recipient allow one winner and return CONCURRENT_UPDATE to the loser."),
        Rule("CF-885", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns HTTP 200 cached response without duplicate request, connection, feed, or outbox side effects."),
        Rule("CF-886", "VALIDATION_CONSTRAINT", "Empty, null, or zero primary fields return VALIDATION_FAILED with field details and leave no partial write."),
        Rule("CF-887", "DNA8_ORDERING", "Timeout or downstream fabric failure returns 504 TIMEOUT with retryAfterMs while retaining an outbox row for retry."),
        Rule("CF-888", "CONCURRENCY_CONSTRAINT", "The same optimistic concurrency rule applies to repeated acceptance paths for an existing request."),
        Rule("CF-889", "IDEMPOTENCY_CONSTRAINT", "Acceptance idempotency deduplicates by tenant-scoped operation key and returns the cached connection response."),
        Rule("CF-890", "VALIDATION_CONSTRAINT", "Acceptance validation rejects empty request and actor identifiers before graph writes."),
        Rule("CF-891", "DNA8_ORDERING", "Feed integration timeout retains the outbox event and never removes the established connection."),
        Rule("CF-892", "CONCURRENCY_CONSTRAINT", "Feed delivery and scoring preserve version checks for same feed item writes."),
        Rule("CF-893", "IDEMPOTENCY_CONSTRAINT", "Feed delivery idempotency prevents duplicate feed inserts for the same connection and viewer."),
        Rule("CF-894", "VALIDATION_CONSTRAINT", "Feed and analytics endpoints reject empty connection or viewer identifiers with VALIDATION_FAILED.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            FriendRequestSocialFeedConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
