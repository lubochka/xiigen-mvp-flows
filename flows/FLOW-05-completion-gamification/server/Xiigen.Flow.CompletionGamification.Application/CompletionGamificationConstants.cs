namespace Xiigen.Flow.CompletionGamification.Application;

public static class CompletionGamificationConstants
{
    public const string FlowId = "FLOW-05";
    public const string FlowSlug = "completion-gamification";
    public const string DynamicEndpoint = "/api/dynamic/completion-gamification";

    public const string Completions = "xiigen-completion-gamification-completions";
    public const string PointLedger = "xiigen-completion-gamification-point-ledger";
    public const string Streaks = "xiigen-completion-gamification-streaks";
    public const string Achievements = "xiigen-completion-gamification-achievements";
    public const string SocialShares = "xiigen-completion-gamification-social-shares";
    public const string PeerGrades = "xiigen-completion-gamification-peer-grades";
    public const string LearningCompletions = "xiigen-completion-gamification-learning-completions";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-05-1",
        "CF-05-2",
        "CF-05-3",
        "CF-05-4",
        "CF-867",
        "CF-868",
        "CF-869",
        "CF-870",
        "CF-871",
        "CF-872",
        "CF-873",
        "CF-874",
        "CF-875",
        "CF-876",
        "CF-877"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "referral_user",
        "freelancer",
        "event_organiser",
        "tenant_admin"
    ];

    public const string LessonCompletionSubmittedEvent = "xiigen.completion-gamification.lesson-completion-submitted.v1";
    public const string CompletionRecordedEvent = "xiigen.completion-gamification.completion-recorded.v1";
    public const string PointsAwardedEvent = "xiigen.completion-gamification.points-awarded.v1";
    public const string LedgerUpdatedEvent = "xiigen.completion-gamification.ledger-updated.v1";
    public const string LevelUpCheckedEvent = "xiigen.completion-gamification.level-up-checked.v1";
    public const string AchievementUnlockedEvent = "xiigen.completion-gamification.achievement-unlocked.v1";
    public const string SocialGatePassedEvent = "xiigen.completion-gamification.social-gate-passed.v1";
    public const string StreakUpdatedEvent = "xiigen.completion-gamification.streak-updated.v1";
    public const string FlowCompletedEvent = "xiigen.completion-gamification.flow-completed.v1";
    public const string PeerGradeRecordedEvent = "xiigen.completion-gamification.peer-grade-recorded.v1";
    public const string TimeoutRetainedEvent = "xiigen.completion-gamification.timeout-retained.v1";
}
