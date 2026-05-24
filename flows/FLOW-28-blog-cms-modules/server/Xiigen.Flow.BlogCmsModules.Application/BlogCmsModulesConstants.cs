namespace Xiigen.Flow.BlogCmsModules.Application;

public static class BlogCmsModulesConstants
{
    public const string FlowId = "FLOW-28";
    public const string FlowSlug = "blog-cms-modules";
    public const string ImplementationAlias = "blog-cms-modules";
    public const string DynamicEndpoint = "/api/dynamic/blog-cms-modules";
    public const string PackageName = "Xiigen.Flow.BlogCmsModules";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "business_partner",
        "event_organiser",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ReaderRoles = ["anonymous", "public_marketplace_visitor", "tenant_user", "tenant_admin", "freelancer", "business_partner", "event_organiser", "platform_admin", "platform_support"];
    public static readonly string[] AuthorRoles = ["tenant_user", "tenant_admin", "freelancer", "business_partner", "event_organiser", "platform_admin"];
    public static readonly string[] AdminRoles = ["tenant_admin", "platform_admin"];
    public static readonly string[] SupportRoles = ["platform_support", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(423, 18).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredStressTests = Enumerable.Range(345, 24).Select(value => $"ST-{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(566, 35).Select(value => $"CF-{value}").Concat(Enumerable.Range(1003, 4).Select(value => $"CF-{value}")).ToArray();

    public const string ContentCollection = "xiigen-blog-cms-content";
    public const string DraftCollection = "xiigen-blog-cms-drafts";
    public const string CommentCollection = "xiigen-blog-cms-comments";
    public const string PublishCollection = "xiigen-blog-cms-publish-gates";
    public const string StressCollection = "xiigen-blog-cms-stress";
    public const string AuditCollection = "xiigen-blog-cms-audit";
}
