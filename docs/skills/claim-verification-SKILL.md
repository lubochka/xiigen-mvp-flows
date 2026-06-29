# claim-verification-SKILL.md — Every claim is a hypothesis until a command confirms it

> **Stack adaptation (xiigen flows).** Universal claim-verification gates ported from
> `llm_mvp_core/docs/skills/claim-verification-SKILL.md`. Adapted to this repo:
> - verification commands target `net10.0` per-flow projects and Angular 21 clients
>   (`dotnet build`/`dotnet test`/`rg` for server; `rg`/`ng test`/`npx playwright test` for
>   client); verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - domain return type is `DataProcessResult<T>` via `MicroserviceBase<TSelf>` — NOT core
>   `OperationResult<T>` (stays in `llm_mvp_core`, must not leak here);
> - manifest / `.xiigen` / wiki-domain claims are verified by **load through the locator**, not
>   by file presence;
> - core ML/DPO/`ILearningMachine` and Claude/Cursor/Codex host-extension claim rows are
>   core-only (G12) and not used here; core logic lives in `llm_mvp_core`.

## When to load
Load at plan-authoring time (classify every claim), at session start (verify ASSUMED claims),
and during execution (verify file refs in real files).

## Core principle
Every claim must be treated as a hypothesis until verified with a command. A claim without a
verification command is an assumption.

## Claim classification

| Class | Definition | What to do |
|-------|-----------|-----------|
| VERIFIED | Command cited that confirms it | Spot-check one data point |
| INFERRED | Reasonable deduction, reasoning shown but not run | Run the verification command |
| ASSUMED | Stated as fact, no command shown | Treat as hypothesis, verify before using |

## High-risk claims that must always be verified

| Claim type | Verification command |
|-----------|---------------------|
| "Method X exists in class Y" | `rg -n "public .*MethodName" flows/FLOW-NN-<slug>/server/.../ClassName.cs` |
| "Interface has method Z" | `rg -n "MethodName" flows/FLOW-NN-<slug>/server --glob "I*.cs"` |
| "Test count is N" | `dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests 2>&1 \| tail -3` (fresh run) |
| "File exists at path P" | `Test-Path "flows/FLOW-NN-<slug>/server/.../ClassName.cs"` (PowerShell) or Glob |
| "DI registration exists" | `rg -n "AddSingleton\|AddScoped\|AddTransient.*InterfaceName" flows/FLOW-NN-<slug>/server` |
| "Namespace is X.Y.Z" | `rg -n "^namespace" flows/FLOW-NN-<slug>/server/.../ClassName.cs \| head -1` |
| "Class implements interface I" | `rg -n "class .*: *IInterfaceName" flows/FLOW-NN-<slug>/server` |
| "Service uses the family wrapper" | `rg -n "DataProcessResult<\|MicroserviceBase<\|Success<\|Failure<" flows/FLOW-NN-<slug>/server/.../<Name>Service.cs` |
| "No core wrapper leak" | `rg -n "OperationResult<" flows/FLOW-NN-<slug>` -> expect 0 |
| "Angular component exists" | `rg -n "@Component" flows/FLOW-NN-<slug>/client/angular/src` |
| "Client tests pass" | `cd flows/FLOW-NN-<slug>/client/angular && ng test --watch=false` |
| "Client e2e passes" | `cd flows/FLOW-NN-<slug>/client/angular && npx playwright test` |
| "Build succeeds" | `dotnet build Xiigen.Flows.slnx 2>&1 \| tail -3` -> `0 Error(s)` |
| "Manifest/`.xiigen` capability is available" | Verify it loads through the locator/runtime, not that the file exists |
| "Full plan is complete" | Verify the completion coverage matrix maps every literal plan requirement to evidence with zero gaps |
| "N review/fix passes completed" / "at least N" | Verify `requested_pass_floor=N`, `real_pass_count=N`, `evidence_packet_count=N`, before/after refs per pass, reviewer/sub-agent id per pass, loaded skills/gates, findings or no-fix justification, recheck evidence. Reject ledger-only evidence. |
| "N cycles completed" / any exact count | Verify N independently inspectable records of the claimed type. A ledger/checklist/table/summary row counts as zero cycles unless it links to a real before/after pass with evidence and recheck. |
| "A sub-agent did it" | Verify sub-agent id/nickname, work order, returned packet, reviewed scope, changed/reviewed files, parent acceptance/rejection. Parent/self work cannot be described as sub-agent work. |
| "Sub-agent completed bounded pass" | Verify work-order scope, exact allowed files, `git_authority`, `commit_push_authorized`, `allowed_git_commands`, `forbidden_commands`, returned `git_commands_run`, `git_write_or_ref_commands_run`, `git_directives_emitted`. False if forbidden git used, out-of-scope files touched, required passes collapsed without parent approval, or git directives emitted without authority. |
| "State allows stopping" | Verify current state, not a future target row: if `next_action` is non-empty or actionable gaps remain, state must be `goal_reached=false`/`stop_allowed=false`. |
| "Commit/push finished the phase" | Verify state/matrix/self-review/readiness metadata agree with current HEAD and no longer name stale pending actions. |
| "Previous completion still stands after challenge" | Reset the new-attempt score, re-read state and matrix, re-verify from live sources. |

