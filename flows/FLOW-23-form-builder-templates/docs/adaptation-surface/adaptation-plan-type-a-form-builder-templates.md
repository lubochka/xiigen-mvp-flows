# FLOW-23 Type A Adaptation Surface

Type A adaptation may change the visible module name, template labels, field examples, gallery grouping, role-facing help copy, marketplace listing copy, and synthetic usage examples.

Locked behavior:

- Template schema validation covers T381-T420 and must remain schema-first, single-responsibility, and fabric-first.
- Published templates are immutable unless a new version is created through optimistic concurrency.
- Template instantiation uses a SETNX-equivalent tenant-scoped lock before creating form instances or dispatching outbox events.
- Usage analytics are append-only, aggregate only, and must reject submitted form values, contact fields, or other PII.
- `flow_module_name` is the only FREEDOM key that may rename the visible module and tenant fork package during the Tier D cascade.
