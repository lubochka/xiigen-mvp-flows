namespace Xiigen.Flow.HistoryBootstrap.Application;

public sealed record HistoryPattern(
    string PatternId,
    string PatternType,
    string KnowledgeScope,
    string ConnectionType,
    string Title,
    string Source,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record BootstrapFromDocumentsResult(int PatternsSeeded, int PlanningDecisionsSeeded, int RagPatternsSeeded, int PatternsFailed);
public sealed record ArchPhilosophyRetrievalResult(IReadOnlyList<IReadOnlyDictionary<string, object?>> Patterns, int TotalReturned);
public sealed record PhilosophySummary(string PatternType, int PatternCount, IReadOnlyList<string> PatternIds);
public sealed record SummarizeResult(string SummarizationRunId, int GroupsCreated, int PatternsProcessed, IReadOnlyList<PhilosophySummary> Summaries);
public sealed record BootstrapOrchestrationResult(string BootstrapRunId, int SeedCount, int RetrievedCount, int SummarizedGroups, string CompletionStatus);
public sealed record HistoryBootstrapOutcome(int HttpStatus, IReadOnlyDictionary<string, object?> Body, bool StateWritten, bool OutboxRetained);
public sealed record HistoryBootstrapDashboard(IReadOnlyList<IReadOnlyDictionary<string, object?>> Completions, IReadOnlyList<IReadOnlyDictionary<string, object?>> Summaries, IReadOnlyDictionary<string, object?> Kpis);
public sealed record HistoryBootstrapBfaRule(string RuleId, string FlowId, string Type, string Description, string ViolationSeverity, string ConnectionType, string KnowledgeScope, string RuleTenantScope);
public sealed record HistoryBootstrapRuleSnapshot(string RuleId, string Severity, string Factory, string Trigger, string Scope, string Description);
public sealed record HistoryBootstrapPlanStep(string Id, string TaskTypeId, string Name, string Responsibility, IReadOnlyDictionary<string, object?> Config);
