namespace Xiigen.Flow.ProfileEnrichment.Application;

public sealed record ProfileEnrichmentOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record ProfileEnrichmentReadModel(
    string ProfileId,
    string UserId,
    IReadOnlyList<string> MatchedBusinessIds,
    IReadOnlyList<string> Suggestions,
    bool PartialResults,
    int SignalsUsed,
    bool Degraded,
    IReadOnlyList<string> ContentItems,
    string CompletionEventName);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
