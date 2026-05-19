# FLOW-05 Type A Adaptation Surface

Type A adaptation may change the visible module name, marketplace listing text, role-facing labels, reward copy, and next-lesson labels.

It must not change:

- Server-side point calculation from the stored questionnaire score.
- The timezone-aware streak calculation from `userTimezoneOffset`.
- The achievement-before-social-sharing privacy gate.
- The idempotency and optimistic concurrency behavior for completion submissions.
- The protected-role matrix and cross-tenant isolation behavior.
- The P10/P11 rules CF-05-1 through CF-05-4 and CF-867 through CF-877.

The canonical FREEDOM key for the module name is `flow_module_name`, with default value `completion-gamification`.


