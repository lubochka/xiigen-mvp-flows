# phase-completion-packager-SKILL.md — What to package when a phase gate passes

> **Stack adaptation (xiigen flows).** Universal packaging gates ported from
> `llm_mvp_core/docs/skills/phase-completion-packager-SKILL.md`. Adapted to this repo:
> - Gate Check = per-flow `dotnet build` + `dotnet test` (xUnit, `net10.0`,
>   `TreatWarningsAsErrors=true`), plus Angular 21 `ng build`/`ng test`/`npx playwright test`
>   for clients; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - STATE / PHASE-COMPLETE / handoff artifacts are JSON/MD in this repo's `docs/`;
> - domain wrapper is `DataProcessResult<T>` via `MicroserviceBase<TSelf>` — NOT core
>   `OperationResult<T>`;
> - **the Trainable-phase / two-PASS / ML Runtime Evidence Firewall gates and the Full-Extension
>   (Claude/Cursor/Codex host, Host Session Auth, No-Blind-Spot UI scenario) gates are core-only
>   (G12) and are NOT run here.** Core models live in `llm_mvp_core`; flows consume them via
>   `.xiigen`/wiki-domain manifests and locators. They are referenced only so a flows package
>   never borrows that wording to overclaim.

## When to load
After every phase gate passes — specifically after `dotnet build` -> 0 Error(s), the phase
Gate B passes, all phase-specific checks pass, and (for clients) `ng test`/Playwright pass.
Never invoke if any gate check fails.

## Continuous-Plan exception

Some Luba-directed plans explicitly forbid stopping between internal roles or phase boundaries
while the full goal remains open. In those plans the package is a checkpoint, not a stop gate.
Before writing any STOP line, check the active plan and state:

```text
goal_reached == true
next_action is empty
completion coverage matrix exists when the plan requires it
completion coverage matrix has zero gaps
Luba did not ask to continue / do not stop
```

If any condition fails, do not write STOP. Write `IN_PROGRESS_NEXT_STEP_KNOWN`, update state
with the next plan-derived action, and continue. A governance/skill repair is not permission to
stop while the requested outcome is open and the next action is known.

## Gate Check (xiigen flows)

```bash
# Build (always)
dotnet build Xiigen.Flows.slnx 2>&1 | tail -3            # 0 Error(s)

# Per-flow tests
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --no-build 2>&1 | tail -5
# Assert: Failed: 0 ; in a multi-flow change, assert this flow's delta (do not assert an
# unrelated repo-wide absolute).

# Client (when present)
cd flows/FLOW-NN-<slug>/client/angular && ng test --watch=false   # Executed N of N SUCCESS
cd flows/FLOW-NN-<slug>/client/angular && npx playwright test     # N passed (0 failed)
```

## Implementation Reality Gate — REQUIRED before status=DONE for code phases

Before writing `status: DONE` for any phase that claims real code, require
`implementation-integrity-SKILL.md` Check 5 PASS. The package must include `implementationReality`
with: actual code path file:line refs from Api entrypoint to Application service to
Infrastructure store; artifact-class match proving JSON/PowerShell/reports are not substitutes;
anti-stub sweep for TODO/FIXME/PLACEHOLDER/NotImplementedException/`Failure<T>("NOT_IMPLEMENTED")`
no-op paths; loaded skills/guides; and behaviour evidence (xUnit test on the real service with a
Fail path; `ng test`/Playwright for client behaviour). If missing, do not write DONE — write
`IN_PROGRESS_NEXT_STEP_KNOWN` with `next_action` = first missing row.

## Review/fix cycle gate

When Luba or the active plan requires several/multiple/repeated review/fix cycles, completion
requires the exact requested floor. Project default is two independent review passes only when
Luba did not name a higher number. If Luba says "minimum 4" / "at least 4", the floor is four.

```text
pre_commit_push_review_floor = N
code_review_cycle_1..N = PASS | ZERO_FINDINGS, with fixes/recheck evidence
unfixed_findings_open = false
documentation_drift_open = false
```

### Review/fix evidence packaging rule

