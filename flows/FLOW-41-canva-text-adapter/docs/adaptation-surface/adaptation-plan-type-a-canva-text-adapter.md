# FLOW-41 Type A Adaptation Surface

The tenant adaptation surface may rename the package display module and tune presentation copy, but it must not change the machine rules that protect the pure Canva-to-shared mapping path.

FREEDOM keys:

- `flow41_module_name`
- `canva_text_adapter_package_display_name`
- `canva_text_adapter_storage_namespace`
- `canva_text_adapter_operator_copy_variant`

Tenant A adaptation: Acme Canva brand text kit.
Tenant B adaptation: Northwind Canva campaign adapter.
Tenant C adaptation: Tessera Canva editorial adapter.

Locked rules: Canva content maps to shared characters, Canva font weights map only to Regular or Bold, Canva alignment maps only to LEFT/CENTER/RIGHT, colors stay RGB strings, write payloads are user-triggered, writer failures increment the failed count without aborting the batch, the pure mapping path imports no Canva SDK, STEP-1-INVARIANTS.md and FUNCTIONAL-SPEC.md remain in the package, and local registry readback must confirm the published package version.
