namespace Xiigen.Flow.EtlDataIntegration.Application;

public static class EtlDataIntegrationConstants
{
    public const string FlowId = "FLOW-14";
    public const string FlowSlug = "etl-data-integration";
    public const string DynamicEndpoint = "/api/dynamic/etl-data-integration";

    public const string EtlPipelines = "xiigen-etl-data-integration-pipelines";
    public const string RawLandingRecords = "xiigen-etl-data-integration-raw-landing";
    public const string StagingRecords = "xiigen-etl-data-integration-staging-records";
    public const string EtlAudits = "xiigen-etl-data-integration-audits";
    public const string QueryAudits = "xiigen-etl-data-integration-query-audits";
    public const string DriftAudits = "xiigen-etl-data-integration-drift-audits";

    public const string PipelinePublishedEvent = "xiigen.etl-data-integration.pipeline-published.v1";
    public const string SourceEventIngestedEvent = "xiigen.etl-data-integration.source-event-ingested.v1";
    public const string QueryExecutedEvent = "xiigen.etl-data-integration.query-executed.v1";
    public const string DriftCheckCompletedEvent = "xiigen.etl-data-integration.drift-check-completed.v1";
    public const string TimeoutRetainedEvent = "xiigen.etl-data-integration.timeout-retained.v1";
    public const int RequiredEventSchemaCount = 25;

    public static readonly string[] RuleIds =
    [
        "CF-940",
        "CF-941",
        "CF-942",
        "CF-943",
        "CF-944"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T213",
        "T214",
        "T215",
        "T216",
        "T217",
        "T218",
        "T219",
        "T220",
        "T221",
        "T222",
        "T223",
        "T224"
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
