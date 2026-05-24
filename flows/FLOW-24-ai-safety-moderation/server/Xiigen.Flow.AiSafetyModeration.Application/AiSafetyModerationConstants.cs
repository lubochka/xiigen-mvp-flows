namespace Xiigen.Flow.AiSafetyModeration.Application;

public static class AiSafetyModerationConstants
{
    public const string FlowId = "FLOW-24";
    public const string FlowSlug = "ai-safety-moderation";
    public const string DynamicEndpoint = "/api/dynamic/ai-safety-moderation";
    public const string PackageName = "Xiigen.Flow.AiSafetyModeration";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "student",
        "parent_guardian",
        "educator",
        "safety_reviewer",
        "content_author",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] StudentRoles = ["student", "parent_guardian", "educator", "tenant_admin", "platform_admin"];
    public static readonly string[] AuthorRoles = ["content_author", "educator", "tenant_admin", "platform_admin"];
    public static readonly string[] SafetyRoles = ["safety_reviewer", "tenant_admin", "platform_admin"];
    public static readonly string[] EducatorRoles = ["educator", "tenant_admin", "platform_admin"];
    public static readonly string[] AdminRoles = ["tenant_admin", "platform_admin"];
    public static readonly string[] ReaderRoles = ["student", "parent_guardian", "educator", "safety_reviewer", "content_author", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes = ["T367", "T368", "T369", "T370", "T371", "T372", "T373", "T374"];
    public static readonly string[] RequiredBfaRules =
    [
        "CF-24-1", "CF-24-2", "CF-24-3", "CF-24-4",
        "CF-458", "CF-459", "CF-460", "CF-461", "CF-462", "CF-463",
        "CF-464", "CF-465", "CF-466", "CF-467", "CF-468", "CF-469",
        "CF-470", "CF-471", "CF-472"
    ];

    public const string ConsentGateCollection = "xiigen-ai-safety-consent-gates";
    public const string LessonCollection = "xiigen-ai-safety-lessons";
    public const string SafetyTokenCollection = "xiigen-ai-safety-tokens";
    public const string QuizAttemptCollection = "xiigen-ai-safety-quiz-attempts";
    public const string StreakCollection = "xiigen-ai-safety-streaks";
    public const string ScheduleCollection = "xiigen-ai-safety-schedules";
    public const string DomainPackCollection = "xiigen-ai-safety-domain-packs";
    public const string CalendarSyncCollection = "xiigen-ai-safety-calendar-syncs";
    public const string ProgressCollection = "xiigen-ai-safety-progress";
    public const string LedgerCollection = "xiigen-ai-safety-gamification-ledger";
    public const string AuditCollection = "xiigen-ai-safety-audit";
}
