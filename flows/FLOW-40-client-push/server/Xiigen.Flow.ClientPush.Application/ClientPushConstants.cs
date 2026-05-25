namespace Xiigen.Flow.ClientPush.Application;

public static class ClientPushConstants
{
    public const string FlowId = "FLOW-40";
    public const string FlowSlug = "client-push";
    public const string ImplementationAlias = "client-push";
    public const string DynamicEndpoint = "/api/dynamic/client-push";
    public const string PackageName = "Xiigen.Flow.ClientPush";

    public static readonly string[] CanonicalRoles = ["platform_admin", "platform_support", "tenant_admin"];
    public static readonly string[] PublicRoles = [];
    public static readonly string[] OperatorRoles = ["platform_admin", "tenant_admin"];
    public static readonly string[] InspectorRoles = ["platform_admin", "platform_support", "tenant_admin"];

    public static readonly string[] RequiredTaskTypes =
    [
        "T587-SSE-CONNECTION-MANAGER",
        "T588-FLOW-EVENT-BRIDGE",
        "T589-SSE-KEEPALIVE-SCHEDULER"
    ];

    public static readonly string[] RequiredBfaRules = ["CF-797", "CF-798", "CF-1062", "CF-1063", "CF-1064", "CF-1065"];
    public static readonly string[] P10BfaRules = ["CF-1062", "CF-1063", "CF-1064", "CF-1065"];

    public static readonly string[] ProhibitedEmitDomains =
    [
        "xiigen.user-registration",
        "xiigen.content",
        "xiigen.social",
        "xiigen.commerce",
        "xiigen.onboarding"
    ];

    public const string ConnectionsCollection = "xiigen-sse-connections";
    public const string DeliveryAuditCollection = "xiigen-client-push-deliveries";
    public const string KeepaliveAuditCollection = "xiigen-client-push-keepalive";

    public const int DefaultKeepaliveIntervalMs = 30_000;
    public const int DefaultDroppedThresholdMs = 120_000;
}
