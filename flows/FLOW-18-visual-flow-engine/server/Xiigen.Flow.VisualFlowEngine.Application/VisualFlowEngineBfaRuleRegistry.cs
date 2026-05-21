namespace Xiigen.Flow.VisualFlowEngine.Application;

public static class VisualFlowEngineBfaRuleRegistry
{
    public static IReadOnlyList<VisualFlowEngineBfaRule> Rules { get; } =
    [
        Rule("CF-18-1", "CANVAS_DRAFT_GATE", "T617 performs BOLA before state reads, rejects published flow edits, writes the draft canvas, writes audit, then emits FlowCanvasUpdated."),
        Rule("CF-18-2", "PUBLICATION_GRAPH_GATE", "T618 performs DFS cycle detection before edge type compatibility, uses optimistic concurrency for DRAFT to PUBLISHED, writes audit, then emits FlowPublished."),
        Rule("CF-18-3", "NODE_REGISTRATION_ATOMICITY", "T619 acquires a SETNX-equivalent node type lock before both writes and releases the lock on any failed dual-write."),
        Rule("CF-18-4", "CODE_INJECTION_AUDIT_LOCK", "T620 acquires a version lock, writes a rollback audit before injection, writes append-only result audit, then emits CodeInjected."),
        Rule("CF-957", "CONCURRENCY_CONSTRAINT", "Concurrent writes to the same visual flow resource use optimistic concurrency; one request wins and another receives CONCURRENT_UPDATE."),
        Rule("CF-958", "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached response and creates no duplicate canvas, audit, lock, injection, or event side effects."),
        Rule("CF-959", "VALIDATION_CONSTRAINT", "Empty, null, or zero primary fields return field-level validation errors with no partial write."),
        Rule("CF-960", "DNA8_TIMEOUT_RECOVERY", "Fabric timeout returns TIMEOUT, retains a recovery outbox event, and leaves retryable state for the queue consumer.")
    ];

    private static VisualFlowEngineBfaRule Rule(string id, string type, string description) =>
        new(id, VisualFlowEngineConstants.FlowId, type, description, "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID");
}
