# implementation-integrity-SKILL.md — Fix the instance AND add the structural guard

> **Stack adaptation (xiigen flows).** Universal integrity checks ported from
> `llm_mvp_core/docs/skills/implementation-integrity-SKILL.md`. Adapted to this repo:
> - per-flow boundary `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   (verified shape: `FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.{Api,Application,Infrastructure,Tests}`);
> - Gate-B = per-flow `dotnet build` + `dotnet test` (xUnit) on `net10.0` with
>   `TreatWarningsAsErrors=true`; Angular 21 `ng test` / `npx playwright test` for clients;
> - domain return type = `DataProcessResult<T>` via `MicroserviceBase<TSelf>`
>   (`Success<T>`/`Failure<T>`) — NOT core `OperationResult<T>` (stays in `llm_mvp_core`);
> - project boundary = real `<ProjectReference>` to the flow's actual projects, not
>   `Compile Include`/`Link` of production source;
> - doc-sync target is this repo's `docs/` (and per-flow `flows/FLOW-NN-<slug>/docs/`) plus
>   the flow's contract files under `flows/FLOW-NN-<slug>/contracts/`;
> - **Check 9 (Allowed Trainable Architecture / ILearningMachine levels), the ML/DPO anti-stub
>   metrics, and the Full-Extension Claude/Cursor/Codex host gate are core-only (G12) and are
>   NOT run here.** Core trainable logic lives in `llm_mvp_core`; flows consume it via
>   `.xiigen`/wiki-domain manifests and locators. They are noted only so a flows report never
>   borrows ML/host wording to overclaim.

## When to load
Load after writing or changing any flow code, contract, or client, before claiming the slice
complete.

## The Golden Rule

**Fix the instance AND add the structural guard that prevents regression.** A session that adds
a new service/interface but does not update the contract files, the docs, and the tests has
fixed the instance without adding the guard. The same gap reappears next session.

## Check 1 — Documentation and contracts updated

After adding or changing any interface, service, endpoint, or contract:

```bash
# The flow's contract reflects the new endpoint/schema
rg -n "NewEndpoint|NewSchema" flows/FLOW-NN-<slug>/contracts
# Repo docs reflect the new capability/status
rg -n "FLOW-NN|<Name>Service" docs
```

Expected: contract/schema block present; docs row reflects current status.

## Check 2 — Tests cover the new pattern

```bash
# Test file present for the new class
rg -n "class .*<Name>.*Tests|<Name>Tests" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests
# Tests run and pass
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --filter "<Name>"
```

Expected: test file present; `Failed: 0`, `Passed: > 0`. For client UI changes, add the
matching `ng test` spec and a Playwright e2e for user-facing flows.

## Check 3 — Project rules applied to the new code

```bash
# No raw throws on the business path — return Failure<T>("CODE", "...") via MicroserviceBase
rg -n "throw new" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<Name>Service.cs
# Domain methods return the family wrapper, not the core one
rg -n "DataProcessResult<" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application
rg -n "OperationResult<" flows/FLOW-NN-<slug>   # expected: 0 matches
```

Expected: business-logic failures flow through `Failure<T>`, not exceptions; public Application
methods return `DataProcessResult<T>`; zero `OperationResult<T>` leaks.

## Check 4 — No new gaps introduced

```bash
dotnet build Xiigen.Flows.slnx 2>&1 | tail -3       # 0 Error(s) (warnings are errors)
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests 2>&1 | tail -5
```

Expected: build clean; test baseline unchanged or improved for the touched flow. In a
multi-flow change, count the per-flow delta; do not assert an unrelated repo-wide absolute.

## Check 5 — Implementation reality / Anti-Stub gate

After any implementation the plan calls real/complete, prove the delivered code is not only a
stub/scaffold/report bundle.

```text
1. Actual code path:
   - Api endpoint -> Application service (MicroserviceBase) -> Infrastructure store/port
   - file:line refs for the controller/minimal-API entrypoint, the called Application method,
     and the Infrastructure read/write it reaches.

2. Not a JSON/report substitute:
   - reports may be evidence, but the slice artifact must include the production code the plan
     asked for; JSON/PowerShell/gate output alone is static evidence, not implementation.

3. Not a stub:
   - no TODO, FIXME, PLACEHOLDER, NotImplementedException, fixed/no-op return, or
     `Failure<T>("NOT_IMPLEMENTED", ...)` left on the claimed-done path.

4. Behaviour evidence when behaviour is claimed:
   - the matching xUnit test exercises the real Application service (not a mock of it) and
     asserts the DataProcessResult outcome, including a Fail path;
   - for client behaviour, an `ng test`/Playwright assertion on the rendered result.
