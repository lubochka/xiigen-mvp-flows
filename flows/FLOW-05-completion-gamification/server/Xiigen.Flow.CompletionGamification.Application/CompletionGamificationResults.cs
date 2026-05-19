namespace Xiigen.Flow.CompletionGamification.Application;

public sealed record CompletionGamificationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool StateStored,
    bool OutboxRetained);

public sealed record CompletionGamificationDashboard(
    string LearnerId,
    int TotalPoints,
    int Level,
    int CurrentStreak,
    int CompletionCount,
    int AchievementCount,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RecentCompletions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Achievements);

public sealed record CompletionGamificationStatus(
    string LessonId,
    string LearnerId,
    string Status,
    int EarnedPoints,
    int TotalPoints,
    int Level,
    int CurrentStreak,
    string LocalDate,
    bool SocialShareAllowed);

public sealed record BfaRuleDefinition(
    string RuleId,
    string FlowId,
    string Type,
    string Description,
    string ViolationSeverity,
    string ConnectionType,
    string KnowledgeScope,
    string TenantId);
