namespace Xiigen.Flow.CanvaTextAdapter.Application;

public static class CanvaTextAdapterConstants
{
    public const string FlowId = "FLOW-41";
    public const string FlowSlug = "canva-text-adapter";
    public const string ImplementationAlias = "canva-text-adapter";
    public const string DynamicEndpoint = "/api/dynamic/canva-text-adapter";
    public const string PackageName = "@xiigen/canva-text-elements-adapter";
    public const string PackageVersion = "1.0.1";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T682-CANVA-READ-ADAPTER",
        "T683-SHARED-STYLE-BRIDGE",
        "T684-CANVA-WRITE-PAYLOAD",
        "T685-LOCAL-REGISTRY-READBACK"
    ];

    public static readonly string[] RequiredBfaRules =
    [
        "CF-PORT-LIVE-01",
        "CF-SCOPE-GLOBAL-01",
        "CF-PRICING-01",
        "CF-C5-READ-01",
        "CF-C5-WRITE-01",
        "CF-C5-PACKAGE-01",
        "CF-C5-SDK-BOUNDARY-01"
    ];

    public const string AdapterRunsCollection = "xiigen-canva-adapter-runs";
    public const string PublicationAuditCollection = "xiigen-canva-adapter-publications";
}
