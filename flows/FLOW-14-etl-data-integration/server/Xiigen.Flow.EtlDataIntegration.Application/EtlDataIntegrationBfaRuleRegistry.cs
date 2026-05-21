namespace Xiigen.Flow.EtlDataIntegration.Application;

public static class EtlDataIntegrationBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRule> Rules =
    [
        Rule("CF-940", "P10_PRIMARY_CONCURRENCY", "Concurrent ETL pipeline publication for the same pipeline allows one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-941", "P10_PRIMARY_IDEMPOTENCY", "Repeated ETL pipeline publication with one idempotency key replays the cached response and creates no duplicate landing records, audits, or events."),
        Rule("CF-942", "P10_PRIMARY_VALIDATION", "Malformed ETL pipeline publication returns field-level validation errors and writes no partial ETL state."),
        Rule("CF-943", "P10_PRIMARY_TIMEOUT", "A downstream fabric timeout returns 504 with retryAfterMs and retains a recovery outbox record without publishing the pipeline."),
        Rule("CF-944", "P10_SECONDARY_CONCURRENCY", "Stale or colliding ETL pipeline writes fail with CONCURRENT_UPDATE and expose the current version.")
    ];

    private static BfaRule Rule(string id, string type, string description) =>
        new(
            EtlDataIntegrationConstants.FlowId,
            id,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
