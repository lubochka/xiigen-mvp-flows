# FLOW-38 Type A Adaptation Surface

The tenant adaptation surface may rename the module and tune presentation copy, but it must not change the machine rules that protect the learning loop.

FREEDOM keys:

- `flow38_module_name`
- `rag_quality_decay_window_days`
- `distilled_rule_min_confidence`
- `xiigen.judge_model`

Tenant A adaptation: Acme RAG quality lab.
Tenant B adaptation: Northwind retrieval feedback hub.
Tenant C adaptation: Tessera RAG learning loop.

Locked rules: cycle and pattern identifiers are stable, quality deltas remain fixed at plus or minus 0.05, scores are clamped to the 0.0 through 1.0 range, wasted cycles never seed distilled guidance, stale writers receive 409, repeated cycle-and-pattern keys replay cached responses, tenant scope comes from TenantContext, and every mutation writes state before emitting a CloudEvent.
