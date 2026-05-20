# FLOW-11 Schema Registry DAG Type A Adaptation Surface

Type A adaptation may change the visible module name, schema domain labels, graph card copy, approval wording, and tenant-specific registry examples.

Locked behavior:
- Schema version registration validates required fields before idempotency, document writes, topology rebuild, or event publication.
- Version monotonicity, dependency cycle detection, and compatibility classification always run inline before publish.
- Breaking changes always enter approval and cannot become active until an explicit approve decision succeeds.
- Published schema state is stored before any CloudEvent leaves the flow.
- DAG rendering is tenant-scoped; nodes, edges, and render cache never cross tenant boundaries.
- Tenant scope is read from TenantContext; request payload tenant ids are rejected.
- Validation failures are recorded without mutating schema versions or history records.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
