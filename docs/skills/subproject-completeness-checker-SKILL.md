# subproject-completeness-checker-SKILL.md — Two pre-gates + T1–T10 before a flow plan is ready

> **Stack adaptation (xiigen flows).** Universal *subproject completeness checker*
> ported from `llm_mvp_core/docs/skills/subproject-completeness-checker-SKILL.md`
> (G08). Greenfield here — no prior author version (core's nearest were several
> goal-delivery / coverage / design-artifact / flow-completeness checkers; this is
> the consolidated checker). The universal core kept: two structural pre-gates run
> first — Pre-Gate 1 (goal→phase mapping with a verification step per goal) and
> Pre-Gate 2 (design artifacts populated, not stubs) — then T1–T10 (phase
> structure, measurable Gate B, prerequisites, result wrapper, DI registration,
> test categories, injectable clock, compensation LIFO, DNA, scope), and a
> READY/NOT_READY score. The stack details are adapted:
> - **Gate B** = `dotnet test --filter "..." → N passed, 0 failed` (`net10.0`,
>   xUnit) and/or `dotnet build Xiigen.Flows.slnx → 0 Error(s)` (0 warnings).
> - **Design artifact populated** = a non-empty contract in `contracts/` + ≥1
>   `[Fact]` in the flow's `Xiigen.Flow.<Name>.Tests`.
> - **T4** = `DataProcessResult<T>` everywhere (XIIGen family — **NOT** core
>   `OperationResult<T>`); **T5** = DI registration via the `Add<Name>Flow()`
>   extension in `server/Xiigen.Flow.<Name>.Api/Program.cs`.
> - **T9 DNA** is taken from this repo's DNA translation
>   (`docs/skills/DNA-DOTNET-TRANSLATION.md`), not core Rules 1–10 verbatim. UI
>   checks apply only to client-bearing flows (Angular 21 + Playwright).

## Core Problem

A flow plan that passes internal consistency checks can still fail to deliver the
stated goals — if those goals were never mapped to specific phases. And a plan
that maps goals to phases can still fail if the design artifacts those phases
depend on are empty stubs. This skill adds two structural pre-gates that run
before all other checks.

---

## When to Invoke

- "Is this flow plan ready for implementation?"
- "What's missing from this phase plan?"
- After any flow phase plan is produced or updated
- Before starting Phase 1 of any multi-phase flow plan
- When `plan-self-validation-SKILL.md` returns incomplete results

One completeness run before implementation = zero "phases complete but goals not delivered" failures.

---

## Section 1 — The Pre-Gate Checks (Run First)

These two block everything else. A plan that fails either is not ready regardless
of how well the remaining checks pass.

### Pre-Gate 1 — Goal-to-Phase Mapping

Verifies every goal element stated by Luba has ≥1 phase and a verification step.

Protocol:
1. Load the verbatim goal statement from the session's goal context record.
2. Decompose into discrete goal elements.
3. For each, identify which phase(s) deliver it.
4. Verify each mapped goal has a concrete verification step (`dotnet test` filter,
   build count, Playwright spec, or observable output).

```markdown
| Goal Element | Phase(s) | Verification Step | Verdict |
|---|---|---|---|
| GE-1: [text] | Phase 2, 3 | dotnet test --filter → 5 passed | APPROVED |
| GE-2: [text] | — | — | BLOCK — goal not covered |
```

**FAIL** = any goal element has zero phases or no verification step. Blocks the
plan regardless of all other checks.

Resolution: add phases that deliver the uncovered element, OR explicitly defer
with Luba's approval (record as DEFERRED with reason).

### Pre-Gate 2 — Design Artifact Populated

Verifies every artifact referenced in the plan exists and is populated (not a stub):
- Each contract referenced: present in `contracts/` (or the Application boundary) and non-empty.
- Each test class referenced: ≥1 `[Fact]`.
- Each DI registration referenced: reachable through the `Add<Name>Flow()` extension.

**FAIL** = any referenced artifact is MISSING or STUB. Resolution: implement the
stub first, OR scope the plan around the gap with explicit acknowledgment.

---

## Section 2 — Technical Completeness Checks (After Pre-Gates Pass)

### T1 — Phase structure declared
Each phase has: goal, output files, Gate B condition, prerequisite list.

