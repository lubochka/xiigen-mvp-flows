# FLOW-24 Type A Adaptation Surface

Type A adaptation may change the visible module name, learner-facing lesson labels, synthetic topic examples, safety review copy, progress-card labels, marketplace listing copy, and synthetic usage examples.

Locked behavior:

- Consent gate evaluation covers T367 and blocks every downstream learning outcome when consent is denied, pending, withdrawn, or absent.
- Lesson composition covers T368 and must preserve compose -> safety gate -> publish ordering.
- Quiz grading covers T369 and must recompute score server-side while queuing gamification asynchronously.
- Learning streak evaluation covers T370 and must use profile or factory timezone, never client timezone.
- Domain packs, calendar sync, and progress tracking cover T371-T374 and must skip safely when optional FREEDOM config is absent.
- Calendar integration uses connector descriptors only; direct provider SDK calls are not part of this flow.
- `flow_module_name` is the only FREEDOM key that may rename the visible module and tenant fork package during the Tier D cascade.

