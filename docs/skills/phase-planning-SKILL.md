# Phase Planning — Pre-Code Validation Gates

> **Stack adaptation (xiigen flows).** Universal pre-code gate battery + Gate -1 authority
> ledger + Sequential Execution-Control ported from
> `llm_mvp_core/docs/skills/phase-planning-SKILL.md`. Adapted to this repo:
> - per-phase **Gate B** = `dotnet build` / `dotnet test` (xUnit) on the per-flow projects
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`,
>   plus Angular 21 Playwright for client phases;
> - "one commit per phase" applies natively to the .NET 10 solution (`Xiigen.Flows.slnx`);
> - standalone Angular 21 phases are separate ordered rows;
> - the domain return type is `DataProcessResult<T>` (NOT core `OperationResult<T>`); fabric
>   contracts come from `Xiigen.Kernel`; DI is the `AddXxxFlow()` extension + minimal-API;
> - "executor does not close the plan" (sticky architect ownership) is formalized into gates.

## The Rule

No code is written without a plan that passes all gates AND receives Luba's written approval.

---

## Gate -1 — Authority Requirements Ledger Coverage

Before infrastructure discovery or phase design, extract authority-chain requirements from the
current human operator / product owner, architect work order, approved active plan, and accepted
reviewer gate. Build: Authority Requirements Ledger, Conversation Delta Ledger, User Prompt
Source Ledger, Replacement Mapping Register, and (for high-risk/large/multi-phase plans) a
Whole-Plan Contradiction Sweep.

Before deriving any requirement, artifact target, work order, cleanup, continuation, repair
scope, or git scope, record the quote-boundary parse:

```text
CURRENT_UNQUOTED_LUBA_INSTRUCTION:
QUOTE_DELIMITER_PARSE:
  detected_quote_marker: <Цитата: | fenced quote | transcript block | angle-bracket block | NONE>
  quoted_span_status=evidence_text_only
  detected_real_task_marker: <Вот задание!!! | Задание: | equivalent current unquoted marker | NONE>
  real_task_span=only the unquoted text after the real-task marker
  may_execute_from_quote=false
CURRENT_TASK_DERIVATION:
  current_task_source=current_unquoted_luba_message | bounded_work_order
  quoted_context_used_as_task_source=false
  active_task_sentence_source:
  evidence_text_created_subrequirement=false
```

For every requirement row, map: `Requirement ID | Conversation source | Plan section |
Gate/test | Artifact | Status (mapped/unmapped/contradicted) | Next action`.

**Gate -1 passes when:** every requirement is mapped to a plan section, gate/test, and
artifact; every plan-shaping prompt/feedback/question/answer has a source entry; no plan
sentence contradicts a requirement; and removing an old mechanism is backed by the required
replacement (not deletion-only). If any row is unmapped or contradicted → stop plan authoring
and repair. Do not proceed to Gate 0.

---

## Sequential Execution-Control Gate

Every phase plan includes `current_execution_pointer` (the only executable row) separate from
`current_turn_artifact_queue`, plus an `ordered_phase_queue` with one row per substep using
exact `phase.step.status` numbering, status ∈ `done | in-progress | remaining | review-block |
blocked`. The first unfinished row is the only executable target; no jumping ahead while an
earlier row is `remaining`/`review-block`/`blocked`. Visible fields: total phases, total
substeps, counts per status, completed phase list, current phase/step, remaining list + names,
a plain-language sentence per phase, and a `requirement → phase → evidence` matrix. Counts must
equal the row statuses.

Every Luba-facing status block (Russian before machine labels):

```text
Фаза X из Y: <человеческое название>. Смысл фазы: <что эта фаза решает для продукта>.
Шаг X.Y из X.N: <человеческое название>. Смысл шага: <что этот шаг проверяет/создаёт>.
Статус: DONE | IN_PROGRESS | NEEDS_FIX | REVIEWING | BLOCKED_WITH_REASON. <одна русская фраза>
Позиция: всего <Y> фаз; сделано <n>; текущая <X>/<X.Y>; осталось <n> фаз и <n> шагов.
Сделано / В процессе / Осталось / Review нашёл / Что исправлено / Что осталось.
Следующее действие: <next bounded sub-agent work order, or NONE>
Commit/continuation allowed: <да/нет>
opaque codes: <technical labels only after the human explanation, or NONE>
```

---

## Dual-Path Host Verification (extension/runtime/scenario phases)

A plan may not model verification as one fragile chain. Each scenario gets two parallel rows:
Path A (automated boundary — xUnit + minimal-API request/response contract) and Path B (local
Angular 21 desktop run with a visible marker + screenshot/video/log). A blocked Path B
downgrades the claim, records the exact screen/state + next repair, and continues other Path A /
host / scenario work — it is not a stop gate. Progress is measured by scenario counters
(`total / fully_joined_proven / partial / not_moved / blocked_with_next_action`), not phase
artifacts. Separate `evidence_accounting_done` from `product_requirement_proven`.

---

## Phase Transition Protocol

At the end of every phase:
```
Step 1: Read Phase N+1 spec from the plan file.
Step 2: Run Gate -1 — authority requirements coverage from the active task authority.
Step 3: Run Gate 0 — fresh infrastructure discovery (not cached from Phase N).
Step 4: Run Gates 1–7 — standards safety, fabric/DI map, test matrix.
Step 5: Run plan-review-SKILL FC checks on the Phase N+1 deliverable list.
Step 6: Present SECTION A (Phase N summary: deliverables, dotnet test before→after delta,
        deviations) and SECTION B (Phase N+1 reviewed plan: gate table, FC battery,
        write-time fixes, file count). STOP only when the deliverable is plan-only, no
        sub-agent work is currently assigned, and Luba approval is required.
