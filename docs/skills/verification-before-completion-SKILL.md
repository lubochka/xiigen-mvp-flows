# verification-before-completion-SKILL.md — Evidence before completion claims

> **Stack adaptation (xiigen flows).** Universal verification gates ported from
> `llm_mvp_core/docs/skills/verification-before-completion-SKILL.md`. Adapted to this repo:
> - **Gate-B** = per-flow `dotnet build` + `dotnet test` (xUnit), plus Angular 21
>   `ng test` / `npx playwright test` for client branches. Verified per-flow shape:
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   (e.g. `FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.{Api,Application,Infrastructure,Tests}`);
> - the domain return type on `Application` public methods is `DataProcessResult<T>`
>   (`IsSuccess`/`ErrorCode`); services inherit `MicroserviceBase<TSelf>` and return
>   `Success<T>(...)` / `Failure<T>("CODE", "message", details)`. This is the XIIGen family
>   wrapper — NOT core `OperationResult<T>` (which stays in `llm_mvp_core` and must not leak here);
> - target framework is `net10.0` (SDK 10.0.300); `TreatWarningsAsErrors=true`, so a build
>   warning is a build failure;
> - core algorithm / trainable / ML / DPO / `ILearningMachine` logic stays in `llm_mvp_core`;
>   this repo consumes it through `.xiigen` / wiki-domain manifests and locators only. The
>   ML Runtime Evidence Firewall and Full-Extension (Claude/Cursor/Codex host) gates from core
>   are **not run here** (G12 boundary); they are referenced only so a flows report never
>   borrows their wording to overclaim.

## When to load
Load before any completion/`done`/`ready` claim, before any commit/push, and at every STOP
that contains a status or result claim.

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
NO COUNT CLAIMS WITHOUT COUNT-MATCHING EVIDENCE
NO SUB-AGENT CLAIMS WITHOUT A NAMED SUB-AGENT PACKET
```

Claiming work is complete without verification is dishonesty, not efficiency. Evidence before
claims, always. Violating the letter of this rule is violating the spirit of this rule.

If you have not run the verification allowed by the current instruction or by the active phase
Gate B, you cannot claim it passes. This skill does not authorize a full-solution
`dotnet test Xiigen.Flows.slnx` by itself: when the active phase is documentation, contract,
schema, or planning work, use the exact verification the active plan names (JSON parse, schema
check, `rg` check, targeted per-flow build, targeted per-flow test count, or git/status
reconciliation). Examples that mention `dotnet test` are for phases whose Gate B requires tests.

## The Gate Function

```
BEFORE claiming any status or expressing satisfaction:

1. IDENTIFY: What command proves this claim?
2. RUN: Execute the FULL command (fresh, complete)
3. READ: Full output, check exit code, count failures
4. VERIFY: Does output confirm the claim?
   - If NO: State actual status with evidence
   - If YES: State claim WITH evidence
5. ONLY THEN: Make the claim

Skip any step = lying, not verifying
```

Count/actor extension:

```text
6. COUNT: If claiming N cycles/reviews/passes, show N independent evidence records of that
   exact type. Ledger/checklist rows are not cycles.
7. ACTOR: If claiming sub-agent work, show sub-agent id/nickname, work order, returned packet,
   reviewed scope, and parent acceptance/rejection. Otherwise say the parent did it, or say
   actor not proven.
```

## Stack commands (xiigen flows)

| Claim | Command | Passing evidence |
|-------|---------|-----------------|
| Flow Api/Application/Infrastructure build | `dotnet build flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Flow tests pass | `dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests` | `Passed! - Failed: 0` |
| Whole-repo build | `dotnet build Xiigen.Flows.slnx` | `0 Error(s)` |
| Angular client unit tests | `cd flows/FLOW-NN-<slug>/client/angular && ng test --watch=false` | `Executed N of N SUCCESS` |
| Angular client e2e | `cd flows/FLOW-NN-<slug>/client/angular && npx playwright test` | `N passed (0 failed)` |
| Domain wrapper correct | `rg "DataProcessResult<" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application` | public Application methods return `DataProcessResult<T>` |
| No core wrapper leak | `rg "OperationResult<" flows/FLOW-NN-<slug>` | 0 matches |
| Service base correct | `rg "MicroserviceBase<" flows/FLOW-NN-<slug>/server` | service inherits `MicroserviceBase<TSelf>` |

## Common failures

| Claim | Requires | Not sufficient |
|-------|----------|----------------|
| Tests pass | `dotnet test` output: `Failed: 0` | Previous run, "should pass" |
| Build clean | `dotnet build` output: `0 Warning(s) 0 Error(s)` (warnings-as-errors) | Linter passing, looks good |
| Bug fixed | Re-run the test exercising the original symptom: passes | Code changed, assumed fixed |
| Regression test works | Red-green cycle verified | Test passes once |
| Phase complete | Gate B condition met with count/assertion | "Tests passing" |
| DataProcessResult correct | Return type verified in file | "I think I used it" |
| Client works | `ng test` / Playwright output | `ng build` succeeded (build ≠ behavior) |

