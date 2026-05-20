namespace Xiigen.Flow.Marketplace.Application;

public sealed record MarketplaceOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record MarketplaceCatalog(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Listings,
    int Count,
    IReadOnlyDictionary<string, int> StatusCounts);

public sealed record MarketplaceAnalytics(
    int ListingCount,
    int ViewCount,
    int InquiryCount,
    double ConversionRate,
    IReadOnlyDictionary<string, int> StatusCounts,
    bool ViewerIdsStored);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
