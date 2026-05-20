namespace Xiigen.Flow.SchemaRegistryDag.Application;

public static class SchemaRegistryDagConstants
{
    public const string FlowId = "FLOW-11";
    public const string FlowSlug = "schema-registry-dag";
    public const string DynamicEndpoint = "/api/dynamic/schema-registry-dag";

    public const string Schemas = "xiigen-schema-registry";
    public const string SchemaHistory = "xiigen-schema-history";
    public const string ApprovalQueue = "xiigen-schema-approval-queue";
    public const string DagTopology = "xiigen-schema-dag-topology";
    public const string DagRenderCache = "xiigen-schema-dag-render-cache";
    public const string ValidationFailures = "xiigen-schema-validation-failures";
    public const string MigrationRuns = "xiigen-schema-migration-runs";
    public const string QualityScores = "xiigen-schema-quality-scores";
    public const string ExportAudits = "xiigen-schema-export-audits";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-11-1",
        "CF-11-2",
        "CF-11-3",
        "CF-11-4",
        "CF-919",
        "CF-920",
        "CF-921",
        "CF-922",
        "CF-923",
        "CF-924",
        "CF-925",
        "CF-926",
        "CF-927",
        "CF-928"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] PublisherRoles =
    [
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public const string RegistrationRequestedEvent = "xiigen.schema-registry.registration-requested.v1";
    public const string VersionCheckInlineEvent = "xiigen.schema-registry.version-check-inline.v1";
    public const string CycleCheckInlineEvent = "xiigen.schema-registry.cycle-check-inline.v1";
    public const string CompatibilityCheckInlineEvent = "xiigen.schema-registry.compatibility-check-inline.v1";
    public const string SchemaValidatedEvent = "xiigen.schema-registry.schema-validated.v1";
    public const string SchemaRejectedEvent = "xiigen.schema-registry.schema-rejected.v1";
    public const string SchemaApprovalRequiredEvent = "xiigen.schema-registry.schema-approval-required.v1";
    public const string SchemaPublishedEvent = "xiigen.schema-registry.schema-published.v1";
    public const string DagRebuiltEvent = "xiigen.schema-registry.dag-rebuilt.v1";
    public const string DagRenderedEvent = "xiigen.schema-registry.dag-rendered.v1";
    public const string SchemaDeprecatedEvent = "xiigen.schema-registry.schema-deprecated.v1";
    public const string MigrationRunRecordedEvent = "xiigen.schema-registry.migration-run-recorded.v1";
    public const string ValidationFailedEvent = "xiigen.schema-registry.validation-failed.v1";
    public const string QualityAnalyzedEvent = "xiigen.schema-registry.quality-analyzed.v1";
    public const string ExportStartedEvent = "xiigen.schema-registry.export-started.v1";
    public const string TimeoutRetainedEvent = "xiigen.schema-registry.timeout-retained.v1";
}
