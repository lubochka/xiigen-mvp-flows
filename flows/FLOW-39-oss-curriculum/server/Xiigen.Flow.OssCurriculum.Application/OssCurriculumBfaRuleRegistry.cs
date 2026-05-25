namespace Xiigen.Flow.OssCurriculum.Application;

public static class OssCurriculumBfaRuleRegistry
{
    public static readonly IReadOnlyList<OssCurriculumBfaRule> Rules =
    [
        new("CF-803", OssCurriculumConstants.FlowId, "VALIDATION_ORDERING", "CurriculumTierAssigner must run before a DPO triple is marked VALIDATED; curriculumTier is required state, not optional metadata.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-804", OssCurriculumConstants.FlowId, "SHADOW_RUN_TRACEABILITY", "Every shadow run record must include ossModel and cycleId so curriculum value is attributable.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1058", OssCurriculumConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "OSS curriculum writes require version-aware optimistic concurrency and return 409 to stale writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1059", OssCurriculumConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated OSS curriculum writes with the same X-Idempotency-Key return cached 200 without duplicate side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1060", OssCurriculumConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero primary fields return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1061", OssCurriculumConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream fabric timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
