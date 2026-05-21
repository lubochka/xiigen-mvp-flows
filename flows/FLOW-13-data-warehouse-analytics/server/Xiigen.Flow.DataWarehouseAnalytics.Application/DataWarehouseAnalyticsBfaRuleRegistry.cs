namespace Xiigen.Flow.DataWarehouseAnalytics.Application;

public static class DataWarehouseAnalyticsBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRule> Rules =
    [
        Rule("CF-935", "P10_PRIMARY_CONCURRENCY", "Concurrent warehouse plan publication for the same plan allows one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-936", "P10_PRIMARY_IDEMPOTENCY", "Repeated warehouse plan publication with one idempotency key replays the cached response and creates no duplicate facts, audits, or events."),
        Rule("CF-937", "P10_PRIMARY_VALIDATION", "Malformed warehouse plan publication returns field-level validation errors and writes no partial warehouse state."),
        Rule("CF-938", "P10_PRIMARY_TIMEOUT", "A downstream fabric timeout returns 504 with retryAfterMs and retains a recovery outbox record without publishing the plan."),
        Rule("CF-939", "P10_SECONDARY_CONCURRENCY", "Stale or colliding warehouse plan writes fail with CONCURRENT_UPDATE and expose the current version.")
    ];

    private static BfaRule Rule(string id, string type, string description) =>
        new(
            DataWarehouseAnalyticsConstants.FlowId,
            id,
            type,
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
