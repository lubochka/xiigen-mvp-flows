# FLOW-18 Visual Flow Engine Type A Adaptation Surface

Type A adaptation may change the visible module name, canvas labels, node palette copy, test-run examples, publication copy, and synthetic tenant examples.

Locked behavior:
- Canvas writing covers T617: every task type is represented by a node, tenant ownership is checked before state changes, provider SDK imports and entity-specific controllers are rejected, audit is written before the canvas update event.
- Flow publication covers T618: DFS cycle detection runs before type compatibility checks, publication uses optimistic concurrency, published and active flows are immutable.
- Node type registration covers T619: a SETNX-equivalent lock is acquired before the node type and capability index writes, and failure releases the lock with evidence.
- Code injection covers T620: a version lock is acquired, raw source text is never accepted in request or event payloads, pre-write audit is stored before injection result evidence, and code events carry hash references only.
- Visual Flow Engine cannot import provider SDKs or mutate tenant scope supplied in request payloads.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate canvas, node type, injection, audit, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming canvas persistence.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
