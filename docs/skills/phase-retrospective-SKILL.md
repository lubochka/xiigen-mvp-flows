# phase-retrospective-SKILL.md — 4-dimension retro hard-gate before the next batch (xiigen flows)
## v1.0: ported from llm_mvp_core phase-retrospective → .NET 10 (dotnet build/test) + Angular 21 (ng build/Playwright)

## When to load
Load after the last phase in a sequential batch completes (Gate B passes for all
phases in the batch).
Load before the next batch of parallel phases begins.
This is a **hard gate**: the next batch does not start until the retrospective is
recorded.

```
Sequential batch:  Phase 1 → Phase 2 → Phase 3
                   ↓
                   Retrospective runs here
                   ↓
Next batch begins: Phase 4, Phase 5, Phase 6 (parallel)
```

---

## The problem this skill solves

A set of phases that completes successfully does not automatically produce
calibrated predictions for the next set. Without a retrospective, cycle-count
predictions stay wrong, capability transfer is unverified, and the next wave
starts on stale assumptions. This skill defines a hard gate: measure before
proceeding, not after.

---

## Section 1 — Four Measurement Dimensions

### Dimension 1 — Cycle Count Accuracy

For each phase, compare predicted cycles to actual cycles:

```
Phase 1: predicted=2 cycles, actual=3 cycles
Phase 2: predicted=2 cycles, actual=1 cycle
Phase 3: predicted=1 cycle, actual=1 cycle
```

MAE (Mean Absolute Error):
```
errors = [|predicted - actual|] for each phase
MAE = sum(errors) / count(phases)
```

**Threshold:** MAE > 1.0 → recalibrate cycle predictions before the next batch.

Common causes in flows:
- Per-flow infrastructure (`.Infrastructure` fabric, DI wiring in `.Api`)
  underestimated.
- Test categories (`e2e-test-matrix-SKILL.md`) surfacing more edge cases than
  predicted.
- Angular client work (Playwright config, RTL, role/state matrix) adding
  unexpected setup.

### Dimension 2 — Pattern Transfer Rate

Count how many reusable patterns/capabilities were promoted to a stable/proven
state since the last retrospective (e.g. a shared standalone component, a shared
service, or a per-flow capability accepted as proven).

```
Proven/promoted patterns added since last retrospective: [N]
Expected rate: approximately 1-2 per 3 completed phases.
```

**If count < expected:** the acceptance step of
`quality-improvement-cycle-SKILL.md` did not complete for some
capabilities/screens. Verify the Cycle Log shows promotion.

> Scope note: the core skill counts `DomainModelRegistry` entries with status
> PROVEN. That registry is **G12 / `llm_mvp_core`** and is **not** the flows data
> source. In flows, D2 is a general discipline measured over reusable
> components/services/capabilities accepted as proven — not over a trained-model
> registry.

### Dimension 3 — Behavioral Test Coverage

For each phase, check that every declared `e2e-test-matrix-SKILL.md` category has
at least one passing test (server xUnit and/or client Playwright):

```bash
# server categories (xUnit traits/filters)
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/... --filter "Category=HappyPath"
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/... --filter "Category=Idempotency"
# client UI categories (Playwright project/grep)
cd flows/FLOW-NN-<slug>/client/angular && npx playwright test --grep "@happy-path"
```

**Threshold:** any N/A category without justification in the phase plan is a gap;
any declared category with 0 passing tests is a completion failure.

### Dimension 4 — Gate B Adherence

For each phase, verify the Gate B condition was met **as stated** (not
approximated):

```
Phase 1 Gate B: "dotnet test --filter 'RsvpService' → 12 passed, 0 failed"
Actual: 12 passed, 0 failed → ADHERED
Phase 2 Gate B: "dotnet build → 0 errors"
Actual: 2 warnings, 0 errors → ADHERED (warnings are not errors)
Phase 3 Gate B: "ng build → 0 errors AND playwright happy-path → pass"
Actual: ng build 0 errors, playwright happy-path FAILED → NOT_ADHERED
```

