namespace Xiigen.Flow.DurableSagasCompliance.Application;

public sealed record DurableSagasComplianceOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record DurableSagasComplianceDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> SagaRuns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ComplianceRecords,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> CompensationRecords,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RetentionArchives,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record DurableSagasComplianceBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record SagaPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config,
    IReadOnlyDictionary<string, object?> Compensation);
