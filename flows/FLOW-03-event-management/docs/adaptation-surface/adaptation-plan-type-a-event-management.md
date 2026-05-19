# FLOW-03 Type A Adaptation Surface

Type A adaptation may change the visible module name, marketplace listing text, role-facing labels, matching copy, and personalization feed labels.

It must not change:

- The dual-write requirement for private profile and global matching summary.
- The success-mode semantics of partial matching results.
- The `PersonalizationCompleted` completion signal.
- The protected-role matrix and cross-tenant isolation behavior.
- The P10/P11 rules CF-854 through CF-857.

The canonical FREEDOM key for the module name is `flow_module_name`, with default value `event-management`.

