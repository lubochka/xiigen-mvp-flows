# FLOW-43 Type-A Adaptation Plan — Webflow Designer Extension

## Adaptation Surface

- FREEDOM key: `flow43_module_name`
- Default package: `@xiigen/webflow-designer-extension`
- Default module name: `webflow-designer-extension`
- Tenant-visible module display: `Webflow Designer Bridge`

## Tenant A Adaptation

Tenant A may rename the module for its marketplace listing, but the CSS shorthand parser, element property router, validation diff buckets, auth policies, and SDK-boundary rule remain unchanged.

## Tenant B Adaptation

Tenant B installs Tenant A's package, verifies local registry readback, and may rename the module while keeping the same Webflow Designer CSS parity rules.

## Tenant C Adaptation

Tenant C installs Tenant B's package and repeats the visual, auth, package, and cross-tenant isolation evidence cells with its own module name.

## Non-Negotiable Rules

- Do not add live Webflow runtime calls to parser or adapter modules.
- Do not drop unknown CSS properties.
- Do not skip native validation diff evidence.
- Do not publish tenant runtime data or secrets in the package bundle.
