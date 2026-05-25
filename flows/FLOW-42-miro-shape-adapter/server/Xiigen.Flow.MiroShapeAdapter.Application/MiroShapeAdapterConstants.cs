namespace Xiigen.Flow.MiroShapeAdapter.Application;

public static class MiroShapeAdapterConstants
{
    public const string FlowId = "FLOW-42";
    public const string FlowSlug = "miro-shape-adapter";
    public const string ImplementationAlias = "miro-shape-adapter";
    public const string DynamicEndpoint = "/api/dynamic/miro-shape-adapter";
    public const string PackageName = "@xiigen/miro-ai-architect";
    public const string PackageVersion = "1.0.0";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T686-MIRO-BOARD-READ-ADAPTER",
        "T687-MIRO-SHAPE-CLASSIFIER",
        "T688-MIRO-SPATIAL-ANALYZER",
        "T689-MIRO-WRITEBACK"
    ];

    public static readonly string[] RequiredBfaRules =
    [
        "CF-PORT-LIVE-01",
        "CF-SCOPE-GLOBAL-01",
        "CF-PRICING-01",
        "CF-M1-READ-01",
        "CF-M1-CLASSIFY-01",
        "CF-M1-SPATIAL-01",
        "CF-M1-WRITE-01",
        "CF-M1-PACKAGE-01",
        "CF-M1-SDK-BOUNDARY-01"
    ];

    public const string AdapterRunsCollection = "xiigen-miro-adapter-runs";
    public const string PublicationAuditCollection = "xiigen-miro-adapter-publications";
}
