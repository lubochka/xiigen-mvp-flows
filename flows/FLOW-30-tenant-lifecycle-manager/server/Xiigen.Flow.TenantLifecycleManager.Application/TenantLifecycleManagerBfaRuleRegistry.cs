namespace Xiigen.Flow.TenantLifecycleManager.Application;

public static class TenantLifecycleManagerBfaRuleRegistry
{
    public static readonly IReadOnlyList<TenantLifecycleManagerBfaRule> Rules =
    [
        new("CF-1011", TenantLifecycleManagerConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Concurrent tenant lifecycle writes require an If-Match version and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1012", TenantLifecycleManagerConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated lifecycle mutations with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1013", TenantLifecycleManagerConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value lifecycle inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1014", TenantLifecycleManagerConstants.FlowId, "OUTBOX_TIMEOUT_RETENTION", "Downstream timeout returns 504 while retaining an outbox record for retry.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
