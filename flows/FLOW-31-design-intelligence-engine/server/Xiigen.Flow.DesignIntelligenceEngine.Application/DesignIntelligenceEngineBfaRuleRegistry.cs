namespace Xiigen.Flow.DesignIntelligenceEngine.Application;

public static class DesignIntelligenceEngineBfaRuleRegistry
{
    public static readonly IReadOnlyList<DesignIntelligenceEngineBfaRule> Rules =
    [
        new("CF-1015", DesignIntelligenceEngineConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Concurrent design intelligence writes require an If-Match version and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1016", DesignIntelligenceEngineConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated design intelligence mutations with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1017", DesignIntelligenceEngineConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value design intelligence inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1018", DesignIntelligenceEngineConstants.FlowId, "OUTBOX_TIMEOUT_RETENTION", "Downstream timeout returns 504 while retaining an outbox record for retry.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
