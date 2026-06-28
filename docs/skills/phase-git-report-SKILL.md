# Phase Git Report

> **Stack adaptation (xiigen flows).** The traceable plan→commits→files report
> ported from `llm_mvp_core/docs/skills/phase-git-report-SKILL.md` (G04). Adapted to
> this repo:
> - Test-gate numbers come from `dotnet test` on the affected per-flow
>   `Xiigen.Flow.<Name>.Tests` (xUnit): read `Failed: 0` and `Passed/total` from the
>   real output, with `total ≥ baseline`.
> - Build half of the gate is `dotnet build Xiigen.Flows.slnx` → 0 errors AND 0
>   warnings (`TreatWarningsAsErrors=true`).
> - Changed files live under `flows/FLOW-NN-<slug>/server/...` (and `client/angular`
>   for client-bearing flows). The report file is appended in `docs/sessions/` or the
>   per-flow `docs/`.
> - Commit convention unchanged: `phase N: description — file list`.

## Purpose

After a phase completes, create a traceable record linking:
- Which plan drove this phase
- Which commits were produced
- Which files changed and why
- Whether the commit message follows the convention

This creates a bidirectional trace: plan → commits → files. Any `git blame` or
`git log` immediately reveals which plan produced the change.

---

## When to Invoke

After the phase gate passes (`dotnet test` shows `Failed: 0` and total ≥ baseline,
and `dotnet build` shows 0 errors / 0 warnings), as the last step before pushing.
Append to the phase execution file under `docs/sessions/` or the per-flow `docs/`.

---

## Procedure

```bash
# 1. Find commits since phase start
git log --oneline --since="[phase start timestamp]" HEAD
# OR: find commits since the baseline taken at session start

# 2. Summary of changed files
git diff --stat HEAD~[n_commits]..HEAD

# 3. Check commit message convention
# Expected format: "phase N: [description] — [list of changed files]"
# Example: "phase 3: OnboardingDeliveryService completion gate — flows/FLOW-01-user-registration/server/.../OnboardingDeliveryService.cs, .../OnboardingDeliveryServiceTests.cs"
```

---

## Output Format

Append to the session/phase file:

```
## Git Report — Phase [N]

Plan file: docs/plans/[PLAN-NAME].md   (or the work order id)
Flow: FLOW-NN-<slug>
Branch: <working branch>   (e.g. UpdateUniversalSkills / DesignToCode / claude/<worktree>)
Commits this phase: [N]

| Hash | Message |
|------|---------|
| [hash7] | phase 3: OnboardingDeliveryService completion gate — flows/FLOW-01-user-registration/server/.../OnboardingDeliveryService.cs |
| [hash7] | phase 3: OnboardingDeliveryService tests — flows/FLOW-01-user-registration/server/.../OnboardingDeliveryServiceTests.cs |

Changed Files:
  flows/FLOW-01-user-registration/server/.../OnboardingDeliveryService.cs       | 187 +++++
  flows/FLOW-01-user-registration/server/.../OnboardingDeliveryServiceTests.cs  |  89 ++++
  docs/sessions/SESSION-NN.md                                                   |  12 +
  3 files changed, 288 insertions(+)

Commit Convention Check:
  ✅ All commits follow "phase N: description — file list" convention
  OR: ⚠️ [N] commits have non-standard messages: [list them]

Build Gate:
  dotnet build Xiigen.Flows.slnx — 0 Error(s), 0 Warning(s)

Test Gate (dotnet test <per-flow .Tests>):
  Before: [N] passed, 0 failed
  After:  [N+M] passed, 0 failed
  Delta:  +[M] new tests added this phase
```

---

## Commit Message Convention

Every commit MUST follow this format:
```
phase N: [what was done] — [list every file changed]

Examples:
  phase 3: OnboardingDeliveryService completion gate — flows/FLOW-01-user-registration/server/.../OnboardingDeliveryService.cs, .../OnboardingDeliveryServiceTests.cs
  phase 4: payment settlement idempotency — flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs
  phase 4: documentation sync — docs/sessions/SESSION-04.md, .xiigen/wiki-domain/<manifest>.json
```

**Why this matters:** any `git log`, `git blame`, or `git bisect` immediately
reveals which plan and phase produced the change. Without this convention you
cannot trace a regression back to the plan that introduced it.

---

## Commit Discipline Rules

```
□ One phase = one commit
□ List every changed file in the commit message
□ Never commit with Failed > 0
□ Never commit with build errors or warnings (0 Error(s) AND 0 Warning(s) required)
□ Include documentation / .xiigen manifest changes in the same commit as the code they describe
□ Doc changes go in the same commit — not a follow-up
```

---

## Anti-Patterns

1. **Non-standard commit messages** — "wip", "fix", "update files" cannot be traced to a plan.
2. **Giant single commit** — if a phase includes several logical changes, consider whether each deserves a separate commit (one phase, not necessarily one change).
3. **Skipping the git report** — the report closes the traceability loop; it is mandatory before declaring a phase complete.
4. **Committing doc / manifest sync in a follow-up commit** — docs are part of the phase; commit them together.
