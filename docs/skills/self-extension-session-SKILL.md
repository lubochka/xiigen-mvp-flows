# Self-Extension Session

> **Stack adaptation (xiigen flows).** Universal SELF-EXTENSION governance chain (two STOP
> gates) ported from `llm_mvp_core/docs/skills/self-extension-session-SKILL.md`. Adapted to this
> repo:
> - **Step 3 build** = `dotnet build Xiigen.Flows.slnx` + `dotnet test` (xUnit) for the affected
>   `Xiigen.Flow.<Name>.Tests`; for client gaps, the Angular 21 build + Playwright;
> - **Step 4 integrity** checks `.cs` (per-flow `server/`) and `.ts` (per-flow
>   `client/angular/src`): docs updated, tests added, Fabric First / `DataProcessResult<T>`
>   (XIIGen wrapper, not core `OperationResult<T>`) / tenant-scope respected, and no new gaps;
> - **Step 5 training-data-gap-audit is N/A** for this repo: per R5/G12 there is no local DPO /
>   trainable unit; the shared model is consumed via `.xiigen/wiki-domain/model-manifest.json`.
>   Record `training_data_gap_audit = N/A (model consumed via manifest; no local training
>   signal)` and complete the other steps in full;
> - the proposal scope level (Step 2) is assigned via `solution-scope-gate-SKILL.md` and lands
>   in a flow layer (`Application` capability / `Infrastructure` provider / `Api` DI / Angular
>   standalone client);
> - **not initiated autonomously** — closing a gap requires explicit direction from Luba.

## Core Problem

When a capability gap is identified — a missing interface, an absent provider,
a named check that does not exist — the natural response is to fill it
immediately during the current session. That shortcut bypasses the gates that
prevent filling a gap with the wrong solution. A SELF-EXTENSION session is a
governed session type specifically for closing capability gaps: it reads the
current state, classifies the gap, proposes a resolution, requires explicit
approval before building, verifies closure, and records the (N/A here) training
audit.

---

## When to Invoke

1. A capability-state read detects a MISSING capability that blocks a planned flow slice
2. Gap-to-proposal work classifies a gap requiring a new interface, provider, or named check
3. A flow phase discovers a missing dependency during infrastructure discovery
4. A promotion/acceptance gate fails because a required piece is absent

**Not initiated autonomously.** The detection is systematic; initiating
a SELF-EXTENSION session requires explicit direction from Luba.

---

## Section 1 — The Governance Chain

```
START

  Step 1: Capability-state read
    Read the current capability state — what is ACTIVE, STUB, MISSING across the
    affected flow layers and contracts.

  Step 2: Gap-to-proposal
    Classify the gap (which flow layer is wrong: Application capability vs
    Infrastructure provider vs Api wiring vs Angular client).
    Produce a resolution proposal (Level 1–5 from solution-scope-gate-SKILL.md).

  ⛔ STOP — present proposal — await Luba explicit "yes" before building

  Step 3: Execute build work
    Fabric interface only / DI registration → treat as MAINTENANCE (no new session)
    New capability class / component → use a PLANNING + EXECUTOR slice
    Config / appsettings / app.config.ts FREEDOM value → MAINTENANCE
    Verify: dotnet build Xiigen.Flows.slnx + dotnet test <Xiigen.Flow.<Name>.Tests>
            (Angular 21 build + Playwright for client gaps)

  Step 4: Implementation integrity
    Verify closure: 4 checks (docs / tests / DNA-and-Fabric-First-and-tenant-scope / no new gaps)
    Produce a JSON integrity report.

  Step 5: Training-data-gap audit
    N/A for this repo — the shared model is consumed via
    .xiigen/wiki-domain/model-manifest.json; there is no local DPO/training signal.
    Record: training_data_gap_audit = N/A (model consumed via manifest).

  ⛔ STOP — present integrity report (+ N/A audit note) — await Luba sign-off

END
```

---

## Section 2 — Session Type vs Other Types

| Session type | Scope | Gates |
|---|---|---|
| EXECUTOR | Write code for a known plan | DNA/Fabric check, Gate B per phase |
| PLANNING | Design new capability | Plan gates, self-questioning |
| ARCHITECT | Structural analysis and synthesis | Reconnaissance gate, convergence |
| REVIEW | Audit a plan or code | Plan review, multi-reviewer, audit protocol |
| DEBUG | Fix a confirmed bug | Bug taxonomy, failing tests |
| **SELF-EXTENSION** | **Close a capability gap** | **Steps 1→2→STOP→3→4→5→STOP** |

The critical distinction from MAINTENANCE: MAINTENANCE applies when the
fix is known and scoped. SELF-EXTENSION applies when the gap is identified
but the resolution is unknown — the proposal step determines the resolution.
Treating SELF-EXTENSION as MAINTENANCE skips the proposal step and the
first STOP gate.

---

## Section 3 — What SELF-EXTENSION Is Not

```
❌ Building a new fabric interface mid-EXECUTOR slice without a proposal
   → A missing interface discovered during execution is a gap.
   → Stop the executor slice. Write the gap. Start a SELF-EXTENSION session.

❌ Adding a named validation check to a flow without checking if it changes
   existing behavior
   → A check that fires on existing flow paths changes their outcomes.
   → Step 4 integrity check must verify no regression (dotnet test green).

❌ Treating all MISSING capabilities as gaps requiring SELF-EXTENSION
   → A MISSING capability that was never needed is not a gap.
   → A MISSING capability that blocks a current flow slice is a gap.
   → The trigger is "blocks a slice" not "appears in the MISSING list".

❌ Building a local trainer to "close" a learning gap
   → R5/G12: the shared model is consumed via manifest, not trained here.
   → Step 5 is N/A; do not invent a local training pipeline.
```

---

## Section 4 — Why Step 5 Is N/A Here (and what replaces it)

In `llm_mvp_core`, Step 5 audits DPO triples produced during the gap window. In
this flows repo there is no local DPO/trainable unit (R5/G12). The shared
trainable model is consumed through `.xiigen/wiki-domain/model-manifest.json`
plus a locator, and any local adaptive leg is the user side.

Therefore Step 5 records `training_data_gap_audit = N/A (model consumed via
manifest; no local training signal)`. If the gap touched the *consumption* path
(e.g., a wrong manifest reference or a stale locator), that is a normal Step 4
integrity item — verify the consumed model still loads (a fresh-load probe) — not
a training-data audit.

---

## Anti-Patterns

1. **"I'll fix it inline and note it at the end"** — inline fixes without
   the proposal step bypass the STOP gate and Luba approval. The STOP gate
   exists to prevent wrong-level fixes (new interface where a DI change sufficed).

2. **"The gap is small — skip the integrity check"** — Step 4 is not optional.
   A one-line fix can still break tenant-scope or Fabric First; the integrity
   check (dotnet test green, no new gaps) is mandatory.

3. **"SELF-EXTENSION doesn't need a capability-state read"** — read the current
   capability state (Step 1) before proposing (Step 2). A proposal without
   evidence of the current state is an essay.

---

## Integration

- the capability-state read provides the Step 1 snapshot
- gap-to-proposal work classifies the gap and determines resolution level
- `solution-scope-gate-SKILL.md` governs the proposal level (Level 1–5) in Step 2
- the implementation-integrity check provides Step 4 closure verification
- the session-mode declaration marks SELF-EXTENSION as a distinct session type before Step 1