Step 7: Begin Phase N+1 only on fresh approval when required; otherwise continue through the
        next bounded sub-agent work order authorized by the active plan and current Luba
        instruction.
```

---

## Gate 0 — Infrastructure Discovery
Run infrastructure discovery first. Verify:
- [ ] Pre-existing failures captured: `dotnet test` run fresh on the flow(s) in scope (or `Xiigen.Flows.slnx`).
- [ ] Artifact boundaries read from live files (not the plan).
- [ ] Every plan `path:line` reference has a verified match.
- [ ] All contracts/classes referenced confirmed to exist via grep.
If discovery is incomplete → stop. Do not continue to Gate 1.

## Gate 1 — Standards Regression Safety
| Rule | Check | Command |
|------|-------|---------|
| No-throw business logic | new methods return `DataProcessResult<T>`, no business `throw` | `grep -n "throw new" [new-files]` → only framework/guard throws |
| Fabric First | new services take interfaces, not concrete | `grep -n "new .*Store(\|new .*Service(" [new-files]` → 0 in constructors |
| Comments | every production line commented | manual review |
| No ML frameworks here | no training/ML libs added to this repo | `grep -n "TorchSharp\|ML\.Net\|ONNX-training" [new-files]` → 0 |
Gate 1 passes when: zero new standards violations in plan scope.

## Gate 2 — Fabric / DI Resolution Map
List every interface the plan touches and confirm an implementation + registration:
| Interface | Implementation(s) | Source | Registered in `AddXxxFlow()`? |
|-----------|------------------|--------|-------------------------------|
| IDocumentStore | InMemoryDocumentStore | `Xiigen.Fabrics.InMemory` | ✓/✗ |
| IIdempotencyStore | InMemoryIdempotencyStore | `Xiigen.Fabrics.InMemory` | ✓/✗ |
| IOutboxStore | InMemoryOutboxStore | `Xiigen.Fabrics.InMemory` | ✓/✗ |
| ITenantContextAccessor | (kernel) | `Xiigen.Kernel` | ✓/✗ |
Red flags: plan instantiates concrete fabrics directly (Fabric First violation); plan adds a
contract dependency without registering it in `AddXxxFlow()` (DI resolution error at runtime).
Gate 2 passes when: all touched interfaces have a complete implementation + registration mapping.

## Gate 3 — Dependency Graph
For any new service depending on multiple interfaces: list ctor dependencies, verify the
`AddXxxFlow()` registration location, verify no circular deps, verify the minimal-API route
wiring in `Program.cs`. Gate 3 passes when: all dependencies registered, no circular deps.

## Gate 4 — Decision Log
Every architectural decision (new contract, new route surface, new fabric dependency, new
algorithm choice, changed public signature) is recorded with: title, date, decision,
alternatives, rationale, affected classes/projects. Gate 4 passes when: zero undocumented
architectural decisions.

## Gate 5 — Test Coverage Matrix
| Component | Unit (xUnit) | Integration | e2e/host | Location |
|-----------|--------------|-------------|----------|---------|
| New `Application` service | `[Fact]` via `FlowFixture.Create()` | minimal-API `WebApplicationFactory<Program>` | Playwright (if client) | `.../Tests/` |
| New route | request/response `[Fact]` | — | Playwright | `.../Tests/` |
| Changed public method | regression `[Fact]` | — | — | `.../Tests/` |
Rules: unit tests construct the service via the in-memory fixture, no real I/O; integration
tests use the minimal-API host and real DI from `AddXxxFlow()`. Gate 5 passes when: the matrix is
complete with file paths for every new component.

## Gate 6 — Doc Sync
Identify docs that must update in the same slice: per-flow `README`/FLOW spec (new route/
behavior), `docs/skills/HOW-TO-USE` index (new skill), this repo's `AGENTS.md`/`CLAUDE.md` if
present (baseline/standard change). Gate 6 passes when: all doc updates identified and assigned
to a specific step.

## Gate 7 — Foundational Principles
| Principle | Gate Question | Answer |
|-----------|--------------|--------|
| P1 Fabric First | every new class depends on interfaces, not concrete fabrics? | ☐ YES / ☐ N/A |
| P2 DataProcessResult | all `Application` public methods return `DataProcessResult<T>`? | ☐ YES / ☐ N/A |
| P3 Comments | every production line commented? | ☐ YES / ☐ N/A |
| P4 Zero Failures | will `dotnet test` show 0 failed after this phase? | ☐ YES / ☐ N/A |
| P5 Test Coverage | does every new class have unit + integration tests? | ☐ YES / ☐ N/A |
| P6 Doc Sync | are all affected docs updated in this phase? | ☐ YES / ☐ N/A |
| P7 No ML here | does the plan keep trainable/ML logic in `llm_mvp_core` (consume via manifest)? | ☐ YES / ☐ N/A |
Gate 7 passes when: all answers are YES or have explicit Luba N/A sign-off.

---

## Gate -2 — Requested Artifact Shape Simulation
Before Gate -1 passes, simulate the repository shape if the phase plan were implemented
literally:
```text
Requested artifact class:           # e.g. new Application service + route + xUnit tests
Expected files/entrypoints:         # per-flow layer files + Program.cs route + Tests
Forbidden substitutes:              # validators/reports/JSON-only/PowerShell instead of real code
Evidence artifacts location:
Pass/fail:
```
The phase fails if the likely output is mostly JSON, reports, validator scripts, or static gate
artifacts instead of real flow code + tests. If a previous implementation already produced the
wrong artifact class, repair skills/guides first and replay this simulation before repairing the
plan.

---

## Passing Criteria

Record in `STATE-Pn.json`:
```json
{
  "phaseGatesPassed": true,
  "gateResults": {
    "gate_minus1_requirements": "PASS",
    "gate0_discovery": "PASS",
    "gate1_standards": "PASS",
    "gate2_fabric_di": "PASS",
    "gate3_deps": "PASS",
    "gate4_decisions": "PASS",
    "gate5_tests": "PASS",
    "gate6_docs": "PASS",
    "gate7_principles": "PASS"
  }
}
```

---

## Anti-Patterns
1. **Skip Gate -1** — a plan can pass internal checks while missing Luba's actual requirements.
2. **Skip Gate 0** — standards checks are meaningless if `path:line` references are wrong.
3. **Gate as checkbox theater** — marking PASS without running the check.
4. **Gate 7 "all applicable"** — requires explicit YES or Luba-signed N/A per principle.
5. **Gates after code is written** — gates are pre-code, not post-code.

## Final Between Bounded Slices Planning Gate
Make the next bounded continuation executable, not aspirational. Required literals for work
orders/review packets: `dual_path_same_scenario_coverage_gate`, `final_between_bounded_slices_forbidden`,
`promise_to_continue_is_not_continuation`, `commit/push boundary is not a stop`,
`next_subagent_started_or_real_blocker`, `scenario coverage`, `Path A`, `Path B`. A scenario is
accepted only when the same scenario + same input pass Path A (xUnit + minimal-API contract) and
Path B (Angular 21 desktop). Before any final response, require the four-question gate:
`scenario_coverage_complete?`, `explicit_user_stop?`, `next_bounded_action_exists?`,
`next_subagent_started_or_real_blocker?`. false/false/true/false forbids final and requires
immediate bounded continuation.

## Integration
Load alongside: `intent-to-plan-SKILL.md`, `plan-self-validation-SKILL.md`,
`plan-review-SKILL.md`, `phase-preflight-SKILL.md`, `plan-execution-feedback-SKILL.md`, and the
four `implementation-doc-*-SKILL.md` files. Required before any code phase.
