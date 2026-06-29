# mvp-round-trip-verification-SKILL.md — 8-step round-trip, minimum 1–6 (xiigen flows)
## v1.0: ported from llm_mvp_core mvp-round-trip-verification → .NET 10 per-flow / Angular 21

## Core Problem

A feature that passes unit tests but cannot be observed end-to-end has not been
delivered. Unit tests verify internal correctness; they do not verify that a
capability flows from its author through every integration point to a consumer
who can observe a result. This skill installs the 8-step round-trip check that
closes the gap between "tests pass" and "feature delivered".

---

## When to Invoke

- Before declaring any flow phase complete that introduces a new public capability
- Before a handoff that claims a feature is delivered
- After any integration point is wired (new service, new DI registration, new pipeline stage)

One round-trip check before STOP = no "works in unit test, broken in pipeline".

---

## Section 1 — The 8-Step Round-Trip

Each step has a responsible actor, an observable output, and a verification command.

| Step | Name | Actor | Observable Output | Verification |
|------|------|-------|------------------|--------------|
| 1 | **Capability authored** | Developer | Class file on disk | `ls flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<Class>.cs` |
| 2 | **Visible in project** | .NET build | Assembly references resolve | `dotnet build <Project>.csproj → 0 Error(s)` |
| 3 | **Published / registered** | Service wiring | DI registration in `.Api` | `grep -n "AddSingleton\|AddTransient\|AddScoped" .../Xiigen.Flow.<Name>.Api/Program.cs` |
| 4 | **Discoverable by consumer** | Project reference | Consumer project resolves the type | `dotnet build <ConsumerProject>.csproj → 0 Error(s)` |
| 5 | **Installed / callable** | Test fixture | Test can instantiate and call | `dotnet test --filter "FullyQualifiedName~<TestClass>" → ≥1 passed` |
| 6 | **Visible in result** | Test assertion | Return value observable | `dotnet test --filter "FullyQualifiedName~<SpecificTest>" → PASSED` |
| 7 | **Consumer runs to completion** | Full pipeline test | No unhandled `DataProcessResult` failure | `dotnet test --filter "FullyQualifiedName~<PipelineTest>" → passed, 0 failed` |
| 8 | **Result observable** | End-to-end assertion | Specific output matches expected | `dotnet test --filter "FullyQualifiedName~<E2ETest>"` OR Playwright observes the Angular page |

**Minimum delivery evidence: Steps 1–6 must all pass.**
Steps 7–8 are required for pipeline-level / user-visible delivery claims.

For an Angular 21 client capability, Step 2 is `ng build` (0 errors), and Step 8
is a Playwright spec that drives the standalone component and observes the result
in the rendered page.

---

## Section 2 — Nomination Format

```markdown
## Round-Trip Nomination

Capability: [<Class> implementing I<Interface>]
Flow: FLOW-NN — [flow name]
Delivery claim: [one sentence — what the consumer can now do]

Step 1 — Authored:
  Command: ls flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<Class>.cs
  Expected: file exists                          Actual: [result]

Step 2 — Visible in project:
  Command: dotnet build .../Xiigen.Flow.<Name>.Application.csproj
  Expected: 0 Error(s)                           Actual: [result]

Step 3 — Registered:
  Command: grep -n "<Class>" .../Xiigen.Flow.<Name>.Api/Program.cs
  Expected: ≥1 match                             Actual: [result]

Step 4 — Discoverable:
  Command: dotnet build .../Xiigen.Flow.<Name>.Api.csproj
  Expected: 0 Error(s)                           Actual: [result]

Step 5 — Callable:
  Command: dotnet test --filter "FullyQualifiedName~<TestClass>"
  Expected: ≥1 passed, 0 failed                  Actual: [result]

Step 6 — Result visible:
  Command: dotnet test --filter "FullyQualifiedName~<SpecificTest>"
  Expected: PASSED; asserts [specific value, e.g. result.Data?.HttpStatus == 201]   Actual: [result]

Step 7 — Pipeline complete (if pipeline claim):
  Command: dotnet test --filter "FullyQualifiedName~<PipelineTest>"
  Expected: [N] passed, 0 failed                 Actual: [result]

Step 8 — Result observable end-to-end (if pipeline / UI claim):
  Command: dotnet test --filter "FullyQualifiedName~<E2ETest>"  OR  npx playwright test <spec>
  Expected: PASSED; [specific assertion / visible page state]    Actual: [result]

Round-trip verdict: PASS | PARTIAL ([steps that failed]) | FAIL
```

---

## Section 3 — Partial Round-Trip Protocol

When Steps 1–6 all pass but Steps 7–8 fail:
- Delivery claim is scoped to the component, not the pipeline.
- Handoff states: "Component delivered; pipeline/UI integration pending".
- The next phase includes Steps 7–8 as mandatory Gate-B conditions.

When any of Steps 1–6 fail:
- Phase is NOT complete.
- The failing step is the next task — do not move to a different step.
- Identify the root cause before fixing.

---

## Anti-Patterns

1. **"Tests pass, so it's delivered"** — passing unit tests satisfy Step 5/6 only.
   Steps 2–4 (build visibility, DI registration, consumer discoverability) can
   fail independently of test results.
2. **"Registration is obvious, skip Step 3"** — a missing DI registration
   produces a runtime `NullReferenceException`, not a build error. Step 3 is not
   optional.
3. **"I'll verify steps 7-8 later"** — "later" becomes "never" when the session
   ends. Steps 7–8 are required before any pipeline/UI delivery claim.
4. **"Partial delivery counts as delivered"** — use "component delivered,
   pipeline/UI integration pending", never "delivered".

---

## Integration

- e2e-test-matrix-SKILL.md Step 5–6 cover Category 1 (happy path); Step 7 covers Category 7 (boundary)
- qa-session-type-SKILL.md reads the round-trip verdict before approving a pipeline delivery claim
- user-journey-testing-SKILL.md SCENARIO-MANIFEST supplies the specific assertions for Step 6/8
