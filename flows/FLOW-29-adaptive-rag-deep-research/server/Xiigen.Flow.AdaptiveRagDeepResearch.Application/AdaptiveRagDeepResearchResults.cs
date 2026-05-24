namespace Xiigen.Flow.AdaptiveRagDeepResearch.Application;

public sealed record AdaptiveRagDeepResearchOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);

public sealed record AdaptiveRagDeepResearchDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ResearchRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RoutingPolicies,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> PromptPromotions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GraphEdits,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Feedback,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record AdaptiveRagDeepResearchBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record AdaptiveRagRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record AdaptiveRagStressResult(string StressTestId, string Status, string Validates);
public sealed record AdaptiveRagPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
