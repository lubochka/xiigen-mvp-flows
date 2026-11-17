# Agent Output Format

> **Stack adaptation (xiigen flows).** The three-file rule, the generic SESSION-0
> plan-review gates, the Authority Requirements Ledger, the ban on a machine log
> standing in for a human status, and the English-first execution board (unless the user requests another language) ported
> from `llm_mvp_core/docs/skills/agent-output-format-SKILL.md` (G07). The
> universal part (every plan produces three files — REFERENCE for humans, STATE
> for navigation, EXECUTION for the executor, never merged; SESSION-0 with an
> Authority Requirements Ledger; the format checks; the human-readable status
> rules) is taken from core. Adapted here:
> - EXECUTION Gate-B = `dotnet test --filter` / per-flow `Xiigen.Flow.<Name>.Tests`
>   (xUnit) and `ng test --watch=false` for client-bearing flows; SESSION GATE =
>   `dotnet build Xiigen.Flows.slnx` -> 0 errors AND 0 warnings
>   (`TreatWarningsAsErrors=true`).
> - STATE is the per-flow STATE-JSON next to `flows/FLOW-NN-<slug>/flow.manifest.json`;
>   REFERENCE/EXECUTION live under `docs/session-files/` or the per-flow `docs/`.
> - The branch field records the current working branch (`UpdateUniversalSkills` /
>   `DesignToCode` / `claude/<worktree>`), never `main`/`master`.
> - Domain return type cited is `DataProcessResult<T>` (`IsSuccess`/`ErrorCode`/
>   `Data`, from `Xiigen.Kernel`) — **not** core `OperationResult<T>`.
> - **The llm_mvp_core host-extension format checks are NOT ported.** FC-36 / FC-37
>   / FC-44 (Full Extension Definition / Full Extensions Readiness / Final
>   Execution Review for Claude, Cursor, Codex) and the Dual-Path / 10-review-cycle
>   / QSG clusters are core-product-specific. This repo has no Claude/Cursor/Codex
>   host scenarios; the ported FC set is the generic plan-quality gates only.

## The Three-File Rule

Every plan produces exactly three files:

| File | Audience | Purpose |
|------|---------|---------|
| `PLAN-FLOW-NN-[PHASE]-REFERENCE.md` | Humans (Luba) | What we're building and why — architecture, decisions, context |
| per-flow STATE-JSON (next to `flow.manifest.json`) | Navigation — resuming sessions | Phase state, test baseline, completed deliverables, next step |
| `PLAN-FLOW-NN-[PHASE]-EXECUTION.md` | Executor — execution guide | Step-by-step, file paths, code blocks, Gate B conditions |

Never merge these. Each file has one audience and one purpose.

Every plan file set must also carry authority-chain requirement evidence:
Authority Requirements Ledger, Conversation Delta Ledger, Replacement Mapping
Register, and Contradiction Sweep. Requirements from the current human operator
/ product owner, an architect work order, an approved active plan, or an
accepted reviewer gate are binding unless that authority explicitly marks them
optional, exploratory, or preference-only.

Removing an old mechanism is not coverage for a required replacement. If the
authority requires a replacement capability, the plan must name the replacement
(class/service/contract), its tests, and how the old behavior is migrated — a
deletion-only change is not coverage.

---

## REFERENCE File Format

```markdown
# [FLOW-NN Phase Name] — Reference

## Goal
One paragraph: what this phase accomplishes and why it matters.

## Architecture Decisions
- DR-01: [decision and rationale]
- DR-02: [decision and rationale]

## Component Map
| Component | Location | Contract |
|-----------|---------|-----------|
| PaymentSettlementService | flows/FLOW-16-marketplace-payments/server/.../Application | ICheckoutSettlementService |

## Dependencies
What must exist before this phase starts.

## What Success Looks Like
How Luba will know this phase is complete. Measurable outcomes.
```

---

## Per-Flow STATE-JSON Format

```json
{
  "flow": "FLOW-16-marketplace-payments",
  "phase": "P4",
  "status": "IN_PROGRESS",
  "startedAt": "2026-06-28T10:00:00Z",
  "testBaselineAtStart": 211,
  "testCountNow": 224,
  "completedDeliverables": [
    "flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs",
    "flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementServiceTests.cs"
  ],
  "pendingDeliverables": [
    "flows/FLOW-16-marketplace-payments/client/angular/src/app/checkout/checkout.service.ts"
  ],
  "nextStep": "STEP 3 — wire the Angular checkout client to /settle",
  "gateResults": {
    "gate0_discovery": "PASS",
    "gate1_dna": "PASS",
    "gate2_fabric": "PASS",
    "gate3_contracts": "PASS",
    "gate4_decisions": "PASS",
    "gate5_tests": "PASS",
    "gate6_docs": "PASS",
    "gate7_principles": "PASS"
  },
  "phaseGatesPassed": true
}
```

---

## EXECUTION File Format

