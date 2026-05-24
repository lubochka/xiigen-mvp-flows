namespace Xiigen.Flow.DynamicFormsWorkflows.Application;

public sealed record DynamicFormsWorkflowsOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record DynamicFormsWorkflowsDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Schemas,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Submissions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Automations,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Analytics,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record DynamicFormsWorkflowsBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record DynamicFormsPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);

public sealed record FormFieldDefinition(
    string Key,
    string Type,
    bool Required,
    IReadOnlyList<string> VisibleToRoles,
    IReadOnlyDictionary<string, object?> Rules);

public sealed record WorkflowRuleDefinition(
    string RuleId,
    string Trigger,
    string TargetState,
    IReadOnlyDictionary<string, object?> Condition);
