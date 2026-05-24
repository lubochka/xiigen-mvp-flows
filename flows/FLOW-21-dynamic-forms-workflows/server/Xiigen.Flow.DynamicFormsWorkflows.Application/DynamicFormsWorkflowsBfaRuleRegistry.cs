namespace Xiigen.Flow.DynamicFormsWorkflows.Application;

public static class DynamicFormsWorkflowsBfaRuleRegistry
{
    public static readonly IReadOnlyList<DynamicFormsWorkflowsBfaRule> Rules =
    [
        Rule("CF-21-1", "FormSchemaPublisher must publish immutable schema versions with optimistic concurrency and no entity-specific controller path."),
        Rule("CF-21-2", "FormSubmissionProcessor must validate every submission against the published schema before any workflow or outbox side effect."),
        Rule("CF-21-3", "AutomationDispatcher must acquire a SETNX-equivalent submission-rule lock before dispatching workflow automation."),
        Rule("CF-21-4", "SubmissionAnalyticsCollector must persist append-only aggregate metrics and never store submitted PII values."),
        Rule("CF-969", "Concurrent form schema publish attempts for the same form must produce one created response and one CONCURRENT_UPDATE conflict."),
        Rule("CF-970", "Repeated X-Idempotency-Key submissions must replay the cached response without duplicate state or outbox effects."),
        Rule("CF-971", "Boundary validation failure must return field-level errors and leave no partial form, submission, automation, analytics, or outbox write."),
        Rule("CF-972", "Downstream workflow timeout must return TIMEOUT, retain recovery outbox intent, and avoid partial committed business state.")
    ];

    private static DynamicFormsWorkflowsBfaRule Rule(string id, string description) =>
        new(
            id,
            DynamicFormsWorkflowsConstants.FlowId,
            "functional-coverage",
            description,
            "BUILD_FAILURE",
            "FLOW_SCOPED",
            "GLOBAL",
            "MASTER_TENANT_ID");
}
