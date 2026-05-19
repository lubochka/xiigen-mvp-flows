namespace Xiigen.Flow.EventManagement.Application;

public sealed record EventManagementOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record EventManagementReadModel(
    string EventId,
    string Title,
    string OrganizerId,
    string StartDate,
    int? Capacity,
    string CapacityLabel,
    string Status,
    IReadOnlyList<string> AudienceSegments,
    IReadOnlyList<string> NotificationChannels,
    int Registrations,
    int Waitlisted);

public sealed record EventPromotionReadModel(
    string EventId,
    string PromotionStatus,
    IReadOnlyList<string> Channels,
    IReadOnlyList<string> FeedPlacements,
    bool SafetyPassed,
    string? RejectionReason);

public sealed record EventAnalyticsReadModel(
    string EventId,
    string CounterKey,
    string AnalyticsType,
    int Count,
    string ExpiresAt,
    bool Tracked);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

