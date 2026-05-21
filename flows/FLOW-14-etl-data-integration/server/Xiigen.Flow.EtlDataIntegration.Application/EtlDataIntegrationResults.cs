namespace Xiigen.Flow.EtlDataIntegration.Application;

public sealed record BfaRule(
    string FlowId,
    string RuleId,
    string RuleType,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record EtlDataIntegrationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record EtlDataIntegrationDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Plans,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RawLandingRecords,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> StagingRecords,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> DriftAudits,
    IReadOnlyDictionary<string, object?> Kpis);
