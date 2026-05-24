namespace Xiigen.Flow.AiSafetyModeration.Application;

public sealed record AiSafetyModerationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateWritten,
    bool OutboxRetained);

public sealed record AiSafetyModerationDashboard(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ConsentGates,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Lessons,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> QuizAttempts,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ProgressRecords,
    IReadOnlyDictionary<string, object?> Kpis);

public sealed record AiSafetyModerationBfaRule(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string RuleTenantScope);

public sealed record AiSafetyPlanStep(
    string Id,
    string TaskTypeId,
    string Name,
    string Responsibility,
    IReadOnlyDictionary<string, object?> Config);

public sealed record SafetyGateToken(
    string TokenId,
    string LessonId,
    string StudentId,
    string SafetyReviewId,
    string IssuedAt,
    string ExpiresAt);
