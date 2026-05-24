namespace Xiigen.Flow.FormBuilderTemplates.Application;

public sealed record FormBuilderTemplatesOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record FormBuilderTemplatesDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Templates,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Instances,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Metrics,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record FormBuilderTemplatesBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string RuleTenantScope);

public sealed record FormBuilderPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);
