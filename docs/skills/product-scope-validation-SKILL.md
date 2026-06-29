# Product Scope Validation

> **Stack adaptation (xiigen flows).** Universal 5-step post-wave product validation ported from
> `llm_mvp_core/docs/skills/product-scope-validation-SKILL.md`. Adapted to this repo:
> - the **user-facing scenario** is an Angular 21 standalone client → flow `Api` →
>   `Application` → `Infrastructure` end-to-end path that exercises **at least two flow
>   outputs**; for API-only flows it is a sequence of HTTP requests across ≥2 modules;
> - **trace** checks grep `.cs` (per-flow `server/`) and `.ts` (per-flow
>   `client/angular/src`); the integration test is an xUnit integration test
>   (`Xiigen.Flow.<Name>.Tests`) and/or an Angular 21 Playwright e2e at the UI seam;
> - **Step 4 business-outcome** checks the `DataProcessResult<T>` *value* a user would
>   recognize as correct — NOT merely that the call returned `Success` (XIIGen wrapper, not core
>   `OperationResult<T>`); contrasting inputs are run through xUnit integration / Playwright;
> - the verdict report is saved as a per-wave artifact.

## Core Problem

A wave of flow phases can all pass Gate B individually while the end-to-end
product story is broken. Each flow tested its own output; no flow tested the
joint output. This skill defines the 5-step post-wave validation that answers:
"Does this wave advance a real product scenario that a user of the app can
experience end-to-end?"

---

## When to Invoke

- After all phases in a parallel wave complete and merge to the working branch
- Before declaring a wave "feature complete"
- When the user reports that individual flows pass but the demo fails
- Before writing the session handoff that closes a multi-flow batch

---

## Section 1 — Five-Step Validation Protocol

Run all five steps in order. A FAIL at any step produces an INCOMPLETE verdict
and halts — do not skip ahead to produce a partial PRODUCT-READY verdict.

### Step 1 — Retrieve Original Product Intent

```
Source: the goal statement at the top of the session or the planning document
        that initiated this wave. Do not paraphrase. Quote verbatim.

Record:
  intent_statement: "[verbatim quote from session goal]"
  wave_scope:       "[list of flows included in this wave]"
```

If the intent statement cannot be found: emit CONTEXT_INSUFFICIENT before proceeding.

### Step 2 — Define a Product Scenario

Translate the intent into a single concrete user-facing scenario:
a sequence of steps a user takes through the Angular 21 client (or a sequence of
HTTP requests for an API-only flow) that exercises the output of this wave
end-to-end.

```
Scenario format:
  User action:     [what the user does in the Angular client, or the HTTP request]
  System response: [what the app shows or the API returns]
  Exercises:       [which flow outputs are exercised]
```

The scenario must name at least two flow outputs. If it only names one,
the scenario is not testing cross-flow integration.

### Step 3 — Trace the Scenario Against Live State

For each step in the scenario:

```
□ Does the required contract/interface exist?   (grep for the name in .cs / .ts)
□ Is it implemented, not stubbed?               (check for STUB marker or TODO body)
□ Does the implementation reach the downstream consumer?
  (trace: client → flow Api endpoint → Application service → Infrastructure store/provider)
□ Is there a test that exercises this integration path?
  (xUnit integration test, or Angular 21 Playwright e2e at the UI seam)
```

A STUB answer at any trace step = INCOMPLETE.
An absent integration test = INCOMPLETE (not FAIL — document the gap).

### Step 4 — Validate Business Outcomes

This step checks product correctness, not code correctness.

For each scenario outcome:

```
WRONG question: "Does the method return DataProcessResult Success?"
RIGHT question:  "Does the returned value produce a result the user would
                 recognize as correct for their action?"
```

Validation heuristics by archetype:

