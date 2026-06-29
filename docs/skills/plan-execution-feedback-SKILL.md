# Plan Execution Feedback

> **Stack adaptation (xiigen flows).** Universal discovery/test-delivery-rate + log-gate
> contract ported from `llm_mvp_core/docs/skills/plan-execution-feedback-SKILL.md`. A
> "write-time fix" here = a wrong assumption about a per-flow layer
> (`Api`/`Application`/`Infrastructure`), a fabric contract in `Xiigen.Kernel`, a DI
> registration in `AddXxxFlow()`, or a `DataProcessResult<T>` signature. Metrics live in
> `STATE-Pn.json`; tests are xUnit per-flow + Angular 21 Playwright. Thresholds 120% / 200%
> and the Human-Readable Log Gate are mandatory.

## Purpose

After each phase, compare planned assumptions vs actual discoveries. Track how often plans
required write-time fixes (assumptions that were wrong and had to be corrected while
writing/reading code).

Execution feedback also preserves user-origin source evidence: every user prompt,
feedback item, correction, rejection, user question, and assistant answer that changes
scope, gates, artifacts, evidence, or execution order must be appended to the active
plan's User Prompt Source Ledger before the next phase is planned or reported complete.

Execution feedback must satisfy the Human-Readable Plan/Execution Log Gate. Section A
cannot be only machine rows: it must state the human-readable stage, current plan step,
what changed, what remains, claims opened/closed, and why the status is true.

When discovery rate consistently exceeds 120%, the planning process is under-investing in
infrastructure discovery (Gate 0).

---

## Metrics

```
Discovery rate     = (write-time fixes actual) / (write-time fixes planned) × 100%
Test delivery rate = (tests delivered) / (tests planned) × 100%
```

A "write-time fix" is any plan assumption that turned out to be wrong and required a
correction when actually writing or reading the `.cs` files.

---

## Interpretation Table

| Discovery Rate | Signal | Required Action for Next Plan |
|---------------|--------|------------------------------|
| ≤ 120% | Normal — planning was accurate | Proceed with standard Gate 0 |
| 121–200% | Gate 0 under-invested | Add explicit `path:line` verification to every plan element |
| > 200% | Plan assumptions are guesses | Do NOT write code without reading every referenced file first |

---

## Session End Measurement

After each phase, record in `STATE-Pn.json`:

```json
{
  "phase": "P2",
  "status": "COMPLETE",
  "planExecutionFeedback": {
    "writetimeFixesPlanned": 2,
    "writetimeFixesActual": 5,
    "discoveryRate": "250%",
    "signal": "GATE_0_UNDER_INVESTED",
    "examples": [
      "Assumed RegisterAsync returned a bare outcome — it returns DataProcessResult<UserRegistrationOutcome>",
      "Assumed the service was registered in Program.cs — it is registered in AddUserRegistrationFlow()",
      "Assumed FlowFixture exposed INotificationStore — it did not; had to add it"
    ],
    "testsPlanned": 10,
    "testsDelivered": 8,
    "testDeliveryRate": "80%",
    "userPromptSourceEntriesAdded": 0,
    "unmappedUserPromptSourceEntries": 0,
    "note": "2 Playwright e2e specs deferred to next phase — Angular client route not ready"
  }
}
```

---

## Project History Tracking

```markdown
## Plan Execution Feedback History

| Phase | WF Planned | WF Actual | Discovery Rate | Signal |
|-------|-----------|-----------|----------------|--------|
| P1 | 1 | 1 | 100% | Normal |
| P2 | 2 | 5 | 250% | Gate 0 under-invested |
| P3 | 3 | ? | ? | Not yet complete |
```

**Pattern detection:** if 2+ consecutive phases have discovery rate > 120%, the planning
process has a systematic problem. Root cause is almost always:
- Plans referenced `path:line` without verifying them (Gate 0 not run).
- Plans assumed service/method signatures without reading the `Application` files (Rule 5 skipped).
- Plans assumed DI registration was in `Program.cs` instead of the `AddXxxFlow()` extension.

---

## Rules

1. Record WF planned vs actual in `STATE-Pn.json` after every phase.
2. Record every plan-shaping user prompt/feedback/question/answer in the User Prompt Source Ledger.
3. If discovery rate > 120% for two consecutive phases: the next phase plan MUST include
   `path:line` citations for every planned element before any code is written.
4. If test delivery rate < 80%: investigate whether tests were deferred for a real reason
   or because the plan over-counted (description blocks vs actual `[Fact]`/`[Theory]` methods or Playwright specs).
5. Report the discovery rate and User Prompt Source Ledger deltas in the Section A summary.

---

## Section A Format (Phase End Summary)

```
## Section A — Phase [N] Execution Summary

Deliverables:
  - [list every file delivered]

Test gate: [N] passed before → [N+M] passed after → +[M] new tests, 0 failures
  (xUnit per-flow `dotnet test`; Playwright for client phases)

Write-time fixes: [N] (planned: [M])
  Discovery rate: [N/M × 100%] — [NORMAL | GATE_0_UNDER_INVESTED]
  [If > 120%: list the top 2-3 examples of what was wrong in the plan]
User prompt source entries added: [N]
Unmapped user prompt source entries: [0 required]

Deviations from plan: [none | describe]
Human-readable stage: [stage]
Current plan step: [step]
Claims opened/closed: [opened] / [closed]
What remains and why: [plain language]
```

---

## Anti-Patterns

1. **Not recording write-time fixes** — cannot improve planning without data.
2. **Under-counting planned WF** — "I planned for 0 surprises" is itself a planning failure.
3. **Accepting > 120% as normal** — two consecutive high-rate phases demand process change.
4. **Attributing high discovery rate to "complex task"** — complexity is not an excuse; complex tasks need deeper Gate 0.

## Integration

Load alongside: `phase-preflight-SKILL.md` (the assumptions whose failures are write-time
fixes), `phase-planning-SKILL.md`. Required by: `planning-session-startup-SKILL.md`
(baseline question).
