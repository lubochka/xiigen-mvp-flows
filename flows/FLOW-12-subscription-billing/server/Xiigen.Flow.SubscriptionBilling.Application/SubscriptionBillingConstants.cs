namespace Xiigen.Flow.SubscriptionBilling.Application;

public static class SubscriptionBillingConstants
{
    public const string FlowId = "FLOW-12";
    public const string FlowSlug = "subscription-billing";
    public const string DynamicEndpoint = "/api/dynamic/subscription-billing";

    public const string Plans = "xiigen-subscription-billing-plans";
    public const string Subscriptions = "xiigen-subscription-billing-subscriptions";
    public const string Invoices = "xiigen-subscription-billing-invoices";
    public const string BillingAudits = "xiigen-subscription-billing-audits";
    public const string MrrMetrics = "xiigen-subscription-billing-mrr";

    public const string PlanPublishedEvent = "xiigen.subscription-billing.plan-published.v1";
    public const string PlanRejectedEvent = "xiigen.subscription-billing.plan-rejected.v1";
    public const string SubscriptionActivatedEvent = "xiigen.subscription-billing.subscription-activated.v1";
    public const string SubscriptionRejectedEvent = "xiigen.subscription-billing.subscription-rejected.v1";
    public const string InvoiceChargedEvent = "xiigen.subscription-billing.invoice-charged.v1";
    public const string InvoiceChargeFailedEvent = "xiigen.subscription-billing.invoice-charge-failed.v1";
    public const string InvoiceVoidedEvent = "xiigen.subscription-billing.invoice-voided.v1";
    public const string MrrRecordedEvent = "xiigen.subscription-billing.mrr-recorded.v1";
    public const string TimeoutRetainedEvent = "xiigen.subscription-billing.timeout-retained.v1";
    public const string SubscriptionCancelledEvent = "xiigen.subscription-billing.subscription-cancelled.v1";

    public static readonly string[] RuleIds =
    [
        "CF-12-1",
        "CF-12-2",
        "CF-12-3",
        "CF-12-4",
        "CF-929",
        "CF-930",
        "CF-931",
        "CF-932",
        "CF-933",
        "CF-934"
    ];

    public static readonly string[] ProtectedRoles =
    [
        "tenant_user",
        "referral_user",
        "freelancer",
        "business_partner",
        "event_organiser",
        "tenant_admin",
        "platform_admin",
        "platform_support"
    ];

    public static readonly string[] BillingOperatorRoles =
    [
        "tenant_admin",
        "platform_admin",
        "platform_support",
        "business_partner"
    ];

    public static readonly string[] PublicRoles =
    [
        "anonymous",
        "public_marketplace_visitor"
    ];
}
