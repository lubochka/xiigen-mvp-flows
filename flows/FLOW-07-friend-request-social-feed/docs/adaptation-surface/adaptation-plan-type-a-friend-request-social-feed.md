# FLOW-07 Type-A Adaptation Surface

## Flow

- Flow ID: FLOW-07
- Flow slug: friend-request-social-feed
- Default module name: friend-request-social-feed
- FREEDOM key: `flow_module_name`

## Allowed Type-A Changes

The tenant adaptation pipeline may change:

- The display module name shown in Angular evidence.
- Friend request labels and relationship copy.
- Default connection names used by local seed data.
- Feed title copy, graph labels, and role-facing descriptions.

The adaptation pipeline must not change:

- Inline privacy checks before friend request state.
- Bidirectional connection writes before publication events.
- Zero-score feed item retention.
- Tenant isolation filters.
- Idempotency keys or replay behavior.
- Cross-tenant JWT isolation behavior.
- Required visual roles or visual cells.

## Required Evidence After Each Adaptation

Every adapted tenant repository must rerun:

- .NET unit tests for `Xiigen.Flow.FriendRequestSocialFeed.Tests`.
- Angular unit tests.
- Auth matrix against the protected membership endpoint.
- Playwright visual capture for all seven roles across three cells.
- Cascade evidence manifest with `moduleName`, `packageVersion`, and source package lineage.
