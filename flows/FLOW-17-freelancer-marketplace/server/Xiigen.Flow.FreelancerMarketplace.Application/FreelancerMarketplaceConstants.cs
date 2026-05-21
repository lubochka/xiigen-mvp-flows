namespace Xiigen.Flow.FreelancerMarketplace.Application;

public static class FreelancerMarketplaceConstants
{
    public const string FlowId = "FLOW-17";
    public const string FlowSlug = "freelancer-marketplace";
    public const string DynamicEndpoint = "/api/dynamic/freelancer-marketplace";

    public const string GigPostings = "xiigen-flow17-gig-postings";
    public const string GigAcceptLocks = "xiigen-flow17-gig-accept-locks";
    public const string Bids = "xiigen-flow17-bids";
    public const string GigAudits = "xiigen-flow17-gig-audits";
    public const string Contracts = "xiigen-flow17-contracts";
    public const string ContractAudits = "xiigen-flow17-contract-audits";
    public const string Deliveries = "xiigen-flow17-deliveries";
    public const string Milestones = "xiigen-flow17-milestones";
    public const string EscrowAudits = "xiigen-flow17-escrow-audits";
    public const string Reviews = "xiigen-flow17-reviews";
    public const string ReviewAudits = "xiigen-flow17-review-audits";
    public const string CompensationSteps = "xiigen-flow17-compensation-steps";
    public const string FreedomConfig = "xiigen-flow17-freedom-config";

    public const string GigAcceptedEvent = "xiigen.flow17.gig-accepted.v1";
    public const string GigAcceptanceFailedEvent = "xiigen.flow17.gig-acceptance-failed.v1";
    public const string FreelancerContractActivatedEvent = "xiigen.flow17.contract-activated.v1";
    public const string ContractFieldImmutableEvent = "xiigen.flow17.contract-field-immutable.v1";
    public const string MilestoneSumMismatchEvent = "xiigen.flow17.milestone-sum-mismatch.v1";
    public const string MilestoneReleasedEvent = "xiigen.flow17.milestone-released.v1";
    public const string MilestoneDisputedEvent = "xiigen.flow17.milestone-disputed.v1";
    public const string EscrowCompensationStartedEvent = "xiigen.flow17.escrow-compensation-started.v1";
    public const string FreelancerReviewSubmittedEvent = "xiigen.flow17.review-submitted.v1";
    public const string DuplicateReviewRejectedEvent = "xiigen.flow17.duplicate-review-rejected.v1";
    public const string TimeoutRetainedEvent = "xiigen.flow17.timeout-retained.v1";

    public static readonly string[] RuleIds =
    [
        "CF-17-1",
        "CF-17-2",
        "CF-17-3",
        "CF-17-4",
        "CF-286",
        "CF-287",
        "CF-288",
        "CF-289",
        "CF-290",
        "CF-291",
        "CF-953",
        "CF-954",
        "CF-955",
        "CF-956"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T613",
        "T614",
        "T615",
        "T616"
    ];

    public static readonly string[] Roles =
    [
        "anonymous",
        "public_visitor",
        "tenant_user",
        "tenant_admin",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "platform_admin",
        "platform_support"
    ];

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

    public static readonly string[] AdminRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] EscrowCompensationStepNames =
    [
        "RESTORE_GIG_STATUS",
        "REFUND_MILESTONE"
    ];
}
