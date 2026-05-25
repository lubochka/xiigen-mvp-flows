namespace Xiigen.Flow.DesignSystemGovernance.Application;

public static class DesignSystemGovernanceConstants
{
    public const string FlowId = "FLOW-37";
    public const string FlowSlug = "design-system-governance";
    public const string ImplementationAlias = "design-system-governance";
    public const string DynamicEndpoint = "/api/dynamic/design-system-governance";
    public const string PackageName = "Xiigen.Flow.DesignSystemGovernance";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes = ["DESIGN-SYSTEM-CLASSIFICATION-001", "HYBRID-GENESIS-PROMPT-001", "DESIGN-DEBT-ANALYSIS-001"];
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1050, 4).Select(value => $"CF-{value}").ToArray();

    public const string ClassificationCollection = "xiigen-design-system-governance-classifications";
    public const string PromptCollection = "xiigen-design-system-governance-prompts";
    public const string DebtCollection = "xiigen-design-system-governance-debt";
    public const string CompatibilityCollection = "xiigen-design-system-governance-compatibility";
    public const string AuditCollection = "xiigen-design-system-governance-audit";
}

