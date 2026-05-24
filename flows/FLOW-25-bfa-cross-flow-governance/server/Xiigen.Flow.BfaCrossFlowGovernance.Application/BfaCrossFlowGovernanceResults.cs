namespace Xiigen.Flow.BfaCrossFlowGovernance.Application;

public sealed record BfaCrossFlowGovernanceOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record BfaCrossFlowGovernanceDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RulePacks,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Conflicts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ImpactReports,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> StressRuns,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record BfaCrossFlowGovernanceBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string RuleTenantScope);

public sealed record BfaGovernancePlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);

public sealed record BfaRuleSnapshot(
    string RuleId,
    string Severity,
    string SourceFlow,
    string TargetFlow,
    string Scope,
    string Description);

public sealed record BfaStressResult(
    string StressTestId,
    string Status,
    string Validates);
