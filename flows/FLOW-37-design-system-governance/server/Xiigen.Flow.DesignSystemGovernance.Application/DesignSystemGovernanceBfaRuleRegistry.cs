namespace Xiigen.Flow.DesignSystemGovernance.Application;

public static class DesignSystemGovernanceBfaRuleRegistry
{
    public static readonly IReadOnlyList<DesignSystemGovernanceBfaRule> Rules =
    [
        new("CF-1050", DesignSystemGovernanceConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Design-system governance writes require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1051", DesignSystemGovernanceConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated design-system governance writes with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1052", DesignSystemGovernanceConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value design-system governance inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1053", DesignSystemGovernanceConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream design-system governance timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
