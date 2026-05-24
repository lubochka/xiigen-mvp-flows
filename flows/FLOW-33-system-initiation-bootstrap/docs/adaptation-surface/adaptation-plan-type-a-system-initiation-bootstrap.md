# FLOW-33 Type-A Adaptation Plan

Flow: FLOW-33 system-initiation-bootstrap

Allowed adaptation keys:
- `flow33_module_name`
- `flow33_review_queue_sla_hours`
- `flow33_allowed_package_categories`
- `flow33_featured_strip_slots`
- `flow33_install_permission_policy`
- `flow33_dependency_install_mode`
- `flow33_discovery_ranking_weights`
- `flow33_settlement_period_days`
- `flow33_fraud_review_threshold`
- `flow33_sandbox_ttl_hours`

Tenant A adaptation: Acme curated flow store.
Tenant B adaptation: Northwind partner template exchange.
Tenant C adaptation: Tessera community pattern library.

Locked rules: published versions are immutable, package artifacts are content-addressed, installs copy logic only, RAG sharing exports blueprints only, connector bindings store Vault references only, settlement uses integer cents, fraud outcomes require human review, sandbox tenants keep the sandbox prefix, and every mutation writes state before emitting a CloudEvent.