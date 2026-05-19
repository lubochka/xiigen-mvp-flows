# FLOW-06 Type-A Adaptation Surface

## Flow

- Flow ID: FLOW-06
- Flow slug: user-groups-communities
- Default module name: user-groups-communities
- FREEDOM key: `flow_module_name`

## Allowed Type-A Changes

The tenant adaptation pipeline may change:

- The display module name shown in Angular evidence.
- Group labels and community copy.
- Default group names used by local seed data.
- Feed title copy and role-facing descriptions.

The adaptation pipeline must not change:

- Membership activation before feed population.
- Server-side tier resolution.
- Tenant isolation filters.
- Idempotency keys or replay behavior.
- Cross-tenant JWT isolation behavior.
- Required visual roles or visual cells.

## Required Evidence After Each Adaptation

Every adapted tenant repository must rerun:

- .NET unit tests for `Xiigen.Flow.UserGroupsCommunities.Tests`.
- Angular unit tests.
- Auth matrix against the protected membership endpoint.
- Playwright visual capture for all six roles across three cells.
- Cascade evidence manifest with `moduleName`, `packageVersion`, and source package lineage.
