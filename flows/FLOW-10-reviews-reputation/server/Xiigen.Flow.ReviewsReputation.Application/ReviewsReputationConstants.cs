namespace Xiigen.Flow.ReviewsReputation.Application;

public static class ReviewsReputationConstants
{
    public const string FlowId = "FLOW-10";
    public const string FlowSlug = "reviews-reputation";
    public const string DynamicEndpoint = "/api/dynamic/reviews-reputation";

    public const string Reviews = "xiigen-reviews";
    public const string ReviewAudit = "xiigen-review-audit";
    public const string ModerationQueue = "xiigen-moderation-queue";
    public const string ReputationScores = "xiigen-reputation-scores";
    public const string ReviewResponses = "xiigen-review-responses";
    public const string ResponseAudit = "xiigen-response-audit";

    public static readonly IReadOnlySet<string> RuleIds = new HashSet<string>
    {
        "CF-10-1",
        "CF-10-2",
        "CF-10-3",
        "CF-10-4",
        "CF-913",
        "CF-914",
        "CF-915",
        "CF-916",
        "CF-917",
        "CF-918"
    };

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "freelancer",
        "business_partner",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ModerationRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] ResponseRoles =
    [
        "business_partner",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] VisualRoles =
    [
        "anonymous",
        "public_marketplace_visitor",
        "tenant_user",
        "freelancer",
        "business_partner",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public const string ReviewAcceptedEvent = "xiigen.reviews-reputation.review.accepted.v1";
    public const string ReviewSubmissionCompletedEvent = "xiigen.reviews-reputation.review.submission.completed.v1";
    public const string ReviewPublishedEvent = "xiigen.reviews-reputation.review.published.v1";
    public const string ReviewRejectedEvent = "xiigen.reviews-reputation.review.rejected.v1";
    public const string ReviewFlaggedForHumanEvent = "xiigen.reviews-reputation.review.flagged-for-human.v1";
    public const string ReputationUpdatedEvent = "xiigen.reviews-reputation.reputation.updated.v1";
    public const string ReviewResponseRejectedEvent = "xiigen.reviews-reputation.review.response.rejected.v1";
    public const string ReviewResponsePublishedEvent = "xiigen.reviews-reputation.review.response.published.v1";
    public const string ReviewRetractedEvent = "xiigen.reviews-reputation.review.retracted.v1";
    public const string TimeoutRetainedEvent = "xiigen.reviews-reputation.timeout-retained.v1";
}
