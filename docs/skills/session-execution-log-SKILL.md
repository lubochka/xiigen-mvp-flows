# Session Execution Log

> **Stack adaptation (xiigen flows).** The structured post-gate execution log
> ported from `llm_mvp_core/docs/skills/session-execution-log-SKILL.md` (G07). The
> universal part (an `EXECUTION-LOG-<id>.json` is written ONLY after the phase
> gate passes; the mandatory `humanReadablePlanExecutionLog` block with stage,
> total/done/remaining counts and names, currentPlanStep, owner, claimsOpened/
> Closed, whyThisStatusIsTrue, nextRequiredAction; `testBaseline.delta == exit -
> entry`) is taken verbatim from core. Adapted here:
> - `buildStatus.command` = `dotnet build Xiigen.Flows.slnx`;
>   `testBaseline.command` = `dotnet test Xiigen.Flows.slnx --no-build` (or the
>   affected per-flow `Xiigen.Flow.<Name>.Tests`, xUnit). Client-bearing flows add
>   an `ngTest` block (`ng test --watch=false`).
> - `buildStatus` must enforce **0 errors AND 0 warnings**: `Directory.Build.props`
>   sets `TreatWarningsAsErrors=true`, so a CS-warning fails the build gate.
> - `filesChanged` covers `flows/FLOW-NN-<slug>/{server,client,contracts}` and any
>   `.xiigen/wiki-domain` manifest touched.
> - The log is per-flow (file id ties to `FLOW-NN`), kept under
>   `docs/session-files/` or per-flow `docs/`.
> - The Russian-first Human-Readable Plan/Execution Log block is mandatory, not
>   just the JSON schema.
> - Domain return type cited is `DataProcessResult<T>` — **not** core
>   `OperationResult<T>`.

## Purpose

Every phase produces a structured execution log. This log is the single source
of truth for what happened in a phase: what files changed, what tests moved,
what was built, and what decisions were made. Future sessions read it.
Audit trails reference it.

---

## When to Write

After every phase gate passes — never before. If the gate fails (tests regress,
build errors or warnings), the execution log is NOT written. A log only exists
for a phase that completed successfully.

---

## File Location and Name

```
docs/session-files/EXECUTION-LOG-FLOW-NN-P[N].json

Examples:
  docs/session-files/EXECUTION-LOG-FLOW-01-P3.json
  docs/session-files/EXECUTION-LOG-FLOW-16-P4.json
```

---

## Schema

```json
{
  "flow": "FLOW-16-marketplace-payments",
  "phaseNumber": "P4",
  "phaseName": "PaymentSettlementService — idempotent settlement + webhook reconcile",
  "completedAt": "2026-06-28",
  "durationMinutes": 52,

  "humanReadablePlanExecutionLog": {
    "stage": "FLOW-16 Phase 4 completion",
    "totalMajorPlanBlocksOrCurrentPhaseTasks": "8 current-phase tasks",
    "completedCountAndNames": "6/8: read, implement, targeted tests, docs sync, gate check, package",
    "currentPlanStep": "STEP 4 - package phase evidence",
    "currentActiveTaskAndOwner": "Package evidence - executor",
    "remainingCountAndNames": "2/8: review, commit/push boundary",
    "currentResultType": "artifact",
    "whatChanged": "Settlement service and its xUnit tests were added; webhook reconcile wired",
    "whatRemains": "Next phase starts FLOW-19 saga compensation",
    "claimsOpened": [],
    "claimsClosed": ["FLOW-16 Phase 4 Gate B"],
    "claimsStillClosedAndWhy": ["Full FLOW-16 readiness remains closed because the Angular checkout client is not yet wired"],
    "whyThisStatusIsTrue": "Build (0 errors, 0 warnings) and the per-flow test gate passed with exact evidence",
    "nextRequiredAction": "Review then commit/push boundary"
  },

  "testBaseline": {
    "entry": 211,
    "exit": 233,
    "delta": 22,
    "command": "dotnet test Xiigen.Flow.MarketplacePayments.Tests --no-build"
  },

  "ngTest": {
    "applicable": false,
    "command": "ng test --watch=false",
    "note": "no Angular client change in this phase"
  },

  "buildStatus": {
    "errors": 0,
    "warnings": 0,
    "command": "dotnet build Xiigen.Flows.slnx"
  },

  "filesChanged": [
    {
      "path": "flows/FLOW-16-marketplace-payments/server/Xiigen.Flow.MarketplacePayments.Application/PaymentSettlementService.cs",
      "type": "created",
      "linesAfter": 187,
      "summary": "Idempotent settlement service; returns DataProcessResult<SettlementOutcome>"
    },
    {
      "path": "flows/FLOW-16-marketplace-payments/server/Xiigen.Flow.MarketplacePayments.Tests/PaymentSettlementServiceTests.cs",
      "type": "created",
      "linesAfter": 312,
      "summary": "22 tests: idempotency, webhook reconcile, failure-code coverage"
    }
  ],

  "artifactsRegistered": {
    "decisionsLocked": [],
    "contractsAdded": ["ICheckoutSettlementService"],
    "flowsExtended": ["FLOW-16-marketplace-payments"]
  },

  "gateResults": [
    { "check": "dotnet build Xiigen.Flows.slnx -> 0 Error(s), 0 Warning(s)", "passed": true, "value": "0 / 0" },
    { "check": "dotnet test (per-flow .Tests) -> Failed: 0", "passed": true, "value": "233 passed, 0 failed" },
    { "check": "DNA Rule 1 — no throw in business logic (DataProcessResult<T> only)", "passed": true },
    { "check": "DNA Rule 2 — no direct instantiation (fabric-first DI)", "passed": true },
    { "check": "flow contracts / .xiigen manifest updated", "passed": true }
  ],

  "gatePassed": true
}
```

