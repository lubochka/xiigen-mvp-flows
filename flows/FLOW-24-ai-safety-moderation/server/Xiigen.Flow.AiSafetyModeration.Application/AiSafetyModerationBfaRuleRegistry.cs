namespace Xiigen.Flow.AiSafetyModeration.Application;

public static class AiSafetyModerationBfaRuleRegistry
{
    public static readonly IReadOnlyList<AiSafetyModerationBfaRule> Rules =
    [
        new("CF-24-1", AiSafetyModerationConstants.FlowId, "CONSENT_GATE_ORDERING", "Consent gate is evaluated before AI content, scheduling, calendar, progress, and gamification outcomes.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-24-2", AiSafetyModerationConstants.FlowId, "MANDATORY_SAFETY_GATE", "Every AI-generated learning content publish requires a SafetyGateToken produced by the moderation gate.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-24-3", AiSafetyModerationConstants.FlowId, "SERVER_SIDE_GRADING", "Quiz score is recomputed on the server and client-provided scores are rejected before state writes.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-24-4", AiSafetyModerationConstants.FlowId, "PROFILE_TIMEZONE_ONLY", "Learning streaks use timezone from the learner profile or factory output, never from client payload.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-458", AiSafetyModerationConstants.FlowId, "SAFETY_GATE_TOKEN_PROTOCOL", "F1002 produces SafetyGateToken and F1003 consumes it before any AI content becomes visible.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-459", AiSafetyModerationConstants.FlowId, "FAIL_CLOSED_TOPOLOGY", "Missing consent or safety gate state fails closed with no downstream side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-460", AiSafetyModerationConstants.FlowId, "CALENDAR_TASK_OWNERSHIP", "Calendar sync records preserve source_flow FLOW-24 and flow-owned task identity.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-461", AiSafetyModerationConstants.FlowId, "CONSENT_BLOCKS_DOWNSTREAM", "Denied, pending, or withdrawn consent blocks all downstream outcomes.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-462", AiSafetyModerationConstants.FlowId, "FREEDOM_GATED_SKIP", "Missing FREEDOM config skips optional delivery/progress outcomes rather than failing the request.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-463", AiSafetyModerationConstants.FlowId, "HYBRID_SYNC_ASYNC_BOUNDARY", "Quiz grading is synchronous while gamification is queued asynchronously after the attempt write.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-464", AiSafetyModerationConstants.FlowId, "APPEND_ONLY_GAMIFICATION_LEDGER", "Gamification ledger entries are append-only and never overwrite prior student history.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-465", AiSafetyModerationConstants.FlowId, "COMPOSE_SAFETY_PUBLISH_ORDER", "Lesson composition order is compose, safety gate, then publish; publish cannot run first.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-466", AiSafetyModerationConstants.FlowId, "CALENDAR_FABRIC_ONLY", "Calendar integration goes through fabric connector descriptors and never direct provider SDK calls.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-467", AiSafetyModerationConstants.FlowId, "GAMIFICATION_SCORE_SOURCE", "Gamification points include source_flow FLOW-24 and quiz attempt lineage.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-468", AiSafetyModerationConstants.FlowId, "NAMESPACED_INDICES", "FLOW-24 state uses flow-scoped collection names and tenant-private records.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-469", AiSafetyModerationConstants.FlowId, "TUTOR_REMINDER_SOURCE", "Scheduled reminders preserve tutor-reminder source metadata.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-470", AiSafetyModerationConstants.FlowId, "AI_CONTENT_CONSENSUS", "AI-generated content must be safety-reviewed and linked to a token before publication.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-471", AiSafetyModerationConstants.FlowId, "TIMEZONE_SOURCE_AUTHORITY", "Client timezone fields are rejected; profile or factory timezone is authoritative.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID"),
        new("CF-472", AiSafetyModerationConstants.FlowId, "NO_DIRECT_CALENDAR_SDK", "Generated code cannot import or reference direct Google, Microsoft, or calendar provider SDKs.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "MASTER_TENANT_ID")
    ];
}
