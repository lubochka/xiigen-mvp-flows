namespace Xiigen.Flow.ReviewsReputation.Application;

public sealed record ReviewsReputationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record ReputationSummary(
    string TargetEntityId,
    string TargetEntityType,
    double Score,
    int PublishedReviewCount,
    string Version);

public sealed record ReviewCollection(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Reviews,
    int Count);

public sealed record ModerationQueueSnapshot(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> PendingItems,
    int Count);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
