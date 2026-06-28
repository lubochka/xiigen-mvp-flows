# wave-assignment-SKILL.md — Classify FOUNDATION vs Wave N, pick gate model, sequence

> **Stack adaptation (xiigen flows).** Universal *wave assignment* ported from
> `llm_mvp_core/docs/skills/wave-assignment-SKILL.md` (G08). Greenfield here — no
> prior author version. The universal core kept: classify every capability before
> planning — FOUNDATION (downstream dependents > 2) vs Wave 0 / 1 / 2+; choose the
> gate model (absolute vs delta); decide sequential vs parallel (≥3 prior
> completions); and record the wave assignment. The stack details are adapted:
> - The **natural wave unit** is a `FLOW-NN-*`. **FOUNDATION** = a shared
>   contract / fabric that ≥3 flows depend on (e.g. shared `Xiigen.Kernel`
>   contracts the flows consume).
> - **Absolute gate** = `dotnet test` over the flow's `Xiigen.Flow.<Name>.Tests`
>   (`net10.0`, xUnit) → `0 failed`; **delta gate** = the count of xUnit tests must
>   not decrease.
> - **Downstream / upstream** come from `flow.manifest.json`. The domain wrapper is
>   `DataProcessResult<T>` (XIIGen family) — **NOT** core `OperationResult<T>`.
> - The wave-assignment record lives in a decision artifact next to the plan /
>   `flow.manifest.json`, not in a core `SUBPROJECT-MAP.md`.

## Core Problem

A capability that should be shared infrastructure gets built as a per-flow
dependency instead. Later, five flows discover they all need the same capability
and each implements it differently. The wave protocol prevents this: it
classifies every capability before planning, identifies infrastructure candidates
(N > 2 dependents), and assigns the right execution model per wave.

---

## When to Invoke

- Before any flow phase is planned (wave assignment is an input to planning)
- When a new shared capability is identified that multiple flows might need
- When `parallel-execution-coordinator-SKILL.md` asks which wave a phase belongs to
- Before assigning the test gate type (absolute vs delta) for a new phase

---

## Section 1 — Wave Structure

| Wave | Type | Execution | Gate model |
|---|---|---|---|
| FOUNDATION | Shared contract/fabric used by many flows | Sequential, must complete before Wave 0 | Absolute (`dotnet test` 0 failures) |
| WAVE 0 | First flow capabilities | Sequential, one at a time | Absolute |
| WAVE 1 | Second-generation capabilities | Sequential, some pattern data available | Absolute |
| WAVE 2+ | Mature capabilities | May run in parallel (flows are runtime-independent) | Delta (test count must not decrease) |

---

## Section 2 — Rule 1: Infrastructure Test

A capability belongs at FOUNDATION when:

> **More than 2 other planned flows depend on this capability's output/contract.**

Apply literally. Count downstream dependents (from `flow.manifest.json`):

```
If dependents > 2: FOUNDATION (shared — build before Wave 0)
If dependents ≤ 2: Wave 0 or later (not infrastructure)

Examples:
  Shared tenant/correlation contracts (Xiigen.Kernel):
    Used by: every flow's Api → N > 2 → FOUNDATION

  A registration-only onboarding contract:
    Used by: FLOW-01 only → 1 dependent → NOT FOUNDATION → Wave 0 or 1
```

**Edge case: expected vs current dependents.** A contract with 1 dependent today
but expected to reach 6 should be FOUNDATION based on intended architecture.
Reassignment is expensive (requires replanning). Plan for the intended state.

**Wrong dependency direction:** a user-facing flow does not become FOUNDATION just
because another flow shares its patterns. The rule is directional: if FLOW-A needs
FLOW-B's runtime output to function, FLOW-B is upstream. If FLOW-A only reuses
FLOW-B's patterns without depending on its output, they are peers at the same wave.

---

## Section 3 — Rule 2: Sequential vs Parallel (Wave 2+ only)

