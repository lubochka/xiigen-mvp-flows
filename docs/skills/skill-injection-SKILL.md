# Skill Injection

> **Stack adaptation (xiigen flows).** The skill-injection contract ported from
> `llm_mvp_core/docs/skills/skill-injection-SKILL.md` (G04). This repo had **no
> skill registry** before — this file plus the sibling `docs/skills/*` set is the
> registry. Adapted bindings:
> - **SK-DNA** → `dna-compliance-guard-SKILL.md` + `DNA-DOTNET-TRANSLATION.md`
>   (the NestJS→.NET seed); grep gates run against `*.cs`; Result type is
>   `DataProcessResult<T>`, base is `MicroserviceBase<TSelf>` (XIIGen family wrapper,
>   not core `OperationResult<T>`).
> - **SK-TEST** → the three test levels as xUnit (`Xiigen.Flow.<Name>.Tests`):
>   unit + integration + per-flow suite; client e2e via Angular 21 + Playwright.
> - **SK-FAB** → per-flow DI interface contracts (`IPaymentFabricService`,
>   `ICanvaWriter`, `IClientPushConnectionPool`, …) and fabric coverage.
> - **SK-DOCS** → `documentation-sync` over `.xiigen` / wiki-domain manifests and the
>   per-flow `docs/`.
> - **Gate-B** anywhere = `dotnet build Xiigen.Flows.slnx` (0 errors AND 0 warnings)
>   + `dotnet test` on the affected per-flow `.Tests`.
> - **SK-VERIFY** is mandatory-additional for any trainable-unit work or any
>   phase-done/completion claim on an algorithm/model. By R5/G12 the trainable units
>   themselves live in `llm_mvp_core`; a flow that ships its own learned unit (rare,
>   explicitly approved) still triggers SK-VERIFY with Layer-5 numeric metrics.

## What This Skill Does

Governs how skill files are selected and injected as context into generation,
implementation, and planning steps. Without skill injection, generated/written
flow code may violate DNA rules or miss required patterns.

Коротко (RU): в рабочем приказе и пакете возврата нельзя ограничиться короткими
метками вроде `SK-DNA` или `SK-TEST`. Нужно человеческим языком назвать, какие
файлы/секции проверяются, какие источники прочитаны, где граница текущего
артефакта, и почему это не разрешает прямое исполнение родительской сессией.

---

## Injectable Skill Blocks (flows mapping)

| Block ID | Skill File(s) | Content | Token Budget |
|----------|--------------|---------|-------------|
| SK-DNA | `dna-compliance-guard-SKILL.md` + `DNA-DOTNET-TRANSLATION.md` | DNA rules + grep checks, `DataProcessResult<T>`/`MicroserviceBase<TSelf>` gate | ≤ 900 tokens |
| SK-FAB | `code-examination-SKILL.md` (chain map) + per-flow DI interface contracts | Fabric coverage + interface contracts (`Api→Application→Infrastructure`) | ≤ 800 tokens |
| SK-PLAN | `planning-session-startup-SKILL.md` + `authority-requirement-binding-SKILL.md` + `phase-planning-SKILL.md` | Pre-code gates, requirements ledger, source map, contradiction sweep | ≤ 1000 tokens |
| SK-REVIEW | `plan-review-SKILL.md` + `goal-delivery-completeness-SKILL.md` + `claim-verification-SKILL.md` + `output-skepticism-SKILL.md` | First-class plan review, correction, claim verification, readiness sweep | ≤ 1800 tokens |
| SK-TEST | `three-level-verification-SKILL.md` (xUnit) | Unit + integration + per-flow suite | ≤ 800 tokens |
| SK-DOCS | `documentation-sync-SKILL.md` | File → canonical docs / `.xiigen` manifest map | ≤ 800 tokens |
| SK-VERIFY | `generated-code-review-SKILL.md` + `verification-before-completion-SKILL.md` | 5-layer generated-code review + Runtime Evidence Firewall (reject validation_only/static/contract-only/UNKNOWN; require numeric metrics for a real trainable unit) | ≤ 1000 tokens |
| SK-QA | `three-level-verification-SKILL.md` + e2e/host + negative + untrained/trained split | Engineering test strategy across layers | ≤ 1000 tokens |
| SK-ARCH | (references `llm_mvp_core` allowed-architecture gate) | Map learned/scorer/selector/router/classifier/KNN/dictionary/pattern mechanisms to the allowed levels (which live in core) | ≤ 1000 tokens |

