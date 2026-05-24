namespace Xiigen.Flow.BfaCrossFlowGovernance.Application;

public static class BfaCrossFlowGovernanceConstants
{
    public const string FlowId = "FLOW-25";
    public const string FlowSlug = "bfa-cross-flow-governance";
    public const string DynamicEndpoint = "/api/dynamic/bfa-cross-flow-governance";
    public const string PackageName = "Xiigen.Flow.BfaCrossFlowGovernance";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "governance_operator",
        "arbiter_reviewer",
        "flow_architect",
        "impact_analyst",
        "release_manager",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] OperatorRoles = ["governance_operator", "tenant_admin", "platform_admin"];
    public static readonly string[] ReviewerRoles = ["arbiter_reviewer", "impact_analyst", "tenant_admin", "platform_admin"];
    public static readonly string[] ArchitectRoles = ["flow_architect", "impact_analyst", "tenant_admin", "platform_admin"];
    public static readonly string[] ReleaseRoles = ["release_manager", "tenant_admin", "platform_admin"];
    public static readonly string[] AdminRoles = ["tenant_admin", "platform_admin"];
    public static readonly string[] ReaderRoles = ["governance_operator", "arbiter_reviewer", "flow_architect", "impact_analyst", "release_manager", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T375", "T376", "T377", "T378", "T379", "T380", "T381",
        "T382", "T383", "T384", "T385", "T386", "T387", "T388"
    ];

    public static readonly string[] RequiredStressTests =
        Enumerable.Range(300, 25).Select(value => $"ST-{value}").ToArray();

    public static readonly string[] RequiredBfaRules =
        Enumerable.Range(473, 72).Select(value => $"CF-{value}").ToArray();

    public const string RulePackCollection = "xiigen-bfa-governance-rule-packs";
    public const string ChangeCollection = "xiigen-bfa-governance-changes";
    public const string ConflictCollection = "xiigen-bfa-governance-conflicts";
    public const string ImpactCollection = "xiigen-bfa-governance-impact";
    public const string StressCollection = "xiigen-bfa-governance-stress";
    public const string ReleaseGateCollection = "xiigen-bfa-governance-release-gates";
    public const string AuditCollection = "xiigen-bfa-governance-audit";
}