### T2 — Gate B is measurable
Every phase has exactly one Gate B as:
- `dotnet test --filter "[filter]" → [N] passed, 0 failed`, or
- `dotnet build Xiigen.Flows.slnx → 0 Error(s)` (0 warnings), or
- a specific count / assertion (incl. a named Playwright spec for UI).

"Works correctly" is not a Gate B. "Passes" without a filter is not a Gate B.

### T3 — Prerequisites verified
Every phase depending on another phase's output lists it as a prerequisite. No
phase assumes an artifact that does not exist (incl. cross-flow from `flow.manifest.json`).

### T4 — DataProcessResult<T> usage declared
Any new public Application method returns `DataProcessResult<T>`. Phases that add
throw-based business logic fail T4. (Do not substitute core `OperationResult<T>`.)

### T5 — DI registration planned
Any new class implementing a contract has a registration step in its phase plan
(the `Add<Name>Flow()` extension in `Program.cs`, or the DI phase is explicitly called out).

### T6 — Test categories declared
For any capability with writes, timers, or multi-step operations, the plan names
which test categories apply (unit / integration / e2e / negative; Playmaker UX for client surfaces).

### T7 — Time-based tests use an injectable clock
Any capability with a TTL, timer, or scheduled operation declares clock injection
(e.g. an `IClock`/`IDateTimeProvider` abstraction) — never `Thread.Sleep`/`Task.Delay` in tests.

### T8 — Compensation chain order declared
Any capability with multi-step rollback declares LIFO order explicitly.

### T9 — DNA compliance
DNA satisfied per `docs/skills/DNA-DOTNET-TRANSLATION.md`:
- every public Application method returns `DataProcessResult<T>`
- fabric-first (contracts, not concrete types)
- every production line has a comment
- no core ML / `OperationResult<T>` leakage into this repo

### T10 — Scope discipline
The plan does not touch flows outside its declared scope. Any cross-flow effect is
called out with explicit justification (and a `flow.manifest.json` edge).

---

## Section 3 — Completeness Score Format

```markdown
## Flow Completeness Check — [Plan Name]

Pre-Gate 1 (Goal-to-Phase Mapping): PASS / FAIL
Pre-Gate 2 (Design Artifact Populated): PASS / FAIL

If Pre-Gate 1 or Pre-Gate 2 FAIL: stop here. Do not evaluate T1–T10.

Technical completeness:
| Check | Status | Note |
|---|---|---|
| T1 — Phase structure | PASS / FAIL | [detail] |
| T2 — Gate B measurable | PASS / FAIL | [detail] |
| T3 — Prerequisites verified | PASS / FAIL | [detail] |
| T4 — DataProcessResult<T> | PASS / FAIL | [detail] |
| T5 — DI registration | PASS / FAIL | [detail] |
| T6 — Test categories | PASS / FAIL | [detail] |
| T7 — Injectable clock | PASS / FAIL | [detail if applicable] |
| T8 — Compensation LIFO | PASS / FAIL | [detail if applicable] |
| T9 — DNA compliance | PASS / FAIL | [detail] |
| T10 — Scope discipline | PASS / FAIL | [detail] |

Score: [N]/12 checks pass

Overall verdict: READY / NOT_READY
Blocking items: [list]
Next step: [resolve blocking items / proceed to Phase 1]
```

---

## Anti-Patterns

1. **"The plan covers the goals implicitly"** — implicit coverage is the failure mode. Goal-to-phase mapping must be explicit; if not written, it does not exist.
2. **"Gate B is 'the tests pass'"** — a Gate B without a filter and count is not checkable. Which tests? How many?
3. **"We can add test categories after implementation"** — declared before coding so implementation satisfies the declared behavior; after means testing what was built, not what was required.
4. **"The artifact will be implemented in a later phase"** — if Phase 2 depends on a Phase 1 artifact, Phase 1 must be a listed prerequisite; silent dependencies cause Phase 2 to fail.

---

## Integration

- `plan-self-validation-SKILL.md` / `goal-delivery-completeness-SKILL.md` provide the goal-delivery version of Pre-Gate 1.
- `design-artifact-completeness-SKILL.md` provides the populated-artifact checks for Pre-Gate 2.
- `e2e-test-matrix-SKILL.md` / `user-journey-testing-SKILL.md` govern T6.
- `claim-as-hypothesis-SKILL.md` provides the DEFERRED mechanism for Pre-Gate 1 gaps.
