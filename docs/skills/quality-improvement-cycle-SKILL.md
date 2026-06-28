# quality-improvement-cycle-SKILL.md — Score-band cycle: fix, re-evaluate, promote or escalate (xiigen flows)
## v1.0: ported from llm_mvp_core quality-improvement-cycle → .NET 10 xUnit + Angular 21 (ng test/Playwright); no DPO machinery

## When to load
Load when a per-flow capability or screen scores below its threshold on first
evaluation and must be improved iteratively.
Load when `dotnet test` / `ng test` passes structurally but the behavioral output
or rendered result does not meet the expected standard.

Do NOT load when the evaluation score is already at or above threshold.
Do NOT load when `systematic-debugging-SKILL.md` has identified an architectural
root cause (architectural fixes come before iteration cycles).

> Scope note for flows: the core skill drives a DPO (Direct Preference
> Optimization) training loop (`DpoTrainingPair`, `ContinuousLearningPipeline`).
> That machinery is **G12 / `llm_mvp_core`** and is **not ported**. In flows,
> "evaluation" means the xUnit suite (server) and `ng test` (Vitest) / Playwright
> (client); "fix" means changing per-flow source, not authoring DPO pairs.

---

## The problem this skill solves

A capability or screen that scores below its threshold on first evaluation must
be improved iteratively. Repeating the same evaluation without changing anything
produces the same score. This skill defines the exact procedure: classify the
score band, identify what failed, make a targeted change, re-evaluate, and either
promote or escalate — with a hard cap of 3 cycles.

---

## Section 1 — Score Band Protocol

Before starting a cycle, classify the score into a band.

| Score | Band | Action |
|-------|------|--------|
| ≥ threshold | PASS | Accept / mark the slice done. No cycle needed. |
| 70–84% of threshold | NEAR-PASS | Run one targeted improvement. Re-evaluate. |
| 60–69% of threshold | PATCH | Run up to 2 targeted improvements. Escalate after 2. |
| < 60% of threshold | STOP | Do NOT run cycles. Escalate. Root cause is architectural. |

Record `cycle_[N]_score = [value]` before each cycle.

Maximum cycle count per capability/screen is **3**. After 3 cycles without
reaching threshold: escalate. Three cycles without improvement = the problem is
in the design, not the inputs.

For UI scores, the score is the `ui-examination-round-SKILL.md` cell score (read
the screen/PNG), not a grep count. For server capabilities, the score is the
xUnit pass-rate / assertion outcome against the expected behavior.

---

## Section 2 — The 9-Step Cycle

### Step 1 — Confirm the Score Band
Read the evaluation output, extract the score, classify into the band table.
If STOP or PASS, do not continue.

### Step 2 — Read Failing Assertions from Evaluation Output

Server (xUnit):
```
[FAIL] RsvpServiceTests.SubmitTwice_Replays
  Expected: result.IsSuccess == true, Data.Status == "GOING"
  Actual:   result.IsSuccess == true, Data.Status == "GOING_DUPLICATE"
```

Client (Vitest / Playwright):
```
[FAIL] member can RSVP and sees a confirmation
  Expected: getByText(/you.?re going/i) visible
  Actual:   no confirmation text rendered
```

Extract: failing assertion ids, the exact symptom, and which component/screen is
failing.

### Step 3 — Classify the Failure Type

| Failure type | Symptom | Fix direction |
|---|---|---|
| Missing server case | Same wrong `DataProcessResult<T>` every cycle | Fix the `.Application`/`.Infrastructure` logic + add the asserting test |
| Missing UI state | Screen doesn't render the right state | Fix the component template/binding + add the Vitest/Playwright assertion |
| Wrong configuration value | Output has wrong constant, not wrong structure | Fix config/appsettings — not logic |
| Architectural issue | Same score after 2 cycles | Escalate — not an input issue |

### Step 4 — Read Current State
```bash
# server: confirm which assertions pass/fail
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/... --filter "FullyQualifiedName~<ClassName>"
# client: confirm which specs pass/fail
cd flows/FLOW-NN-<slug>/client/angular && npx vitest run   # or: npx playwright test
```

### Step 5 — Generate the Fix

For a **server behavior gap** — fix the service, then assert the specific output:
```csharp
// Fix RsvpService so a replay returns the original result, then assert it
[Fact]
public async Task SubmitTwice_Replays_ReturnsOriginalStatus()
{
    // Arrange: real in-memory fabric + service
    // Act: call SubmitAsync twice with the same idempotency key
    // Assert: second result.Data?.Status == "GOING" (not a duplicate marker)
}
```