---

## Writing Rules

1. Write AFTER the gate passes — never speculatively
2. All number fields must be exact — no estimates or "approximately"
3. `delta` must equal `exit - entry` — if negative, the gate should have failed
4. `filesChanged` must list every file touched during the phase
5. `completedAt` is the timestamp of the gate passing, not phase start
6. `gatePassed` is always `true` — a log only exists for a passed phase
7. `buildStatus` must show 0 errors AND 0 warnings (warnings are build failures here)
8. Human-Readable Plan/Execution Log Gate: every log must include `stage`,
   total major plan blocks and/or current-phase tasks, completed count and
   names, `currentPlanStep`, current active task and owner, remaining count and
   names, current result type, `whatChanged`, `whatRemains`, `claimsOpened`,
   `claimsClosed`, claims still closed and why, `whyThisStatusIsTrue`, and
   `nextRequiredAction` in language a human can read without decoding machine
   status fields
9. Human gate for chat logs: if Luba cannot tell from the log what is done,
   what is running, what remains, whether the current result is code, artifact,
   review, policy, or evidence-only, which claims remain closed, and what
   bounded action comes next, the log fails and triggers governance/skill
   repair before product continuation.

---

## Positive Example

A FLOW-16 Phase 4 log produced after `PaymentSettlementService` implementation:
- `testBaseline.entry: 211` — confirmed from `dotnet test` before the phase started
- `testBaseline.exit: 233` — confirmed from `dotnet test` after the phase gate passed
- `testBaseline.delta: 22` — 233 - 211 = 22 ✅
- `buildStatus.errors: 0`, `buildStatus.warnings: 0` — confirmed from `dotnet build` output
- `filesChanged` — lists every `.cs` file created or modified
- `gateResults` — every check from the EXECUTION file listed with pass/fail

---

## Negative Examples

```
WRONG: Writing the log before the gate runs
  -> Log exists but tests actually regressed — the log is a lie

WRONG: Estimated test counts
  "testBaseline.exit": "approximately 233"
  -> Must be the actual number from dotnet test output

WRONG: Missing filesChanged
  "filesChanged": []  when 4 files were actually modified
  -> Every touched file must appear in filesChanged

WRONG: Ignoring warnings
  "buildStatus": { "errors": 0 }  with 3 CS warnings present
  -> TreatWarningsAsErrors=true: 3 warnings = build failure = no log

WRONG: Machine-only primary status
  Gate rows exist, but there is no human-readable stage, current plan step,
  total/done/current/remaining counts and names, current result type, what
  changed, what remains, claims opened/closed, or why.
  -> The log may be parseable, but it fails the Human-Readable Plan/Execution
     Log Gate.

WRONG: delta is negative
  "entry": 233, "exit": 222, "delta": -11
  -> Negative delta = regression = gate should have failed
  -> If this log exists, the phase was incorrectly declared complete
```

---

## Integration

```
Written by:      phase-completion-packager-SKILL.md (after gate passes)
Read by:         Next session's prerequisites check (to verify test baseline)
                 chain-arithmetic-audit-SKILL.md (to verify test count chain)
Cross-checks:    implementation-integrity-SKILL.md (gateResults must include doc/contract sync)
Companions:      session-state-crystallization-SKILL.md (≤40-line snapshot),
                 session-handoff-SKILL.md (cross-session brief)
```
