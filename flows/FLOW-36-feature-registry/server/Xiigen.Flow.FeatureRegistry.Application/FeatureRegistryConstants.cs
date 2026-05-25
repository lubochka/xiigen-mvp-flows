namespace Xiigen.Flow.FeatureRegistry.Application;

public static class FeatureRegistryConstants
{
    public const string FlowId = "FLOW-36";
    public const string FlowSlug = "feature-registry";
    public const string ImplementationAlias = "feature-registry";
    public const string DynamicEndpoint = "/api/dynamic/feature-registry";
    public const string PackageName = "Xiigen.Flow.FeatureRegistry";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(567, 7).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = ["CF-808", "CF-809", "CF-813", .. Enumerable.Range(1035, 15).Select(value => $"CF-{value}")];

    public const string FeatureCollection = "xiigen-feature-registry-features";
    public const string SignalCollection = "xiigen-feature-registry-signals";
    public const string AdapterCollection = "xiigen-feature-registry-adapters";
    public const string SimulationCollection = "xiigen-feature-registry-simulations";
    public const string AuditCollection = "xiigen-feature-registry-audit";
}

