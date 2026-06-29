# parallel-execution-coordinator-SKILL.md — Three behavior switches when running flows in parallel

> **Stack adaptation (xiigen flows).** Universal *parallel execution coordinator*
> ported from `llm_mvp_core/docs/skills/parallel-execution-coordinator-SKILL.md`
> (G08). Greenfield here — no prior author version (and note: core's neighbour was
> named `parallel-wave-coordinator`; this file restores the canonical
> `parallel-execution-coordinator` name). The universal core kept: when parallel
> mode is active, switch three behaviors — (1) pre-allocated artifact ranges
> instead of live queries, (2) delta gate instead of absolute baseline, (3)
> compare-and-swap (CAS) lifecycle writes instead of blind writes — plus three
> session-start questions. The stack details are adapted:
> - Parallelism is natural per `FLOW-NN-*` (flows are runtime-independent).
> - **Delta gate** = the count of xUnit tests in the target flow's
>   `Xiigen.Flow.<Name>.Tests` must not decrease (`dotnet test --filter`,
>   `net10.0`).
> - **Pre-allocated ranges** govern artifact numbering recorded in
>   `flow.manifest.json` / the flow's `docs/`.
> - **Prerequisite Gate B** is read from the flow manifest / the upstream flow's
>   state. State / CAS lifecycle writes go to the state artifact next to the plan,
>   not a core `STATE.json` / `SUBPROJECT-MAP.md`. The domain wrapper is
>   `DataProcessResult<T>` (XIIGen family) — **NOT** core `OperationResult<T>`.

## Core Problem

Sequential and parallel flow execution look identical from the outside — both have
Phase A through Phase N, both produce Gate B completions. The difference is
entirely in three behaviors that must switch when parallel execution is active.
Without this skill, a parallel executor races for artifact numbers, applies stale
baseline checks, and overwrites concurrent lifecycle state.

---

## When to Invoke

- When a session begins with a parallel-wave marker set in the flow state artifact
- Before Phase A of any parallel flow execution
- When multiple flow phases are planned to run concurrently
- When any gate check runs in a parallel execution session

---

## Section 1 — Three Questions at Session Start

Read the flow state artifact. Answer all three before any Phase A action.

### Question 1: Am I in parallel mode?

```
parallelWave: null → SEQUENTIAL mode
  Use absolute gate, live artifact boundaries, blind lifecycle write. This skill is dormant.

parallelWave: N (any number) → PARALLEL mode
  Switch all three behaviors in Section 2. This skill is active.
```

### Question 2: What are my artifact ranges?

```
PARALLEL mode only:

1. Read: flow.manifest.json / the flow's docs → pre-allocated artifact ranges for this flow

2. If the flow has no pre-allocated range entry:
   STOP — pre-allocation planning has not run for this wave.
   Do NOT query live docs. Do NOT estimate ranges.
   Report: "[FlowName] has no pre-allocated artifact ranges. Run pre-allocation planning first."

3. If an entry exists: use those test count / capability / phase ranges exclusively.
   Do NOT verify against live docs. Ranges are immutable once allocated.

4. If a collision is detected during Phase A (artifact id already claimed):
   STOP — report conflict. Do not overwrite. Await instruction.
```

### Question 3: Have I verified the prerequisite flow?

```
PARALLEL mode only (Phase A, before any other action):

Read prerequisiteFlow from the state artifact / flow.manifest.json.
If present: verify that flow's Gate B shows PASS before proceeding.
If its Gate B has not passed:
   STOP — prerequisite flow phase is incomplete. Report which flow and phase blocks. Do not proceed.
```

---

## Section 2 — Three Behavior Switches in Parallel Mode

### Switch 1: Gate model (absolute → delta)

```
SEQUENTIAL (parallelWave: null):
  dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests
  Assert: total passing ≥ {absolute_baseline}

PARALLEL (parallelWave: N):
  dotnet test --filter "FullyQualifiedName~<Name>"
  Assert: {flow_tests} shows {expected_delta} new tests passing
  Total baseline is NOT checked here — verified post-merge.
```

Why: in parallel mode, other flow phases may not yet be merged. The absolute total
is unknowable until all parallel phases complete and merge.

### Switch 2: Artifact number assignment (live query → pre-allocated)

```
SEQUENTIAL (parallelWave: null):
  Read the decision artifact → last D-NN + 1 (live query, safe)

PARALLEL (parallelWave: N):
  Read the pre-allocated range for this flow from flow.manifest.json / its docs
  Use the next number from the range only — do NOT query live docs for "last used"
  (another parallel instance may have claimed numbers from the live range)
```

### Switch 3: Lifecycle status write (blind write → CAS)

```
SEQUENTIAL (parallelWave: null):
  Write flow status → ACTIVE  (blind write — no concurrent writers possible)

PARALLEL (parallelWave: N):
  Write flow status → ACTIVE, expected current status = NOT_STARTED (CAS)
  If CAS fails (another instance claimed it):
   STOP — conflict detected. Report and await instruction.
  If CAS succeeds: continue.
```

---

## Section 3 — Execution Log in Parallel Mode

In parallel mode, log the delta, not the absolute:

```json
{
  "parallelWave": 2,
  "waveBaselineEntry": 312,
  "testDelta": 9,
  "testBaselineExit": null
}
```

`testBaselineExit` is null in parallel mode — the final total is unknown until all
parallel phases complete. `testDelta` is the count this phase added.

---

## Section 4 — Worked Examples

**Correct parallel execution sequence:**

```
1. Read state artifact → parallelWave = 2 → PARALLEL mode active
2. Check prerequisite → FLOW-00-bundle-activation phase → Gate B = PASS → proceed
3. Read pre-allocated ranges → use decision numbers D-15..D-18 for this flow
4. CAS write: flow status NOT_STARTED → ACTIVE → { success: true } → continue
5. Implement phase, run tests
6. Gate check: dotnet test --filter "FullyQualifiedName~UserRegistration" → +9 new tests pass
7. Record testDelta = 9, testBaselineExit = null
```

**Wrong — parallel mode using sequential behaviors:**

```
1. Read state artifact → parallelWave = 2
2. Query live decision artifact → returns D-14 (last used)
   But another parallel instance already claimed D-15 and D-16
3. Register D-15 → COLLISION → decision log corrupted, duplicate D-15 entries
```

---

## Anti-Patterns

1. **"In parallel mode, running absolute gate check"** — absolute baseline is unknowable in parallel mode. Use delta gate only.
2. **"Reading live docs for artifact numbers in parallel mode"** — another instance may read the same docs simultaneously. Use pre-allocated ranges.
3. **"Blind lifecycle status write in parallel mode"** — CAS is mandatory; a blind write overwrites another instance's claim.
4. **"Proceeding when pre-allocated ranges are missing"** — STOP. Missing pre-allocation means the planning session that assigns ranges has not run. Do not estimate.

---

## Integration

- `flow.manifest.json` / the flow's `docs/` store the pre-allocated ranges for parallel execution phases.
- Gate B per phase switches to delta measurement in parallel mode.
- `wave-assignment-SKILL.md` determines whether a flow is eligible for Wave 2+ parallel and the gate model.
- `planning-session-startup-SKILL.md` reads the state artifact to detect `parallelWave`.
