# FLOW-14 ETL Data Integration Type A Adaptation Surface

Type A adaptation may change the visible module name, connector labels, warehouse zone names, dashboard copy, and synthetic tenant examples.

Locked behavior:
- ETL pipeline publication must cover task types T213 through T224 with one responsibility per step.
- Plan steps must stay fabric-first: no provider SDK import list and no entity-specific controller mode.
- Every service step must declare DataProcessResult behavior.
- At least twenty-five CloudEvent schemas must be listed before a plan can publish.
- Plan publication uses optimistic concurrency: one concurrent writer wins and every loser receives `CONCURRENT_UPDATE`.
- Repeated writes with one idempotency key replay the cached response and create no duplicate audits, facts, or events.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without publishing the plan.
- Event ingestion stores raw landing and staging documents before publishing the ingestion CloudEvent.
- Tenant scope is read from TenantContext; request payload tenant ids are rejected.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
