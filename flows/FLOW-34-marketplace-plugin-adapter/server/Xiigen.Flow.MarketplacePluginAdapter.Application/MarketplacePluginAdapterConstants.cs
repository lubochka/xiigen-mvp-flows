namespace Xiigen.Flow.MarketplacePluginAdapter.Application;

public static class MarketplacePluginAdapterConstants
{
    public const string FlowId = "FLOW-34";
    public const string FlowSlug = "marketplace-plugin-adapter";
    public const string ImplementationAlias = "marketplace-plugin-adapter";
    public const string DynamicEndpoint = "/api/dynamic/marketplace-plugin-adapter";
    public const string PackageName = "Xiigen.Flow.MarketplacePluginAdapter";

    public static readonly string[] CanonicalRoles = ["anonymous", "public_marketplace_visitor", "tenant_user", "tenant_admin", "business_partner", "platform_admin", "freelancer", "platform_support"];
    public static readonly string[] PublicRoles = ["anonymous"];
    public static readonly string[] OperatorRoles = ["tenant_admin", "business_partner", "platform_admin"];
    public static readonly string[] InspectorRoles = ["tenant_user", "tenant_admin", "business_partner", "platform_admin", "freelancer", "platform_support"];

    public static readonly string[] RequiredTaskTypes = ["T543", "T544", "T545", "T546"];
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1027, 4).Select(value => $"CF-{value}").ToArray();

    public const string BootstrapCollection = "xiigen-platform-adapters";
    public const string RegistryCollection = "xiigen-platform-registry";
    public const string GapCollection = "xiigen-platform-adapter-validation-gaps";
    public const string SmokeCollection = "xiigen-platform-adapter-packages";
    public const string AuditCollection = "xiigen-platform-adapter-audit";
}