> **G12 note.** Core rows for ML runtime gates (`validation_only`, static/contract/mixed
> metrics, checkpoints, DPO), plugin-host orchestration, and Claude/Cursor/Codex
> Extension-Host invocation are **not used here** — those belong to `llm_mvp_core`. A flows
> claim must not import that wording to imply trained/host capability this repo does not own.

## Completion claim rejection rules

Reject or downgrade any completion claim whose supporting artifact contains:

```text
validation_only: true
metric_type contains static / contract / mixed
required numeric field = UNKNOWN
build/static-contract check used as runtime/behaviour proof
report cannot be reproduced by the current runner/generator code
manifest/`.xiigen` file presence used as proof that it loads
ledger rows used as evidence for completed cycles
parent/self review described as sub-agent review
count_claimed greater than independent_evidence_count
sub-agent claim without sub-agent id or returned packet
```

Correct downgrades:
- static/contract evidence -> "contract gate passed";
- validation-only evidence -> "validation artifact passed";
- build evidence -> "build passed";
- manifest presence -> "manifest file exists; load not proven";
- ledger/checklist evidence -> "ledger/checklist exists; cycles not proven";
- parent/self review evidence -> "parent/self reviewed; sub-agent review not proven".

Do not combine these into "done", "ready", "accepted", or `TASK_FULLY_COMPLETE`. A downgrade,
blocker, not-proven, or status-only artifact is evidence accounting, not completion: it creates
a next concrete work item unless all requirements are proven or Luba explicitly stops.

```text
evidence_accounting_done:
product_requirement_proven:
next_concrete_work_item:
```

## Completion coverage claims

Treat a completion report as a claim about the parent plan, not only about the report file:

```text
1. Extract every literal requirement from the active plan's final gate.
2. Extract exact sets the plan names: flow names, endpoint classes, scenario classes, contract
   files, source-of-truth reports, metric fields, state fields.
3. Compare actual artifacts to those sets.
4. Reject the claim if any row is missing, mismatched, validation-only, static-only, or
   narrower than the plan scope.
```

`passed: true` in a child report is a child-report claim. It is not a parent-plan claim until
the matrix proves the child report covers the parent requirement. If a prior completion claim
is challenged, treat it as a fresh attempt: show the previous score, reset to 0, add discovered
holes to state, continue from the first hole.

## Renumbering / count claims

When a plan specifies a count/ordinal (next test count, next flow number, next decision id),
verify against current file state — the file may have changed since the last session:

```
Claim: "The next flow is FLOW-30"
Verification: ls flows/ | rg "FLOW-[0-9]+" | sort | tail -1  -> highest existing; next = max + 1
```

## Anti-patterns

1. Accepting test counts from session summaries — run `dotnet test` fresh; counts drift.
2. Accepting interface signatures from memory — `rg` the file; signatures change.
3. "It should be there, I added it last session" — verify.
4. Verification command but wrong pattern — anchor it: `"public .*Search"` not just `"Search"`.
5. Verifying existence but not shape — file exists + method name exists ≠ method has the
   expected signature / return type.
