namespace Xiigen.Flow.AdsPlatform.Application;

public static class AdsPlatformConstants
{
    public const string FlowId = "FLOW-20";
    public const string FlowSlug = "ads-platform";
    public const string DynamicEndpoint = "/api/dynamic/ads-platform";

    public const string ConsentRecords = "xiigen-flow20-consent-records";
    public const string AuctionBidLocks = "xiigen-flow20-auction-bid-locks";
    public const string AdvertiserBudgets = "xiigen-flow20-advertiser-budgets";
    public const string AuctionAudits = "xiigen-flow20-auction-audits";
    public const string FraudAudits = "xiigen-flow20-fraud-audits";
    public const string PoliticalAudits = "xiigen-flow20-political-audits";
    public const string HumanReviewQueue = "xiigen-flow20-human-review-queue";
    public const string CampaignDeliveries = "xiigen-flow20-campaign-deliveries";

    public const string ConsentGateFailedEvent = "xiigen.flow20.ads.consent-gate-failed";
    public const string AdDeliveryAuthorizedEvent = "xiigen.flow20.ads.delivery-authorized";
    public const string BidAcceptedEvent = "xiigen.flow20.ads.bid-accepted";
    public const string BidRejectedEvent = "xiigen.flow20.ads.bid-rejected";
    public const string FraudDetectedEvent = "xiigen.flow20.ads.fraud-detected";
    public const string FraudCheckPassedEvent = "xiigen.flow20.ads.fraud-check-passed";
    public const string PoliticalReviewPendingEvent = "xiigen.flow20.ads.political-review-pending";
    public const string PoliticalContentApprovedEvent = "xiigen.flow20.ads.political-content-approved";
    public const string ConsentUpdatedEvent = "xiigen.flow20.ads.consent-updated";
    public const string RequestTimeoutRetainedEvent = "xiigen.flow20.ads.timeout-retained";

    public static readonly string[] RuleIds =
    [
        "CF-20-1",
        "CF-20-2",
        "CF-20-3",
        "CF-20-4",
        "CF-965",
        "CF-966",
        "CF-967",
        "CF-968"
    ];

    public static readonly string[] RequiredPlanTaskTypes =
    [
        "T287",
        "T288",
        "T289",
        "T290",
        "T291",
        "T292",
        "T293",
        "T294",
        "T295",
        "T296",
        "T297",
        "T298",
        "T299",
        "T300",
        "T301",
        "T302",
        "T303",
        "T304",
        "T305",
        "T306"
    ];

    public static readonly string[] EnforcementTaskTypes =
    [
        "T625",
        "T626",
        "T627",
        "T628"
    ];

    public static readonly string[] PrimaryRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "tenant_admin",
        "ad_moderator",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "tenant_admin",
        "ad_moderator",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] AdvertiserRoles =
    [
        "business_partner",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ConsentRoles =
    [
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ReviewRoles =
    [
        "tenant_admin",
        "ad_moderator",
        "platform_admin",
        "platform_support"
    ];
}
