namespace Xiigen.Flow.CmsPublishing.Application;

public static class CmsPublishingConstants
{
    public const string FlowId = "FLOW-22";
    public const string FlowSlug = "cms-publishing";
    public const string DynamicEndpoint = "/api/dynamic/cms-publishing";
    public const string PackageName = "Xiigen.Flow.CmsPublishing";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "tenant_user",
        "content_author",
        "content_editor",
        "legal_reviewer",
        "publisher",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] AuthorRoles = ["content_author", "content_editor", "publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] ApprovalRoles = ["content_editor", "legal_reviewer", "publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] PublisherRoles = ["publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] AnalyticsRoles = ["content_editor", "publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] ReaderRoles = ["tenant_user", "content_author", "content_editor", "legal_reviewer", "publisher", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T341", "T342", "T343", "T344", "T345", "T346", "T347", "T348", "T349", "T350",
        "T351", "T352", "T353", "T354", "T355", "T356", "T357", "T358", "T359", "T360",
        "T361", "T362", "T363", "T364", "T365", "T366", "T367", "T368", "T369", "T370",
        "T371", "T372", "T373", "T374", "T375", "T376", "T377", "T378", "T379", "T380"
    ];

    public static readonly string[] ServiceTaskTypes = ["T633", "T634", "T635", "T636"];

    public const string ContentCollection = "xiigen-content";
    public const string ApprovalCollection = "xiigen-content-approvals";
    public const string AnalyticsCollection = "xiigen-content-analytics";
    public const string ScheduleLockCollection = "xiigen-publish-schedule-locks";
    public const string AuditCollection = "xiigen-content-audit";
}