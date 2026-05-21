namespace Xiigen.Flow.SaasMultiTenancy.Application;

public sealed record BfaRule(
    string FlowId,
    string RuleId,
    string RuleType,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record SaasMultiTenancyOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record SaasMultiTenancyDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Tenants,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> FreedomConfig,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> QuotaCounters,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> LifecycleAudits,
    IReadOnlyDictionary<string, object?> Kpis);
