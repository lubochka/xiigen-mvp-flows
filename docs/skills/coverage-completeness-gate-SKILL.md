# coverage-completeness-gate-SKILL.md — Evidence scope must never fall short of claim scope

> **Stack adaptation (xiigen flows).** Universal coverage gate ported from
> `llm_mvp_core/docs/skills/coverage-completeness-gate-SKILL.md`. Adapted to this repo:
> - "subproject" maps to a **flow** (or one of its `Api/Application/Infrastructure/Tests`
>   projects) and/or an Angular 21 client; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - the improvement metric is `dotnet test` (xUnit) output / `ng test` / `npx playwright test`
>   output, never an estimate or a build-passed proxy;
> - "pipeline/plan complete" = all named flows pass with `Failed: 0` on `net10.0`;
> - core ML/`ILearningMachine`/DPO quality metrics are core-only (G12) and out of scope here.

## When to load
Fires before every STOP that contains a claim of improvement, completion, or progress.

Trigger phrases: "improved", "fixed", "resolved", "complete", "done"; percentages ("-X%",
"X% reduction"); completion claims ("all tests pass", "all flows updated", "pipeline complete");
progress claims ("significantly improved", "major progress").

## Section 1 — The coverage gate table

| Claim type | Required coverage before the claim is valid |
|------------|---------------------------------------------|
| "This flow improved" | All affected classes/projects in that flow tested (`dotnet test` on its Tests project) |
| "These N flows improved" | All N flows have passing test deltas |
| "Pipeline is improving" | ≥ 2 of N components have test evidence |
| "Pipeline / plan is complete" | All N components pass; per-flow `dotnet test` -> 0 failed; `dotnet build Xiigen.Flows.slnx` -> 0 Error(s) |
| "This service is complete" | Application service + its xUnit tests (incl. a Fail path) pass |
| "Client X works" | `ng test` and/or Playwright e2e pass for that client |
| "X% improvement in [metric]" | Metric is from `dotnet test`/`ng test`/Playwright output, not estimated |

## Section 2 — Coverage check before STOP

For every claim trigger detected, run this before STOP fires:

```
COVERAGE CHECK (coverage-completeness-gate):

Claim detected: "[exact quote]"
Claim type: [single flow | N flows | pipeline | service | client | metric]

Coverage required: [per table in Section 1]
Coverage actual:
  This session touched: [list flows / projects / clients]
  Each fully tested (dotnet test / ng test / playwright evidence): [YES/NO per item]
  Test count delta: [+N from baseline, per flow]

Coverage verdict:
  SUFFICIENT — claim matches evidence scope -> STOP may fire
  INSUFFICIENT — [actual] < [required] -> revise claim per Section 3
```

If INSUFFICIENT: do not suppress the response — downgrade the claim. The work is preserved.

## Section 3 — Claim downgrade protocol

| Original claim | Coverage gap | Downgraded claim |
|---|---|---|
| "FLOW-01 is complete" | Application tested, Infrastructure not | "UserRegistrationService + its tests pass. Infrastructure store + e2e not yet verified." |
| "All flows updated" | 3 of 6 flows have test deltas | "FLOW-01/02/03 pass with deltas. FLOW-04/05/06 not yet tested." |
| "50% improvement in validation quality" | Counted `rg` matches, not test assertions | "50% reduction in failing assertions in <Name>ServiceTests. End-to-end quality unverified." |
| "Client done" | `ng build` passed, no `ng test`/Playwright | "Client builds. UI behaviour not verified — run ng test + Playwright before claiming done." |

A downgraded claim is not a failure — it is an honest report. A session that correctly states
its own coverage limit is more trustworthy than one that ships an unfalsifiable result.

## Section 4 — Threshold reasoning

- **Why 100% for "this flow improved":** a flow is a small scope; if its Infrastructure project
  was not tested, that behaviour is unknown. No partial "improved" for a specific class.
- **Why test output for "X% improvement":** code-change counts (lines, renames) do not measure
  behavioural improvement; only `dotnet test`/`ng test`/Playwright deltas count.
- **Why the pipeline claim requires component evidence:** claiming the pipeline improved
  without testing each component is claiming a train arrived on time because the departure
  station looked busy. Each flow shows its own test evidence.

## Section 5 — Integration

- `output-skepticism-SKILL.md` Q2 (scope validity) is the upstream version of this gate.
- `verification-before-completion-SKILL.md` gates FRESHNESS; this gate gates SCOPE. Both must
  pass before STOP.

## Section 6 — Anti-patterns

1. "I improved what I worked on — that's valid" — valid AT THE SCOPE of what you worked on. The
   gate catches scope inflation (pipeline claim from component work).
2. "Downgrading makes the session look bad" — the opposite; honest coverage limits are
   trustworthy.
3. "The gate is too strict for short sessions" — short sessions make short-scope claims; the
   gate is proportional.
4. "I'll note the caveat in the notes section" — a caveat alongside an overclaimed headline is
   still an overclaimed headline. Downgrade the claim itself, not a footnote.