```text
requested_review_fix_floor = N
real_pass_count = <valid packets only>
evidence_packet_count = <inspectable packet refs>
ledger_row_count = <summary rows>
ledger_rows_are_evidence = false
```

Each counted packet must include reviewer/sub-agent id, artifact before/after refs, loaded
skills/gates, findings, fix refs or no-fix justification, and recheck evidence. If
`real_pass_count`, `evidence_packet_count`, and `requested_review_fix_floor` do not match, or
the package relies on `ledger_row_count` to close the floor, write `IN_PROGRESS_NEXT_STEP_KNOWN`
with the next missing pass as the next action.

Commit/push packaging must also prove verification did not fail before the git boundary. Any
printed failure from `git diff --check`, `git diff --cached --check`, tests, JSON/JSONL parse,
schema checks, or builds keeps the package failed even if a later commit command succeeds. Never
chain `git diff --cached --check; git commit; git push` in one `;` sequence; split or guard,
verify the precheck exit code, and treat a failed precheck as a fix loop.

Required verification literals: `commit is forbidden if a pre-commit gate prints
warnings/errors`; `do not chain commit/push after a gate command unless exit code is checked`.

Commit group packaging must prove exact staging:

```text
required_commit_group = [exact reviewer/validation ledger artifact paths]
glob_used_only_as_supplement = true
staged_files_command = git diff --cached --name-only
required_commit_group_missing_from_staged = []
```

If any required path is absent from `git diff --cached --name-only`, the package hard-fails
before commit. A follow-up commit for a missed required validation artifact is a commit-boundary
defect to repair, not a normal second commit.

## Target Outcome Completion Packaging Rule

When Luba names a target outcome, the package must close that exact outcome, not a narrower
honest packet produced during execution.

```text
literal_luba_target_outcome = <exact current quote>
observable_end_state = <product/runtime state that must be true>
final_evidence_proves_target_outcome = true/false
not_proven_target_items = [...]
not_proven_items_have_current_luba_scope_narrowing = true/false
review_floor_target_matches_luba_outcome = true/false
```

Completion hard-fails `DONE`/`complete`/`ready`/`TASK_FULLY_COMPLETE` when final evidence leaves
any target-outcome row NOT_PROVEN unless Luba's current message explicitly narrows scope after
seeing the artifact. "NOT_PROVEN and not claimed" is truthful wording, but it is not PASS when
the binding requirement was to prove/deliver that outcome; then write:

```text
status = IN_PROGRESS_NEXT_STEP_KNOWN
false_completion_loophole = target_outcome_replaced_by_honest_narrow_packet
next_action = repair_plan_to_cover_target_outcome_then_rerun_required_reviews
```

If `actual_review_target` is a narrower packet, N reviews of it do not satisfy N reviews of plan
sufficiency. Repair the review target and rerun the floor against Luba's named outcome.

## Human-Readable Plan/Execution Log Gate

Every completion/phase packet must include a human-readable section for Luba before machine
evidence:

```text
stage:
total_major_plan_blocks_or_current_phase_tasks:
completed_count_and_names:
current_plan_step:
current_active_task_and_owner:
remaining_count_and_names:
current_result_type: code | artifact | review | policy | evidence-only | commit boundary
what_changed:
what_remains:
claims_opened:
claims_closed:
claims_still_closed_and_why:
why_this_status_is_true:
next_required_action:
```

Machine-only JSON/state/ledger rows cannot be the primary status. Binding human-first template:

```text
Фаза N/M: <человеческое имя фазы>
Что решает фаза: <одно простое русское предложение>
Шаг NN.MM/NN.Total: <человеческое имя шага>
Что решает шаг: <одно простое русское предложение>
Сейчас: <человеческий перевод DONE/IN_PROGRESS/NEEDS_FIX/REMAINING/BLOCKED; технический label только в скобках>
Что найдено: <что review/status проверка увидела, или "пока не проверялось">
Что исправлено: <что fix изменил, или "пока ничего не менялось">
Что осталось: <оставшаяся работа и boundary простыми словами>
Счетчики фазы: сделано <X>/<Y>, осталось <Y-X>/<Y>
Счетчики всего плана: сделано <A>/<B>, осталось <B-A>/<B>
```

