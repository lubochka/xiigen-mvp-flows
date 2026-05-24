namespace Xiigen.Flow.DynamicFormsWorkflows.Application;

public static class DynamicFormsWorkflowsConstants
{
    public const string FlowId = "FLOW-21";
    public const string FlowSlug = "dynamic-forms-workflows";
    public const string DynamicEndpoint = "/api/dynamic/dynamic-forms-workflows";

    public const string FormSchemas = "xiigen-flow21-form-schemas";
    public const string PublishedSchemaVersions = "xiigen-flow21-published-schema-versions";
    public const string FormSubmissions = "xiigen-flow21-form-submissions";
    public const string AutomationLocks = "xiigen-flow21-automation-locks";
    public const string AutomationDispatches = "xiigen-flow21-automation-dispatches";
    public const string AnalyticsEvents = "xiigen-flow21-analytics-events";
    public const string AuditRecords = "xiigen-flow21-audit-records";

    public const string SchemaPublishedEvent = "xiigen.flow21.forms.schema-published";
    public const string SubmissionAcceptedEvent = "xiigen.flow21.forms.submission-accepted";
    public const string AutomationDispatchedEvent = "xiigen.flow21.forms.automation-dispatched";
    public const string AnalyticsCollectedEvent = "xiigen.flow21.forms.analytics-collected";
    public const string TimeoutRetainedEvent = "xiigen.flow21.forms.timeout-retained";

    public static readonly string[] RuleIds =
    [
        "CF-21-1",
        "CF-21-2",
        "CF-21-3",
        "CF-21-4",
        "CF-969",
        "CF-970",
        "CF-971",
        "CF-972"
    ];

    public static readonly string[] RequiredPlanTaskTypes =
    [
        "T307",
        "T308",
        "T309",
        "T310",
        "T311",
        "T312",
        "T313",
        "T314",
        "T315",
        "T316",
        "T317",
        "T318",
        "T319",
        "T320",
        "T321",
        "T322",
        "T323",
        "T324",
        "T325",
        "T326",
        "T327",
        "T328",
        "T329",
        "T330",
        "T331",
        "T332",
        "T333",
        "T334",
        "T335",
        "T336",
        "T337",
        "T338",
        "T339",
        "T340"
    ];

    public static readonly string[] EnforcementTaskTypes =
    [
        "T629",
        "T630",
        "T631",
        "T632"
    ];

    public static readonly string[] PrimaryRoles =
    [
        "anonymous",
        "tenant_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] AuthorRoles =
    [
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] SubmitterRoles =
    [
        "tenant_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin"
    ];

    public static readonly string[] OperationsRoles =
    [
        "tenant_admin",
        "platform_admin"
    ];
}