**Threshold:** any NOT_ADHERED gate means the phase is not complete. The
retrospective notes it and the phase is re-opened before the next batch.

---

## Section 2 — Retrospective Record Format

```markdown
## Phase Retrospective — [SessionId] — Phases [N-M]

Covered phases: [Phase 1, Phase 2, Phase 3]
Date: [date]

### Dimension 1 — Cycle Count Accuracy
| Phase | Predicted | Actual | Error |
|---|---|---|---|
| Phase 1 | 2 | 3 | 1 |
| Phase 2 | 2 | 1 | 1 |
| Phase 3 | 1 | 1 | 0 |

MAE: 0.67 — within threshold (≤1.0)
Action: no recalibration needed

### Dimension 2 — Pattern Transfer Rate
Proven/promoted patterns since last retrospective: 2
Expected: 1-2 — PASS

### Dimension 3 — Behavioral Test Coverage
| Category | Tests | Status |
|---|---|---|
| Happy Path | 8 passed | PASS |
| Idempotency | 3 passed | PASS |
| UI Happy Path (Playwright) | 2 passed | PASS |
| Compensation | N/A — read-only capability | N/A (justified) |

### Dimension 4 — Gate B Adherence
| Phase | Gate B | Actual | Adhered |
|---|---|---|---|
| Phase 1 | 12 passed | 12 passed | YES |
| Phase 2 | 0 errors | 0 errors | YES |
| Phase 3 | ng build 0 + playwright pass | playwright failed | NO |

Gate B NOT_ADHERED: Phase 3 — must be resolved before next batch

### Calibration Adjustments
- Dimension 1: no change
- Dimension 4: Phase 3 gate not met — reopen Phase 3, resolve the failing UI spec

### Next Batch Clear to Proceed: NO
Blocking: Phase 3 Gate B not met
```

---

## Section 3 — Next Batch Gate

The next batch does not start until:

1. Retrospective record exists (completed).
2. All Gate B conditions are ADHERED (Dimension 4).
3. MAE ≤ 1.0 OR recalibration adjustments recorded (Dimension 1).
4. No pattern-transfer blocking issue (Dimension 2).

If `Next Batch Clear to Proceed: NO`:
- Fix blocking items.
- Re-run the affected verification (`dotnet test` / `ng build` / Playwright).
- Update the retrospective record.
- Flip the flag to YES.
- Do not proceed until the flag is YES.

---

## Anti-patterns

1. **"The phases all passed, skip the retrospective"** — passing phases do not
   produce calibration data automatically. The retrospective is the calibration
   step.

2. **"Gate B was approximately met"** — Gate B is binary. `ng build` clean but
   Playwright failing is not approximate success; it is a failed gate. Re-open.

3. **"We'll do the retrospective at the end of the project"** — retrospectives
   are per-batch, not per-project. Waiting means the next batch runs on stale
   calibration.

4. **"MAE 1.5 is close enough"** — MAE > 1.0 means predictions are consistently
   off by more than one cycle. Recalibrate now.

---

## Integration

- `quality-improvement-cycle-SKILL.md`: produces the cycle-count data for
  Dimension 1 and the promotion data for Dimension 2.
- `e2e-test-matrix-SKILL.md`: declares the categories verified in Dimension 3.
- `plan-self-validation-SKILL.md`: ensures phases have Gate B conditions to check
  in Dimension 4.
- `improvement-measurement-SKILL.md`: the per-phase observable deltas feed the
  retrospective's evidence.

---

## Stack adaptation notes (xiigen flows)

- D4 Gate B = `dotnet build` / `dotnet test` (net10.0, xUnit) per-flow **and**
  `ng build` (+ Playwright where UI is in scope).
- D3 categories come from xUnit traits and Playwright project/grep tags.
- D2 is a general discipline over reusable flows components/services/capabilities;
  the core `DomainModelRegistry`/PROVEN source stays in `llm_mvp_core` (G12).
- This skill trains no model; it is a calibration hard-gate only.
