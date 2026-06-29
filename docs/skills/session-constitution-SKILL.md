# Session Constitution — Supreme Law of Every Session

> **Stack adaptation (xiigen flows).** Universal session-start / session-end / critique-response
> constitution ported from `llm_mvp_core/docs/skills/session-constitution-SKILL.md`. Adapted to
> this repo:
> - **build/test baselines** = `dotnet build Xiigen.Flows.slnx` + `dotnet test` (xUnit) for the
>   affected flow `Xiigen.Flow.<Name>.Tests`, plus the Angular 21 build / Playwright for client
>   branches — NOT core `dotnet test LlmFromScratch.sln`;
> - DNA checks are this repo's invariants: no `throw` for business logic (use
>   `DataProcessResult<T>` via `MicroserviceBase<TSelf>` `Success<T>`/`Failure<T>` — NOT core
>   `OperationResult<T>`), Fabric First (no direct `new` of a concrete store/provider), and
>   tenant-scope isolation;
> - **Forbidden Decisions** add enterprise items: **No-Secrets-in-repo** (no AWS keys / JWT
>   signing keys / Vault tokens / tenant secrets committed), changing a flow's public Web API
>   contract, and adding a new shared dependency;
> - the **sub-agent commit/push boundary is ported verbatim**: an artifact/code/docs-producing
>   sub-agent does NOT commit or push; `STATE next_action` / a passed gate is NOT commit
>   authority; the parent reviews and owns the commit boundary;
> - the per-flow boundary is
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` plus
>   `client/angular/src`.

## What This Skill Does

Governs session start, session end, and the critique-response protocol.
Every session follows this protocol without exception.

---

## Session Start Protocol (Run Every Time)

Project-local override: this protocol is subordinate to AGENTS.md, CLAUDE.md,
Luba's current instruction, and the active plan/state. It cannot force a branch
switch, full build, full test run, or a user-facing stop when the active plan
names a different allowed verification and a known `next_action`.

```
Step 1 — Branch check
  git branch --show-current
  -> Record the current branch. Do not switch branches unless Luba explicitly asks
     for that branch action in the current task.

Step 2 — Clean build baseline
  dotnet build Xiigen.Flows.slnx
  -> Run only when the active plan Gate B or Luba's current instruction requires it.

Step 3 — Test baseline
  dotnet test <affected Xiigen.Flow.<Name>.Tests> 2>&1 | tail -10
  -> Run only when the active plan Gate B or Luba's current instruction requires it.

Step 4 — Read session plan
  Read the active plan / work order completely.
  Record one specific fact from the end of the file (proof of reading).

Step 5 — Read STATE (if resuming)
  Read the current flow/session state for phase context.
```

**Do not skip steps.** A build baseline from a previous conversation is not a baseline.

---

## Session End Protocol (Before Every Commit)

```
Step 1 — DNA check
  grep -rn "throw new" flows/*/server --include="*.cs"
  → Zero matches in business logic (use DataProcessResult<T> via Success/Failure)
  grep -rn "= new [A-Z][a-zA-Z]*Store\|= new [A-Z][a-zA-Z]*Service\|= new [A-Z][a-zA-Z]*Provider" flows/*/server --include="*.cs"
  → Zero direct instantiation of concrete fabric/provider types (Fabric First)

Step 2 — Comment / readability audit
  Review every changed file: production lines carry intent comments.
  Spot-check: open each changed file, scroll top to bottom.

Step 3 — Test gate
  dotnet test <affected Xiigen.Flow.<Name>.Tests> 2>&1 | tail -10
  -> Run only when the active Gate B or Luba's current instruction requires it.
     Must show Failed: 0 when required. For client branches, Angular 21 Playwright green.

Step 4 — Documentation sync
  Update the matching project docs (flow-map, contracts, DECISIONS-LOCKED) in the
  same slice. Update before committing, not after.

Step 5 — STATE update
  Save the flow/session state with deliverables, test count, and next step.

Step 6 — Commit
  git add <specific files — not "git add -A">
  git commit -m "<flow/phase>: <description> — <list every changed file>"
  git push origin <current-branch>
