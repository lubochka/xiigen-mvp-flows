namespace Xiigen.Flow.FreelancerMarketplace.Application;

public sealed record FreelancerMarketplaceOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateChanged,
    bool OutboxRetained);

public sealed record FreelancerMarketplaceDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Gigs,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Contracts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Milestones,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Reviews,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record FreelancerMarketplaceBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
