# FLOW-39 Type A Adaptation Surface

The tenant adaptation surface may rename the module and tune presentation copy, but it must not change the machine rules that protect the learning loop.

FREEDOM keys:

- `flow39_module_name`
- `oss_curriculum_shadow_model_set`
- `oss_curriculum_min_cycles`
- `xiigen.judge_model`

Tenant A adaptation: Acme OSS curriculum lab.
Tenant B adaptation: Northwind shadow-run training hub.
Tenant C adaptation: Tessera plateau coaching loop.

Locked rules: CurriculumTierAssigner runs before validation, archetype-to-tier mapping is machine fixed, shadow run records always include ossModel and cycleId, exactly three learning signals are stored, plateau detection recommends pre-seeding only, stale writers receive 409, repeated operation keys replay cached responses, tenant scope comes from TenantContext, and every successful mutation writes state and appends outbox before queue broker dispatch.

