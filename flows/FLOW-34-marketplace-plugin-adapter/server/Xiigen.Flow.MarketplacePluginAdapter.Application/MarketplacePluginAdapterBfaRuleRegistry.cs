namespace Xiigen.Flow.MarketplacePluginAdapter.Application;

public static class MarketplacePluginAdapterBfaRuleRegistry
{
    public static readonly IReadOnlyList<MarketplacePluginAdapterBfaRule> Rules =
    [
        new("CF-1027", MarketplacePluginAdapterConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Adapter package writes require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1028", MarketplacePluginAdapterConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated adapter generation mutations with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1029", MarketplacePluginAdapterConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value adapter package inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1030", MarketplacePluginAdapterConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream adapter fabric timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
