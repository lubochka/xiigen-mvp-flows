namespace Xiigen.Flow.FormBuilderTemplates.Application;

public static class FormBuilderTemplatesBfaRuleRegistry
{
    public static readonly IReadOnlyList<FormBuilderTemplatesBfaRule> Rules =
    [
        new("CF-23-1", FormBuilderTemplatesConstants.FlowId, "SCHEMA_VALIDATION_ORDER", "JSON Schema validation, required field enforcement, and type compatibility execute in fixed order.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-23-2", FormBuilderTemplatesConstants.FlowId, "OCC_IMMUTABILITY", "Published template versions use optimistic concurrency and immutable schema semantics.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-23-3", FormBuilderTemplatesConstants.FlowId, "SETNX_INSTANTIATION_LOCK", "Template instantiation uses a per template/context lock, variable binding, defaults merge, and lock release.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-23-4", FormBuilderTemplatesConstants.FlowId, "APPEND_ONLY_USAGE", "Usage metrics are append-only, PII-free, tenant-private, and popularity-scored.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-435", FormBuilderTemplatesConstants.FlowId, "SLOT_SCHEMA_BINDING", "Slot schema is validated against the referenced data source schema.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-448", FormBuilderTemplatesConstants.FlowId, "ASYNC_EVENT_GATE", "All async template events use the approved CloudEvents outbox path.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-446", FormBuilderTemplatesConstants.FlowId, "EXPORT_QUALITY", "Code export must meet the quality threshold before package publication.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-449", FormBuilderTemplatesConstants.FlowId, "QUEUE_IDEMPOTENCY", "Queue consumers check idempotency before processing.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-445", FormBuilderTemplatesConstants.FlowId, "LAYOUT_CONSTRAINTS_FIRST", "Layout constraints are validated before solve.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-433", FormBuilderTemplatesConstants.FlowId, "TEMPLATE_MODE_READ_ONLY", "Template mode is read-only and produces no side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-447", FormBuilderTemplatesConstants.FlowId, "DAG_TEMPLATE_START", "The first template DAG step is the canonical template context initialization step.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-444", FormBuilderTemplatesConstants.FlowId, "READ_ONLY_CONTEXT_VERIFY", "Read-only context is verified after entering template mode.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-977", FormBuilderTemplatesConstants.FlowId, "CONCURRENCY_CONSTRAINT", "Concurrent writes split into one success and one CONCURRENT_UPDATE response.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-978", FormBuilderTemplatesConstants.FlowId, "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns the cached response with no duplicate side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-979", FormBuilderTemplatesConstants.FlowId, "VALIDATION_NO_PARTIAL_WRITE", "Empty/null/zero primary field returns VALIDATION_FAILED and writes no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-980", FormBuilderTemplatesConstants.FlowId, "DNA8_ORDERING", "Fabric timeout returns TIMEOUT while retaining outbox evidence for retry.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID")
    ];
}
