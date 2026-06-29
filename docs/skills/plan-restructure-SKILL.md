# Plan Restructure

> **Stack adaptation (xiigen flows).** Universal delta-restructure contract ported from
> `llm_mvp_core/docs/skills/plan-restructure-SKILL.md`. Apply when adding a new per-flow
> `Infrastructure` adapter, a new fabric contract dependency, a new minimal-API route, or
> a new gate to an existing FLOW REFERENCE-PLAN / SESSION file — without breaking the
> downstream assumptions of neighbouring flows or the `.xiigen`/wiki-domain manifests this
> repo consumes from `llm_mvp_core`. List 1 ("what does NOT change") and List 2 ("what
> changes and where") are mandatory. Gate-B for every restructured phase = `dotnet build` +
> `dotnet test`.

## Core Problem

A flow plan written before new infrastructure (new fabric adapter, new route, new phase
gate) was completed can have correct phase content but still be wrong: it does not
reference the new infrastructure, its phases are too large for one session, it lacks the
new gates, and it does not produce the outputs the new infrastructure expects. Rewriting
the entire plan introduces errors into things that were correct. This skill defines the
delta analysis that preserves correct content while adding only what changed.

**Do NOT use for a flow that has no existing plan.** For brand-new flows, use the
intent→plan + phase-planning path from scratch.

---

## When to Invoke

- When an existing flow plan was written before new shared infrastructure (a new fabric
  contract in `Xiigen.Kernel`, a new in-memory fabric in `Xiigen.Fabrics.InMemory`, a new
  `AddXxxFlow()` registration shape) became available.
- When a plan has phases too large for one session (context-loss risk).
- When a plan's gates do not include new Gate-B checks (`dotnet build`/`dotnet test`,
  Playwright) that now apply.

---

## Section 1 — Read Before Writing

Read the existing plan completely before writing anything. Read it as if executing it.
While reading, annotate per section:

```
□ Is this still correct given the new infrastructure?
□ Does this assume something that no longer exists or has a new contract (e.g. a changed fabric signature)?
□ Is there a new gate/check that should be here (new dotnet test filter, new Playwright spec)?
□ Does this produce output that a neighbouring flow or a consumed .xiigen manifest expects?
```

**Do not start writing until you have read the complete plan.** A section that looks
correct in Phase A may have downstream assumptions that break in Phase E.

---

## Section 2 — The Delta Analysis

Write two explicit lists before touching any session file.

### List 1: What does NOT change

Be explicit. Name each thing:

```
UNCHANGED FROM EXISTING PLAN:
- The per-flow layer split Api/Application/Infrastructure/Tests and its project references
- The domain return type DataProcessResult<T> on all Application public methods
- The minimal-API route templates already defined in Program.cs
- The xUnit FlowFixture setup and the existing [Fact]/[Theory] list
- The Gate-B "dotnet test → exact pass count, 0 fail" rule at every phase
- One commit per phase boundary
```

This list matters: a session executing the file must not re-examine these. Without the
explicit "unchanged" list, correct content gets "improved" into incorrect content.

### List 2: What changes and exactly where

For each change: (1) which phase, (2) new step / modified gate / new output, (3) what new
infrastructure drives it, (4) additive or replacement.

```
PHASE A — New step A0: fabric availability pre-scan (additive)
  Driven by: new INotificationStore fabric contract
  Position: before existing A1
  Action: confirm INotificationStore is registered in AddXxxFlow() and provided by FlowFixture

PHASE B — Modified B2: add Gate "dotnet test --filter ~Notification" (additive to gate)
  Driven by: new notification route in Program.cs
  Position: at end of Phase B gate, after existing checks
  On FAIL: phase completion BLOCKED until the new xUnit tests pass

PHASE D — New output: Angular 21 Playwright spec for the new client route (additive)
  Driven by: new client surface
  Position: when recording e2e evidence
```

---

## Section 3 — Session Split Criteria

Apply to each phase:

- **Criterion 1 — Context-loss risk.** A phase needs its own session file if losing
  context mid-phase makes correct resumption impossible (e.g. a multi-route generation
  phase where each route feeds the next). Independent fixture seeding can share one file.
- **Criterion 2 — Decision-tree depth.** A phase with branching that produces significantly
  different execution paths (e.g. status-code mapping × validation branches × idempotency
  states) needs its own detailed session file.
- **Criterion 3 — Cognitive load.** A phase that requires more in working memory than a
  fresh session can hold (e.g. test files using fixture patterns + DI wiring + multiple
  fabric mocks) gets its own session file.
- **Criterion 4 — Companion document requirement.** Every session file ends with a
  `STATE.json` update, a phase completion package, and a clean termination point.

---

## Section 4 — Document Architecture

Before writing any session file, write a short architecture block:

```
DOCUMENT ARCHITECTURE:

FLOW-NN-<slug>-INFRASTRUCTURE-GATE.md
  Purpose: all prerequisite checks (build, test baseline, fabric availability). Initializes STATE.json.
  Ends with: STATE.json initialized. STOP.

FLOW-NN-<slug>-SESSION-A.md
  Purpose: Phase A — additive steps prepended; existing content unchanged.
  Ends with: phase completion package. STOP.

FLOW-NN-<slug>-SESSION-B.md
  Purpose: Phase B (dedicated — context-loss risk, deep branching).
  Ends with: phase completion package. STOP.
```

---

## Section 5 — Unchanged List Hard Rule

Without the explicit UNCHANGED list, a session executing the restructured plan will
re-examine and potentially modify correct content. Write the list. Name each thing. Do not
write "most things are unchanged" — that is not a list.

---

## Anti-Patterns

1. **Rewriting the entire plan** — the delta list identifies what changed; preserve unchanged content verbatim.
2. **Session files that don't cold-start** — every session file begins with: read STATE.json, verify previous phase complete, load governance chain.
3. **SESSION-BRIEF that says "see previous session"** — the brief must contain the key facts, not a pointer.
4. **Combining phases with context-loss risk** — a deep generation phase must be its own session file.
5. **Not writing the unchanged list** — without it, the executor re-examines correct content.

## Integration

Load alongside: `phase-planning-SKILL.md`, `plan-self-validation-SKILL.md`,
`plan-execution-feedback-SKILL.md`. Required by: `planning-session-startup-SKILL.md`.
