namespace Xiigen.Flow.RagQualityFeedback.Application;

public sealed record RagQualityFeedbackOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record RagQualityFeedbackDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CycleOutcomes,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> QualityUpdates,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> BatchRecords,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> DistilledRules,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GapSignals,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record RagQualityFeedbackBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record RagQualityFeedbackRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record RagQualityFeedbackPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);


