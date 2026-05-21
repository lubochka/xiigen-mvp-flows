# FLOW-19 Durable Sagas Compliance Type A Adaptation Surface

Type A adaptation may change the visible module name, saga-operation labels, audit-screen copy, export examples, marketplace listing copy, and synthetic tenant examples.

Locked behavior:
- Saga orchestration covers T621: every FLOW-19 task type has a durable plan step, the first step lock is written before state changes, state is persisted before CloudEvents dispatch, and concurrent writes return `CONCURRENT_UPDATE`.
- Compensation covers T622: rollback runs in serial LIFO order, each compensation step takes a SETNX-equivalent lock, and compensation stops on the first failed step.
- Compliance audit writing covers T623: audit records are append-only, hash-addressed, written with `retentionExpiresAt`, and stamped `PLATFORM_ONLY`.
- Retention enforcement covers T624: purge requires both policy expiry and no legal hold, archives before purge completion events, and is driven by FREEDOM retention keys.
- Durable Sagas Compliance cannot import provider SDKs or mutate tenant scope supplied in request payloads.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate saga, compliance, retention, export, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming saga persistence.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
