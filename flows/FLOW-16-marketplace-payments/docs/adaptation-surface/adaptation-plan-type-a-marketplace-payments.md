# FLOW-16 Marketplace Payments Type A Adaptation Surface

Type A adaptation may change the visible module name, checkout labels, escrow labels, refund queue labels, seller payout copy, and synthetic tenant examples.

Locked behavior:
- Checkout covers T609: buyer ownership check, SETNX-style cart lock, optimistic inventory reservation, audit write, then checkout event.
- Payment capture covers T610: idempotency, platform fee split, seller split, escrow hold, PII scrubbing, append-only payment audit, and order confirmation.
- Escrow covers T611: release, dispute, reversal, refund compensation, and non-deleting dispute state.
- Seller payout covers T612: payout lock, vault-reference-only payout write, payout audit, and payout completion event.
- Buyer dispute freezes seller payout synchronously before the dispute event is emitted.
- Payout hold notification is recorded in the same tenant-scoped operation that freezes seller payout release.
- Marketplace payments cannot import or mutate social graph services.
- Tenant scope is read from tenant context; request payload tenant ids are rejected.
- Idempotent retries replay the cached response and create no duplicate charge, escrow, audit, payout, or event records.
- Timeout simulation returns `TIMEOUT` with `retryAfterMs` and retains a recovery outbox event without confirming an order.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
