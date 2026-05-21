# FLOW-15 SaaS Multi-Tenancy Type A Adaptation Surface

Type A adaptation may change the visible module name, workspace labels, member copy, billing labels, integration labels, and synthetic tenant examples.

Locked behavior:
- Tenant provisioning covers T605 through T608: provisioning, configuration, quota materialization, and lifecycle.
- Tenant provisioning uses a SETNX-style reservation before tenant state is written.
- Machine-managed keys remain immutable: tenant id, master workspace, provisioned date, and subscription tier.
- Master workspace identity comes only from a trusted tier definition or platform constant; request payload overrides are rejected.
- Quota counters are materialized from tier definitions in one atomic batch.
- Suspension changes tenant status and cascades to subscription handling without deleting tenant records.
- Termination emits a data purge request for the analytics flow instead of deleting records inline.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate tenant, audit, quota, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without activating the tenant.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
