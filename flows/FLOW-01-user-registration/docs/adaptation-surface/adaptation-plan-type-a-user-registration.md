# FLOW-01 Type A Adaptation Surface

## Freedom Key

`flow_module_name`

## Purpose

The module name controls tenant fork naming, marketplace listing text, and the visible Angular title for the bundle activation flow. A Type A adaptation may change only the module name and tenant-facing labels. It must not change the FLOW-01 business rules, authorization matrix, BFA rule ids, contracts, tenant isolation behavior, or package secret policy.

## Required Adaptation Evidence

- `tenant.config.json` records `moduleName` and appends one `adaptationHistory` item.
- Angular UI title and tests reflect the new module name.
- The adapted tenant repo passes .NET tests, Angular tests, and the 3 roles x 3 visual cell matrix.
- The adapted package is packed and published to the local registry.
- The adapted package is listed in the marketplace with a readback record.
- Cross-tenant JWT isolation remains green before the next tenant installs the package.
