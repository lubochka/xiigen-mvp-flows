# Audit Protocol (xiigen flows, .NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield. Ported from
> `llm_mvp_core/docs/skills/audit-protocol-SKILL.md` (G11). The universal part
> carries over: the **10-point audit checklist**, the **severity tags**
> (BLOCKING / WRITE-TIME / INFORMATIONAL) with non-negotiable consistency rules, the
> **audit output format**, and the **mandatory NOT-CHECKED declaration**. Adapted here:
> - **Gate thresholds** (Point 7) are expressed as exact
>   **`dotnet test → N passed, 0 failed`** and **`dotnet build → 0 Error, 0 Warning`**
>   (`TreatWarningsAsErrors=true`), and **Playwright → N passed** for client-bearing flows.
>   Never "~approximately passes."
> - **Path verification** (Point 6) checks paths against the real per-flow layout
>   `flows/FLOW-NN-<name>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   and `client/angular/src`.
> - **Commit atomicity** (Point 4) = GOVERNANCE and PRODUCTION committed **before**
>   TEST-ONLY; DOCS travel in the **same** commit as the code (greenfield — commit
>   order is set from scratch, so this matters).
> - **API-shape verification** (Point 3) checks `I*` contract shapes and that domain
>   returns are `DataProcessResult<T>` (not core `OperationResult<T>`, which is 0× in
>   flows `src`).
> - This audit naturally feeds the per-flow QA/visual-evidence gate already in the
>   repo (`docs/e2e-snapshots`, `docs/visual-evidence`).

## Core Problem

An ad-hoc audit varies by reviewer and time available. One reviewer checks chain
arithmetic and skips blast radius. Another checks DNA compliance but skips API
shapes. The result is findings that depend on who ran the audit, not on what the
plan contains. This skill defines a 10-point checklist that every audit runs
against, with severity tags that are consistent and non-negotiable.

---

## When to Invoke

- During any plan review (before declaring a plan "ready for execution").
- When reviewing another agent's output or plan.
- After a multi-phase plan's Phase 0 validation.
- Before any phase commit in a flow (the commit-boundary audit).

---

## Section 1 — 10-Point Audit Checklist

Every audit must explicitly address all 10 points. Points not checked must be
listed in a "NOT CHECKED" section with a reason.

| # | Check | Governing skill | Pass condition (flows) |
|---|---|---|---|
| 1 | **Chain arithmetic** | chain-arithmetic-audit-SKILL.md | baseline + Σ(deltas) = final for every metric; no magic-number jumps |
| 2 | **Blast radius tagging** | blast-radius-tagger-SKILL.md | every deliverable tagged TEST-ONLY/GOVERNANCE/PRODUCTION/INTERFACE/DOCS; INTERFACE has a rollback/all-caller plan |
| 3 | **API shape verification** | api-shape-verification-SKILL.md | every `I*` import has a verified signature; domain returns are `DataProcessResult<T>` |
| 4 | **Commit atomicity** | blast-radius-tagger-SKILL.md | GOVERNANCE + PRODUCTION committed before TEST-ONLY; DOCS in the same commit as code; no orphaned partial commits |
| 5 | **Write-time fix verifiability** | — | every write-time fix has a specific `grep` / `dotnet test` / `dotnet build` command that confirms it landed |
| 6 | **Path verification** | — | every path verified against the real per-flow layout (`*/Api`, `*/Application`, `*/Infrastructure`, `*/Tests`, `client/angular/src`) — not assumed from plan context |
| 7 | **Gate thresholds** | plan-review-SKILL.md | every pass/fail threshold is explicit — `dotnet test → 12 passed, 0 failed`, never "~approximately passes" |
| 8 | **Severity assignment** | — | every finding tagged BLOCKING / WRITE-TIME / INFORMATIONAL |
| 9 | **Severity consistency** | — | no finding tagged BLOCKING may resolve as "minor" in the same review |
| 10 | **Coverage declaration** | — | audit output ends with an explicit list of what was NOT checked and why |

### Review/Fix Pass Evidence Add-On

If the audit or work order requires at least N review/fix passes, the audit must
verify N distinct evidence packets. A Review-Fix Ledger is only an index, not the
evidence. Required per packet: reviewer/sub-agent id, artifact before/after refs,
loaded skills/gates, findings or no-fix justification, fix refs when needed, and
recheck evidence. If the audit sees N ledger rows but fewer than N valid packets,
the verdict is `IN_PROGRESS_NEXT_STEP_KNOWN`, not pass.

### Audit Sufficiency Boundary

An audit may claim only the scope it checked. "Phase-N scope completed" is allowed
with an explicit bounded scope; "audit sufficient/complete/ready/closed" is
forbidden unless a breadth-challenge pass is green. After focused findings, ask and
answer: **"What would a broader audit likely find that this artifact missed?"** Name
the likely-missed zones, whether they were checked, and which claims remain
explicitly out-of-scope.

---

## Section 2 — Severity Definitions

| Tag | Meaning | Required action |
|---|---|---|
| **BLOCKING** | Execution cannot proceed without fixing this | Stop, fix, re-audit before any Phase 1 work |
| **WRITE-TIME** | Fix during execution, pre-approved | Document as a WF-N entry with a verification command |
| **INFORMATIONAL** | Noted, no action required | Log only, no gate impact |

### Severity Rules (non-negotiable)

1. Every finding has exactly ONE severity tag.
2. A finding tagged BLOCKING that resolves as "minor" is a protocol violation — reclassify or escalate.
3. No finding may downgrade from BLOCKING to WRITE-TIME without explicit approval.
4. A plan with any BLOCKING finding cannot proceed to Phase 1.

---

## Section 3 — Audit Output Format

```markdown
## Audit Report — [Plan Name or Session ID]
Date: [date]
Auditor: [session]
Plan version: [N]