**Hard limit: maximum 3 core candidate skill blocks per generation/implementation
call.** Mandatory-additional blocks (below) are exempt from the cap.

**Exception — SK-VERIFY is mandatory-additional.** Whenever the work touches a
trainable unit, OR makes any "phase done"/completion claim on an algorithm/model,
OR reviews generated code, SK-VERIFY is injected IN ADDITION to the cap and is
NEVER dropped. Skipping SK-VERIFY for trainable work is the V20 failure mode
(json/stub passed as phase-done).

**Exception — SK-QA is mandatory-additional.** Whenever a work order writes or
changes flow code, a trainable unit, a host/extension surface, OR makes any
runtime/e2e claim, SK-QA is injected IN ADDITION to the cap (alongside SK-VERIFY)
and is NEVER dropped. SK-TEST covers unit+integration+suite; SK-QA additionally
forces e2e/host (Angular 21 + Playwright where a flow has a client), negative
cases, and the two-PASS untrained/trained expectations.

**Exception — SK-ARCH is mandatory-additional.** Whenever work mentions learned,
semantic, trainable, adaptive, DPO, selector, router, classifier, scorer, KNN,
dictionary, counter, pattern matching, `TryDecide*`, `ChooseBestPattern`,
`SelectBestPattern`, or `CountMatchingSignals`, SK-ARCH is injected IN ADDITION to
the cap and is NEVER dropped. It runs before repair and before accepting a plan,
review, code slice, dataset, or completion claim. (In this repo the allowed levels
live in `llm_mvp_core`; SK-ARCH here routes such a mechanism to a core unit or
downgrades the claim — a flow does not host its own trainable authority by R5/G12.)

Rationale: injecting more than 3 core candidate blocks truncates the core
instructions in the context window.

---

## Activation Rules (MACHINE — not configurable)

```
SK-VERIFY: trainable-unit work OR any phase-done/completion claim on an algorithm/model OR generated-code review → ALWAYS (priority 0, exempt from the 3-block cap)
SK-QA:    flow code/contract change OR trainable unit work OR host/extension surface OR any runtime/e2e claim → ALWAYS (mandatory-additional, exempt from the cap)
SK-ARCH:  learned/semantic/trainable/adaptive/DPO claim OR scorer/selector/router/classifier/KNN/dictionary/pattern matcher OR deterministic-shortcut question → ALWAYS (priority 0, exempt from the cap)
SK-DNA:   new class being added OR dna_compliance check fails
SK-FAB:   new interface being added OR new DI dependency chain in a flow
SK-PLAN:  iteration ≤ 2 OR phase boundary (phase transition)
SK-REVIEW: plan review OR user correction OR shallow planning/review challenge OR final readiness sweep
SK-TEST:  test coverage incomplete OR bug-fix (always needs test first)
SK-DOCS:  last step of session OR canonical doc / .xiigen manifest sync triggered
```

---

## Selection Algorithm

```
Priority order when > 3 core candidates: DNA > FAB > PLAN/REVIEW > TEST > DOCS

1. Collect core candidates from the activation rules above.
2. Take the top 3 by priority → the capped core set.
3. Append ALL triggered mandatory-additional blocks (SK-VERIFY, SK-QA, SK-ARCH),
   exempt from the cap.
4. Return the deduped merge: capped core + all triggered mandatory-additional.
```

---

## When to Inject Per Task Type

| Task | Inject |
|------|--------|
| New flow class from scratch | SK-DNA + SK-FAB + SK-TEST |
| Bug fix | SK-TEST + SK-DNA |
| Phase planning | SK-PLAN + SK-DNA |
| Plan review or user correction | SK-REVIEW + SK-PLAN |
| Architect work order to sub-agent | SK-PLAN + SK-FAB |
| Session end | SK-DOCS |
| Interface change | SK-FAB + SK-DNA |
| Generated flow code / codegen step | SK-DNA + SK-TEST + SK-VERIFY |
| Trainable unit (rare, approved) | SK-DNA + SK-FAB + SK-VERIFY + SK-ARCH |
| Phase-done / completion claim on an algorithm or model | SK-VERIFY (always) |
| Any flow code / host / trainable work order (test strategy required) | SK-QA (always) |

