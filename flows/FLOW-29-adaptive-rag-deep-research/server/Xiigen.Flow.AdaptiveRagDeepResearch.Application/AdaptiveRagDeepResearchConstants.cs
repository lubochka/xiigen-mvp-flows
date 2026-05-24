namespace Xiigen.Flow.AdaptiveRagDeepResearch.Application;

public static class AdaptiveRagDeepResearchConstants
{
    public const string FlowId = "FLOW-29";
    public const string FlowSlug = "adaptive-rag-deep-research";
    public const string ImplementationAlias = "adaptive-rag-deep-research";
    public const string DynamicEndpoint = "/api/dynamic/adaptive-rag-deep-research";
    public const string PackageName = "Xiigen.Flow.AdaptiveRagDeepResearch";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "tenant_admin",
        "prompt_approver",
        "human_decision_maker",
        "agency_access",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ReaderRoles = ["anonymous", "public_marketplace_visitor", "tenant_user", "tenant_admin", "prompt_approver", "human_decision_maker", "agency_access", "platform_admin", "platform_support"];
    public static readonly string[] ResearchRoles = ["tenant_user", "tenant_admin", "prompt_approver", "human_decision_maker", "agency_access", "platform_admin"];
    public static readonly string[] ControlPlaneRoles = ["tenant_admin", "prompt_approver", "human_decision_maker", "platform_admin"];
    public static readonly string[] SupportRoles = ["platform_support", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(441, 27).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredStressTests = Enumerable.Range(369, 21).Select(value => $"ST-{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(601, 26).Select(value => $"CF-{value}").Concat(Enumerable.Range(1007, 4).Select(value => $"CF-{value}")).ToArray();

    public const string ResearchRunCollection = "xiigen-adaptive-rag-research-runs";
    public const string RoutingPolicyCollection = "xiigen-adaptive-rag-routing-policies";
    public const string PromptPromotionCollection = "xiigen-adaptive-rag-prompt-promotions";
    public const string GraphEditCollection = "xiigen-adaptive-rag-graph-edits";
    public const string FeedbackCollection = "xiigen-adaptive-rag-feedback";
    public const string StressCollection = "xiigen-adaptive-rag-stress";
    public const string AuditCollection = "xiigen-adaptive-rag-audit";
}
