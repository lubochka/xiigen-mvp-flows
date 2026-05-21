namespace Xiigen.Flow.DurableSagasCompliance.Application;

public static class DurableSagasComplianceBfaRuleRegistry
{
    public static IReadOnlyList<DurableSagasComplianceBfaRule> Rules { get; } =
    [
        Rule("CF-19-1", "SAGA_PERSISTENCE_ORDER", "T621 persists saga state with OCC and acquires a step lock before any step body; checkpoints are stored before events."),
        Rule("CF-19-2", "COMPENSATION_ORDER", "T622 runs compensation serially in LIFO order with a comp-lock per step and stops on the first failure."),
        Rule("CF-19-3", "COMPLIANCE_IMMUTABILITY", "T623 writes append-only PLATFORM_ONLY compliance records with retentionExpiresAt and auditHash at write time."),
        Rule("CF-19-4", "RETENTION_DUAL_GATE", "T624 purges only after expiry and legal-hold gates pass, archives before delete, and uses a FREEDOM-driven schedule."),
        Rule("CF-961", "CONCURRENCY_CONSTRAINT", "POST /api/dynamic/durable-sagas-compliance allows one concurrent writer and returns CONCURRENT_UPDATE for the loser."),
        Rule("CF-962", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached response and creates no duplicate state, audit, or event side effects."),
        Rule("CF-963", "VALIDATION_CONSTRAINT", "Empty, null, or incomplete primary saga input returns VALIDATION_FAILED with no partial write."),
        Rule("CF-964", "DNA8_TIMEOUT_RECOVERY", "Fabric timeout returns TIMEOUT, keeps a recovery outbox record, and leaves state retryable.")
    ];

    private static DurableSagasComplianceBfaRule Rule(string id, string type, string description) =>
        new(
            id,
            DurableSagasComplianceConstants.FlowId,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
