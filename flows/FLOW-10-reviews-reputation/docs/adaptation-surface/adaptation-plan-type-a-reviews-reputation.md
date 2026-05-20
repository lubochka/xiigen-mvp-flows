# FLOW-10 Reviews Reputation Type A Adaptation Surface

Type A adaptation may change the visible module name, public score labels, moderation queue copy, review source wording, and tenant-specific trust language.

Locked behavior:
- Eligibility is checked before validation, idempotency, audit, review storage, or event publication.
- Review identifiers are derived by the platform from tenant, reviewer, target entity, and target type.
- Audit and event payloads never include review text or owner response text.
- Moderation has three paths only: publish, reject, or wait for human review.
- Reputation scores use published reviews only and recalculate after retraction.
- Owner responses require ownership, allow one response unless a policy rejection permits revision, and never update the score.

The `flow_module_name` FREEDOM key controls tenant fork naming and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, marketplace readback, and cross-tenant JWT isolation gate.
