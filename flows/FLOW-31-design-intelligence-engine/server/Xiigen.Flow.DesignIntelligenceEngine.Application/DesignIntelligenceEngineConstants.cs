namespace Xiigen.Flow.DesignIntelligenceEngine.Application;

public static class DesignIntelligenceEngineConstants
{
    public const string FlowId = "FLOW-31";
    public const string FlowSlug = "design-intelligence-engine";
    public const string ImplementationAlias = "design-system-governance";
    public const string DynamicEndpoint = "/api/dynamic/design-intelligence-engine";
    public const string PackageName = "Xiigen.Flow.DesignIntelligenceEngine";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] ControlPlaneRoles = ["platform_admin"];
    public static readonly string[] SupportRoles = ["platform_admin", "platform_support"];
    public static readonly string[] TenantConsoleRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(489, 27).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1015, 4).Select(value => $"CF-{value}").ToArray();
    public static readonly string[] RequiredDesignStates =
    [
        "imported",
        "tokens_extracted",
        "component_map_ready",
        "compliance_validated",
        "governance_published",
        "needs_review"
    ];

    public const string AnalysisCollection = "xiigen-design-intelligence-analyses";
    public const string TokenCollection = "xiigen-design-intelligence-tokens";
    public const string ComplianceCollection = "xiigen-design-intelligence-compliance";
    public const string GovernanceCollection = "xiigen-design-intelligence-governance";
    public const string AuditCollection = "xiigen-design-intelligence-audit";
}
