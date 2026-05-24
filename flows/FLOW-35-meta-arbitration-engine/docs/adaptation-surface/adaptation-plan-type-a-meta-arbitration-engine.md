# FLOW-35 Type-A Adaptation Plan

Flow: FLOW-35 meta-arbitration-engine

Allowed adaptation keys:
- `flow35_module_name`
- `flow35_meta_policy_set`
- `flow35_escalation_sla_hours`
- `flow35_decision_confidence_threshold`
- `flow35_halt_requires_security_ack`
- `flow35_briefing_option_limit`
- `flow35_append_only_retention_days`
- `flow35_conflict_priority_weights`
- `flow35_support_readonly_scope`
- `flow35_timeout_retry_after_ms`

Tenant A adaptation: Acme arbiter conflict console.
Tenant B adaptation: Northwind model consensus desk.
Tenant C adaptation: Tessera governance verdict board.

Locked rules: decision logs are append-only, stale writers receive 409, repeated idempotency keys replay cached responses, tenant scope comes from TenantContext, every ESCALATE or HALT writes an EscalationBriefing, and every mutation writes state before emitting a CloudEvent.
