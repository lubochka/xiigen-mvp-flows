namespace Xiigen.Flow.RagQualityFeedback.Application;

public static class RagQualityFeedbackBfaRuleRegistry
{
    public static readonly IReadOnlyList<RagQualityFeedbackBfaRule> Rules =
    [
        new("CF-1054", RagQualityFeedbackConstants.FlowId, "OPTIMISTIC_CONCURRENCY", "RAG quality feedback writes require version-aware optimistic concurrency and return 409 to stale cycle/pattern writers.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1055", RagQualityFeedbackConstants.FlowId, "IDEMPOTENCY_REPLAY", "Repeated RAG quality feedback writes with the same cycleId and patternId return cached 200 without duplicate quality deltas or outbox effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1056", RagQualityFeedbackConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero-value RAG quality feedback inputs return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1057", RagQualityFeedbackConstants.FlowId, "TIMEOUT_OUTBOX_RETENTION", "Downstream RAG quality feedback timeout preserves an outbox retry event and returns a retryable 504 response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}

