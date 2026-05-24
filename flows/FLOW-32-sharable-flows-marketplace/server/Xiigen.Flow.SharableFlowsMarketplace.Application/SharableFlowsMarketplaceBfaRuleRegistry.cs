namespace Xiigen.Flow.SharableFlowsMarketplace.Application;

public static class SharableFlowsMarketplaceBfaRuleRegistry
{
    public static readonly IReadOnlyList<SharableFlowsMarketplaceBfaRule> Rules =
    [
        new("CF-1019", SharableFlowsMarketplaceConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Marketplace package writes require an If-Match version and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1020", SharableFlowsMarketplaceConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated marketplace mutations with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1021", SharableFlowsMarketplaceConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value marketplace package inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1022", SharableFlowsMarketplaceConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}