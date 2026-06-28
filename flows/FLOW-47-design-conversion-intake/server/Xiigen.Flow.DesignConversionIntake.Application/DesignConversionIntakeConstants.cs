namespace Xiigen.Flow.DesignConversionIntake.Application;

public static class DesignConversionIntakeConstants
{
    public const string FlowId = "FLOW-47";
    public const string FlowSlug = "design-conversion-intake";
    public const string PackageName = "@xiigen/design-conversion-intake";
    public const string DynamicEndpoint = "/flow-47/design-conversion";
    public const string JobsCollection = "xiigen-design-conversion-jobs";

    public static readonly string[] SubmitRoles = ["platform_admin"];
    public static readonly string[] StatusRoles = ["platform_admin", "platform_support"];
    public static readonly string[] KnownStatuses = ["queued", "completed", "failed", "dispatch_failed", "not_found"];
}
