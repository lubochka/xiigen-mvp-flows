# FLOW-36 Type-A Adaptation Plan

Flow: FLOW-36 feature-registry

Allowed adaptation keys:
- `flow36_module_name`
- `flow36_ft_namespace_prefix`
- `flow36_signal_score_threshold`
- `flow36_cost_ceiling`
- `flow36_adapter_target_stack`
- `flow36_simulation_retry_limit`
- `flow36_support_readonly_scope`
- `flow36_tenant_readonly_scope`
- `flow36_porting_decision_mode`
- `flow36_timeout_retry_after_ms`

Tenant A adaptation: Acme feature porting control.
Tenant B adaptation: Northwind platform readiness hub.
Tenant C adaptation: Tessera porting lab.

Locked rules: FT identifiers are stable, portingCandidate=false resolves to PROHIBITED before cost or adapter work, tenant overrides cannot change machine-owned classification, stale writers receive 409, repeated idempotency keys replay cached responses, tenant scope comes from TenantContext, and every mutation writes state before emitting a CloudEvent.
