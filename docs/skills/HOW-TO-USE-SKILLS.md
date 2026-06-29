# HOW-TO-USE-SKILLS.md — When to load which skill (xiigen flows, .NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield index. Ported from the core
> single-source-of-truth index `llm_mvp_core/docs/HOW-TO-USE-SKILLS.md` (G11). The
> universal part carries over: one self-contained index with an explicit **Load
> order**, the supremacy rule **AGENTS.md and CLAUDE.md outrank every skill file**,
> the ban on a user-facing STOP / approval-wait / branch-switch / full test run
> without an explicit Gate B, the `Session type → skills` table, the
> `Activation triggers` table, the `lifecycle-stage → skills` map, and the rule
> that every multi-step plan carries its own plan-governance orchestration block.
> Adapted here:
> - **Stack** = .NET 10 per-flow `Api / Application / Infrastructure / Tests`
>   (xUnit) + Angular 21 standalone clients. There is no `server/`+`client/` monorepo
>   like xiigen mvp — each flow is `flows/FLOW-NN-<name>/{server,client}`.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0 Error, 0 Warning;
>   `TreatWarningsAsErrors=true`) + `dotnet test` (Failed 0) for backend, and
>   `ng test --watch=false` / Playwright for client-bearing flows.
> - Domain return type = `DataProcessResult<T>` (the flows wrapper). Core
>   `OperationResult<T>` stays in `llm_mvp_core` (0× in `flows/` `src`).
> - **`.xiigen/wiki-domain` is a consumed source, not a local model.** Per R5 the
>   common models and their training stay in `llm_mvp_core`; flows only reads the
>   manifests it ships with.
> - **Prerequisite:** flows must also grow a root `AGENTS.md` / `CLAUDE.md` that
>   this index obeys (greenfield — they do not exist yet). Until they do, this
>   index points at the core agent files as the binding governance and the
>   per-skill stack-adaptation blockquotes as the authoritative local override.

---

## Load order: always start here

    Step 1: read CLAUDE.md + AGENTS.md (flows root once they exist;            (every session)
            until then the core agent files + the per-skill stack-adaptation header)
    Step 2: read the active plan/state and identify the allowed verification   (every session)
    Step 3: load the session-constitution skill                                (every session)
    Step 4: load the session-type skill (table below)                          (every session)
    Step 5: load activation triggers if applicable

Project-local override:

```text
AGENTS.md and CLAUDE.md outrank every skill file.
No skill may introduce a user-facing STOP, approval wait, branch switch, or full
`dotnet test` / `ng test` unless Luba's current instruction or the active plan
Gate B requires it.
```

Skill-local words such as `STOP`, `hard stop`, `must be on main`, `run all tests`,
or `wait for answer` are internal checks only. They do not override:

```text
continue while next_action is known;
use the current branch unless Luba explicitly asks to switch;
run only the verification allowed by the active phase;
answer in Luba's language (Russian unless she asks otherwise);
commit/push when the phase deliverable requires it.
```

Before any commit/push boundary, failed verification output is a hard fail even
if a later commit command succeeds. The PowerShell wrapper
`scripts\pre-commit-check.ps1` (and any commit script) must check `$LASTEXITCODE`
immediately after `git diff --check`, `dotnet build`, `dotnet test`, JSON parse,
and schema checks, and abort before stage/commit/push on any nonzero value. Do not
chain a verification command into commit/push in a way that can continue after the
verification failed.

Partial progress is not a stop boundary. A skill may package a phase, write a
report, or recommend a checkpoint, but if the active state has
`goal_reached=false`, `stop_allowed=false`, and a non-empty `next_action`, the
executor must continue that next action.

Soft stops are forbidden. Skill wording such as "continue next", "wait for
approval", "status only", "checkpoint complete", or "handoff" must be converted to
continued execution when the state has a known `next_action`.

Every multi-step plan must also include a plan-governance orchestration block.
If the active plan does not say which skills govern plan creation, plan review,
execution, code review, documentation sync, completion verification, role
transitions, state updates, score penalties, and anti-loophole repair, the
executor must add that missing governance before executing the plan.

---

## Required skill orchestration by lifecycle stage

