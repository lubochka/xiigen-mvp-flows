namespace Xiigen.Flow.CompletionGamification.Application;

public static class CompletionGamificationBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-05-1", "SERVER_AUTHORITY", "PointsCalculator derives earned points server-side from questionnaireResult.scorePercent; earnedPoints is never accepted from the request body."),
        Rule("CF-05-2", "TIMEZONE_STREAK", "StreakManager requires userTimezoneOffset and computes the learner local date from UTC plus offset minutes."),
        Rule("CF-05-3", "PRIVACY_GATE", "Social sharing is emitted only after achievement unlock and explicit learner sharing consent."),
        Rule("CF-05-4", "SCOPE_ISOLATION", "Completion, ledger, streak, achievement, social, peer-grade, learning-completion, and outbox records are isolated by tenant context."),
        Rule("CF-867", "CONCURRENCY_CONSTRAINT", "Concurrent completion writes for the same lesson and learner allow one winner and return CONCURRENT_UPDATE to the loser."),
        Rule("CF-868", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached completion response without duplicate documents or outbox side effects."),
        Rule("CF-869", "VALIDATION_CONSTRAINT", "Boundary validation rejects missing primary completion fields and leaves no partial write."),
        Rule("CF-870", "DNA8_ORDERING", "Downstream fabric timeout returns 504 retry hint while retaining an outbox record for deduplicated retry."),
        Rule("CF-871", "CONCURRENCY_CONSTRAINT", "Optimistic version checks reject stale completion updates with CONCURRENT_UPDATE."),
        Rule("CF-872", "IDEMPOTENCY_CONSTRAINT", "Repeated reward calculation with the same idempotency key replays the cached result."),
        Rule("CF-873", "VALIDATION_CONSTRAINT", "Invalid score, lesson, learner, or timezone values return field-level validation errors."),
        Rule("CF-874", "DNA8_ORDERING", "Partial queue failure keeps stored state and an outbox retry record rather than losing reward work."),
        Rule("CF-875", "CONCURRENCY_CONSTRAINT", "Duplicate completion submissions for an already-completed lesson never double-award points."),
        Rule("CF-876", "IDEMPOTENCY_CONSTRAINT", "Social-share and peer-grade mutations deduplicate by operation key."),
        Rule("CF-877", "VALIDATION_CONSTRAINT", "Zero, null, or empty primary inputs are rejected before any completion state is written.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            CompletionGamificationConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
