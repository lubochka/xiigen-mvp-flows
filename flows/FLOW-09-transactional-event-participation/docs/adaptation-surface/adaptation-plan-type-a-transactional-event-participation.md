# FLOW-09 Type A Adaptation Surface - Transactional Event Participation

Type A adaptation may change the visible module name, event positioning copy, ticket card labels, venue category names, refund policy helper text, and tenant-specific checkout language.

The following constraints are MACHINE and must not change:

- Seat reservation is written before payment clearing, ticket issue, QR token issue, analytics, or downstream event publication.
- Idempotency guarantees one payment and one ticket per tenant-scoped participation key.
- Fraud-service unavailability fails open only with an audit event and recoverable outbox evidence.
- Group booking is all-or-nothing: if any member cannot be ticketed, no group, payment, or ticket record is stored.
- Fee calculation stays inline and pure; it must not write documents or publish a standalone fee event.
- QR token generation and redemption stay inside the platform ticket boundary.
- Refund retry exhaustion emits a failed refund event and creates a compliance review record.
- Analytics stores aggregate counts only and never persists participant identifier arrays.
- Tenant isolation, auth role matrix, endpoint descriptors, BFA rules, and package secret policy cannot be adapted.

The `flow_module_name` FREEDOM key controls tenant fork naming, event-ticketing text, and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, and cross-tenant JWT isolation gate.
