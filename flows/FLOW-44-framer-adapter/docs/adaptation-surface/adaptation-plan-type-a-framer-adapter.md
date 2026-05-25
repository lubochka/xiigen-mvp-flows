# FLOW-44 Type-A Adaptation Plan — Framer Text Elements Adapter

## Adaptation Surface

- FREEDOM key: `flow44_module_name`
- Default package: `@xiigen/framer-text-elements-adapter`
- Default module name: `framer-adapter`
- Tenant-visible module display: `Framer Text Bridge`

## Tenant A Adaptation

Tenant A may rename the module for its marketplace listing, but the Framer text-node filter, typography mapping, paint normalization, plugin-data storage, auth policies, and SDK-boundary rule remain unchanged.

## Tenant B Adaptation

Tenant B installs Tenant A's package, verifies local registry readback, and may rename the module while keeping the same Framer text element read/write parity rules.

## Tenant C Adaptation

Tenant C installs Tenant B's package and repeats the visual, auth, package, and cross-tenant isolation evidence cells with its own module name.

## Non-Negotiable Rules

- Do not add live Framer runtime calls to parser, mapper, or test modules.
- Do not drop unknown Framer node fields when preserving adapter metadata.
- Do not skip text-node filter, style mapping, write contract, or plugin-data storage evidence.
- Do not publish tenant runtime data or secrets in the package bundle.
