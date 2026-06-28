# plan-review-SKILL.md — Check a plan before implementation

> **Stack adaptation (xiigen flows).** Universal plan-review gates ported from
> `llm_mvp_core/docs/skills/plan-review-SKILL.md`. Adapted to this repo:
> - **Gate-B** = `dotnet build` + `dotnet test` (xUnit) per-flow `Tests` project, plus
>   Angular 21 Playwright for client branches;
> - the reviewer checks the per-flow boundary
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   (verified shape: `FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.{Api,Application,Infrastructure,Tests}`);
> - the domain return type on `Application` public methods is `DataProcessResult<T>` (XIIGen
>   family wrapper; services inherit `MicroserviceBase<TSelf>` with `Success<T>`/`Failure<T>`)
>   — NOT core `OperationResult<T>` (which stays in `llm_mvp_core` and must not leak here);
> - core algorithm/trainable logic stays in `llm_mvp_core`; this repo consumes it via
>   `.xiigen`/wiki-domain manifests and locators only;
> - §5 hierarchy review (ILearningMachine levels) is core-specific (G12) and not run here.

## When to load
Load for ARCHITECT sessions before writing a plan. Load for REVIEW sessions when evaluating a
submitted plan.

## Plan Review First-Class Gate 0

Plan review is a first-class sibling of planning. The reviewer must read the target plan and
cited references **directly** before accepting, rejecting, or summarizing any writer/sub-agent
report. A writer report, state file, old plan, or sub-agent packet is evidence only until
direct read-back confirms it.

Required review packet for every finding:

```text
Gate 0 verdict: PASS | FAIL
target_plan_read_directly=true
cited_refs_read_directly=true
writer_report_is_evidence_only=true
exact_line_or_absence_evidence:
impacted_requirement:
impacted_skill_guide_or_plan_section:
required_fix:
completion_evidence_after_fix:
re_review_evidence_after_fix:
re_review_trigger:
```

A review that only paraphrases a writer packet or says "looks covered" without direct source
evidence fails Gate 0.

## Dishonest Claim Rejection Gate

Reject any plan, status, packet, or final response that inflates the evidence behind a claim.

```text
claim_scope:
evidence_scope:
evidence_type:                 # dotnet test output | build output | Playwright run | artifact presence
subagent_id_or_NONE:
count_claimed:
independent_evidence_count:
ledger_rows_counted_as_cycles=false
parent_self_review_counted_as_subagent_review=false
commands_claimed_without_execution=false
downgrade_or_repair_required:
```

Hard fail when "N cycles/reviews/passes" is claimed without N independent records; when
checklist/generated/ledger rows are counted as cycles; when parent/self review is counted as
sub-agent review; when a sub-agent claim omits id/work order/returned packet/reviewed
scope/parent acceptance; or when `dotnet test`/`dotnet build`/Playwright/git is described as
executed without actual output. Downgrade the claim to the exact evidence-supported wording or
require governing skill/guide/agent-file repair before approval.

## Parent-Facing Human-First Status Review Gate

Reject any parent status update, review packet, sub-agent packet summary, or final response
that opens with machine-log language instead of human meaning. The first user-visible sentence
must state total tasks/scenarios when known, done count, current phase/step meaning, what is
being checked/fixed, what remains, and whether the claim is scoped/evidence-only or accepted
dual-path scenario coverage. Machine labels (packet names, JSON rows, STATE flags, validator
names) may appear only after the human explanation. The wording must say the sub-agent is
checking/writing the scoped item and the parent is reviewing/orchestrating.

## Plan-Not-Ready Is Repair Loop, Not Stop

"Do not stop" does not authorize an executor. While the plan is not closed/readable/exact, or
the dual-path scenario gate has not passed, return to planning repair — not execution, not a
final stop. A prematurely-launched executor is stopped and replaced by bounded governance
repair, then the plan-readiness workflow resumes.

## Order of checks

Run FC-20 through the Part-A doc checks BEFORE any technical-feasibility check (FC-1 through
FC-12). A fail on the Design Artifact Completeness gate, FC-20, FC-21, FC-26, FC-14, FC-15,
FC-16, FC-17, or FC-19 is a HARD FAIL: return the plan with the missing-section list. The plan
is BLOCKED and admission is forbidden until every failing check is remediated. Do not accept
implementation or phase completion when FC-25 fails.

---

## FC-0 — Authority Requirements Ledger traceability

Every plan must start from authority-chain requirements, not only plan-internal consistency.
Reject the plan if: the Authority Requirements Ledger is missing; the Conversation Delta Ledger
is missing after a new correction/review; the User Prompt Source Ledger is missing; a
plan-shaping prompt/feedback/question/answer is absent from the source ledger; any binding
requirement is unmapped; a rejected/superseded mechanism was removed without its required
replacement; or any plan section contradicts a binding requirement.

---

## Design Artifact Completeness gate — hard fail BEFORE technical checks

