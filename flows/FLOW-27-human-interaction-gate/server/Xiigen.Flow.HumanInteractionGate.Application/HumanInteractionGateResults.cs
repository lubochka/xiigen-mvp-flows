namespace Xiigen.Flow.HumanInteractionGate.Application;

public sealed record HumanInteractionGateOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record HumanInteractionGateDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> OpenTasks,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Claims,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CompletionEvaluations,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record HumanInteractionGateBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string RuleTenantScope);

public sealed record HumanGateRuleSnapshot(
    string RuleId,
    string Severity,
    string Factory,
    string Trigger,
    string Scope,
    string Description);

public sealed record HumanGateStressResult(
    string StressTestId,
    string Status,
    string Validates);

public sealed record HumanGatePlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);
