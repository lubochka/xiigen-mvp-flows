namespace Xiigen.Flow.VisualFlowEngine.Application;

public sealed record VisualFlowEngineOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateChanged,
    bool OutboxRetained);

public sealed record VisualFlowEngineDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Canvases,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> NodeTypes,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> InjectionResults,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> TestRuns,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record VisualFlowEngineBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record FlowNode(
    string Id,
    string NodeTypeId,
    string Category,
    string TaskTypeId,
    string InputType,
    string OutputType,
    IReadOnlyDictionary<string, object?> Config);

public sealed record FlowEdge(
    string From,
    string To,
    string SourceOutputType,
    string TargetInputType);
