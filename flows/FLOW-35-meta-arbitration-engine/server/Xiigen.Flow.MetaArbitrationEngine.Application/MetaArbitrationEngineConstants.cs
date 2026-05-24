namespace Xiigen.Flow.MetaArbitrationEngine.Application;

public static class MetaArbitrationEngineConstants
{
    public const string FlowId = "FLOW-35";
    public const string FlowSlug = "meta-arbitration-engine";
    public const string ImplementationAlias = "meta-arbitration-engine";
    public const string DynamicEndpoint = "/api/dynamic/meta-arbitration-engine";
    public const string PackageName = "Xiigen.Flow.MetaArbitrationEngine";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(580, 10).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1031, 4).Select(value => $"CF-{value}").ToArray();

    public const string DecisionCollection = "xiigen-meta-arbitration-decisions";
    public const string ConflictCollection = "xiigen-meta-arbitration-conflicts";
    public const string BriefingCollection = "xiigen-meta-arbitration-briefings";
    public const string PanelCollection = "xiigen-meta-arbitration-panels";
    public const string AuditCollection = "xiigen-meta-arbitration-audit";
}

