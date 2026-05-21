# FLOW-17 Freelancer Marketplace Type A Adaptation Surface

Type A adaptation may change the visible module name, gig labels, milestone labels, dispute copy, review copy, and synthetic tenant examples.

Locked behavior:
- Gig acceptance covers T613: client ownership check, SETNX-style gig acceptance lock, bid status validation, optimistic version check, audit write, then gig accepted event.
- Milestone contract management covers T614: client, gig, and freelancer identifiers are immutable after activation; milestone sums must equal the contract total.
- Delivery gate escrow control covers T615: milestone release requires submitted delivery; disputes never move funds; failed releases create reverse-order compensation records and never delete state.
- Freelancer review writing covers T616: reviews are directional, append-only, duplicate-protected per engagement direction, and audit evidence never stores raw comment text.
- Freelancer Marketplace cannot import or mutate social graph services.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate acceptance, contract, release, review, audit, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming gig acceptance.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
