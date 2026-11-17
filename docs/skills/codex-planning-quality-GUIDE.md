# Codex Planning Quality Guide

> **Stack adaptation (xiigen flows).** Universal planning-quality guide ported from
> `llm_mvp_core/docs/skills/codex-planning-quality-GUIDE.md`. The guiding rule is the same:
> start a plan/review from the architecture decisions and the **current target artifact**,
> not from an old packet or another model's plan. For this repo:
> - dual-path verification = an Automated boundary path (xUnit per-flow + minimal-API
>   request/response contract) PLUS a real Angular 21 desktop scenario;
> - benchmark plans and the consumed `.xiigen` / wiki-domain manifests from `llm_mvp_core`
>   are **evidence-only**;
> - core logic lives in `llm_mvp_core`; flow logic lives in the per-flow
>   `Api`/`Application`/`Infrastructure` layers, returning `DataProcessResult<T>`;
> - Gate-B = `dotnet build` + `dotnet test` (xUnit), Playwright for client.

## What this guide prevents

Shallow planning, focus drift, replacing a real host scenario with a synthetic one, and
dishonest planning claims (ledger rows counted as cycles; parent/self review called
sub-agent review; tests/builds claimed without execution).

## 1. Start with the current artifact queue

Before any planning content, write:

```text
CURRENT TASK:        <Luba's latest unquoted task or the exact bounded work order>
NOT THE TASK:        <tempting but forbidden work: continuing another model's plan, editing dcm-*/claude-*/codex-*, repairing an old plan>
EVIDENCE-ONLY:       <benchmark plans, quoted transcripts, old plans, sub-agent packets, consumed .xiigen/wiki-domain manifests>
```

The current first queue item is the only artifact that can be called "the target". Foreign
artifacts (`dcm-*`, `claude-*`, `codex-*`) are read-only evidence unless Luba's latest
unquoted message assigns them.

## 2. Architecture-question-first gate

A plan/review starts from architecture decisions, not from prose or an old packet. For a
flow plan, the architecture ground block names:

```text
Flow + layers touched:   FLOW-NN-<slug> → Api / Application / Infrastructure / Tests (+ Angular 21 client)
Domain return type:      DataProcessResult<T> on all Application public methods (NOT core OperationResult<T>)
Fabric dependencies:     which Xiigen.Kernel contracts (IDocumentStore / IIdempotencyStore / IOutboxStore / ITenantContextAccessor / ...)
DI registration:         via Add<Name>Flow() extension, resolved in Program.cs minimal-API
Route surface:           which app.MapPost/MapGet templates
Boundary:                core algorithm logic stays in llm_mvp_core; this repo consumes manifests/locators only
Open architecture decisions for Luba: <list, or NONE>
```

## 3. Dual-path host verification (this stack)

Do not reduce verification to one fragile chain. For each scenario require BOTH:

- **Path A — Automated boundary.** Same scenario through the adapter/API/core
  request-response: xUnit unit tests (`FlowFixture.Create()`, `Xiigen.Fabrics.InMemory`)
  and/or minimal-API integration tests (`WebApplicationFactory<Program>`; each flow exposes
  `public partial class Program;`). Evidence: `dotnet test --filter ... → N passed, 0 failed`,
  request/response body + status code, `DataProcessResult<T>` outcome.
- **Path B — Local desktop.** The same scenario run in the real Angular 21 client on this
  computer with a visible marker plus screenshot/video/log evidence.

Rules:
- JSON-only, adapter/core-only, test-only hook, process/window-list, or synthetic helper UI
  cannot replace Path B; screenshot-only cannot replace Path A.
- A scenario is accepted only when the same scenario + same input pass both paths.
- If Path B cannot reach the client route (host limit, auth, missing UI route), record a
  host-path blocker with the exact screen/state and the next repair; do NOT convert it into
  a stop and do NOT restore an impossible live-host readiness gate.
- Status wording: `scenario coverage: total / accepted / api_only / ui_only / blocked / not_started`,
  plus per-scenario Path A and Path B status.

## 4. No dishonest planning claims

A plan/review/status that inflates evidence is rejected. Required honesty block:

