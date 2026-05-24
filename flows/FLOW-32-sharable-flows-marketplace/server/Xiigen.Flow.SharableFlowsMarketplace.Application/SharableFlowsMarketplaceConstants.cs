namespace Xiigen.Flow.SharableFlowsMarketplace.Application;

public static class SharableFlowsMarketplaceConstants
{
    public const string FlowId = "FLOW-32";
    public const string FlowSlug = "sharable-flows-marketplace";
    public const string ImplementationAlias = "sharable-flows-marketplace";
    public const string DynamicEndpoint = "/api/dynamic/sharable-flows-marketplace";
    public const string PackageName = "Xiigen.Flow.SharableFlowsMarketplace";

    public static readonly string[] CanonicalRoles = ["anonymous", "public_marketplace_visitor", "tenant_user", "tenant_admin", "freelancer", "business_partner", "platform_admin", "referral_user", "platform_support", "event_organiser"];
    public static readonly string[] PublicRoles = ["anonymous", "public_marketplace_visitor", "tenant_user", "referral_user"];
    public static readonly string[] InstallerRoles = ["tenant_admin", "platform_admin"];
    public static readonly string[] PublisherRoles = ["freelancer", "business_partner", "platform_admin"];
    public static readonly string[] CuratorRoles = ["platform_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(516, 20).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1019, 4).Select(value => $"CF-{value}").ToArray();

    public const string PackageCollection = "xiigen-sharable-flow-packages";
    public const string InstallCollection = "xiigen-sharable-flow-installs";
    public const string BlueprintCollection = "xiigen-sharable-rag-blueprints";
    public const string UsageCollection = "xiigen-sharable-flow-usage";
    public const string SettlementCollection = "xiigen-sharable-flow-settlements";
    public const string SandboxCollection = "xiigen-sharable-flow-sandboxes";
    public const string AuditCollection = "xiigen-sharable-flow-audit";
}