## Red flags — STOP

- Using "should", "probably", "seems to".
- Expressing satisfaction before verification ("Great!", "Perfect!", "Done!").
- About to commit without running the allowed Gate B verification.
- Relying on a build run from minutes ago.
- "I added comments / handled errors everywhere" — `rg` first, then claim.
- A required verification printed failure output before commit/push; that is a hard fail even
  if a later `git commit` or `git push` succeeds.
- A command chain can continue into stage/commit/push after failed verification. Run each
  verification (`git diff --check`, `git diff --cached --check`, JSON parse, schema check,
  build, test) as its own step, inspect the exit code, and stop before any commit/push on
  nonzero. Never put a required verification and `git commit`/`git push` in one
  `;`-separated PowerShell line.
- A commit group stages by broad `glob` alone instead of exact `required_commit_group` paths
  from the reviewer/validation ledger.

Required verification literals: `commit is forbidden if a pre-commit gate prints
warnings/errors`; `do not chain commit/push after a gate command unless exit code is checked`;
`chained_precommit_check_bypass`.

## Gate B format

Every placement states its Gate B before writing code. A Gate B is a command with a measurable
output, not a vague claim.

```
Gate B: dotnet test flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Tests --filter "FullyQualifiedName~Registration" shows Failed: 0
Gate B: rg -c "OperationResult<" flows/FLOW-01-user-registration returns 0
Gate B: ng test --watch=false shows Executed 12 of 12 SUCCESS
```

"Works correctly" is not a Gate B.

## Evidence Firewall (flows scope)

For any capability/behaviour claim, `passed: true` in a JSON/report file is not enough. The
verification must prove the same kind of thing as the claim. Reject or downgrade a completion
claim when the supporting artifact contains:

```text
validation_only: true
metric_type contains static, contract, or mixed
required numeric field = UNKNOWN
build/static-contract check used as proof of runtime/behaviour
report cannot be reproduced by the current runner/generator code
manifest/`.xiigen` file presence used as proof that the unit loads
```

Required downgrades:
- contract/static evidence proves only contract/static readiness;
- build evidence proves only build readiness;
- validation-only evidence proves only schema coverage;
- a `.xiigen`/manifest file's presence proves only file creation — the boundary proof for a
  manifest/wiki-domain claim is its **load through the locator**, not its existence.

Fresh evidence must be reproducible from the current runner/generator. If a persisted report
says a set/flow/metric passed but the current code path would now produce a different or
narrower result, the report is stale: mark the completion claim failed, record the gap, and
regenerate from the current code path.

> **G12 note.** The core ML Runtime Evidence Firewall (DPO/training/checkpoint/numeric held-out
> metrics, Dual-Path Host Verification for Claude/Cursor/Codex) is **not run here**. Core models
> live in `llm_mvp_core`. A flows report must never adopt that ML/host wording to imply trained
> capability or live-host coverage that this repo does not own.

## Completion Coverage Matrix

For a multi-step plan, fresh verification also means proving the command covers every literal
requirement in the active plan. Before any final completion claim require a matrix:

```text
requirement_id | literal_requirement | expected_literal_set | actual_artifact |
actual_value | comparison_operator | passed | gap | next_plan_action
```

Reject completion when:
- a lower-level report says `passed: true` but the matrix lacks the plan's full literal set;
- a top-level bucket count is used instead of the required underlying items;
- "at least one" is used where the plan says "all", "every", or lists exact items
  (e.g. "all N flows built" needs a per-flow `dotnet build`/`dotnet test` row for each flow);
- the final readiness report omits required source-of-truth reports;
- state says `goal_reached: true` while a matrix row still has a gap;
- state/matrix/self-review name an old commit or stale pending next action after commit/push.

If any matrix row fails, the correct result is not a final report. Set state to
`IN_PROGRESS_NEXT_STEP_KNOWN`, copy the first failed row into `next_action`, and continue.

## Post-completion challenge rule

When the challenge is to a previous completion claim, treat it as a fresh verification attempt:
reset the new-attempt score to 0, re-read the plan/state/matrix, re-verify from live sources,
and do not defend the old report from memory. `passed: true` is not accepted when the report
cannot be reproduced from the current runner code.

## Why this matters

- Zero failures is a hard gate — not "green except pre-existing".
- Every placement has a metric measurable before and after.
- Gate B is a comparison/count/assertion, not a vague claim.
- Committing with failures wastes reviewer time and masks regressions.
