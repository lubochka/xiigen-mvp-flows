namespace Xiigen.Flow.UserGroupsCommunities.Application;

public static class UserGroupsCommunitiesConstants
{
    public const string FlowId = "FLOW-06";
    public const string FlowSlug = "user-groups-communities";
    public const string DynamicEndpoint = "/api/dynamic/user-groups-communities";

    public const string Groups = "xiigen-user-groups";
    public const string Memberships = "xiigen-group-memberships";
    public const string JoinRequests = "xiigen-group-join-requests";
    public const string Subscriptions = "xiigen-group-subscriptions";
    public const string Feeds = "xiigen-group-feeds";
    public const string AccessLogs = "xiigen-group-access-logs";
    public const string Analytics = "xiigen-group-analytics";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-06-1",
        "CF-06-2",
        "CF-06-3",
        "CF-06-4",
        "CF-878",
        "CF-879",
        "CF-880",
        "CF-881",
        "CF-882",
        "CF-883"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "platform_admin"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "tenant_admin",
        "freelancer",
        "platform_admin"
    ];

    public const string GroupJoinRequestedEvent = "xiigen.user-groups.join-requested.v1";
    public const string AccessCheckInlineEvent = "xiigen.user-groups.access-check-inline.v1";
    public const string MembershipAcceptedEvent = "xiigen.user-groups.membership-accepted.v1";
    public const string MembershipRejectedEvent = "xiigen.user-groups.membership-rejected.v1";
    public const string MembershipLeftEvent = "xiigen.user-groups.membership-left.v1";
    public const string TierAssignedEvent = "xiigen.user-groups.tier-assigned.v1";
    public const string FeedAccessCheckInlineEvent = "xiigen.user-groups.feed-access-check-inline.v1";
    public const string FeedPopulatedEvent = "xiigen.user-groups.feed-populated.v1";
    public const string AnalyticsUpdatedEvent = "xiigen.user-groups.analytics-updated.v1";
    public const string TimeoutRetainedEvent = "xiigen.user-groups.timeout-retained.v1";
}
