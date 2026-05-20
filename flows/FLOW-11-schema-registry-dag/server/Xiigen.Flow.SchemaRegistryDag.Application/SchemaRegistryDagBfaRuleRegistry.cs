namespace Xiigen.Flow.SchemaRegistryDag.Application;

public static class SchemaRegistryDagBfaRuleRegistry
{
    public static readonly IReadOnlyList<BfaRuleDefinition> Rules =
    [
        Rule("CF-11-1", "THREE_STATE_DFS", "DAG cycle detection must use WHITE, GRAY, and BLACK traversal states so diamonds pass and back edges fail."),
        Rule("CF-11-2", "OCC_PUBLISH_GATE", "Schema publication must store the version with an optimistic concurrency token before emitting publication events."),
        Rule("CF-11-3", "BREAKING_APPROVAL_GATE", "BREAKING schema changes must route to approval governance before publication."),
        Rule("CF-11-4", "SCOPE_ISOLATION", "All schema registry reads and writes are tenant-scoped from TenantContext; request bodies cannot supply tenant scope."),
        Rule("CF-919", "P10_PRIMARY_CONCURRENCY", "Concurrent schema registration against the same schema/version splits into one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-920", "P10_PRIMARY_IDEMPOTENCY", "Repeated schema registration with one idempotency key replays the cached response and creates no duplicate side effects."),
        Rule("CF-921", "P10_PRIMARY_VALIDATION", "Empty or malformed schema registration input returns field-level validation errors and writes no partial state."),
        Rule("CF-922", "P10_PRIMARY_TIMEOUT", "A downstream write timeout returns 504 and retains a recovery outbox record without partial schema publication."),
        Rule("CF-923", "P10_APPROVAL_CONCURRENCY", "Concurrent approval decisions for one pending schema split into one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-924", "P10_APPROVAL_IDEMPOTENCY", "Repeated approval decisions with one key replay the cached approval response without double publishing."),
        Rule("CF-925", "P10_VALIDATION_NO_SIDE_EFFECTS", "Schema validation failures return all errors and write only validation evidence, never a published schema."),
        Rule("CF-926", "P10_MIGRATION_TIMEOUT", "Migration timeout records the run before processing and emits a retained retry event."),
        Rule("CF-927", "P10_DEPRECATION_CONCURRENCY", "Concurrent deprecation of one schema version has one winner and one CONCURRENT_UPDATE loser."),
        Rule("CF-928", "P10_RENDER_IDEMPOTENCY", "DAG rendering is cache-aware and repeated render requests return the same topology without rebuilding side effects.")
    ];

    private static BfaRuleDefinition Rule(string id, string type, string description) => new(
        id,
        SchemaRegistryDagConstants.FlowId,
        type,
        description,
        "BUILD_FAILURE",
        "FLOW_SCOPED",
        "GLOBAL",
        "MASTER_TENANT_ID");
}