Run `design-artifact-completeness-SKILL.md` (C1–C5) on every artifact the plan builds on. A
referenced artifact that fails C1 (missing) or C2 (stub: `NotImplementedException`, `throw`,
or a constant `Failure("NOT_READY", ...)` / empty outcome, or 0 `[Fact]`/`[Theory]`) blocks
treating it as a complete input. The plan must implement it first, scope around it explicitly,
or mark it DEFERRED with Luba's reason.

---

## FC-20 — Readable-Plan Admission Gate (R3) — HARD FAIL

**Trigger:** any plan that touches existing code, routes through existing components, or
modifies a contract.

**Check:** the plan ALREADY CONTAINS (not merely promises) Part A in Luba's format:
- §0 — simplest UML importable into draw.io (Mermaid `classDiagram` acceptable). Every class
  has a `note for` annotation; a legend table follows; a per-connection simulation table
  covers every arrow with example input + expected `DataProcessResult<T>` / `Results.Json`
  outcome.
- §1 — algorithm descriptions in Russian, plain language, WITHOUT code fragments, WITH class
  references: exact class + implementing method + parameters + step-by-step.
- §2 — per algorithm: where the logic sits (file + class + method + parameters), a detailed
  step-by-step explanation (not a functional summary), human analogy, concrete example values,
  a "what it does NOT do" statement, and a C# signature block returning `Task<DataProcessResult<T>>`.
- §3 — exact short code citations with precise `path:line` reflecting code AS ACTUALLY
  IMPLEMENTED; every threshold/condition has a WHY; every non-obvious call has a HOW.
- §4 — status table: one row per component, status symbol, what-works/what-is-stub.

**Hard fail when:** Part A is absent; present as a heading only with "will be written later";
§0 is a flowchart image rather than a class diagram; §0 lacks `note for` on every class or
lacks a legend; §1 is English-only prose; §1/§2 names a class but not the implementing method,
its parameters, and a step-by-step algorithm; §2/§3 citations do not exist in the current repo;
§2 or §3 is missing; or Part A describes what will be built rather than what is implemented.

**Return:** block execution; list every missing Part A section. A promise to add it later is
still HARD FAIL. (§5 hierarchy is core-specific/G12 and not required in this repo.)

---

## FC-21 — Hidden Determinism (R4)

**Trigger:** any plan step that selects, routes, ranks, matches, classifies, or generates.

**Check:** in this repo most flow logic is deterministic domain code (validation, idempotency,
status-code mapping) — that is correct and must be **labelled deterministic**. The hidden-
determinism failure is the inverse: claiming a learned/trainable unit while the implementation
is deterministic, OR placing trainable model logic in this repo at all. Trainable/learned model
work belongs in `llm_mvp_core`; this repo consumes it through `.xiigen`/wiki-domain manifests
and locators only.

**Hard fail when:** a plan step claims a trainable/learned mechanism inside this repo (rather
than consuming a `llm_mvp_core` manifest); OR a deterministic helper is dressed up as "smart"/
"semantic"/"trained" without saying it is deterministic. List each step and require the
deterministic-vs-learning label and the correct boundary (consume from core, do not implement
training here).

---

## FC-22 — Model/Set Repo Split (R5)

Common model + creation logic + common training sets live in `llm_mvp_core` (saved, imported,
exported, upgradeable); this repo consumes them via manifests/locators. A plan that stores or
trains common models inside xiigen flows without explicit Luba approval is a hard fail.

---

## FC-23 — Agent/Layer Boundary Simplicity (R6)

Host/agent and cross-repo boundary logic must be a thin set of calls that send a concrete task
and receive a concrete answer. Core algorithm logic must not leak into the thin layer. Within a
flow, the minimal-API `Api` layer must stay thin (route → `Application` service call → map
`DataProcessResult<T>` to `Results.Json`); business logic belongs in `Application`. A plan that
puts core algorithm logic in the `Api`/host layer fails this gate.

---

## FC-24 — External Tool Permission (R7)

A plan that uses an external AI tool/service not required by the task without Luba's explicit
permission is a hard violation. If such a tool is genuinely needed, the plan must record the
request for Luba's explicit permission before use.

---

## FC-25 — Implementation Reality / Anti-Stub Completion Gate

Do not accept implementation or phase completion when the "done" evidence is only a stub, a
validator, a report, or a static check. For a "done" claim the plan must show: the service was
loaded through the flow DI (`AddXxxFlow()`), invoked on the target scenario, and verified by
`dotnet test` (exact pass count) and/or Playwright. A method that always returns
`Failure("NOT_READY", ...)` or an empty outcome is a scaffold, not a feature. Two legitimate
PASS states must never be conflated: `PASS_AS_HONEST_UNTRAINED_SCAFFOLD` (honest not-ready,
no hidden determinism, proven honest by tests) vs `PASS_AS_REAL_FEATURE` (real behavior, real
tests, real e2e).

---

## FC-26 — Plan Testability Strategy

