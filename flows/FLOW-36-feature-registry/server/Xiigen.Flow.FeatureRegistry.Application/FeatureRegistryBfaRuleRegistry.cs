namespace Xiigen.Flow.FeatureRegistry.Application;

public static class FeatureRegistryBfaRuleRegistry
{
    public static readonly IReadOnlyList<FeatureRegistryBfaRule> Rules =
    [
        new("CF-808", FeatureRegistryConstants.FlowId, "PORTING_CANDIDATE_PROHIBITION", "portingCandidate=false must resolve to PROHIBITED and must not run cost estimation or adapter generation.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-809", FeatureRegistryConstants.FlowId, "PROHIBITED_GUARD_FIRST", "The PROHIBITED guard must be evaluated synchronously before cost, adapter, or simulation work begins.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-813", FeatureRegistryConstants.FlowId, "MACHINE_CLASSIFICATION_LOCK", "portingCandidate is MACHINE-owned and cannot be overridden by tenant configuration or request payload.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1035", FeatureRegistryConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Feature registry writes require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1036", FeatureRegistryConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated feature registry writes with the same X-Idempotency-Key return cached 200 without duplicate state or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1037", FeatureRegistryConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value feature registry inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1038", FeatureRegistryConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream feature registry timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1039", FeatureRegistryConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Repeated concurrency paths use the same feature-version split of one winner and one 409 loser.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1040", FeatureRegistryConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated idempotency keys across porting decision requests reuse cached success bodies.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1041", FeatureRegistryConstants.FlowId, "FIELD_VALIDATION", "Boundary feature signal values fail with field-level validation and no partial write.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1042", FeatureRegistryConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Signal aggregation timeout leaves retryable outbox evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1043", FeatureRegistryConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Adapter plan writes use stale-version rejection.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1044", FeatureRegistryConstants.FlowId, "IDEMPOTENCY_REPLAY", "Adapter plan retries are deduplicated by idempotency key.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1045", FeatureRegistryConstants.FlowId, "FIELD_VALIDATION", "Adapter and simulation requests reject empty target platform values.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1046", FeatureRegistryConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Adapter simulation timeout leaves retryable outbox evidence.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1047", FeatureRegistryConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "Feature porting orchestrator writes preserve optimistic concurrency.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1048", FeatureRegistryConstants.FlowId, "IDEMPOTENCY_REPLAY", "Feature porting orchestrator retries reuse cached responses.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1049", FeatureRegistryConstants.FlowId, "FIELD_VALIDATION", "Feature porting orchestrator rejects empty orchestration inputs.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}

