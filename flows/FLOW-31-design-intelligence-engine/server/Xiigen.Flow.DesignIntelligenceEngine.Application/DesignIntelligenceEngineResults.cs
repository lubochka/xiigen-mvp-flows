namespace Xiigen.Flow.DesignIntelligenceEngine.Application;

public sealed record DesignIntelligenceEngineOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record DesignIntelligenceEngineDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Analyses,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> TokenRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ComplianceRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GovernanceReports,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record DesignIntelligenceEngineBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record DesignIntelligenceRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record DesignIntelligencePlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
