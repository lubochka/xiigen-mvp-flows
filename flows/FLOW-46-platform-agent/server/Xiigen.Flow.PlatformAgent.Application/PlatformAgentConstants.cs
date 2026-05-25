namespace Xiigen.Flow.PlatformAgent.Application;

public static class PlatformAgentConstants
{
    public const string FlowId = "FLOW-46";
    public const string FlowSlug = "platform-agent";
    public const string PackageName = "@xiigen/platform-agent";
    public const string DynamicEndpoint = "/api/dynamic/platform-agent";
    public const string SessionsCollection = "xiigen-agent-sessions";
    public const string AgentActionsCollection = "xiigen-agent-actions";
    public const string TenantTopologiesCollection = "xiigen-tenant-topologies";
    public const string FlowTemplatesCollection = "xiigen-flow-templates";
    public const string RagPatternsCollection = "xiigen-rag-patterns";
    public const string PlanningDecisionsCollection = "xiigen-planning-decisions";
    public const string PromptsCollection = "xiigen-prompts";
    public const string TrainingDataCollection = "xiigen-training-data";
    public const string ContributionsCollection = "xiigen-agent-contributions";

    public static readonly string[] OperatorRoles = ["platform_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] RequiredTaskTypes = ["T650-AgentRunOrchestrator", "T651-TenantScopeGateway", "T652-PlatformContextEnricher", "T653-SuperJudgeArbiter", "T654-AgentActionPublisher", "T655-PatternContributor", "T656-AgentChatClient"];
    public static readonly string[] RequiredBfaRules = ["CF-839", "CF-840", "CF-841", "CF-1082", "CF-1083", "CF-1084", "CF-1085", "CF-1086", "CF-1087", "CF-1088", "CF-1089", "CF-1090", "CF-1091", "CF-1092", "CF-1093", "CF-1094", "CF-1095"];
    public static readonly string[] ActionTypes = ["ADVISE", "PROPOSE_EDIT", "CREATE_FLOW", "APPLY_GLOBAL"];
    public static readonly string[] SuperJudgeVerdicts = ["OVERRIDE_PASS", "OVERRIDE_BLOCK", "DEFER_TO_AF9"];
    public static readonly string[] SensitiveFields = ["tenantId", "tenantDomain", "apiKeyHints"];
    public static readonly string[] GlobalContributionCollections = [RagPatternsCollection, PlanningDecisionsCollection, PromptsCollection];
}