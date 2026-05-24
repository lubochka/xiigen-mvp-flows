namespace Xiigen.Flow.CmsPublishing.Application;

public sealed record CmsPublishingOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record CmsPublishingDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Content,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Approvals,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Schedules,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Analytics,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record CmsPublishingBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record CmsPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);