```

For bounded sub-agents, Step 6 is forbidden unless the work order is
commit/push-only or explicitly grants commit/push for the exact file set.
`STATE next_action`, a passed gate, or a known next phase is NOT commit/push
authority. If a sub-agent returns changed files without commit/push authority,
the sub-agent stops after the evidence report and the parent architect owns
review plus the next commit-boundary work order.

Artifact-producing, code-producing, documentation-producing, data-producing,
report-producing, plan-producing, and governance-producing sub-agent work
orders must not run Step 6 in the same work order. They return artifacts and
evidence only. Parent review performs the commit/push boundary after accepting
the packet, or launches a separate commit-only sub-agent work order with the
exact file list and commit message.

When `commit_push_authorized=false`, git write/ref commands are forbidden as
commands, not only as outcomes. A bounded sub-agent must not run `git add`,
`git commit`, `git push`, `git checkout`, `git reset`, `git branch`, `git tag`,
`git merge`, `git rebase`, or any staging/ref-writing/history/branch/publish
command unless the work order explicitly grants that authority for the exact
file set. Read-only `git status`, `git diff`, `git log`, `git show`, and
`git rev-parse` are allowed only when listed in the work order.

If a sub-agent report claims it committed, pushed, staged, changed refs, or
emitted git directives without exact authority, parent review treats the packet
as a process violation even when the files are useful. The artifacts go through
quarantine validation; the agent is not delegated more write work in that phase
family until the parent records and repairs the loophole.

---

## Critique-Response Protocol

When a plan, approach, or implementation is in doubt:

```
Rule: Ask ONE precise question only when the active plan, state, local files, and
document RAG cannot derive a safe next action. If the next action is known, continue
instead of asking.
```

Wrong:
```
"Should I use approach A? Or B? Or maybe C? What about D? Which do you prefer?
And also should I change the contract? And one more thing..."
```

Right:
```
"FLOW-01 UserRegistrationService currently returns DataProcessResult<UserRegistrationOutcome>
for Register. Should EmailVerificationService return DataProcessResult<EmailVerificationOutcome>
the same way, or a thinner DataProcessResult<bool>?"
```

A precise question has:
- One specific decision point
- The current state described
- The two concrete options
- Nothing else

**Wait for the answer before writing code only when this is a real blocker.**

---

## Forbidden Decisions (Escalate to Luba)

Never decide these unilaterally:

| Forbidden | Why |
|-----------|-----|
| Add a new shared NuGet / npm dependency | Dependency policy is product scope |
| Change an existing public Web API contract / DTO signature | Breaks all API consumers |
| Change an Angular standalone component's public inputs | Breaks client consumers |
| Skip the zero-failures test gate for any reason | No exceptions |
| Delete a file that was part of a prior slice | May be referenced |
| Change the test baseline count down | Regressions require approval |
| Create a new flow project (`*.csproj` / new `Xiigen.Flow.*`) | Architecture decision |
| Commit any secret (AWS key, JWT signing key, Vault token, tenant secret) | No-Secrets invariant |

---

## Session Complexity Scaling

| Session Type | Required Start Steps | Required End Steps |
|-------------|---------------------|-------------------|
| Bug fix (single file) | Steps 1-3 | Steps 1, 3, 6 |
| Feature (single flow layer) | Steps 1-4 | Steps 1-6 |
| Phase (multiple flow layers) | Steps 1-5 | Steps 1-6 + doc sync |
| Architecture (contract changes) | Steps 1-5 + read all affected files | Steps 1-6 + full doc sync |

Scale up, never down. When uncertain, use the more thorough protocol.

---

## Self-Verification Loop (Session End)

Before reporting session complete, run this checklist mentally:

```
□ Build baseline: recorded at session start?
□ Test count: same or higher than start?
□ Failed: 0 — verified with a fresh dotnet test run?
□ DNA: no throw in business logic (DataProcessResult via Success/Failure)?
□ DNA: no direct fabric/provider instantiation?
□ Tenant-scope: no read/write outside the flow's tenant scope?
□ No-Secrets: nothing secret staged?
□ Read proof recorded for every touched file?
□ Gate B: every placement's Gate B explicitly met?
□ Docs synced: relevant canonical docs (flow-map, contracts, DECISIONS-LOCKED) updated?
□ STATE saved: flow/session state written?
□ Commit message: lists every changed file?
```

**"I think I covered everything" is not a checklist result.**
Run through each checkbox with evidence.

---

## Skill Registry

Skills are loaded on demand. Load a skill when the session task activates it:

| Situation | Load Skill |
|-----------|-----------|
| Declaring session type | session-scope-resolution-SKILL.md |
| Binding upstream requirements | authority-requirement-binding-SKILL.md |
| Choosing solution scope | solution-scope-gate-SKILL.md |
| Classifying a decision point | four-tier-decision-classification-SKILL.md |
| Constant / value classification | freedom-machine-SKILL.md |
| Product vs implementation choice | no-product-decisions-SKILL.md |
| Reviewing an interface/architecture | multi-reviewer-design-SKILL.md |
| Capturing a decision | decision-capture-SKILL.md |
| Reopening a locked decision | decision-reopening-SKILL.md |
| Catching architect failure modes | architect-behavior-classifier-SKILL.md |
| Responding to a "why"/quality challenge | conversation-intent-and-self-reflection-SKILL.md |
| Closing a capability gap | self-extension-session-SKILL.md |
