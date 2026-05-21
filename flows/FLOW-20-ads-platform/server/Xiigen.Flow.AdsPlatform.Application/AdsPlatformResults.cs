namespace Xiigen.Flow.AdsPlatform.Application;

public sealed record AdsPlatformOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record AdsPlatformDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Deliveries,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Consents,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> FraudAudits,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> PoliticalReviews,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record AdsPlatformBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);

public sealed record AdsPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);

public sealed record ConsentGateResult(
    bool Passed,
    string Reason,
    IReadOnlyDictionary<string, object?> Evidence);

public sealed record BudgetReservationResult(
    bool Accepted,
    long RemainingBudgetCents,
    IReadOnlyDictionary<string, object?> Evidence);

public sealed record FraudReviewResult(
    bool Passed,
    double Score,
    double Threshold,
    IReadOnlyDictionary<string, object?> Evidence);

public sealed record PoliticalReviewResult(
    string Status,
    double MinScore,
    double Divergence,
    IReadOnlyDictionary<string, object?> Evidence);