**Run in parallel when:**
- ≥3 flows of prior waves have completed (pattern data available)
- No cross-flow dependency between the parallel flows (`flow.manifest.json`)
- The delta gate is valid (no pre-existing test failures)
- Review bandwidth exists for simultaneous completion reviews

**Run sequentially when:**
- Fewer than 3 prior flows completed
- Any cross-flow dependency exists (A's runtime output feeds B's implementation)
- Pre-existing test failures (delta gate unreliable until absolute is clean)

Flows are naturally runtime-independent, which makes Wave 2+ parallel realistic —
but only after the patterns and a clean baseline exist.

---

## Section 4 — Rule 3: Gate Type Selection

```
FOUNDATION:          Absolute gate (failures == 0) — any failure blocks ALL downstream flows
WAVE 0, WAVE 1:      Absolute gate — establishing the baseline; no prior history for delta
WAVE 2+ sequential:  Absolute gate — sequential is safe
WAVE 2+ parallel:    Delta gate (test count must not decrease) — absolute total unknowable mid-wave
```

**Delta gate definition:** `testCount at phase end ≥ testCount at phase start`. A
delta gate does NOT pass if tests were deleted to remove failures.

**Delta gate risk:** it can pass with 3 new failures if 4 new tests were also
added. That is why it is restricted to Wave 2+ parallel where the baseline is
stable and `parallel-execution-coordinator-SKILL.md`'s CAS semantics prevent races.

---

## Section 5 — Wave Assignment Record

For every flow and shared capability, record the assignment in a decision artifact
next to the plan (reference it from `flow.manifest.json`):

```markdown
## Wave Assignment — [FlowName or Capability]

**Question:** Should [X] be FOUNDATION or Wave N?
**Downstream count:** [N flows depend on this]
**Reasoning:** [why this wave, not another]
**Principle applied:** [N > 2 → FOUNDATION / sequential because no pattern data / etc.]
**Gate type:** [ABSOLUTE / DELTA]
**Outcome:** [FOUNDATION / WAVE 0 / WAVE 1 / WAVE 2 parallel]

**Prerequisites (must be complete before this flow starts):**
  - [FOUNDATION capability A]
  - [upstream FLOW-MM contract B]
```

---

## Section 6 — Prerequisite Chain Format

Every flow declares its prerequisites explicitly (mirrors `flow.manifest.json`):

```json
{
  "flowId": "FLOW-01-user-registration",
  "wave": 1,
  "hardPrerequisites": ["shared-tenant-correlation-contracts"],
  "softPrerequisites": [],
  "downstream": ["FLOW-02-profile-enrichment"]
}
```

A flow cannot start if any `hardPrerequisite` is not complete. `softPrerequisites`
are recommended but not blocking — document why the soft dependency exists but is
not hard.

---

## Anti-Patterns

1. **Assigning a wave without counting downstream dependents** — "this seems like infrastructure" is not the protocol. Count them. N > 2 → FOUNDATION.
2. **Delaying infrastructure capabilities to keep the critical path short** — infrastructure delays become compound blockers; Wave 0 flows discovering a missing FOUNDATION mid-implementation is the most expensive rework.
3. **Wave 2+ parallel without checking cross-flow dependencies** — if A's runtime output feeds B's implementation, they are sequential. Parallel is safe only when genuinely independent.
4. **Using delta gate for Wave 0 or 1** — no baseline exists for delta comparison early. Always absolute for FOUNDATION, Wave 0, Wave 1.
5. **Assigning current wave, not intended wave** — plan for the intended architecture; reassignment after implementation is expensive.

---

## Integration

- `parallel-execution-coordinator-SKILL.md` uses the wave assignment to determine gate model (absolute vs delta).
- `prerequisite-chain-SKILL.md` verifies hard prerequisites are complete before a flow starts.
- `flow.manifest.json` is the canonical record of current wave assignments and cross-flow edges.
- `planning-session-startup-SKILL.md` baseline question checks gate model against wave assignment.
