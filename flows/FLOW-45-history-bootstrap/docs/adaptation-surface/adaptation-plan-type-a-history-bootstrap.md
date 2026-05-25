# FLOW-45 History Bootstrap Adaptation Surface

## Scope

FLOW-45 adapts the display name and tenant-facing bootstrap framing only. The underlying architecture-history contract stays fixed: approved source validation, GLOBAL architecture patterns, DESIGN_REASONING and RAG routing, summary storage before event append, idempotent run replay, and tenant-scoped dashboard readback.

## FREEDOM Key

`flow45_module_name`

Default value: `history-bootstrap`

## TYPE-A Adaptation

The AI adaptation may rename the module for a tenant marketplace listing, for example:

- Tenant A: `acme-history-rag-bootstrap`
- Tenant B: `northwind-architecture-memory`
- Tenant C: `tessera-history-bootstrap`

The adaptation must not change task type coverage, BFA rules, auth posture, endpoint descriptors, package identity, tenant isolation, or outbox ordering.

## Evidence Required

Every adapted tenant repository must capture the same role matrix as the source flow:

- `platform_admin`
- `platform_support`
- `tenant_admin`

Each role captures C2 desktop English, C4 desktop Hebrew RTL, and C6 mobile English. The tenant manifest must record the cascade point and the source tenant from which the package was installed.
