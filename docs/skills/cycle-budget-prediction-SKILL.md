# cycle-budget-prediction-SKILL.md — Deterministic cycle budget before implementation

> **Stack adaptation (xiigen flows).** Universal budget formula ported from
> `llm_mvp_core/docs/skills/cycle-budget-prediction-SKILL.md`. Adapted to this repo:
> - Gate B in every budget card is a flows command — per-flow
>   `dotnet test --filter "X" -> N passed` (xUnit, `net10.0`) or `ng test`/`npx playwright test`;
>   verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - "structurally identical capability exists" = a comparable flow Application service /
>   Infrastructure adapter / Angular 21 component already in the repo;
> - first-occurrence is judged against the **inventory of existing flows/projects**, not a model
>   registry;
> - the inversion case uses the family wrapper boundary: a capability that must NOT return
>   `DataProcessResult<T>` where the family default requires it (rare) — NOT core
>   `OperationResult<T>`;
> - core ML/`DomainModelRegistry`/DPO sources are core-only (G12) and not used here.

## When to load
- Before writing any phase plan that includes new capability work for a flow.
- When a plan review encounters an unbudgeted phase.
- When the retrospective shows MAE (Mean Absolute Error — average gap between predicted and
  actual cycles) > 1.0 for the last batch.
- When a capability class appears for the first time in the repo.

## Section 1 — Cycle budget formula

```
cycleBudget = archetypeBaseline
            + firstOccurrencePenalty
            + noveltySum
            − clarityDiscount
            + inversionBonus
```

| Component | Value | When applied |
|---|---|---|
| `archetypeBaseline` | 1 | Always — every capability class starts here |
| `firstOccurrencePenalty` | +1 | The capability class has never been built in this repo before |
| `noveltySum` | +0.5 per new pattern | Each new contract/interface, behavioural invariant, DI pattern, or wrapper-boundary crossing introduced for the first time |
| `clarityDiscount` | −1 | The capability is derivable from existing rules alone — no novel judgment |
| `inversionBonus` | +1 | Correct behaviour inverts a default (rare — e.g. must NOT return `DataProcessResult<T>` at a boundary that normally requires it) |

**Maximum budget: 4** (all penalties, zero discount). **Minimum budget: 1** (clarity discount,
no penalties). A budget > 4 means split the capability into smaller pieces first.

## Section 2 — Component derivation rules

**archetypeBaseline = 1** — every capability class gets one cycle (one complete implementation
attempt with a Gate B result), regardless of complexity.

**firstOccurrencePenalty (+1)** — add when the capability does not yet exist in any flow (no
prior flow built this class). Do NOT add when a prior flow built the same class and this phase
only extends it.

**noveltySum (+0.5 per new named pattern)** — a specific named artifact not yet in the repo.
Count each if new:
- new contract/interface (`I<Name>`) not yet in any flow's `server/`;
- new behavioural invariant (a rule not in the repo's decisions/contracts);
- new DI registration pattern (different from existing `Program.cs` registrations);
- new `DataProcessResult<T>` / `MicroserviceBase<TSelf>` boundary not used before.

Count 0.5 per item; round total to nearest 0.5.

**clarityDiscount (−1)** — subtract when ALL are true: every method signature is already
defined in a verified contract; the algorithm is derivable from existing rules; no new
behavioural invariants; and at least one structurally identical capability already exists
(e.g. a sibling flow's service of the same shape). If any condition is false: no discount.

**inversionBonus (+1)** — only when correct behaviour requires explicitly overriding a default
at a specific boundary. Rare; document the override before adding the bonus.

## Section 3 — Budget card format

```markdown
## Cycle Budget — [CapabilityClass] (FLOW-NN-<slug>)

archetypeBaseline:        1
firstOccurrencePenalty:   +N  [reason: "first time I<Name> built in the repo" / "exists in FLOW-XX — no penalty"]
noveltySum:               +N  [items: I<Name> (+0.5), new invariant (+0.5), ...]
clarityDiscount:          −N  [reason: "all methods defined; structurally identical to FLOW-XX <Name>Service" / "n/a"]
inversionBonus:           +N  [reason: "must override default wrapper at boundary — documented" / "n/a"]

Predicted cycle budget:   N

Gate B for this budget:
  "Phase [X] completes in ≤ N cycles with ≥ 1 test passing per cycle:
   dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --filter '<X>' -> N passed"
```

## Section 4 — Budget-to-phase wiring

```markdown
Phase [N] — [CapabilityClass]
Cycle budget: [predicted] cycles (cycle-budget-prediction-SKILL.md)
Gate B: [exact passing condition — per-flow dotnet test count / ng test / playwright]
Retrospective feed: actual vs predicted recorded at phase end for MAE tracking
```

## Section 5 — MAE recalibration trigger

If the retrospective shows MAE > 1.0 for the last batch:
1. List all phases where `|predicted − actual| > 1`.
2. For each, identify which component was wrong (baseline over-estimated? noveltySum missed a
   new pattern?).
3. Adjust component weights for the next batch (record consistent penalty errors; add a
   de-duplication rule if noveltySum items were double-counted).
4. Do NOT adjust baseline below 1.

## Anti-patterns

1. "Simple capability, budget = 0" — archetypeBaseline is always 1.
2. "Set budget = 5 to be safe" — budget caps at 4; a budget > 4 means split the capability.
3. "No Gate B needed" — the card requires a Gate B; a phase with no Gate B cannot feed MAE.
4. "noveltySum = 0 because all interfaces exist" — check behavioural invariants too; a new rule
   is a new named pattern even if interfaces are pre-existing.
5. "MAE > 1.0 but close enough" — MAE > 1.0 is a hard recalibration trigger; there is no "close
   enough".
