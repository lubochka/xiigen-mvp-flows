namespace Xiigen.Flow.Marketplace.Application;

public static class MarketplaceConstants
{
    public const string FlowId = "FLOW-08";
    public const string FlowSlug = "marketplace";
    public const string DynamicEndpoint = "/api/dynamic/marketplace";

    public const string PublicationAudits = "xiigen-marketplace-publication-audits";
    public const string Listings = "xiigen-marketplace-listings";
    public const string Drafts = "xiigen-marketplace-listing-drafts";
    public const string Catalog = "xiigen-marketplace-catalog";
    public const string FeedBatches = "xiigen-marketplace-feed-batches";
    public const string Analytics = "xiigen-marketplace-analytics";
    public const string ModerationReviews = "xiigen-marketplace-moderation-reviews";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-08-1",
        "CF-08-2",
        "CF-08-3",
        "CF-08-4",
        "CF-895",
        "CF-896",
        "CF-897",
        "CF-898",
        "CF-899",
        "CF-900",
        "CF-901",
        "CF-902"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "tenant_admin",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "tenant_admin",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "platform_admin",
        "platform_support"
    ];

    public const string SaveRequestedEvent = "xiigen.marketplace.save-requested.v1";
    public const string ModerationInlineEvent = "xiigen.marketplace.moderation-inline.v1";
    public const string PriceCheckInlineEvent = "xiigen.marketplace.price-check-inline.v1";
    public const string ListingPublishedEvent = "xiigen.marketplace.listing-published.v1";
    public const string ListingDraftedEvent = "xiigen.marketplace.listing-drafted.v1";
    public const string ListingIndexedEvent = "xiigen.marketplace.listing-indexed.v1";
    public const string FeedGeneratedEvent = "xiigen.marketplace.feed-generated.v1";
    public const string AnalyticsRecordedEvent = "xiigen.marketplace.analytics-recorded.v1";
    public const string HumanReviewRequestedEvent = "xiigen.marketplace.moderation-human-review-requested.v1";
    public const string TimeoutRetainedEvent = "xiigen.marketplace.timeout-retained.v1";
}
