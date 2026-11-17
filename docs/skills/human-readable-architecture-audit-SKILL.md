# Human-Readable Architecture Audit

> **Stack adaptation (xiigen flows).** The human-readable architecture/audit gate
> ported from `llm_mvp_core/docs/skills/human-readable-architecture-audit-SKILL.md`
> (G07). The universal part (the audit is readable by Luba as a human architect,
> English summary first unless the user requests another language; mandatory PlantUML / Mermaid `classDiagram` importable to
> draw.io plus data-flow simulations; a no-code algorithm explanation naming the
> actual classes/methods; per-finding code quotes with `file:line` plus `current behavior` /
> `why` / `required behavior`; the Concrete Repair Blueprint Gate; the Audit
> Sufficiency Boundary and Delta-Severity Feedback gates) is taken from core.
> Adapted here:
> - UML is **per-flow**: the `net10.0` server (`Xiigen.Flow.<Name>.{Api,Application,
>   Infrastructure}`), the Angular 21 client, and the contracts in
>   `flows/FLOW-NN-<slug>/contracts`.
> - Code citations point at `flows/FLOW-NN-<slug>/server/**/*.cs` and
>   `flows/FLOW-NN-<slug>/client/angular/**/*.ts`.
> - **Trainable / DPO / checkpoint repair blueprints are `N/A` for flows** and must
>   be marked explicitly so. Flows consume `.xiigen/wiki-domain` manifests from
>   `llm_mvp_core` and train no models of their own (G12). The
>   deterministic-vs-learned boundary is therefore stated as `N/A` unless a
>   specific flow genuinely introduces a learned component (then it is a core
>   concern, audited there).
> - The deterministic-shortcut sweep (a hidden lookup/counter/regex posing as
>   "smart" logic) **is** kept — it is universal code-review hygiene and applies to
>   server services and Angular logic alike.
> - Domain return type cited is `DataProcessResult<T>` — **not** core
>   `OperationResult<T>`.
> - **The llm_mvp_core host-extension clusters are NOT ported.** The Dual-Path Host
>   Verification block, the Claude/Cursor/Codex scenario coverage, and the
>   10-review-cycle-for-three-extensions gate are core-product-specific.

## When to load

Load this skill for architect, planning, plan review, code review,
documentation, and audit artifacts that explain a flow's architecture, server
services, Angular surfaces, contracts, or code risks. Also load it when Luba asks
for a human-readable architecture explanation, UML, no-code algorithms, code quotes, or
a human-readable architecture audit; when an audit claims sufficiency / completion
/ closure; or when a later broader audit in the same area finds BLOCKING/HIGH
issues.

## Human-Readable Architecture Artifact Gate

The artifact must be readable by Luba as a human architect, in English unless
the user explicitly requests another language. Machine labels may exist, but they are
secondary anchors. They cannot be the main explanation.

Hard fail:
- table-only, shortcode-heavy, schema-first, or schema-only text, or text that ignores the requested language;
- prompt/spec text that tells a future executor what to invent;
- code dumps without a no-code explanation;
- generic rows such as "wrap in a service", "add a contract", "add tests",
  "create schema", or "make typed state".

## Required Human Format

Every architecture, code-review, documentation, or audit artifact that touches a
flow's architecture must include:

1. A clear English summary in human terms unless the user requests another language.
2. A solved architecture block for every major area: problem, selected design,
   rejected alternatives, UML, data flow, current code refs, future
   files/classes/methods/signatures/dependencies, deterministic-vs-learned
   boundary or explicit `N/A`, gates/tests, failure modes, and docs.
3. UML importable to draw.io: PlantUML or Mermaid `classDiagram`, plus concrete
   data-flow simulations and expected branch outcomes. For a flow, show the
   `Xiigen.Flow.<Name>.Api` endpoint -> `.Application` service ->
   `DataProcessResult<T>` -> Angular client -> rendered UI path.
4. A no-code algorithm explanation: what happens, where, in what order, with
   class/function references, but not a code dump.
5. A code-quote section. For each finding, give exact file + line + short quote
   (`flows/FLOW-NN-<slug>/server/**/*.cs` or `client/angular/**/*.ts`), then:
   - current behavior;
   - why this is wrong/acceptable;
   - required behavior;
   - affected files/classes/records/methods/data rows;
   - how it works (for flows: deterministic/helper — **not** learned, G12);
   - affected contracts/manifests (`flow.manifest.json`, `.xiigen/wiki-domain`);
   - affected tests/e2e/qa (xUnit / `ng test` / Playwright);
   - lifecycle (request -> service -> result -> UI).
6. A concrete solution immediately under each problem quote. A later appendix is
   not enough.

The solved architecture block is required even when the artifact is "only an
audit" or "only a review". Findings without adjacent solved design are not enough
to authorize downstream planning or implementation.

## Deterministic Boundary Sweep (kept; learned-state blueprint is N/A)

