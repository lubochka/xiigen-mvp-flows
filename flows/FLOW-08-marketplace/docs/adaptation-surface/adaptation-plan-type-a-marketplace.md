# FLOW-08 Type A Adaptation Surface — Marketplace

Type A adaptation may change the visible module name, seller-facing marketplace copy, category labels, price-cap policy text, card ordering copy, and tenant-specific positioning.

The following constraints are MACHINE and must not change:

- Audit record is written before moderation, price validation, listing storage, catalog indexing, feed generation, analytics, or event emission.
- Moderation rejection saves a DRAFT and is not treated as a thrown business exception.
- Negative prices are rejected and zero-price listings remain valid free listings.
- Catalog indexing is idempotent and version-keyed.
- Feed generation emits aggregate counts only, never buyer, seller, listing, or viewer identifiers beyond the tenant-scoped record.
- Analytics uses `inquiries / (views || 1)` and stores aggregate counters only.
- Tenant isolation, auth role matrix, endpoint descriptors, BFA rules, and package secret policy cannot be adapted.

The `flow_module_name` FREEDOM key controls tenant fork naming, marketplace listing text, and the visible Angular title. Adapted packages must still pass the full auth matrix, visual matrix, Docker gate, GitHub Actions gate, and cross-tenant JWT isolation gate.
