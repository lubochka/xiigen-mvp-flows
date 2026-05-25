namespace Xiigen.Flow.DesignSystemGovernance.Application;

public sealed record DesignSystemGovernanceOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record DesignSystemGovernanceDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Classifications,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Prompts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> DebtReports,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CompatibilityReports,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record DesignSystemGovernanceBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record DesignSystemGovernanceRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record DesignSystemGovernancePlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);