```markdown
# [FLOW-NN Phase Name] — Execution

## SESSION GATE
- Build baseline: dotnet build Xiigen.Flows.slnx -> 0 errors, 0 warnings
- Test baseline: dotnet test (per-flow .Tests) -> 211 passed, 0 failed
                 [+ ng test -> N passed, for client-bearing flows]
- Branch: [current working branch — e.g. UpdateUniversalSkills]

---

## STEP 1 — [Description]

**File:** `flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs`

**Gate B:** `dotnet test --filter "PaymentSettlement"` shows Failed: 0, Passed: >= 5

**Code:**
[exact code block — returns DataProcessResult<T>, never throws]

**Test:**
[exact xUnit test block]

---

## STEP 2 — [Description]

**File:** `...`
**Gate B:** `...`

[...]

---

## SESSION GATE (End)

```
dotnet build Xiigen.Flows.slnx -> 0 errors, 0 warnings
dotnet test (per-flow .Tests) --no-build -> Failed: 0, Total >= [baseline + N new tests]
[+ ng test --watch=false -> N passed, for client-bearing flows]
```

## STATE UPDATE
[What to write to the per-flow STATE-JSON after this step]

## STOP — Await Luba Approval Before Next Phase
```

---

## SESSION-0 Plan Review Gate

Before any execution plan is approved, run three gates:

### Gate A — Format Checks (FC)

```
FC-0: Does the file include an Authority Requirements Ledger mapping every
      binding requirement from the active human/product/architect/plan/reviewer
      authority to plan section + test/gate + artifact, with no unmapped,
      deletion-only, or contradicted rows?
FC-0a: Does the file include a Conversation Delta Ledger, Replacement Mapping
       Register, and Contradiction Sweep, and do required replacements map to
       actual replacement capabilities rather than deletion-only changes?
FC-0b: Does every status/count/sub-agent claim have evidence matching its exact
       scope: ledger rows are not cycles, parent/self review is not sub-agent
       review, and N cycles/reviews/passes require N independent evidence
       records?
FC-1: Does every STEP have a Gate B that is a command + expected output?
FC-2: Does every STEP reference a real file path (not a placeholder)?
FC-3: Does every new contract/interface appear in the flow contracts /
      flow.manifest.json update plan?
FC-4: Does the plan violate any of the DNA rules (DataProcessResult<T> everywhere,
      fabric-first, etc.)?
FC-5: Does every STEP that creates a class include a test step?
FC-6: Does the plan include a final dotnet test (per-flow .Tests) count check
      (and ng test for client-bearing flows)?
FC-7: Are all abbreviations and FLOW-NN names expanded on first use?
FC-8: Does every code block have Rule 3 comments on every line?
FC-9: Does the plan list every file it will create or modify?
FC-10: Does the SESSION GATE section list the exact dotnet build/test commands
       (0 errors AND 0 warnings)?
FC-11: Is the STOP marker present before the phase transition?
FC-12: Is the STATE UPDATE section present after every session gate?
```

These are the generic plan-quality format checks. (The llm_mvp_core
host-extension checks FC-36 / FC-37 / FC-44 and the 10-review-cycle / QSG
clusters are not part of this repo's gate — see the stack-adaptation header.)

### Gate B — Independent Review

Get independent review for plans that:
- Span more than one flow
- Add or change a public contract / minimal-API endpoint
- Introduce a new algorithm or a new Angular surface

### Gate C — Luba Approval

Present the plan. Wait for explicit written approval. Do not begin until it is received.

---

## Human-Readable Status Rule

A status, review, or final report must be readable by Luba first. Machine labels
(STATE codes, gate ids, JSON rows, packet names) may appear only after a plain
human explanation with counts. See `output-readability-gate-SKILL.md` for the
full Execution Board and No-Machine-Log-To-User rules. The minimum a status must
carry:

```text
Phase X of Y: <name>. Purpose: <what this phase solves for the flow/user>.
Step X.Y of X.N: <name>. Purpose: <what this step checks/creates>.
Status: DONE | IN_PROGRESS | NEEDS_FIX | REVIEWING | BLOCKED_WITH_REASON. <one human sentence>
Done: <counts + concrete artifacts>.
In progress: <current action and who is doing it>.
Remaining: <numbered remaining steps + human meaning>.
Next action: <next bounded action, or NONE>.
```

A bare phase code, `passed: true`, or a JSON/STATE label is not a valid
user-facing status unless preceded by this human explanation.

---

## Anti-Patterns

1. **Merging REFERENCE and EXECUTION into one file** — audiences and purposes conflict
2. **EXECUTION without Gate B per step** — steps without gates have no completion criteria
3. **STATE without test count** — resuming sessions cannot establish a regression baseline
4. **STOP marker missing** — the executor continues past the phase boundary without approval
5. **Placeholder file paths** — `flows/TODO/` is not a file path; do not put it in EXECUTION
6. **Ignoring warnings in the SESSION GATE** — `TreatWarningsAsErrors=true`; a warning is a build failure
7. **Machine log replaces user status** — a STATE row, JSON report, or bare gate id
   without the human-readable phase/step/count/next-action block is not a valid
   user-facing status. Opaque codes are secondary only.
8. **Ledger rows called cycles** — a table with N rows is not N review/fix cycles.
   Count claims require N independent evidence records.
9. **Parent work called sub-agent work** — parent/self review cannot be described
   as sub-agent review without a named sub-agent packet.

---

## Integration

- `session-file-authoring-SKILL.md` governs the internal crystallization of each session file
- `session-output-contract-SKILL.md` governs whether the right files are produced
- `output-readability-gate-SKILL.md` governs the English-first board (unless the user requests another language) and machine-status bans
- `session-execution-log-SKILL.md` is the post-gate EXECUTION-LOG companion to the STATE file
