# FLOW-34 Type-A Adaptation Plan

Flow: FLOW-34 marketplace-plugin-adapter

Allowed adaptation keys:
- `flow34_module_name`
- `flow34_review_queue_sla_hours`
- `flow34_allowed_package_categories`
- `flow34_featured_strip_slots`
- `flow34_install_permission_policy`
- `flow34_dependency_install_mode`
- `flow34_discovery_ranking_weights`
- `flow34_settlement_period_days`
- `flow34_fraud_review_threshold`
- `flow34_sandbox_ttl_hours`

Tenant A adaptation: Acme curated flow store.
Tenant B adaptation: Northwind partner template exchange.
Tenant C adaptation: Tessera community pattern library.

Locked rules: published versions are immutable, package artifacts are content-addressed, installs copy logic only, RAG sharing exports blueprints only, connector bindings store Vault references only, settlement uses integer cents, fraud outcomes require human review, sandbox tenants keep the sandbox prefix, and every mutation writes state before emitting a CloudEvent.