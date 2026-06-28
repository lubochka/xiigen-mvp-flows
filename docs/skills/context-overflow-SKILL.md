# Context Overflow

> **Stack adaptation (xiigen flows).** The three-threshold context-budget protocol
> ported from `llm_mvp_core/docs/skills/context-overflow-SKILL.md` (G07). The
> universal part (3 thresholds — `> 35%` continue normally; `<= 35%` wrap-up:
> finish the current step, save STATE `PAUSED_CONTEXT_LIMIT`, record a Resume-From
> note, commit; `<= 25%` emergency stop with immediate commit; plus a Session
> Resumption Protocol at the next start) is taken verbatim from core. Adapted here:
> - Resume / wrap-up commands use this stack: `git branch --show-current` (confirm
>   the **current working branch**, not `main`), `dotnet build Xiigen.Flows.slnx`,
>   `dotnet test Xiigen.Flows.slnx --no-build` (or the affected per-flow
>   `Xiigen.Flow.<Name>.Tests`, xUnit), and `ng test` for client-bearing flows.
> - STATE status is written to the **per-flow STATE-JSON** next to
>   `flows/FLOW-NN-<slug>/flow.manifest.json`, with `status: PAUSED_CONTEXT_LIMIT`.
> - Emergency commit covers the changed `flows/FLOW-NN-<slug>/{server,client,
>   contracts}`. **Commit to the current working branch — never `main`/`master`,
>   and do not auto-push** unless the active work order grants push.
> - Domain return type cited is `DataProcessResult<T>` — **not** core
>   `OperationResult<T>`.

## Overview

When a session runs out of context, in-progress work may be lost. This skill governs
what to do at three threshold levels so nothing important is lost.

---

## The Three Thresholds

### Threshold 1: > 35% Context Remaining — Continue Normally

Continue executing the plan. No special action required.

### Threshold 2: <= 35% Context — Wrap-Up Mode

Switch to wrap-up mode:

```
1. Finish the current step completely (do not start a new step)
2. Save the per-flow STATE-JSON with status: "PAUSED_CONTEXT_LIMIT"
3. Record exactly which step was in progress
4. Run dotnet test (per-flow .Tests) and record the current count
   (+ ng test for client-bearing flows)
5. Write the "Resume From" note (see format below)
6. Commit whatever is complete to the current working branch (even partial phase progress)
7. Do NOT start the next step
8. Do NOT push unless the active work order grants push
```

Resume From note format:
```
RESUME FROM: [timestamp]
  Flow: FLOW-NN-<slug>
  Last completed step: STEP N — [description]
  Current test count: [N] passed, 0 failed (per-flow .Tests)  [+ ng test: N passed]
  Next step: STEP N+1 — [description, file, Gate B]
  In-progress work: [describe anything done but not committed]
  Blockers: [any unresolved issues]
```

### Threshold 3: <= 25% Context — Emergency Stop

Stop immediately. Save and commit regardless of step completion status.

```
1. Stop mid-step if necessary
2. Save the per-flow STATE-JSON:
   {
     "flow": "FLOW-NN-<slug>",
     "status": "INCOMPLETE_CONTEXT_OVERFLOW",
     "pausedAt": "[timestamp]",
     "lastCompletedStep": "STEP N",
     "partialWork": "[describe what was done but not finished]",
     "testCount": "[N] from last run",
     "nextStep": "STEP N+1"
   }
3. git add [all changed files under flows/FLOW-NN-<slug>/...]
4. git commit -m "WIP: context limit — FLOW-NN phase N partial, step N+1 pending"
   (current working branch only — never main/master)
5. Report to Luba with the STATE file path
   (push only if the active work order explicitly grants push)
```

---

## How to Estimate Context Remaining

The session has no direct token counter visible. Use these proxies:

| Signal | Likely Threshold |
|--------|-----------------|
| Can still see early session messages in context | > 35% |
| System reminder shows compressed earlier messages | ≈ 35% |
| Large blocks of conversation appear summarized | <= 25% |
| Responses feel slower or shorter | Possible limit approaching |

When uncertain: assume lower estimate and wrap up.

---

## Per-Flow STATE-JSON at Context Wrap-Up

```json
{
  "flow": "FLOW-16-marketplace-payments",
  "phase": "P4",
  "status": "PAUSED_CONTEXT_LIMIT",
  "pausedAt": "2026-06-28T14:30:00Z",
  "testBaselineAtStart": 211,
  "testCountNow": 224,
  "completedDeliverables": [
    "flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs",
    "flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementServiceTests.cs"
  ],
  "lastCompletedStep": "STEP 2 — PaymentSettlementService unit tests",
  "nextStep": "STEP 3 — webhook reconcile integration test",
  "partialWork": "none",
  "blockers": "none"
}
```

---

## Session Resumption Protocol

At the start of the next session, when resuming from context wrap-up:

```
1. git branch --show-current -> confirm on the current working branch
   (UpdateUniversalSkills / DesignToCode / claude/<worktree>), NOT main
2. dotnet build Xiigen.Flows.slnx -> 0 errors, 0 warnings
3. dotnet test Xiigen.Flows.slnx --no-build -> record new baseline
   (+ ng test for client-bearing flows)
4. Read the per-flow STATE-JSON -> find lastCompletedStep and nextStep
5. Read all files referenced in completedDeliverables (Rule 5)
6. Continue from nextStep
```

Do not re-do completed steps. The STATE file is the source of truth about what was done.

---

## Anti-Patterns

1. **Trying to finish the phase before the context limit** — rushing causes mistakes; wrap cleanly
2. **Not saving STATE** — the next session has no way to know where to resume
3. **Not committing at wrap-up** — uncommitted work can be lost
4. **Over-estimating context remaining** — when uncertain, use the emergency stop protocol
5. **Committing to main or auto-pushing at wrap-up** — commit to the current working branch only; push only with explicit work-order authority
