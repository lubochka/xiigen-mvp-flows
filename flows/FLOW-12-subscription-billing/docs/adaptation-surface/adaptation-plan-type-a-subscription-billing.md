# FLOW-12 Subscription Billing Type A Adaptation Surface

Type A adaptation may change the visible module name, plan labels, billing copy, invoice display wording, and synthetic tenant examples.

Locked behavior:
- Plan publication validates required fields and positive integer cents before idempotency, document writes, audit writes, or event publication.
- Plan publication uses optimistic concurrency: one concurrent writer wins and every loser receives `CONCURRENT_UPDATE`.
- Subscription activation validates payment through the payment fabric and stores only a vaulted payment method reference.
- Raw payment tokens are rejected and never persisted.
- Cancelled or paused subscriptions emit an invoice-voided event and never attempt a payment charge.
- Billing cycle charging stores invoice state and audit evidence before the charge event leaves the flow.
- Billing-cycle idempotency replays the first invoice response and cannot double-charge.
- Revenue analytics normalize annual and monthly plans into monthly recurring revenue without storing subscriber identifiers in aggregate metrics.
- Tenant scope is read from TenantContext; request payload tenant ids are rejected.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
