namespace Xiigen.Flow.HistoryBootstrap.Application;

public static class HistoryBootstrapBfaRuleRegistry
{
    public static readonly IReadOnlyList<HistoryBootstrapBfaRule> Rules =
    [
        Rule("CF-803_BOOTSTRAP_BEFORE_FLOW_EXECUTION", "BOOTSTRAP_PREREQUISITE", "History bootstrap must run before dependent flow execution reads architecture memory."),
        Rule("CF-804_ARCH_PATTERN_GLOBAL_SCOPE", "GLOBAL_SCOPE_ONLY", "ARCH_PATTERN records must be GLOBAL scoped and must not inherit tenant-local scope."),
        Rule("CF-HB1-DESIGN_REASONING_TO_PLANNING", "DESIGN_REASONING_ROUTE", "DESIGN_REASONING patterns are stored in planning decisions for downstream planning retrieval."),
        Rule("CF-HB2-ARCH_PATTERN_TO_RAG", "ARCH_PATTERN_ROUTE", "ARCH_PATTERN and RAG patterns are stored in the RAG pattern index for retrieval."),
        Rule("CF-HB3-SUMMARY_STORE_BEFORE_QUEUE", "OUTBOX_ORDER", "Summary and completion state must be stored before CloudEvents are appended."),
        Rule("CF-HB4-IDEMPOTENT_BOOTSTRAP_RUN", "IDEMPOTENCY", "The same bootstrapRunId must return the existing completion without duplicating state or events."),
        Rule("CF-HB5-TENANT_SCOPED_DASHBOARD", "SCOPE_ISOLATION", "Dashboard readback is scoped by the active TenantContext.")
    ];

    private static HistoryBootstrapBfaRule Rule(string id, string type, string description) =>
        new(id, HistoryBootstrapConstants.FlowId, type, description, "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT");
}
