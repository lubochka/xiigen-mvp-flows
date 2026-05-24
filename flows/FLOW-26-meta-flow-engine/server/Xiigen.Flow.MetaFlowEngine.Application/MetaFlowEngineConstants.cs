namespace Xiigen.Flow.MetaFlowEngine.Application;

public static class MetaFlowEngineConstants
{
    public const string FlowId = "FLOW-26";
    public const string FlowSlug = "meta-flow-engine";
    public const string DynamicEndpoint = "/api/dynamic/meta-flow-engine";
    public const string PackageName = "Xiigen.Flow.MetaFlowEngine";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "platform_support",
        "flow_architect",
        "prompt_approver",
        "human_decision_maker",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] OperatorRoles = ["flow_architect", "prompt_approver", "tenant_admin", "platform_admin"];
    public static readonly string[] ReviewerRoles = ["platform_support", "human_decision_maker", "tenant_admin", "platform_admin"];
    public static readonly string[] ArchitectRoles = ["flow_architect", "tenant_admin", "platform_admin"];
    public static readonly string[] ReleaseRoles = ["human_decision_maker", "tenant_admin", "platform_admin"];
    public static readonly string[] AdminRoles = ["tenant_admin", "platform_admin"];
    public static readonly string[] ReaderRoles = ["platform_support", "flow_architect", "prompt_approver", "human_decision_maker", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T389", "T390", "T391", "T392", "T393", "T394", "T395", "T396",
        "T397", "T398", "T399", "T400", "T401", "T402", "T403", "T404",
        "T405", "T406", "T407", "T408", "T409", "T410", "T411", "T412"
    ];

    public static readonly string[] RequiredStressTests =
        Enumerable.Range(300, 25).Select(value => $"ST-{value}").ToArray();

    public static readonly string[] RequiredBfaRules =
        Enumerable.Range(510, 35).Select(value => $"CF-{value}")
            .Concat(Enumerable.Range(989, 7).Select(value => $"CF-{value}"))
            .ToArray();

    public const string RulePackCollection = "xiigen-meta-flow-rule-packs";
    public const string ChangeCollection = "xiigen-meta-flow-self-build-runs";
    public const string ConflictCollection = "xiigen-meta-flow-gap-scans";
    public const string ImpactCollection = "xiigen-meta-flow-plans";
    public const string StressCollection = "xiigen-meta-flow-stress";
    public const string ReleaseGateCollection = "xiigen-meta-flow-promotions";
    public const string AuditCollection = "xiigen-meta-flow-audit";
}

