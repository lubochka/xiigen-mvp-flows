namespace Xiigen.Flow.MetaArbitrationEngine.Application;

public static class MetaArbitrationEngineBfaRuleRegistry
{
    public static readonly IReadOnlyList<MetaArbitrationEngineBfaRule> Rules =
    [
        new("CF-1031", MetaArbitrationEngineConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Meta-arbitration conflict decisions require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1032", MetaArbitrationEngineConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated arbitration decisions with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1033", MetaArbitrationEngineConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value arbitration conflict inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1034", MetaArbitrationEngineConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream arbitration fabric timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}