```text
claim_scope:
evidence_scope:
evidence_type:                          # dotnet test output | build output | Playwright run | artifact presence
subagent_id_or_NONE:
count_claimed:
independent_evidence_count:
ledger_rows_counted_as_cycles=false
parent_self_review_counted_as_subagent_review=false
commands_claimed_without_execution=false
downgrade_or_repair_required:
```

Hard fail when "N cycles/reviews/passes" is claimed without N independent records; when
ledger/checklist/generated rows are counted as cycles; when parent/self review is counted
as sub-agent review; or when `dotnet test`/`dotnet build`/Playwright/git is described as
executed without execution evidence.

## 5. Status board template (human-first, English unless the user requests another language, before machine labels)

```text
Phase X of Y: <human-readable name>. Purpose: <what this phase solves for the product>.
Step X.Y of X.N: <human-readable name>. Purpose: <what this step verifies/creates>.
Status: DONE | IN_PROGRESS | NEEDS_FIX | REVIEWING | BLOCKED_WITH_REASON. <one plain-language sentence>
Position: <Y> phases total; <n> done; current <X>/<X.Y>; <n> phases and <n> steps remaining.
Done: <counts + concrete artifacts>.
In progress: <current action and who/what is doing it>.
Remaining: 1. <step + meaning>  2. <step + meaning>
Review findings: <...>   What was fixed: <...>   What remains: <...>
Next action: <next bounded sub-agent work order, or NONE>
Commit/continuation allowed: <yes/no; exactly what is allowed>
opaque codes: <technical labels only after the human explanation, or NONE>
```

Machine labels (packet names, JSON rows, STATE flags, validator names) may appear only
AFTER the plain human explanation.

## 6. Benchmark Delta Report (how another model's plan enters planning)

Useful detail from a benchmark plan enters ONLY through a filled report — never by
continuing the benchmark's work:

```text
benchmark_item:
source_line_or_section:
current_target_status:
architecture_compatible:                # fits per-flow layers + DataProcessResult<T> + Fabric First?
accept_reject_defer:
destination_section:
Gate_B:                                 # real dotnet/Playwright command
completion_evidence:                    # exact line/section read-back in the current artifact
```

Include Top-N valuable additions, an accepted/rejected/deferred enrichment ledger, and a
conflict/non-conflict table. An accepted compatible row is incomplete until the current
user-visible artifact contains the mapped detail with line/section read-back. Silent
omission of compatible useful benchmark detail is a plan-depth failure.

## 7. Per-Subtask Solved Block (depth requirement)

Every subtask gets a solved block, not an executor prompt:

```text
human explanation:
refs:                                   # path:line in this repo, or "future file, no current citation exists"
algorithm:                              # plain English unless the user requests another language; class + method + parameters + steps
concrete files/classes/methods:         # per-flow layers
Gate B / test strategy:                 # dotnet test filter / dotnet build / Playwright + the test layers
owner:
artifact:
alternatives considered:
worked example / simulation:            # concrete input → DataProcessResult<T> outcome / HTTP status
failure mode:
```

Citation Density: enough current `path:line` citations to prove every relied-on
contract/pattern. A sparse-citation solved block is a fail.

## 8. Deterministic-vs-learning boundary

For any subtask that selects, routes, ranks, matches, classifies, or generates: state
explicitly whether it is a deterministic helper or a learned mechanism. In this repo most
flow logic is deterministic domain code (validation, idempotency, status mapping) — that is
correct and must be labelled deterministic. Do NOT dress a deterministic lookup as a
learned/trainable unit; trainable model work belongs in `llm_mvp_core`, and this repo
consumes it through manifests/locators only.

## 9. Review gates

A plan/review packet is rejected if it: lacks the CURRENT TASK / NOT THE TASK / EVIDENCE-ONLY
anchor; lets a benchmark/quoted/foreign artifact become the delegated target; promises future
continuation without a started next sub-agent id/packet or a real blocker; reduces
verification to a single fragile chain instead of Path A + Path B; or counts ledger rows as
cycles.

## Integration

Pair with `planning-session-startup-SKILL.md` (startup gates), `phase-planning-SKILL.md`
(gates), `plan-review-SKILL.md` (review gates), `plan-self-validation-SKILL.md` (Phase 0),
and `planning-quality-forward-test-PROMPT.md` (regression).
