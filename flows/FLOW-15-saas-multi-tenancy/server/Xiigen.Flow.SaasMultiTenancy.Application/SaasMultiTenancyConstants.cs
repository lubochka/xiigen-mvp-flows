namespace Xiigen.Flow.SaasMultiTenancy.Application;

public static class SaasMultiTenancyConstants
{
    public const string FlowId = "FLOW-15";
    public const string FlowSlug = "saas-multi-tenancy";
    public const string DynamicEndpoint = "/api/dynamic/saas-multi-tenancy";
    public const string MasterTenantId = "xiigen-master-00000000-0000-0000-0000-000000000001";

    public const string Tenants = "xiigen-saas-multi-tenancy-tenants";
    public const string FreedomConfig = "xiigen-saas-multi-tenancy-freedom-config";
    public const string ProvisionLocks = "xiigen-saas-multi-tenancy-provision-locks";
    public const string ProvisionAudits = "xiigen-saas-multi-tenancy-provision-audits";
    public const string ConfigAudits = "xiigen-saas-multi-tenancy-config-audits";
    public const string QuotaCounters = "xiigen-saas-multi-tenancy-quota-counters";
    public const string LifecycleAudits = "xiigen-saas-multi-tenancy-lifecycle-audits";
    public const string TierDefinitions = "xiigen-saas-multi-tenancy-tier-definitions";

    public const string TenantProvisionedEvent = "xiigen.saas-multi-tenancy.tenant-provisioned.v1";
    public const string TenantProvisioningFailedEvent = "xiigen.saas-multi-tenancy.tenant-provisioning-failed.v1";
    public const string TenantConfigurationUpdatedEvent = "xiigen.saas-multi-tenancy.tenant-configuration-updated.v1";
    public const string ConfigKeyImmutableEvent = "xiigen.saas-multi-tenancy.config-key-immutable.v1";
    public const string TenantQuotaMaterializedEvent = "xiigen.saas-multi-tenancy.tenant-quota-materialized.v1";
    public const string TenantSuspendedEvent = "xiigen.saas-multi-tenancy.tenant-suspended.v1";
    public const string TenantTerminatedEvent = "xiigen.saas-multi-tenancy.tenant-terminated.v1";
    public const string TenantReactivatedEvent = "xiigen.saas-multi-tenancy.tenant-reactivated.v1";
    public const string TenantLifecycleRejectedEvent = "xiigen.saas-multi-tenancy.tenant-lifecycle-rejected.v1";
    public const string DataPurgeRequestedEvent = "xiigen.data-warehouse-analytics.data-purge-requested.v1";
    public const string TimeoutRetainedEvent = "xiigen.saas-multi-tenancy.timeout-retained.v1";

    public static readonly string[] RuleIds =
    [
        "CF-15-1",
        "CF-15-2",
        "CF-15-3",
        "CF-15-4",
        "CF-945",
        "CF-946",
        "CF-947",
        "CF-948"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T605",
        "T606",
        "T607",
        "T608"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] TenantOperatorRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] MachineLockedKeys =
    [
        "tenantId",
        "masterTenantId",
        "provisionedAt",
        "subscriptionTier"
    ];
}