`Фаза N/M, шаг NN.MM/NN.Total, статус X` alone is a packaging failure when the packet is for
Luba. Technical labels (STATE flags, JSON paths, validator names, `NEEDS_FIX`, `DONE`, `PASS`)
must be translated in the same update into what review found, what fix did, and what remains.

## Sequential Execution-Control Gate

Completion/phase packages for multi-phase plans include `current_execution_pointer` separately
from artifact targeting, plus an `ordered_phase_queue` whose row ids use exact
`phase.step.status` form. Allowed execution-control statuses are exactly:

```text
done | in-progress | remaining | review-block | blocked
```

Mark only the first unfinished row as the current executable target. Fail packaging when a later
phase is active while an earlier row is `remaining`, `review-block`, or `blocked`. Required
fields: total phases; total substeps; counts per status; completed list; current phase/step;
remaining list; remaining action count + names; one human sentence per phase; human-readable
`requirement -> phase -> evidence` matrix. Counts must equal `ordered_phase_queue` row statuses.

## Procedure — three artifacts in order

**Artifact 1 — STATE-Pn.json (machine-readable):**

```json
{
  "phase": "P3",
  "phaseName": "FLOW-01 idempotent registration + Api Fail mapping",
  "flow": "FLOW-01-user-registration",
  "status": "DONE",
  "completedAt": "2026-06-28",
  "gateResults": {
    "build": "PASS — 0 Error(s)",
    "testGate": "PASS — 8 passed (+8 from 0), 0 failed",
    "client": "N/A | PASS — ng test 12/12, playwright 4 passed"
  },
  "implementationReality": "PASS — Api -> UserRegistrationService -> IDocumentStore path + conflict Fail-path test",
  "filesChanged": [
    "flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs",
    "flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Tests/RegistrationTests.cs"
  ]
}
```

**Artifact 2 — PHASE-COMPLETE-Pn.md (human-readable, for Luba):** "What Was Built" is prose
(not a file list) explaining the meaning of the work; the Gate Results table is exact; the last
line is STOP only when the active plan permits a stop — in continuous plans it is
`IN_PROGRESS_NEXT_STEP_KNOWN` with the next phase.

**Artifact 3 — Session handoff note:** branch (current working branch, not main unless Luba
asked), last commit hash + subject, test baseline at end, next phase entry point (file:line),
open items only if a Luba decision is needed.

## Anti-patterns

1. Writing the package before the gate passes — gate first, always.
2. Stopping on a checkpoint in a continuous plan — package and keep going while the full goal
   remains open.
3. "What Was Built" as a file list — prose that explains meaning.
4. Marking a code phase DONE from JSON/PowerShell/static evidence — the Implementation Reality
   Gate must pass first (real Api→Application→Infrastructure path + Fail-path test).
5. Collapsing several review/fix cycles into one summary review — the exact requested floor
   applies.
6. **Final between bounded slices** — a final-style summary or commit/push checkpoint is allowed
   to close the workflow only when Luba explicitly stopped/paused/asked answer-only or all active
   requirements are complete. If `remaining_count > 0` and `next_bounded_action_exists=true`, do
   not stop; start/wait/review/repair the next bounded action. A promise to continue ("I will
   continue", "следующий шаг") is not continuation unless the next bounded sub-agent was actually
   started or a real blocker exists.
7. Borrowing core ML/host wording (trained, DPO, checkpoint, live-host, two-PASS) to imply
   capability this repo does not own — G12 boundary violation.

> **G12 note.** Core's Trainable-phase gate, two-PASS gate (`PASS_AS_HONEST_UNTRAINED_SCAFFOLD`
> vs `PASS_AS_TRAINED_FEATURE`), and 10-cycle Full-Extension Execution Review Gate are for
> `llm_mvp_core` trainable units and Claude/Cursor/Codex hosts. They are not packaged here.
