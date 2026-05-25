namespace Xiigen.Flow.FramerAdapter.Application;

public static class FramerAdapterConstants
{
    public const string FlowId = "FLOW-44";
    public const string FlowSlug = "framer-adapter";
    public const string ImplementationAlias = "framer-adapter";
    public const string DynamicEndpoint = "/api/dynamic/framer-adapter";
    public const string PackageName = "@xiigen/framer-text-elements-adapter";
    public const string PackageVersion = "1.0.0";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T700-Framer-text-node-filter",
        "T701-Framer-typography-style-mapping",
        "T702-Framer-runtime-write-facade",
        "T703-Framer-plugin-data-storage"
    ];

    public static readonly string[] RequiredBfaRules =
    [
        "CF-804_TEXT_NODE_FILTER",
        "CF-805_TEXT_STYLE_MAPPING",
        "CF-806_PLUGIN_DATA_STORAGE",
        "CF-FR1-NO_FIGMA_RUNTIME",
        "CF-FR1-PACKAGE-01",
        "CF-FR1-LIVE-AUTH-01",
        "CF-FR1-SCOPE-01"
    ];

    public static readonly string[] FontStyleNames =
    [
        "Thin", "ExtraLight", "Light", "Regular", "Medium", "SemiBold", "Bold", "ExtraBold", "Black"
    ];

    public const string AdapterRunsCollection = "xiigen-framer-adapter-runs";
    public const string PublicationAuditCollection = "xiigen-framer-adapter-publications";
}