For a **UI state gap** — fix the template/binding, then assert the visible result:
```ts
test('member sees confirmation after RSVP', async ({ page }) => {
  await page.goto('/?role=tenant_user&state=rsvp');
  await page.getByRole('button', { name: /rsvp/i }).click();
  await expect(page.getByText(/you.?re going/i)).toBeVisible(); // the fixed visible state
});
```

For a **wrong configuration value** — fix config, re-run; if it fails again the
issue is in logic, not config. Do NOT author DPO pairs (that is `llm_mvp_core`).

### Step 6 — Apply the Fix
Write the fix to the correct file (server `.Application`/`.Infrastructure`, or the
client component). Keep `DataProcessResult<T>` return types; comment new lines.

### Step 7 — Record What Changed
```
cycle_[N]_version: "1.[N].0"
cycle_[N]_fix: "[short description of what was changed]"
```
Do not re-evaluate without recording the change. Same fix + same evaluation =
same result.

### Step 8 — Re-Run Evaluation
```bash
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/...   # record new count
cd flows/FLOW-NN-<slug>/client/angular && npx vitest run               # and/or playwright
```
Read the output. Re-apply the band classification.

### Step 9 — Compare and Decide

| Result | Action |
|--------|--------|
| Score ≥ threshold | PASS — accept the slice. |
| Improved but below threshold, cycle count < 3 | Record cycle. Return to Step 2. |
| Score same (±2%) | PLATEAU — not an input issue. Escalate. |
| Score worse | REGRESSION — escalate immediately; note the likely cause. |
| Cycle count = 3, still below threshold | ESCALATE — 3 is the maximum. |

---

## Section 3 — Cycle Log

Maintain per capability/screen:

```
[FLOW-04 RsvpView] Cycle Log:
  Cycle 1: score=68%, failing=[SubmitTwice_Replays], fix=replay returns original status
  Cycle 2: score=80%, failing=[confirmation not visible], fix=render visible confirmation
  Cycle 3: score=92%, failing=[], PASSED → slice accepted
```

---

## Section 4 — Escalation Report Format

When escalation is required (score < 60%, plateau, or 3 cycles without promotion):

```
ESCALATION REQUIRED
Capability/Screen: [FLOW-NN / name]
Final score:       [value] (cycle [N])
Lowest component:  [which assertion/cell has the lowest pass rate]
Still failing:     [assertion/cell] — [N] cycles did not fix this
Root cause assessment: [architectural / state design / contract]
Fixes tried:       [list of changes made]
Last evaluation output: [attach dotnet test / vitest / playwright output]
Recommended action: [specific next step — redesign component / change contract / change architecture]
```

---

## Anti-patterns

1. **"Run another cycle without changing anything"** — same inputs → same
   outputs. A cycle without a fix is waste.

2. **"Fix the output, not the source"** — fix the per-flow source (service or
   component); the next evaluation will fail again otherwise.

3. **"Continue past 3 cycles"** — 3 cycles without promotion means the design is
   wrong, not the specific change. Escalate for architectural review.

4. **"Score < 60% → still run cycles"** — below 60% is architectural. No targeted
   change fixes a broken design.

5. **"Author a DPO pair to fix it"** — DPO/training is `llm_mvp_core` (G12). In
   flows, fix the source and assert the behavior.

---

## Integration

- `improvement-measurement-SKILL.md`: consumes the before/after score values
  from this skill's Cycle Log.
- `systematic-debugging-SKILL.md`: handles the escalation path (architectural
  root cause).
- `ui-examination-round-SKILL.md`: provides the UI cell scores this cycle uses.
- `coverage-completeness-gate-SKILL.md`: a promotion/acceptance claim must come
  with Cycle Log evidence.

---

## Stack adaptation notes (xiigen flows)

- "Evaluation" = `dotnet test` (xUnit, net10.0) for server + `ng test` (Vitest) /
  Playwright for client. "Fix" = per-flow source change.
- DPO / `DpoTrainingPair` / `ContinuousLearningPipeline` machinery is **not**
  ported — it stays in `llm_mvp_core` (G12). This skill is the universal
  band/9-step/max-3/Cycle-Log discipline only.
