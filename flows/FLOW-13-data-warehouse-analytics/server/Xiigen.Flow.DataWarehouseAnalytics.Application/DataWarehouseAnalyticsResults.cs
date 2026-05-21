namespace Xiigen.Flow.DataWarehouseAnalytics.Application;

public sealed record BfaRule(
    string FlowId,
    string RuleId,
    string RuleType,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record DataWarehouseAnalyticsOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record DataWarehouseAnalyticsDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Plans,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RawEvents,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> MetricFacts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> QualityAudits,
    IReadOnlyDictionary<string, object?> Kpis);