```

If any row is missing, integrity is NOT confirmed: state is `IN_PROGRESS_NEXT_STEP_KNOWN` with
`next_action` set to the first missing reality-evidence row.

> **G12 note.** Numeric held-out ML metrics (accuracy/loss/reward, cpu_ms, ram_mb) and the
> "unit changes decision after training" probe are core-only and not required here, because no
> trainable model is owned by this repo. Do not invent them; do not claim them.

## Check 6 — Real project boundary, not test-only source linking

```text
1. The flow's Api/Application/Infrastructure/Tests `.csproj` files exist in the flow folder.
2. The Tests project references the real projects through <ProjectReference>.
3. The Tests project does NOT link production source via `Compile Include`/`Link` as the main
   implementation path.
4. A per-flow `dotnet build` runs against the real projects.
5. The state/report says what the evidence proves and what it does not prove.
```

After editing any state/report JSON, rerun a JSON parse and `git diff --check` for the changed
files before claiming the gate passed. Any printed failure from `git diff --check`, tests,
JSON/JSONL parse, schema checks, or builds before commit/push keeps integrity failed even if a
later commit/push succeeds. Never chain `git diff --cached --check; git commit; git push` in
one `;` sequence; split or guard, verify the precheck exit code, and treat a failed precheck as
a fix-loop trigger.

Required verification literals: `commit is forbidden if a pre-commit gate prints
warnings/errors`; `do not chain commit/push after a gate command unless exit code is checked`.

## Check 7/8 — UI/UX integrity (Angular 21 clients)

When a flow ships a user-facing Angular client and the plan asked for UI/QA/UX evidence,
integrity is not confirmed by click-path automation alone:

```text
1. Human-facing labels: actions, inputs, result, errors have readable labels.
2. Accessibility metadata: primary controls expose accessible name / aria-* / help text.
3. Guidance and safety: examples say what to enter and expect; errors are visible/understandable.
4. Layout and focus: primary panels have stable bounds; keyboard/focus path is explicit enough
   to automate (Playwright).
5. No primary-path jargon: internal status keys may appear in debug views only.
```

Missing rows keep the slice at `IN_PROGRESS_NEXT_STEP_KNOWN`. (For flows without a client, this
check is `na`.)

## Check 10 — Bounded sub-agent scope integrity

When implementation/validation evidence comes from a sub-agent, verify it stayed inside the
exact work order before accepting evidence:

```text
work_order_scope | exact_allowed_files | actual_changed_files |
implementation_authorized_for_this_slice | commit_push_authorized_for_this_exact_file_set |
allowed_git_commands | forbidden_commands | parent_will_perform_commit_push_after_review |
may_derive_next_phase | must_stop_after_report | subagent_self_review_is_evidence_only |
parent_review_readback_done | subagent_notification_checked_for_commit_push_claims
```

Integrity fails when the sub-agent created later-phase artifacts, committed/pushed without a
commit-only work order or exact file-set authorization, ran any git write/ref command while
`commit_push_authorized=false`, emitted git directives without exact authority, or relied on
its own `parent-review` file as if parent review had happened. The repair is
quarantine/validate/record/patch/continue under parent control, not silent acceptance and not
default revert.

## Report format

```json
{
  "implementationIntegrityReport": {
    "capability": "UserRegistrationService.RegisterAsync",
    "flow": "FLOW-01-user-registration",
    "checks": {
      "documentationAndContractsUpdated": { "status": "PASS", "evidence": "contracts/endpoints/... updated" },
      "testsPresent": { "status": "PASS", "evidence": "dotnet test ...Tests --filter Registration -> 8 passed, 0 failed" },
      "projectRulesApplied": { "status": "PASS", "evidence": "rg throw new -> 0; rg OperationResult< -> 0" },
      "noNewGaps": { "status": "PASS", "evidence": "dotnet build Xiigen.Flows.slnx -> 0 Error(s)" },
      "implementationReality": { "status": "PASS", "evidence": "Api->Application->Infrastructure path + Fail-path test" }
    },
    "verdict": "INTEGRITY_CONFIRMED"
  }
}
```

## Anti-patterns

1. Adding a new service without updating the flow contract/docs — the docs will not reflect
   reality next session.
2. Fixing the instance but not adding the structural guard — the same gap reappears.
3. "I'll add the doc update next session" — docs are part of the current commit.
4. Counting "complete" from JSON/report/scaffold evidence — a report proves only the report;
   Check 5 must prove the real Api→Application→Infrastructure path.
5. Borrowing core ML/host wording (trained, DPO, live-host, checkpoint) to imply capability
   this repo does not own — G12 boundary violation.