### Section A — Findings

| # | Point | Status | Severity | Finding |
|---|---|---|---|---|
| 1 | Chain arithmetic | PASS | — | baseline 18 + 4 = 22; final declared 22 ✓ |
| 2 | Blast radius | PASS | — | all 5 deliverables tagged |
| 3 | API shape | FAIL | BLOCKING | IBidStore.PlaceBid() returns DataProcessResult<BidResult> in plan, interface declares Task<bool> — mismatch |
| 4 | Commit atomicity | PASS | — | GOVERNANCE commit before TEST-ONLY ✓; DOCS in code commit ✓ |
| 5 | WF verifiability | PASS | — | all 3 write-time fixes have grep/dotnet verification commands |
| 6 | Path verification | FAIL | WRITE-TIME | Xiigen.Flow.AdsPlatform.Application/BidStore.cs listed but not confirmed to exist |
| 7 | Gate thresholds | PASS | — | Gate B = "dotnet test → 22 passed, 0 failed" |
| 8 | Severity assignment | PASS | — | 1 BLOCKING, 1 WRITE-TIME, 0 INFORMATIONAL |
| 9 | Severity consistency | PASS | — | no downgrade instances |
| 10 | Coverage declaration | see below | — | |

### Section B — NOT CHECKED (with reason)

| Point | What was not checked | Reason |
|---|---|---|
| 3 (partial) | IBidStore injection test in Xiigen.Flow.AdsPlatform.Tests | Test file not written yet; cannot verify shape until it exists |

### Final Gate

BLOCKING findings: 1
→ Plan cannot proceed to Phase 1 until the IBidStore return-type mismatch (Point 3) is resolved.

WRITE-TIME findings: 1
→ WF-1: confirm Xiigen.Flow.AdsPlatform.Application/BidStore.cs exists before writing to it
   Verification command: ls flows/FLOW-20-ads-platform/server/Xiigen.Flow.AdsPlatform.Application/BidStore.cs

Verdict: NOT READY — 1 BLOCKING finding requires resolution
```

---

## Anti-Patterns

1. **"The audit found no issues"** — an audit with no findings either genuinely
   passed all 10 points or skipped some. Declare what was checked.
2. **"This issue is minor but I tagged it BLOCKING"** — if minor, tag INFORMATIONAL.
   BLOCKING means execution stops. Do not inflate severity.
3. **"The NOT-CHECKED section is embarrassing so I skip it"** — an audit without it
   implies all 10 points were checked. Declare omissions.
4. **"Gate thresholds don't need counts — 'tests pass' is clear enough"** — Point 7
   requires exact counts. "Tests pass" leaves "which tests?" and "how many?"
   unanswered. A Gate B that a third party cannot verify is not a Gate B.
5. **"BLOCKING finding resolved itself after I re-read"** — a BLOCKING finding
   retracted without a specific code change is a consistency violation. Either it was
   wrong (downgrade to INFORMATIONAL with a reason) or it is still valid.

---

## Integration

- `plan-review-SKILL.md` invokes this audit protocol at its review gate.
- `chain-arithmetic-audit-SKILL.md` governs Point 1.
- `blast-radius-tagger-SKILL.md` / `blast-radius-SKILL.md` govern Points 2 and 4.
- `api-shape-verification-SKILL.md` governs Point 3.
- `iron-rule-derivation-SKILL.md` and `archetype-failure-map-SKILL.md` supply the
  Gate-B thresholds that Point 7 verifies.
