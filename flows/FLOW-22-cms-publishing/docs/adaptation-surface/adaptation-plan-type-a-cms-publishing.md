# FLOW-22 CMS Publishing Type A Adaptation Surface

Type A adaptation may change the visible module name, form labels, field examples, workflow branch labels, tenant-facing help copy, marketplace listing copy, and synthetic analytics examples.

Locked behavior:
- Form schema publishing covers T629: schema versions are published through one dynamic endpoint, use optimistic concurrency, and cannot be overwritten by stale writers.
- Form submission processing covers T630: submissions are validated against the published schema before workflow state, analytics, or outbox side effects.
- Automation dispatch covers T631: each submission and workflow rule pair uses a SETNX-equivalent lock before dispatching a workflow event.
- Submission analytics covers T632: analytics records are append-only aggregate evidence and never persist submitted personal details.
- FLOW-22 also covers the original T307 through T340 plan-step inventory. Every task type must have one single-responsibility plan step, use fabric interfaces, avoid entity-specific controllers, and return `DataProcessResult`.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate schema, submission, automation, analytics, audit, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming schema state.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