```
REGISTRATION / ONBOARDING archetype (e.g. FLOW-01):
  Wrong: Register returns Success
  Right: a verification token is issued, the user record is stored, the outbox
         records the welcome event, and a duplicate registration is rejected

SCORING / RANKING archetype:
  Wrong: score is non-null
  Right: the score is in the expected range AND higher scores correspond to
         better candidates (verify with 2 contrasting inputs)

RETRIEVAL archetype (doc-wiki via consumed model):
  Wrong: returns a non-empty answer
  Right: the answer matches the most relevant document (spot-check with an
         unambiguous query)

PAYMENT / TRANSACTION archetype:
  Wrong: returns Success
  Right: the balance/ledger reflects the transaction AND an idempotent retry
         does not double-charge
```

### Step 5 — Produce Product Validation Report

```markdown
## Product Validation Report — Wave [N]

### Intent Statement
[verbatim from Step 1]

### Scenario Tested
[from Step 2]

### Verdict
PRODUCT-READY | INCOMPLETE | BROKEN_DEPENDENCY

### Evidence
| Trace Step | Status | Evidence |
|---|---|---|
| [flow A output] → [flow B output] | PASS/FAIL/STUB | [grep result or test name] |

### Gaps (INCOMPLETE only)
| Gap | Type | Blocking? |
|---|---|---|
| [missing integration test] | DOCUMENTATION | No |
| [stub in critical path] | IMPLEMENTATION | Yes |

### Business Outcome Check
| Outcome | Correct? | Evidence |
|---|---|---|
| [scenario outcome 1] | YES/NO | [spot-check result] |
```

---

## Section 2 — Verdict Definitions

**PRODUCT-READY:** All trace steps pass, no stubs in critical path, business
outcomes verified with at least two contrasting inputs.

**INCOMPLETE:** Some trace steps fail or stubs exist in the path. The feature
is not ready for user-facing demonstration. List all gaps; classify each as
DOCUMENTATION (missing test, not blocking) or IMPLEMENTATION (blocking).

**BROKEN_DEPENDENCY:** A downstream component does not exist or its contract
has changed since the upstream was written. This is not the same as INCOMPLETE —
it requires a new phase to resolve, not just a gap document.

---

## Section 3 — Common Failure Patterns

```
1. "Wave passed Gate B but the demo crashes"
   Cause: Gate B checked each flow in isolation.
   Fix:   Run Step 3 trace — find the first nil/stub/missing contract in path.

2. "Retrieval returns an answer but it's wrong"
   Cause: Step 4 business outcome check was skipped.
   Fix:   Run contrasting-input spot-check (right answer vs wrong answer query).

3. "Business outcome correct for one input, wrong for another"
   Cause: Edge case missed.
   Fix:   Add the failing case as an xUnit integration test; fix the flow.

4. "Can't find original intent statement"
   Cause: Goal was never written verbatim. Paraphrased goals drift.
   Fix:   Ask Luba to state the intent before proceeding with Step 1.
```

---

## Anti-Patterns

1. **"All Gate B checks passed — wave is ready"** — Gate B is per-phase.
   Product scope validation is post-wave. Different gate, different scope.

2. **"Business outcome is 'the method runs without error'"** — this is Step 3
   (code correctness), not Step 4 (product correctness). Run both.

3. **"Only one flow in the scenario"** — a single-flow scenario does not test
   integration. Add the downstream consumer or pick a different scenario.

4. **"Verdict is INCOMPLETE but no gaps listed"** — INCOMPLETE without a gap list
   is unusable. Name every gap. Classify each as DOCUMENTATION or IMPLEMENTATION.

5. **"Proceeding to next wave before closing BROKEN_DEPENDENCY gaps"** — a broken
   dependency in the current wave becomes a compounding blocker in the next.

---

## Integration

- `multi-reviewer-design-SKILL.md` reviews the design before the wave; this skill validates the product after the wave
- `authority-requirement-binding-SKILL.md` supplies the verbatim intent + binding requirements for Step 1
- the Product Validation Report is included in the session handoff that closes the wave
