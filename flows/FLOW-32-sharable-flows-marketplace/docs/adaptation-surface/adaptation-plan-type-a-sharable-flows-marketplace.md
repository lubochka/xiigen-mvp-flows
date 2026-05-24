# FLOW-32 Type-A Adaptation Plan

Flow: FLOW-32 sharable-flows-marketplace

Allowed adaptation keys:
- `flow32_module_name`
- `flow32_review_queue_sla_hours`
- `flow32_allowed_package_categories`
- `flow32_featured_strip_slots`
- `flow32_install_permission_policy`
- `flow32_dependency_install_mode`
- `flow32_discovery_ranking_weights`
- `flow32_settlement_period_days`
- `flow32_fraud_review_threshold`
- `flow32_sandbox_ttl_hours`

Tenant A adaptation: Acme curated flow store.
Tenant B adaptation: Northwind partner template exchange.
Tenant C adaptation: Tessera community pattern library.

Locked rules: published versions are immutable, package artifacts are content-addressed, installs copy logic only, RAG sharing exports blueprints only, connector bindings store Vault references only, settlement uses integer cents, fraud outcomes require human review, sandbox tenants keep the sandbox prefix, and every mutation writes state before emitting a CloudEvent.