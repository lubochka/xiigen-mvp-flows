namespace Xiigen.Flow.FriendRequestSocialFeed.Application;

public sealed record FriendRequestSocialFeedOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record FriendRequestSocialFeedConnectionStatus(
    string ConnectionId,
    string RequestId,
    string SenderUserId,
    string RecipientUserId,
    string Status,
    double ConnectionStrength,
    string Version);

public sealed record FriendRequestSocialFeedFeed(
    string ConnectionId,
    string ViewerUserId,
    double ConnectionStrength,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Items);

public sealed record FriendRequestSocialFeedAnalytics(
    int ConnectionCount,
    int PendingRequestCount,
    double AverageStrength,
    IReadOnlyDictionary<string, int> StrengthBuckets);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
