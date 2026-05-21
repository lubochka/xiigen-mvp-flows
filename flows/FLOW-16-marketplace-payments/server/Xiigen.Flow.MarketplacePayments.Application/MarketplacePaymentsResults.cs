namespace Xiigen.Flow.MarketplacePayments.Application;

public sealed record MarketplacePaymentsOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateChanged,
    bool OutboxRetained);

public sealed record MarketplacePaymentsDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Payments,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Escrows,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Payouts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Refunds,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record MarketplacePaymentsBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
