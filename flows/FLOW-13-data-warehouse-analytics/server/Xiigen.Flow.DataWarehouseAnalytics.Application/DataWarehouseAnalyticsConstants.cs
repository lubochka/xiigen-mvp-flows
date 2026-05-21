namespace Xiigen.Flow.DataWarehouseAnalytics.Application;

public static class DataWarehouseAnalyticsConstants
{
    public const string FlowId = "FLOW-13";
    public const string FlowSlug = "data-warehouse-analytics";
    public const string DynamicEndpoint = "/api/dynamic/data-warehouse-analytics";

    public const string WarehousePlans = "xiigen-data-warehouse-analytics-plans";
    public const string RawEvents = "xiigen-data-warehouse-analytics-raw-events";
    public const string MetricFacts = "xiigen-data-warehouse-analytics-metric-facts";
    public const string AnalyticsAudits = "xiigen-data-warehouse-analytics-audits";
    public const string QueryAudits = "xiigen-data-warehouse-analytics-query-audits";
    public const string QualityAudits = "xiigen-data-warehouse-analytics-quality-audits";

    public const string PlanPublishedEvent = "xiigen.data-warehouse-analytics.plan-published.v1";
    public const string EventIngestedEvent = "xiigen.data-warehouse-analytics.event-ingested.v1";
    public const string QueryExecutedEvent = "xiigen.data-warehouse-analytics.query-executed.v1";
    public const string QualityCheckCompletedEvent = "xiigen.data-warehouse-analytics.quality-check-completed.v1";
    public const string TimeoutRetainedEvent = "xiigen.data-warehouse-analytics.timeout-retained.v1";

    public static readonly string[] RuleIds =
    [
        "CF-935",
        "CF-936",
        "CF-937",
        "CF-938",
        "CF-939"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T169",
        "T170",
        "T171",
        "T172",
        "T173",
        "T174",
        "T175",
        "T176",
        "T177",
        "T178",
        "T179",
        "T180",
        "T181",
        "T182",
        "T183",
        "T184",
        "T185",
        "T186",
        "T187",
        "T188"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] AnalyticsOperatorRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support",
        "business_partner"
    ];

    public static readonly string[] PublicRoles =
    [
        "anonymous",
        "public_marketplace_visitor"
    ];
}
