# Plan Self-Validation

> **Stack adaptation (xiigen flows).** Universal Phase-0 gate (ledger / contradiction sweep
> / replacement mapping / phantom-skill / order) ported from
> `llm_mvp_core/docs/skills/plan-self-validation-SKILL.md`. Runs BEFORE Phase 1. In this
> repo Phase 0 also checks that plan references resolve to per-flow layers
> `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` and to the `.xiigen` /
> wiki-domain manifests this repo consumes from `llm_mvp_core`. Gate-B conditions are
> `dotnet build` + `dotnet test` (xUnit) per-flow (plus Angular 21 Playwright for client
> phases), NOT a generic "tests pass". The domain wrapper is `DataProcessResult<T>`.

## Core Problem

A plan that references a skill that does not exist yet, or omits the phase that creates a
skill it later invokes, will fail at execution time with no useful diagnostic. Phase 0
catches those failures before Phase 1.

Phase 0 also validates authority-chain requirements. Requirements from the current human
operator / product owner, architect work order, approved active plan, or accepted reviewer
gate are binding unless that authority explicitly marks them optional, exploratory, or
preference-only. They are not wishlist items.

Before Phase 1, build an Authority Requirements Ledger, Conversation Delta Ledger,
Replacement Mapping Register, and Contradiction Sweep. Every binding requirement must map
to a plan section, gate/test, and artifact. Every rejected or superseded mechanism must map
to the required replacement capability. Deletion-only coverage is not valid.

Dishonest-claim prevention is part of Phase 0. Before Phase 1, the plan must prove that
every count/status/completion/sub-agent claim has evidence matching its exact scope. A
ledger row, checklist row, generated table, or summary cannot be counted as a review/fix
cycle. Parent/self review cannot be counted as sub-agent review. If the plan requires N
reviews/cycles/passes, it must define the N independent evidence records, ids, work orders,
reviewed scopes, findings or no-fix justifications, and recheck evidence that will count.

---

## When to Invoke

- Before any multi-phase (≥ 2 phases) plan begins execution.
- At the end of plan authoring, before handing a plan to an executor.
- Whenever a plan references skills, contracts, or artifacts that may not exist yet.

One Phase 0 gate before Phase 1 = no phantom-skill failures mid-execution.

---

## Section 1 — The Phase 0 Requirement

Every multi-phase plan must contain a **Phase 0: Plan Self-Validation** that runs all
checks below before Phase 1. Phase 0 is non-negotiable. If a plan does not include Phase 0,
add it before execution begins.

Phase 0 gate: all checks pass → Phase 1 may proceed. Any check fails → execution stops; the
gap is resolved (repair skills/guides first when a prior implementation already reached the
wrong shape).

---

## Section 2 — The Checks

### Check 0 — Authority Requirements Ledger Completeness

Extract authority-chain requirements and map each:
```
Requirement ID: R-[short-name]
Conversation source: [message / paraphrase]
Plan section: [section or UNMAPPED]
Gate/test: [dotnet build / dotnet test --filter ... → exact count / Playwright spec, or UNMAPPED]
Artifact: [path or UNMAPPED]
Status: PASS | UNMAPPED | CONTRADICTED
Next action: [repair action]
```
If any row is unmapped or contradicted → BLOCKED.

### Check 1 — Goal Delivery Completeness

Every element of the stated goal maps to exactly one phase with a Gate B condition.
```
Goal element: [element text]
Assigned phase: Phase [N] — [phase name]
Gate B: dotnet test --filter "[filter]" → [count] passed, 0 failed   (or dotnet build → 0 Error(s) / Playwright green)
Status: PASS | BLOCKED
```
If any goal element has no assigned phase → BLOCKED.

### Check 2 — Phantom Skill Detection

Every skill referenced in any phase must either already exist in `docs/skills/` with a
matching filename, OR be created by a phase that runs before the referencing phase.
```bash
ls docs/skills/[skill-name]-SKILL.md
```
```
Skill referenced: [skill-name]-SKILL.md
Referenced in: Phase [N]
Exists now: YES / NO
Created by: Phase [M] (M < N) / NOT CREATED → BLOCKED
```

### Check 3 — Reference Resolution Matrix (this stack)

Every `path:line`, class, contract, route, or consumed manifest the plan references must
resolve in the current repo or in a phase that creates it first:
```
Reference: [Xiigen.Flow.<Name>.<Layer> class / Xiigen.Kernel contract / Program.cs route / .xiigen or wiki-domain manifest]
Resolves now: YES / NO
Verify: grep / ls / Read result
Created by: Phase [M] / EXTERNAL (consumed from llm_mvp_core) / NOT FOUND → BLOCKED
Status: PASS | BLOCKED
```
A reference to a `DataProcessResult<T>` method, an `AddXxxFlow()` registration, or a
fabric contract that does not exist and is not created by an earlier phase → BLOCKED.

### Check 4 — Execution Order Consistency

1. A skill/doc-registration phase runs AFTER the skill-creating phase.
2. No phase depends on an artifact produced by a later phase.
3. Gate B conditions are testable with `dotnet test`, `dotnet build`, or Playwright — not
   "visually inspect" / "check manually".
```
Phase [N] dependency: [artifact]
Produced by: Phase [M]
Order: M < N → PASS | M > N → BLOCKED
```

---

## Section 3 — Execution Phase Template

Every CREATE phase follows this template:

```markdown
## Phase [N]: [Phase Name]

**Pre-condition:**
- [specific file or test count that must be true before this phase starts]
- Verified by: `dotnet test --filter "[filter]" → [expected count] passed`

**Output files:**
- `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.<Layer>/[File].cs` — [what it does]
- `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/[File]Tests.cs` — [what is tested]

**Content specification:**
- [methods, contracts, routes, behaviors the output must contain]
- Project standards: OperationResult-style discipline via DataProcessResult<T> (no throw for business failures), Fabric First (ctor interfaces), every production line commented

**Blast radius:**
- Files read / written / referencing this output

**Gate B:**
`dotnet test --filter "[filter]" → [exact count] passed, 0 failed`   (and/or `dotnet build → 0 Error(s)` / Playwright green)

**Post-condition:**
- [assertion that the output is complete and correct]
```

---

## Section 4 — Phase 0 Output Format

```markdown
## Phase 0: Plan Self-Validation (plan-self-validation-SKILL)

Check 0 — Authority Requirements Ledger Completeness: PASS | [N] unmapped/contradicted
Check 1 — Goal Delivery Completeness: PASS | [N] BLOCKED
Check 2 — Phantom Skill Detection: PASS | [N] phantom skills
Check 3 — Reference Resolution Matrix: PASS | [N] unresolved references
Check 4 — Execution Order Consistency: PASS | [N] order violations

PHASE 0 VERDICT: PASS → Phase 1 may proceed
               | FAIL → [list blocking items] → resolved before Phase 1
```

---

## Anti-Patterns

1. **"Skip Phase 0 for small plans"** — phantom skills in 2-phase plans fail just as hard.
2. **"I'll fix phantom skills as I go"** — a mid-execution discovery stops the executor cold.
3. **"Gate B is 'tests pass'"** — a Gate B is `dotnet test → 47 passed, 0 failed`. Exact count, exact command.
4. **"Doc/skill registration doesn't need its own phase"** — it does, and it runs after the creating phase.

## Integration

- `intent-to-plan-SKILL.md` produces the plan structure this skill validates.
- `design-artifact-completeness-SKILL.md` Check 1–2 backs Check 3 (a referenced artifact that is a stub is unresolved).
- Required by: `phase-planning-SKILL.md` (Gate -1), `planning-session-startup-SKILL.md`.
