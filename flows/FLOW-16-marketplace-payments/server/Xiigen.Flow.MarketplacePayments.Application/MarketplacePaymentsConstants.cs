namespace Xiigen.Flow.MarketplacePayments.Application;

public static class MarketplacePaymentsConstants
{
    public const string FlowId = "FLOW-16";
    public const string FlowSlug = "marketplace-payments";
    public const string DynamicEndpoint = "/api/dynamic/marketplace-payments";

    public const string CartLocks = "xiigen-marketplace-payments-cart-locks";
    public const string Inventory = "xiigen-marketplace-payments-inventory";
    public const string CheckoutAudits = "xiigen-marketplace-payments-checkout-audits";
    public const string PaymentLocks = "xiigen-marketplace-payments-payment-locks";
    public const string PaymentRecords = "xiigen-marketplace-payments-payment-records";
    public const string PaymentAudits = "xiigen-marketplace-payments-payment-audits";
    public const string NonRepudiationAudits = "xiigen-marketplace-payments-non-repudiation-audits";
    public const string EscrowHolds = "xiigen-marketplace-payments-escrow-holds";
    public const string EscrowAudits = "xiigen-marketplace-payments-escrow-audits";
    public const string CompensationStepRecords = "xiigen-marketplace-payments-compensation-steps";
    public const string PayoutLocks = "xiigen-marketplace-payments-payout-locks";
    public const string PayoutRecords = "xiigen-marketplace-payments-payout-records";
    public const string PayoutAudits = "xiigen-marketplace-payments-payout-audits";
    public const string SellerVaultRefs = "xiigen-marketplace-payments-seller-vault-refs";
    public const string SellerPayoutHolds = "xiigen-marketplace-payments-seller-payout-holds";
    public const string RefundQueue = "xiigen-marketplace-payments-refund-queue";
    public const string FreedomConfig = "xiigen-marketplace-payments-freedom-config";

    public const string CheckoutReservedEvent = "xiigen.marketplace-payments.checkout-reserved.v1";
    public const string CheckoutRejectedEvent = "xiigen.marketplace-payments.checkout-rejected.v1";
    public const string MarketplaceOrderConfirmedEvent = "xiigen.marketplace-payments.order-confirmed.v1";
    public const string EscrowReleasedEvent = "xiigen.marketplace-payments.escrow-released.v1";
    public const string EscrowDisputedEvent = "xiigen.marketplace-payments.escrow-disputed.v1";
    public const string PaymentReversedEvent = "xiigen.marketplace-payments.payment-reversed.v1";
    public const string PayoutCompletedEvent = "xiigen.marketplace-payments.payout-completed.v1";
    public const string PayoutHoldNotifiedEvent = "xiigen.marketplace-payments.payout-hold-notified.v1";
    public const string TimeoutRetainedEvent = "xiigen.marketplace-payments.timeout-retained.v1";

    public static readonly string[] RuleIds =
    [
        "CF-16-1",
        "CF-16-2",
        "CF-16-3",
        "CF-16-4",
        "CF-262",
        "CF-265",
        "CF-268",
        "CF-949",
        "CF-950",
        "CF-951",
        "CF-952"
    ];

    public static readonly string[] RequiredTaskTypes =
    [
        "T609",
        "T610",
        "T611",
        "T612"
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

    public static readonly string[] CompensationStepNames =
    [
        "REFUND_PAYMENT",
        "RESTORE_INVENTORY"
    ];
}
