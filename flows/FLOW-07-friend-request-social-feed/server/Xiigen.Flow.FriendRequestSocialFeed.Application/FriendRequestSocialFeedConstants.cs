namespace Xiigen.Flow.FriendRequestSocialFeed.Application;

public static class FriendRequestSocialFeedConstants
{
    public const string FlowId = "FLOW-07";
    public const string FlowSlug = "friend-request-social-feed";
    public const string DynamicEndpoint = "/api/dynamic/friend-request-social-feed";

    public const string FriendRequests = "xiigen-friend-requests";
    public const string Connections = "xiigen-social-connections";
    public const string FeedItems = "xiigen-social-feed-items";
    public const string FeedScores = "xiigen-social-feed-scores";
    public const string FeedDeliveries = "xiigen-social-feed-deliveries";
    public const string Notifications = "xiigen-social-notifications";
    public const string Analytics = "xiigen-social-analytics";
    public const string PrivacyRules = "xiigen-social-privacy-rules";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-07-1",
        "CF-07-2",
        "CF-07-3",
        "CF-07-4",
        "CF-884",
        "CF-885",
        "CF-886",
        "CF-887",
        "CF-888",
        "CF-889",
        "CF-890",
        "CF-891",
        "CF-892",
        "CF-893",
        "CF-894"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "business_partner",
        "platform_admin"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "business_partner",
        "platform_admin"
    ];

    public const string RequestRequestedEvent = "xiigen.friend-request.request-requested.v1";
    public const string RequestSentEvent = "xiigen.friend-request.request-sent.v1";
    public const string RequestAcceptedEvent = "xiigen.friend-request.request-accepted.v1";
    public const string RequestDeclinedEvent = "xiigen.friend-request.request-declined.v1";
    public const string ConnectionEstablishedEvent = "xiigen.friend-request.connection-established.v1";
    public const string FeedItemGeneratedEvent = "xiigen.friend-request.feed-item-generated.v1";
    public const string FeedItemScoredEvent = "xiigen.friend-request.feed-item-scored.v1";
    public const string FeedDeliveredEvent = "xiigen.friend-request.feed-delivered.v1";
    public const string NotificationSentEvent = "xiigen.friend-request.notification-sent.v1";
    public const string AnalyticsRecordedEvent = "xiigen.friend-request.analytics-recorded.v1";
    public const string TimeoutRetainedEvent = "xiigen.friend-request.timeout-retained.v1";
}