Every plan that touches code, a contract, or a runtime path must declare, per Part A/Part B
branch, a test strategy across: unit (xUnit via `FlowFixture`), integration (minimal-API
`WebApplicationFactory<Program>`), e2e/host (Angular 21 Playwright), QA automation, UX
automation, negative, and regression. A branch with no test strategy is an admission-blocking
hole. "Tests later" is a fail.

---

## FC-13 — Output format and style match
The plan matches Luba's requested format, audience, language, and detail level. A technically
correct artifact in the wrong format is a failed artifact.

## FC-14 — Implementation doc: UML diagrams (per-connection simulation) — HARD FAIL
§0 class diagram has `note for` on every class, a legend, all arrows, and a per-connection
simulation table covering EVERY arrow with example input + expected outcome
(`DataProcessResult<T>` / `Results.Json` status). §1 flowcharts use human-language nodes and a
per-branch simulation for every branch. See `implementation-doc-uml-SKILL.md` and
`uml-simulation-to-testcase-SKILL.md`.

## FC-15 — Implementation doc: algorithm descriptions — HARD FAIL
Each algorithm names the exact class + implementing method + parameters + step-by-step in
Russian, with human analogy, example values, "что НЕ делает", and a `Task<DataProcessResult<T>>`
signature block. See `implementation-doc-algorithms-SKILL.md`.

## FC-16 — Implementation doc: code references — HARD FAIL
Each cited code block has WHY/HOW/WHAT and a real `path:line` reflecting code as implemented.
See `implementation-doc-code-refs-SKILL.md`.

## FC-17 — Implementation doc: status table
One row per component, allowed status symbol (no core `Bootstrap`), concrete
what-works/what-is-stub in this flow's domain methods. See `implementation-doc-status-SKILL.md`.

## FC-19 — Plan section parity (Part A equals Part B on core) — HARD FAIL
Part A core (architecture + product decisions + algorithms) is identical to Part B core. Part B
may add technical detail (exact `dotnet test` filters, gate numbers, CI steps, Angular UI/UX
specs, error handling) but must not contradict Part A, add components invisible to Part A, use a
different algorithm, or route data differently. A whole missing branch in either direction is a
FAIL.

---

## FC technical battery (run after the doc/admission checks)

- **FC-1 Count drift** — declared file/test counts match the actual deliverable list.
- **FC-2 Path errors** — every `path:line` resolves in this repo.
- **FC-3 Phantom components** — every class/contract/route referenced exists or is created by an earlier phase.
- **FC-4 Duplicate test numbers** — no duplicate `[Fact]`/`[Theory]` names or Playwright spec ids.
- **FC-5 Missing coverage** — every new component has a test row (xUnit + Playwright where applicable).
- **FC-6 Stale numbers** — baseline counts match the current `dotnet test` output.
- **FC-7 Phase placement** — each step is in the right phase; no forward dependency.
- **FC-8 Self-containment** — each session file cold-starts (read STATE, verify previous phase, load governance).
- **FC-9 Gate B as literal command** — every Gate B is `dotnet build`/`dotnet test --filter ... → exact value` or a named Playwright spec, not "works correctly".
- **FC-10 Type contract propagation** — `DataProcessResult<T>` is used consistently on `Application` public methods; no core `OperationResult<T>` leak; `DataProcessError(Code, Message, Details)` shape respected.
- **FC-11 Overview-detail consistency** — REFERENCE-PLAN capabilities match EXECUTION steps.
- **FC-12 Principles compliance** — Fabric First (ctor interfaces), no-throw business logic via `DataProcessResult<T>`, every production line commented, no external ML frameworks in this repo.

---

## The 5 original questions (still required)
1. Does the plan address every intent clause from Luba's request?
2. Is each phase plain-language with a single responsibility and a measurable Gate B?
3. Does Part A exist at full depth and match Part B on core?
4. Is every referenced artifact real (not a stub) and reachable (registered in `AddXxxFlow()`)?
5. Is the test strategy declared per branch across the real test layers?

---

## No product decisions rule
The plan must address Luba's clauses and no more. "While I'm here" phases that add unrequested
capabilities are rejected.

## Final Between Bounded Slices Review Gate
A status/commit/push/report is not a stop while authorized work remains. Reject any packet that
promises future continuation without a started next sub-agent id/packet or a real blocker.
Required wording: scenario coverage `total / accepted / api_only / ui_only / blocked /
not_started`, per-scenario Path A (xUnit + minimal-API contract) and Path B (Angular 21 desktop)
status. Do not restore an impossible live-host readiness gate; a blocked Path B records the exact
screen/state and next repair.

## Integration

Load alongside the four `implementation-doc-*-SKILL.md` files, `uml-simulation-to-testcase-SKILL.md`,
`design-artifact-completeness-SKILL.md`, `plan-self-validation-SKILL.md`, `phase-planning-SKILL.md`,
and `codex-planning-quality-GUIDE.md`.
