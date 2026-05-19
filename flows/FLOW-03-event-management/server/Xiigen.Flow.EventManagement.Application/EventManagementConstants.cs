namespace Xiigen.Flow.EventManagement.Application;

public static class EventManagementConstants
{
    public const string FlowId = "FLOW-03";
    public const string FlowSlug = "event-management";
    public const string DynamicEndpoint = "/api/dynamic/event-management";

    public const string Events = "xiigen-dotnet-events";
    public const string Registrations = "xiigen-dotnet-event-registrations";
    public const string Promotions = "xiigen-dotnet-event-promotions";
    public const string AnalyticsCounters = "xiigen-dotnet-event-analytics";
    public const string AudienceMatches = "xiigen-dotnet-event-audience-matches";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-03-1",
        "CF-03-2",
        "CF-03-3",
        "CF-03-4",
        "CF-858",
        "CF-859",
        "CF-860",
        "CF-861",
        "CF-862"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "business_partner",
        "freelancer",
        "event_organiser",
        "platform_admin"
    ];

    public const string CreationRequestedEvent = "xiigen.event-management.creation-requested.v1";
    public const string EventCreatedEvent = "xiigen.event-management.event-created.v1";
    public const string EventPublishedEvent = "xiigen.event-management.event-published.v1";
    public const string PromotionRequestedEvent = "xiigen.event-management.promotion-requested.v1";
    public const string PromotionPublishedEvent = "xiigen.event-management.promotion-published.v1";
    public const string PromotionRejectedEvent = "xiigen.event-management.promotion-rejected.v1";
    public const string RegistrationRecordedEvent = "xiigen.event-management.registration-recorded.v1";
    public const string AnalyticsFinalizedEvent = "xiigen.event-management.analytics-finalized.v1";
    public const string PromotionCampaignCompletedEvent = "PromotionCampaignCompleted";
    public const string TimeoutRetainedEvent = "xiigen.event-management.timeout-retained.v1";
}

