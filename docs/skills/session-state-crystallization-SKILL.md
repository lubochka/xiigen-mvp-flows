# Session State Crystallization

> **Stack adaptation (xiigen flows).** The ≤40-line snapshot discipline ported
> from `llm_mvp_core/docs/skills/session-state-crystallization-SKILL.md` (G07).
> The universal part (a hard ≤40-line cap; the snapshot writes only what CHANGED;
> the fields EXECUTION POSITION / BLOCKING PREREQUISITES with a literal verify
> command / ACTIVE BUGS / ARTIFACT BOUNDARIES / per-flow STATUS / DECISIONS THIS
> SESSION) is taken verbatim from core. Adapted here:
> - **Test count** = `dotnet test Xiigen.Flows.slnx --no-build` (or the affected
>   per-flow `Xiigen.Flow.<Name>.Tests`, xUnit) and `ng test` for client-bearing
>   flows — not `dotnet test` on a monolith and not a memorized number.
> - **SUBPROJECT STATUS → per-FLOW STATUS**: `FLOW-NN` → `COMPLETE` / `ACTIVE` /
>   `PENDING` / `BLOCKED` with a one-line reason if not COMPLETE.
> - The snapshot is kept per-flow next to `flows/FLOW-NN-<slug>/flow.manifest.json`,
>   or under `docs/session-files/`.
> - Domain return type cited in the snapshot is `DataProcessResult<T>`
>   (`IsSuccess`/`ErrorCode`/`Data`, from `Xiigen.Kernel`) — **not** core
>   `OperationResult<T>`.
> - `Last commit` is read live; branch is the current working branch
>   (`UpdateUniversalSkills` / `DesignToCode` / `claude/<worktree>`), never
>   `main`/`master`.

## When to Invoke

At the end of every planning, investigation, or analysis session — before reporting complete.
The snapshot is the LAST deliverable of every session.

Code sessions produce a commit and a per-flow STATE-JSON update instead —
the crystallization step is the STATE-JSON write.

---

## The ≤40-Line Rule

The snapshot must be ≤40 lines. Hard cap, not a guideline.

If you cannot fit session state in 40 lines:
1. You are repeating information already in a prior snapshot
2. Write only what CHANGED since the last snapshot
3. Cut anything derivable from a command (test count from `dotnet test`, not memorized)

A 200-line snapshot is a documentation replacement, not a snapshot.
The snapshot must stay lean enough that the next session reads it in 30 seconds.

---

## Snapshot Format

File: `docs/SESSION-STATE-SNAPSHOT--[YYYY-MM-DD].md` (or per-flow next to `flow.manifest.json`)
Replaces: any prior snapshot (only the latest is active)

```markdown
# SESSION-STATE-SNAPSHOT — [YYYY-MM-DD]
# Session type: [PLANNING | INVESTIGATION | CODE | ANALYSIS]
# Produced by: [brief session description]

## EXECUTION POSITION
Next work:    [one sentence — the next executable action]
Can start:    [YES | NO — if NO, list what is blocking]

## BLOCKING PREREQUISITES
[For each blocker: description + verify command + expected result]
Example:
  ICheckoutService.SettleAsync must be merged first
  Verify: grep -n "SettleAsync" flows/FLOW-16-marketplace-payments/server/.../ICheckoutService.cs
  Expected: line present

## ACTIVE BUGS
[BUG-NN | one-line description | status: OPEN/FIXED/DEFERRED]
Example:
  FLOW16-BUG-01 | settlement double-charge on retry | FIXED — idempotency key added
  FLOW16-BUG-02 | webhook signature mismatch on edge | OPEN

## ARTIFACT BOUNDARIES
Next flow/phase:  [FLOW-NN — next in sequence]
Test count:       [N — run: dotnet test Xiigen.Flows.slnx --no-build 2>&1 | tail -3]
Last commit:      [hash] [message first line]

## PER-FLOW STATUS
[FLOW-NN | status: COMPLETE/ACTIVE/PENDING/BLOCKED | one-line reason if not COMPLETE]
Example:
  FLOW-01-user-registration | COMPLETE | OnboardingDeliveryService, 24 tests passing
  FLOW-16-marketplace-payments | ACTIVE | settlement integration tests pending
  FLOW-19-durable-sagas-compliance | BLOCKED | waiting for FLOW-16 to complete

## DECISIONS MADE THIS SESSION
[one sentence per decision — only decisions made IN THIS SESSION]
Example:
  D-2026-06-28-1: FLOW-16 settlement uses an idempotency key per checkout id
  D-2026-06-28-2: per-flow STATE-JSON serializes next to flow.manifest.json for human readability

## SKILLS LOADED
Skills consulted this session: [list]
```

---

## How to Produce Each Field

### EXECUTION POSITION
From: the last explicitly decided "what to do next" in this session.
Not from memory — state what was decided in this session only.
If not decided: write "UNKNOWN — determine at next session start."

### BLOCKING PREREQUISITES
Only unmet prerequisites. Copy the verification command literally.
A paraphrased command cannot be run at the next session start.

### ACTIVE BUGS
From: the per-flow bug log or the current session's bug discoveries.
FIXED = fixed in this session (include the fix method).
DEFERRED = explicit decision to defer (include the reason).
OPEN = unresolved.

### ARTIFACT BOUNDARIES
Run fresh at the end of the session — never from memory:
```bash
dotnet test Xiigen.Flows.slnx --no-build 2>&1 | tail -3
git log --oneline -1
# client-bearing flows additionally:
# (cd flows/FLOW-NN-<slug>/client/angular && ng test --watch=false)
```

### PER-FLOW STATUS
From: what was explicitly verified in this session.
A flow is COMPLETE only when `dotnet test` (its `Xiigen.Flow.<Name>.Tests`) shows
all its tests passing, and `ng test` passes for a client-bearing flow.

### DECISIONS MADE THIS SESSION
ONLY decisions made in this session. Historical decisions go in the flow's
locked-decisions doc. Format: `D-[YYYY-MM-DD]-[N]: [one sentence]`

---

## The Gate

A session cannot claim complete without producing this snapshot.

Line count check before reporting done:
```bash
# Count lines in the snapshot
wc -l docs/SESSION-STATE-SNAPSHOT--$(date +%Y-%m-%d).md
# Expected: ≤ 40
# If > 40: rewrite with only what changed since last snapshot
```

The snapshot replaces:
- Manually maintained "current state" notes
- Hard-to-find information about session execution position

The snapshot does NOT replace:
- The per-flow STATE-JSON (code session phase state — different purpose)
- The flow's locked-decisions doc (full decision records with rationale)
- `flows/FLOW-NN-<slug>/contracts` (full interface/contract reference)

---

## Anti-Patterns

1. **Snapshot > 40 lines** — rewrite with only what changed
2. **Verification commands paraphrased** — copy literal commands; paraphrases cannot be run
3. **"No decisions made this session"** — every session makes at least one decision; if genuinely none, write "No architectural decisions — [session type] session"
4. **Snapshot produced after session claims complete** — the snapshot IS the last step; produce it before claiming done
5. **Copying per-flow status from the last snapshot without verifying** — status must come from what was actually checked in this session
