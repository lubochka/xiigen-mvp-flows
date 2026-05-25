# FLOW-40 Type A Adaptation Surface

The tenant adaptation surface may rename the module and tune presentation copy, but it must not change the machine rules that protect the tenant-scoped client push channel.

FREEDOM keys:

- `flow40_module_name`
- `client_push_keepalive_interval_ms`
- `client_push_dropped_connection_threshold_ms`
- `client_push_operator_copy_variant`

Tenant A adaptation: Acme live event bridge.
Tenant B adaptation: Northwind SSE ops console.
Tenant C adaptation: Tessera realtime client bridge.

Locked rules: FlowEventBridge must not emit into user-facing flow domains, SSE connection pool pushes must be tenant scoped, stale writers receive 409, repeated operation keys replay cached responses, expired correlation ids fail closed, missing connections do not retry, keepalive cleanup runs before ping emission, tenant scope comes from TenantContext, and every successful event bridge mutation writes state and appends outbox before connection-pool delivery.