---

## Work-order skill + guide binding

A bounded sub-agent work order MUST enumerate (a) the exact skills it must load
and apply, (b) the required guides, and (c) the required injection blocks (e.g.
SK-VERIFY for trainable/algorithm/phase-done work, SK-QA for any code/host/
trainable work order). The returned evidence packet MUST confirm which skills,
guides, and blocks were loaded and applied, and MUST return `test_layer_results`
for each declared test layer. A trainable-unit packet without SK-VERIFY loaded and
its Layer-5 numeric metrics is an invalid packet — the architect rejects it.

---

## Bounded work-order boundary injection

Every bounded sub-agent work order must include the boundary-control fields
alongside the skill list:

```text
CURRENT TASK:
NOT THE TASK:
EVIDENCE-ONLY:
requested_artifact_type
allowed_write_scope
exact_current_instruction_quote
required_skills_guides_and_injection_blocks
human_readable_status_required = true
human_readable_progress_board_required = true
exact_review_scope_required = true
code_review_completeness_scope_required_for_code_slices = true
allowed_verification = dotnet build Xiigen.Flows.slnx (0/0); dotnet test <per-flow .Tests>; git diff --name-only
forbidden_broad_verification
commit_push_boundary_owner = parent_after_review | separate_commit_only_work_order | allowed_with_exact_quote
work_order_scope
exact_allowed_files
git_authority = forbidden | parent_only | allowed_with_exact_quote
commit_push_authorized
allowed_git_commands
forbidden_commands
parent_will_perform_commit_push_after_review = true for normal artifact/code/content slices
may_derive_next_phase = false unless explicitly granted
must_stop_after_report = true
parent_review_required_after_return = true
parent_direct_write_forbidden_when_subagent_only = true when the parent/current session is architect-only / sub-agent-only / non-executor
parent_local_sidecar_work_forbidden_while_waiting = true
parent_read_only_verification_allowed_only_after_packet_return = true
```

For plan/review/governance/skill/documentation deliverables, implementation after
the deliverable is forbidden until a fresh post-delivery acceptance quote
explicitly accepts the artifact and starts implementation.

When `commit_push_authorized=false`, `forbidden_commands` must explicitly block
`git add`, `git commit`, `git push`, `git checkout`, `git reset`, `git branch`,
`git tag`, `git merge`, `git rebase`, and any git write/ref command. Read-only git
commands may appear only under `allowed_git_commands`. A packet that ran git
write/ref commands without authority is invalid and quarantined pending parent
validation.

Commit/push authority is injected only into a **commit-only** work order that
forbids artifact/code/content edits, lists the exact file set to stage, lists the
exact commit message, and forbids next-phase derivation.

Every returned sub-agent packet must include:

```text
git_authority_seen
git_commands_run
git_write_or_ref_commands_run
git_directives_emitted
```

### Context boundary for bounded sub-agents

Inject a clean/minimal task context by default
(`subagent_context_mode = clean_minimal`): the agent receives the current work
order, exact files, required skills/guides, and no old continuation/commit/push
authority. Use `full_history_exception` only when explicitly justified, and then
mark `old_history_authority = evidence_only`, `state_next_action_authority = false`,
`work_order_fields_are_only_authority = true`.

---

## Context Hygiene

Each generation/implementation/planning step receives a CLEAN context — skill
blocks are injected fresh, not accumulated from previous steps. Re-inject the
relevant blocks at each step.

---

## Anti-Patterns

1. **Injecting every available block every step** — select a small core set plus triggered mandatory-additional blocks; injecting everything causes context overflow.
2. **Not injecting SK-DNA for new classes** — DNA violations in flow code are hard to find later.
3. **Carrying skill context between steps** — accumulation pollutes later steps with stale guidance.
4. **Skipping injection because "I know the rules"** — systematic injection prevents drift.
5. **Omitting SK-VERIFY for trainable-unit or phase-done work** — lets json/stub deliverables pass as phase-done with only build+contract+DNA evidence (the V20 failure). SK-VERIFY is mandatory-additional, never dropped.
6. **Omitting SK-QA for code/host/trainable work** — lets a slice ship with no declared test strategy (no e2e/host, no untrained/trained split). This is the testability-gap failure. SK-QA is mandatory-additional, never dropped.
