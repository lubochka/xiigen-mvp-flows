namespace Xiigen.Flow.FormBuilderTemplates.Application;

public static class FormBuilderTemplatesConstants
{
    public const string FlowId = "FLOW-23";
    public const string FlowSlug = "form-builder-templates";
    public const string DynamicEndpoint = "/api/dynamic/form-builder-templates";
    public const string PackageName = "Xiigen.Flow.FormBuilderTemplates";

    public static readonly string[] CanonicalRoles =
    [
        "anonymous",
        "tenant_user",
        "template_designer",
        "template_reviewer",
        "form_operator",
        "template_publisher",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] DesignerRoles = ["template_designer", "template_publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] ReviewerRoles = ["template_reviewer", "template_publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] OperatorRoles = ["form_operator", "template_designer", "tenant_admin", "platform_admin"];
    public static readonly string[] PublisherRoles = ["template_publisher", "tenant_admin", "platform_admin"];
    public static readonly string[] ReaderRoles = ["tenant_user", "template_designer", "template_reviewer", "form_operator", "template_publisher", "tenant_admin", "platform_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T381", "T382", "T383", "T384", "T385", "T386", "T387", "T388", "T389", "T390",
        "T391", "T392", "T393", "T394", "T395", "T396", "T397", "T398", "T399", "T400",
        "T401", "T402", "T403", "T404", "T405", "T406", "T407", "T408", "T409", "T410",
        "T411", "T412", "T413", "T414", "T415", "T416", "T417", "T418", "T419", "T420"
    ];

    public static readonly string[] ServiceTaskTypes = ["T637", "T638", "T639", "T640"];
    public static readonly string[] AllowedFieldTypes = ["string", "number", "boolean", "object", "array", "null"];

    public const string TemplateCollection = "xiigen-form-templates";
    public const string InstanceCollection = "xiigen-form-instances";
    public const string MetricsCollection = "xiigen-form-template-metrics";
    public const string LockCollection = "xiigen-form-template-locks";
    public const string AuditCollection = "xiigen-form-template-audit";
}
