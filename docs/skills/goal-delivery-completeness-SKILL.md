# goal-delivery-completeness-SKILL.md — Does the plan deliver what was asked? (first check)

> **Stack adaptation (xiigen flows).** Universal goal-coverage gate ported from
> `llm_mvp_core/docs/skills/goal-delivery-completeness-SKILL.md`. Adapted to this repo:
> - verification steps use the flows stack — per-flow `dotnet test --filter`/`dotnet build`
>   (xUnit, `net10.0`), Angular 21 `ng test`/`npx playwright test`; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - a manifest / `.xiigen` / wiki-domain goal is verified by **load through the locator**, not
>   file presence;
> - core ML/`ILearningMachine`/DPO and Claude/Cursor/Codex host gates are core-only (G12) and
>   not applied here.

## When to load
- At every plan review, as the FIRST check — before all other checks.
- When a plan changes its phase list — re-run to confirm goal coverage still holds.
- Before any STOP — confirm nominated phases map to goals.
- Any time Luba asks "does this plan cover everything?"

## Section 1 — Position in review order

This check runs **first**. Before prerequisite, rules-compliance, scope, or any other review.
**If this check returns BLOCK, no other check runs** — auditing internal consistency of a plan
that misses the goal is wasted effort. Fix coverage first.

## Section 2 — Inputs (strict isolation)

Exactly two inputs:

- **Input 1 — verbatim goal statement:** the exact text Luba stated, not paraphrased. If none
  is recorded, the check halts and asks Luba to state the goal explicitly.
- **Input 2 — phase list with 1-line purpose per phase:** just phase name + 1-line purpose
  (not full content, test lists, or file lists).

Inputs NOT received: codebase state, prior plan versions, other check verdicts, session
history. The isolation is intentional — a two-input check produces a verdict anyone can verify
in minutes.

## Section 3 — Protocol (5 steps)

**Step 1 — Decompose the goal** into discrete, independently deliverable/verifiable elements.

```
Goal: "Add idempotent registration to FLOW-01 and expose it via the Api with a Fail path"
GE-1: idempotent registration added to UserRegistrationService
GE-2: exposed via the Api endpoint, including the validation/conflict Fail path
```

**Step 2 — Parse the phase list** (name + 1-line purpose).

```
Phase 1: UserRegistrationService.RegisterAsync — idempotency + DataProcessResult outcomes
Phase 2: Api endpoint wiring — maps DataProcessResult to HTTP, including Fail mapping
Phase 3: Tests — Gate B: dotnet test ...Tests --filter Registration -> 8 passed, 0 failed
```

**Step 3 — Build Goal → Phase mapping** (which phase(s) deliver each element).

**Step 4 — Verify each mapped goal has a verification step.** Acceptable:
- a `dotnet test --filter` and count: `dotnet test ...Tests --filter "Registration" -> 5 passed`;
- a build condition: `dotnet build flows/FLOW-01-.../...Api -> 0 Error(s)`;
- a concrete observable: `ng test` assertion / Playwright step / a contract field read.

Reject goal coverage if the verification allows any of these as final proof:

```text
validation_only: true
metric_type contains static, contract, or mixed
required numeric field = UNKNOWN
build/static-contract check used as runtime/behaviour proof
manifest/`.xiigen` file presence without load/locate evidence
```

If a goal says "works", "uses", "exposes", or "end-to-end", the plan must name the test/Gate B
that executes that path; otherwise the goal is `BLOCK - no runtime proof`. For final completion
the plan must also name a completion coverage matrix comparing each goal element and final-gate
literal set to actual evidence. A phase can pass its own gate and still fail goal delivery when
the matrix proves one element was never covered.

Reject goal coverage when the plan allows: "at least one" where a goal needs all
classes/projects/flows; category buckets instead of required subtype coverage; a final
readiness report that omits any child gate as source of truth; or state `goal_reached: true`
without a zero-gap matrix.

**Step 5 — Produce the verdict table.**

```markdown
| Goal Element | Phase(s) | Verification Step | Verdict |
|---|---|---|---|
| GE-1: idempotent registration | Phase 1, Phase 3 | dotnet test ...--filter Registration -> 5 passed | APPROVED |
| GE-2: Api + Fail path | Phase 2, Phase 3 | endpoint test asserts 409/validation DataProcessResult | APPROVED |
```

## Section 4 — Verdict classes

- **APPROVED** — element has ≥1 phase and a verification step.
- **BLOCK — goal element not covered** — zero phases assigned. Resolution: add phases, or
  explicitly defer with Luba's approval.
- **BLOCK — no way to verify** — phases assigned but no verification step. Resolution: add one.
- **CHALLENGE** — phase assignment is ambiguous. Resolution: restate the phase purpose
  concretely, re-run.

Aggregate: any BLOCK -> plan rejected, no other checks run; any CHALLENGE (no BLOCK) ->
returned for clarification; all APPROVED -> proceed to remaining checks.

## Section 5 — Output format

```markdown
## Goal Delivery Completeness Verdict
Goal statement (verbatim): "[exact text]"
Goal elements: GE-1: [...]; GE-2: [...]
Plan phases: Phase 1: [name — purpose]; ...
Goal -> Phase mapping: [table]
Aggregate verdict: [APPROVED | BLOCK | CHALLENGE]
Unmapped goals: [n of N]; Unverified goals: [n of N]; Ambiguous: [n of N]
Next action: [reject + add phases for GE-x | clarify | proceed]
```

## Section 6 — Worked examples

**A — Full coverage (APPROVED).** Goal: "Implement FLOW-04 attendance combining check-in and
gamification." GE-1 check-in, GE-2 gamification; Phases 1–4 implement + test both paths; both
verified by the 12-passing suite -> APPROVED.

**B — Missing coverage (BLOCK).** Goal: "Add registration AND make the throttle configurable."
Phase 1 hardcodes the throttle; no phase adds configuration. GE-2 -> `BLOCK - not covered`.
Resolution: add Phase 1b "Replace hardcoded throttle with `IConfiguration`/`IOptions` keys."

**C — Ambiguous (CHALLENGE).** Goal: "Improve registration quality." Phase 1: "Add a layer."
-> CHALLENGE. Resolution: "Add duplicate-email guard; verify conflict path returns
`Failure<T>("CONFLICT", ...)` and a test asserts it." Re-run -> APPROVED.

## Anti-patterns

1. "The goal is obviously covered by Phase 3" — obvious coverage is where drift hides; the
   mapping is mechanical on purpose.
2. "I'll add the verification step during implementation" — a step in the plan is a commitment;
   a step "in implementation" is a wish. This check evaluates the plan.
3. "CHALLENGE is easier than BLOCK" — gaming; write phases that either deliver the goal or
   don't.
4. "Multi-goal statements can share one phase" — they can, but both goals must be explicitly
   mapped to it.
5. "10 review rounds surely caught coverage" — correctness rounds do not audit goal coverage;
   only this check does.
