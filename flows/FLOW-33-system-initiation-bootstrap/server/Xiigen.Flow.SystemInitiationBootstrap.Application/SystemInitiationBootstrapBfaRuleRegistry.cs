namespace Xiigen.Flow.SystemInitiationBootstrap.Application;

public static class SystemInitiationBootstrapBfaRuleRegistry
{
    public static readonly IReadOnlyList<SystemInitiationBootstrapBfaRule> Rules =
    [
        new("CF-1023", SystemInitiationBootstrapConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Bootstrap registry writes require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1024", SystemInitiationBootstrapConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated bootstrap mutations with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1025", SystemInitiationBootstrapConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value bootstrap plan inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1026", SystemInitiationBootstrapConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream bootstrap fabric timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
