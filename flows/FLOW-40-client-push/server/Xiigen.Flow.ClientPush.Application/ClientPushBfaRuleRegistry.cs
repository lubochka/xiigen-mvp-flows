namespace Xiigen.Flow.ClientPush.Application;

public static class ClientPushBfaRuleRegistry
{
    public static readonly IReadOnlyList<ClientPushBfaRule> Rules =
    [
        new("CF-797", ClientPushConstants.FlowId, "CROSS_DOMAIN_READ", "FlowEventBridge may subscribe to user-facing events but must not emit into user-facing flow domains.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-798", ClientPushConstants.FlowId, "TENANT_ISOLATION", "ISseConnectionPool.pushEvent must scope by tenant and block cross-tenant delivery attempts.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1062", ClientPushConstants.FlowId, "CONCURRENCY_CONSTRAINT", "POST /api/dynamic/client-push uses optimistic concurrency; stale writers return 409 CONCURRENT_UPDATE.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1063", ClientPushConstants.FlowId, "IDEMPOTENCY_CONSTRAINT", "X-Idempotency-Key replay returns cached 200 with no duplicate side effects.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1064", ClientPushConstants.FlowId, "FIELD_VALIDATION", "Empty, null, or zero primary fields return field-level 400 errors and write no partial state.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT"),
        new("CF-1065", ClientPushConstants.FlowId, "DNA8_ORDERING", "Fabric timeout returns 504 TIMEOUT while retaining an outbox retry event for queue recovery.", "BUILD_FAILURE", "FLOW_SCOPED", "GLOBAL", "TENANT_CONTEXT")
    ];
}
