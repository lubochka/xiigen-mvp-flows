namespace Xiigen.Flow.TenantLifecycleManager.Application;

public static class TenantLifecycleManagerConstants
{
    public const string FlowId = "FLOW-30";
    public const string FlowSlug = "tenant-lifecycle-manager";
    public const string ImplementationAlias = "tenant-lifecycle";
    public const string DynamicEndpoint = "/api/dynamic/tenant-lifecycle-manager";
    public const string PackageName = "Xiigen.Flow.TenantLifecycleManager";

    public static readonly string[] CanonicalRoles = ["tenant_admin", "platform_admin", "platform_support"];
    public static readonly string[] ControlPlaneRoles = ["platform_admin"];
    public static readonly string[] SupportRoles = ["platform_admin", "platform_support"];
    public static readonly string[] TenantStatusRoles = ["tenant_admin", "platform_admin", "platform_support"];

    public static readonly string[] RequiredTaskTypes = Enumerable.Range(470, 10).Select(value => $"T{value}").ToArray();
    public static readonly string[] RequiredBfaRules = Enumerable.Range(1011, 4).Select(value => $"CF-{value}").ToArray();
    public static readonly string[] RequiredLifecycleStates =
    [
        "requested",
        "provisioning",
        "active",
        "health_degraded",
        "suspended",
        "offboarding",
        "archived",
        "failed"
    ];

    public const string LifecycleCollection = "xiigen-tenant-lifecycle-records";
    public const string HealthCollection = "xiigen-tenant-lifecycle-health";
    public const string OffboardingCollection = "xiigen-tenant-lifecycle-offboarding";
    public const string AuditCollection = "xiigen-tenant-lifecycle-audit";
}