For every suspicious lookup table / counter / regex / private decision method,
check whether it is a hidden deterministic shortcut posing as something smarter.
Because flows train no models of their own (G12), the trainable-replacement
blueprint is **N/A**; the requirement here is to label the helper honestly and
keep it inside a well-formed service.

```text
current field/method:
current runtime calculation:
is this a hidden deterministic shortcut posing as smart logic? yes/no
honest label (deterministic helper / eval-only / internal baseline):
where it lives (server service / Angular logic):
input/output (DataProcessResult<T> shape):
failure codes returned:
tests/e2e/qa proving the behavior:
trainable/DPO/checkpoint blueprint: N/A (flows consume .xiigen/wiki-domain from llm_mvp_core; no local model)
```

If a flow genuinely introduces a learned component, that is a core
(`llm_mvp_core`) concern with its own trainable-architecture audit — it is not
materialized inside a flow here. Record it as out-of-scope and route it to core.

## Concrete Repair Blueprint Gate (non-learned form)

For every problem quote, the repair must name concrete artifacts, not labels:

```text
current quote (file:line):
current behavior:
why this is wrong/acceptable:
required behavior:
replacement class/service/component:
input/output records and DataProcessResult<T> shape:
old private method replaced by:
new runtime call chain (Api -> Application -> result -> Angular):
affected contracts / flow.manifest.json / .xiigen manifests:
tests/e2e/qa (xUnit / ng test / Playwright):
```

If the row contains only labels or a prompt/spec for later work, it is not a
solution. The review fails until the blueprint names concrete classes, methods,
tests, and contract/manifest changes.

## Audit Sufficiency Boundary Gate

A readable audit may still be too narrow. Do not claim "достаточно" ("sufficient"), "готово" ("ready"),
"закрыто" ("closed"), "architecture audit sufficient", or "architecture audit complete"
unless the audit scope was broad enough and the breadth challenge pass is green.

Allowed bounded wording:
- "v1 scope completed" only when the exact checked scope is named;
- "focused audit found X" only when broader architecture sufficiency is not implied.

Forbidden completion wording:
- "Architecture audit sufficient/complete" without a green breadth challenge;
- "готово/закрыто" ("ready/closed") when the audit only checked selected quotes, one file, one
  service, one endpoint, or one Angular component;
- any schema-only or prompt/spec completion claim that hides unexamined adjacent
  runtime zones.

Minimum breadth challenge for a flow audit — after focused findings, run a second
pass over adjacent zones:
- request validation and failure-code coverage on every endpoint;
- idempotency / retry behavior where the flow mutates state;
- contract / `flow.manifest.json` drift vs the implemented service;
- Angular error-path rendering and accessible names;
- fallback-to-default / silent-success shortcuts.

Parent review must ask and answer:

```text
What would a broader audit likely find that this artifact missed?
```

The answer must list likely missed zones, what was checked now, what remains
explicitly out-of-scope, and whether the final wording is bounded.

## Delta-Severity Feedback Gate

If the next audit in the same area finds BLOCKING/HIGH issues that should have
been discoverable, treat the previous "sufficient/ready/closed" review as a
retroactive review-skill failure.

Record the repair:

```text
previous audit/review claim:
later BLOCKING/HIGH finding:
why it should have been discoverable:
why the previous review missed it:
skill/guide/index/agent rule patched:
new breadth challenge row:
completion wording corrected:
```

Return to governance repair before polishing the artifact when this gate fails.

## Iterative Readability Review

When Luba explicitly asks for iterative audit readability improvement, the review
floor is minimum 6 cycles, preferably 10. Each cycle records which skill/gate was
applied, what failed, what was fixed, and why the artifact became more
human-readable. A cycle is a real review/fix action with an inspectable
before/after reference — not a ledger row. `real_cycle_count` counts only valid
cycles in order; N ledger rows after one review is not N cycles.

If a cycle finds a missing skill/gate, stop polishing the artifact and return to
governance repair first.

## Review Verdict

Reject the artifact when:
- the human summary is missing or does not use the required language (English by default, or the language requested by the user);
- UML has no concrete simulation examples;
- algorithm text is code-first rather than no-code;
- code quotes lack adjacent concrete solutions;
- solved architecture blocks are missing for architecture areas;
- the deterministic-vs-learned boundary is missing or not explicitly `N/A`;
- a hidden deterministic shortcut is present without an honest label;
- a solution is generic, schema-only, table-only, or prompt/spec;
- audit completion wording overclaims sufficiency without a green breadth challenge;
- a later broader audit found discoverable BLOCKING/HIGH issues and the previous
  review did not record a Delta-Severity Feedback Gate repair.

## Integration

Cross-reference with:
- `output-readability-gate-SKILL.md` (prose + dashboard readability + execution board);
- `implementation-doc-uml-SKILL.md`;
- `implementation-doc-algorithms-SKILL.md`;
- `implementation-doc-code-refs-SKILL.md`;
- `generated-code-review-SKILL.md` (deterministic-shortcut sweep detail);
- `audit-protocol-SKILL.md`.
