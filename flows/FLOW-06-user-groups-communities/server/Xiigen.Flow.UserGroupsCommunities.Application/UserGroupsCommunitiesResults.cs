namespace Xiigen.Flow.UserGroupsCommunities.Application;

public sealed record UserGroupsCommunitiesOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record UserGroupsCommunitiesMembershipStatus(
    string GroupId,
    string UserId,
    string Status,
    string Tier,
    bool IsActive,
    bool FeedAvailable,
    int PendingApprovals,
    string Version);

public sealed record UserGroupsCommunitiesDiscovery(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Groups,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RecommendedGroups,
    string ViewerRole);

public sealed record UserGroupsCommunitiesFeed(
    string GroupId,
    string UserId,
    string Tier,
    bool Skipped,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Items);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
