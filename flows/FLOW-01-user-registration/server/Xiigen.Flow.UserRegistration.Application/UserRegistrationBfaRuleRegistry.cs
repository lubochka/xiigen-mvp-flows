namespace Xiigen.Flow.UserRegistration.Application;

public static class UserRegistrationBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-01-1", "ORDERING_CONSTRAINT", "Email uniqueness is checked before a user registration record is created."),
        Rule("CF-01-2", "RATE_LIMIT_CONSTRAINT", "Resend verification is rate-limited by a MACHINE rule with window duration from FREEDOM config."),
        Rule("CF-01-3", "DNA8_ORDERING", "Verification token document is stored before VerificationEmailRequested is emitted."),
        Rule("CF-01-4", "SCOPE_ISOLATION", "Every registration, verification, resend, and onboarding record is tenant-scoped."),
        Rule("CF-850", "CONCURRENCY_CONSTRAINT", "Concurrent registration on the same normalized email returns one success and one CONCURRENT_UPDATE response."),
        Rule("CF-851", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached registration response with no duplicate side effects."),
        Rule("CF-852", "ORDERING_CONSTRAINT", "Boundary validation returns VALIDATION_FAILED with no partial user registration write."),
        Rule("CF-853", "DNA8_ORDERING", "Downstream timeout returns TIMEOUT while retaining an outbox record for retry.")
    ];

    private static BfaRuleDefinition Rule(string ruleId, string type, string description) =>
        new(
            ruleId,
            UserRegistrationConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
