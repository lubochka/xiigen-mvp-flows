# improvement-measurement-SKILL.md — Observable delta required before any improvement claim (xiigen flows)
## v1.0: ported from llm_mvp_core improvement-measurement → .NET 10 (dotnet test/build) + Angular 21 (ng build/Playwright)

## When to load
Load for any per-flow change claiming a capability improvement.
Load before any phase report that includes an improvement percentage or
before/after metric.
Load before any claim of "X is now complete" or "X improved by N%".

---

## The problem this skill solves

An internal metric that improved is not an improvement until a user-facing or
behavioral change can be described. Test counts can rise while the capability
regresses. `dotnet build` passes while a `.Application` service produces the
wrong `DataProcessResult<T>`. `ng build` succeeds while the screen renders the
wrong thing.

**Observable behavioral delta is required before any improvement claim is
permitted.**

---

## Section 1 — The Two-Layer Protocol

For any capability change, two layers must be present before an improvement
claim is valid.

### Layer 1 — Internal metrics (necessary, not sufficient)

Internal metrics confirm structural correctness. Required. Not sufficient alone.

| Metric type | What it confirms | What it does NOT confirm |
|---|---|---|
| xUnit test count increased | More scenarios covered | The service produces better results |
| `dotnet build` errors = 0 | Server code compiles | Correct behavior |
| `ng build` errors = 0 | Client compiles | The screen renders the right thing |
| Vitest/Playwright count up | More UI scenarios covered | The user sees a better result |

Layer 1 format (required in every status that includes a capability claim):

```
LAYER 1 — Internal metrics:
  Server: dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/...
          → [before] → [after] tests ([delta]), 0 failed
          dotnet build → 0 errors
  Client: ng build → 0 errors; ng test (Vitest) → [count], 0 failed
  New tests: [list test names]
```

### Layer 2 — Observable delta (required for an improvement claim)

For every changed component, one concrete statement describing what the system
now does differently for a specific input. For flows there are two surfaces:

```
LAYER 2 — Observable delta:

  Server API: [endpoint/method]: request [concrete input] now returns
    [concrete DataProcessResult<T> shape — Data fields / Error?.Code], was [before].
    Example: POST /flows/FLOW-04/rsvp { eventId:"E1", userId:"U1" } now returns
      IsSuccess=true, Data.Status="GOING"; replay of the same idempotency key
      returns the same result (was: duplicate row).

  Angular screen: [route + ?role=…&state=…]: now renders [concrete visible result],
    was [before]. Example: /?role=tenant_user&state=rsvp now shows the visible
    confirmation "You're going" after submit (was: silent, no feedback).
```

**"No sentence = no improvement claim"** — if the change cannot be stated as a
concrete input → output difference (on the API or on the screen), the
improvement has not been measured.

For a UI improvement, the observable delta is the before/after cell deltas from
`ui-examination-round-SKILL.md` (read the screen/PNG), not a grep count.

---

## Section 2 — Claim Validity Rules

### Component-level claim: "the RSVP service capability improved"

Requires:
- Layer 1: xUnit test count delta with passing tests listed.
- Layer 2: one sentence per changed method describing input → output change on
  the `DataProcessResult<T>`.
- Gate B evidence from `verification-before-completion-SKILL.md`.

Valid example:

```
RSVP service capability improved.
LAYER 1: 4 → 9 xUnit tests (+5); all pass; Failed: 0; dotnet build → 0 errors.
LAYER 2: RsvpService.SubmitAsync(eventId,userId) returns IsSuccess=true,
  Data.Status="GOING"; second call with the same key returns the same result
  (Error?.Code == null), where before it created a duplicate. Asserted in
  RsvpServiceTests.SubmitTwice_Replays.
```

### Flow-level claim: "the FLOW-NN client journey improved"

Requires:
- Layer 1: test count delta across server + client; `ng build` 0 errors.
- Layer 2: one sentence per screen/stage describing the visible behavior change.
- Coverage completeness: every affected screen has UI evidence
  (`coverage-completeness-gate-SKILL.md`).

---

## Section 3 — The STOP Gate

Before any status that includes an improvement percentage or before/after metric:

```
IMPROVEMENT CLAIM GATE (improvement-measurement-SKILL):

Claim: "[stated improvement]"

Layer 1 check:
  □ Internal metrics present (dotnet test/build counts; ng build/Vitest counts)
  □ Layer 1 format block present in this response

Layer 2 check:
  □ Observable delta present for each changed component (API and/or screen)
  □ At least one concrete input/output example per changed component
  □ "Behavioral delta not yet verified" stated explicitly if not established

Coverage check:
  □ Component-level: all affected tests listed
  □ Flow-level: all affected screens/stages have UI or API evidence
  □ coverage-completeness-gate-SKILL.md check run

Verdict: VALID | INVALID — [reason if invalid]

If INVALID: revise before stopping.
  Option A: Provide the observable (run a specific test / Playwright check and
            quote the assertion / visible result).
  Option B: Downgrade the claim to what Layer 1 establishes:
            "Test count improved — behavioral improvement not yet verified."
```

---

## Section 4 — "No Behavioral Test" Declaration

If the change added tests but none verify a behavioral observable:

```
LAYER 2 STATUS: TESTS ONLY.
Test count improved. Behavioral improvement not yet verified.
Claim revised to: "[N] tests added.
  Behavioral delta: not established — [the specific test/Playwright check that
  would verify it]."
```

This is not a failure — it is an honest claim. "Tests added, behavior not yet
verified" is more useful than "+N tests = capability improved".

---

## Section 5 — Anti-patterns

1. **"Test count rose — capability improved"** — tests measure coverage, not
   correctness of results. Establish the behavioral delta.

2. **"`dotnet build` / `ng build` passes — feature complete"** — build confirms
   compilation, not behavior. State what the component does for a specific input.

3. **"Obviously the screen got better — I added the component"** — "obvious" is
   prediction. State the concrete visible delta (read the screen/PNG).

4. **"The test asserts the behavior"** — quote the assertion / visible result.
   The claim must stand without reading the test file.

5. **"Lint/grep score improved, so the UI improved"** — that is a Layer 1 proxy,
   not an observable delta. UI observable delta = read the screen.

---

## Integration

- `output-skepticism-SKILL.md`: the proxy check — "could this metric improve
  without the capability improving?"
- `coverage-completeness-gate-SKILL.md`: the claimed scope must match the
  evidence scope.
- `ui-examination-round-SKILL.md`: supplies before/after cell deltas as the
  Layer 2 observable for UI changes.
- `quality-improvement-cycle-SKILL.md`: provides the before/after score values
  this skill requires for an iterative improvement claim.

---

## Stack adaptation notes (xiigen flows)

- Layer 1 = `dotnet test` / `dotnet build` (server, xUnit, net10.0) **and**
  `ng build` / `ng test` (Vitest) / Playwright (client).
- Layer 2 = a concrete request → `DataProcessResult<T>` on a flow API endpoint,
  or a concrete `?role=…&state=…` route → visible rendered result.
- This skill is governance/measurement discipline — it trains no model. The
  common model lives in `llm_mvp_core` (G12) and is consumed via manifest.