```text
plan creation:
  authority-requirement-binding
  reference-inventory-and-source-map
  planning-session-startup
  reconnaissance-gate
  intent-to-plan
  plan-self-validation
  specificity-calibration
  claim-verification

plan review:
  authority-requirement-binding
  plan-review
  audit-protocol            (10-point checklist — see audit-protocol-SKILL.md)
  claim-verification
  specificity-calibration
  output-skepticism

execution:
  code-execution
  code-examination
  change-propagation
  verification-before-completion

code review:
  generated-code-review
  self-verification
  test-integrity
  dna-compliance-guard      (DNA via DNA-DOTNET-TRANSLATION.md — DataProcessResult, fabric DI)

documentation sync:
  documentation-sync
  document-hierarchy

completion verification:
  coverage-completeness-gate
  verification-before-completion
  phase-completion-packager
```

---

## Session type → skills

| Session type | Load before first output |
|---|---|
| EXECUTOR | dev-safety-SKILL.md + code-execution-SKILL.md |
| ARCHITECT | plan-review-SKILL.md + no-product-decisions-SKILL.md |
| REVIEW | plan-review-SKILL.md + audit-protocol-SKILL.md |
| QA / e2e | api-shape-verification-SKILL.md + integration-testing-SKILL.md + ui-testing-SKILL.md |
| NEW FLOW | flow-implementation-guide (mvp) translated to per-flow `Api/Application/Infrastructure/Tests` + blast-radius-SKILL.md |

---

## Activation triggers — load when condition is true

| When | Load |
|---|---|
| About to claim "done" or "passes" | verification-before-completion-SKILL.md (run the verification first) |
| Plan has no Gate B | plan-review-SKILL.md (Gate-B = exact `dotnet build` / `dotnet test` counts) |
| New, clarified, or strengthened requirement from active task authority | authority-requirement-binding-SKILL.md + plan-review-SKILL.md |
| New `.cs` service file created in a flow | dev-safety-SKILL.md + dna-compliance-guard-SKILL.md (DataProcessResult return, fabric DI) |
| New `I*.cs` contract created or changed | blast-radius-SKILL.md (Assessor) + blast-radius-tagger-SKILL.md (INTERFACE = CRITICAL fan-out across flows for shared Kernel/Fabrics contracts) |
| Any test fails unexpectedly | mental-debug-SKILL.md |
| Test passes but the output value is wrong | mental-debug-SKILL.md (error-silent-swallow) |
| A stub was modified (not just created) to pass a test | test-integrity-SKILL.md |
| Reading a FREEDOM/config value | iron-rule-derivation-SKILL.md (read via `IFreedomConfigService.GetAsync(key)`, never `IConfiguration` — 0× in flows) |
| Before implementing any capability class | archetype-failure-map-SKILL.md (declare the score-bracket action table first) |
| Before writing any test class for a capability | iron-rule-derivation-SKILL.md (derive the iron rules from failure modes first) |
| During any plan review / before "ready for execution" | audit-protocol-SKILL.md (10-point audit + NOT-CHECKED section) |
| A context package / generated-code prompt scores 0.65–0.89 | prompt-patch-authoring-SKILL.md (one bottleneck, NAMED_CHECK WRONG/CORRECT, regression check) |
| New Angular 21 standalone component or screen added | ui-testing-SKILL.md (Playwright + visual evidence) |
| About to commit a mixed session (tests + production + docs + skills) | blast-radius-tagger-SKILL.md (GOVERNANCE first, DOCS in the same commit as code) |
| Any reference to a common/shared model or its training | STACK-MAP.md (ML stays in `llm_mvp_core`, consumed via `.xiigen/wiki-domain` manifest — note-only here) |

---

## Notes

- This index is the single source of truth for the flows skill library. When two
  versions of any index appear, archive the older ones — do not let parallel
  "tables of contents" compete (the entropy lesson from xiigen mvp).
- ML / DPO / graph-sync skills (G12 family) are **not** ported into flows. Per R5
  they remain in `llm_mvp_core`; flows consumes them through
  `.xiigen/wiki-domain/{domain-manifest.json,model-manifest.json}` and
  `relations/code-trace.json`. Where a skill has an ML face, only its non-ML
  governance face is ported (see STACK-MAP.md).
- The stack-correct facts above (`net10.0`, `TreatWarningsAsErrors=true`, Angular 21
  standalone, `scripts\pre-commit-check.ps1`, `DataProcessResult<T>`,
  `IFreedomConfigService.GetAsync`) are verified against the live flows repo at port
  time; counts drift as code grows.
