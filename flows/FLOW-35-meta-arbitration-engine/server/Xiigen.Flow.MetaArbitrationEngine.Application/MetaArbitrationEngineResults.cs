namespace Xiigen.Flow.MetaArbitrationEngine.Application;

public sealed record MetaArbitrationEngineOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record MetaArbitrationEngineDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Decisions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Conflicts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Briefings,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Panels,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record MetaArbitrationEngineBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record MetaArbitrationRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record MetaArbitrationPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

