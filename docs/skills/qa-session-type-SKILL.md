# qa-session-type-SKILL.md — Post-Gate-B QA review session (xiigen flows)
## v1.0: ported from llm_mvp_core qa-session-type → .NET 10 per-flow / xUnit + Angular 21

## Core Problem

A phase that declares itself complete may have passed Gate B on the wrong
baseline or delivered outputs that pass unit tests but fail cross-flow
integration. The QA session is a distinct mode that verifies completion claims
**independently** before phase progression is approved. It is NOT the same as
running `dotnet test` — it is a structured review of the claim, not just the code.

The verdict is strictly binary: **APPROVED | DEFECTS_FOUND**. No "partial", no
"conditional", no "approved if X is fixed next session".

---

## When to Invoke

- Optional for sequential single-flow phases
- Recommended for parallel phases (several flows completing simultaneously)
- When another reviewer or Luba questions a completion claim from a prior session

---

## Section 1 — QA Session Inputs

A QA session cannot start without all three inputs:

1. **Phase completion package** — the flow's completion report / session brief / execution log.
2. **Acceptance criteria** — the `SCENARIO-MANIFEST.md` from user-journey-testing,
   or the quality gates declared in the phase plan.
3. **Prior issue inventory** — any open issues this phase was expected to close.

---

## Section 2 — Five-Step QA Protocol

### Step 1 — Verify completion claims

Do not trust completion claims without verification:

```bash
# Verify the test gate was actually zero failures (not just claimed)
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/Xiigen.Flow.<Name>.Tests.csproj 2>&1 | tail -5
# Must show "Failed: 0" with a real count — not just "passed"

# For an Angular client claim, verify build + e2e gate
# (cd flows/FLOW-NN-<slug>/client/angular && npm run build && npx playwright test)
```

### Step 2 — Run acceptance criteria checks

For each acceptance criterion in the phase plan, run a command with a binary
result:

```bash
dotnet test --filter "FullyQualifiedName~Activate_ValidBundle_Returns201"
# Expected: 1 passed, 0 failed

dotnet test --filter "FullyQualifiedName~Activate_StaleVersion_ReturnsConcurrentUpdate"
# Expected: 1 passed, 0 failed
```

A failing verification command = a DEFECT that must appear in the defect log.

### Step 3 — Integration path check

For flows whose output is consumed by a downstream flow:

```bash
# Verify the interface the downstream consumer calls exists
grep -rn "I<DownstreamContract>" flows --include="*.cs" | grep "interface"
# Expected: ≥1 hit

# Verify the implementation is not a stub
grep -rn "TODO\|throw new NotImplementedException\|return default" \
    flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<ServiceFile>.cs
# Expected: 0 hits in production code
```

### Step 4 — (note-only) Training-data quality

> **note-only (core_specific, do NOT make this a mandatory flows step):** in
> `llm_mvp_core` this step audits DPO (Direct Preference Optimization) triples
> destined for `IncrementalTrainer`. Common/trainable models live in
> `llm_mvp_core` (mapping R5); flows do not produce DPO triples here. If a flow
> phase ever feeds a common-model training set, route that audit to the core
> repo — do not invent a DPO gate inside flows.

### Step 5 — Produce QA report

```markdown
# QA REPORT — FLOW-NN <Name> Phase P
## Date: [date]   Reviewer: [who ran the QA session]
## Verdict: APPROVED | DEFECTS_FOUND

## ACCEPTANCE CRITERIA RESULTS
| Criterion | Command | Result |
|---|---|---|
| Valid bundle → 201 | dotnet test --filter "...Returns201" | PASS |
| Stale ifMatch → CONCURRENT_UPDATE | dotnet test --filter "...ConcurrentUpdate" | FAIL — see DEFECTS |

## TEST GATE VERIFICATION
- dotnet test: Failed: 0 ✅ / ❌
- Integration path: PASS / FAIL / N/A
- Angular client (if any): build + Playwright PASS / FAIL / N/A

## DEFECTS FOUND
| ID | Description | Severity | Blocking? |
|---|---|---|---|
| QA-001 | [description] | HIGH | YES |

## VERDICT: APPROVED | DEFECTS_FOUND
```

---

## Section 3 — Defect Severity Classification

**BLOCKING** — phase may not progress until resolved:
- Any `dotnet test` failure
- A stub / `NotImplementedException` / `TODO` in a method Gate B claims complete
- Missing issue-inventory entry for a known open issue
- An acceptance criterion fails its verification command
- An interface a downstream flow calls is missing or stubbed

**NON-BLOCKING** — noted and tracked; may progress with explicit authorization:
- Documentation gaps (missing XML doc comment)
- Test coverage below ideal but not zero
- Style issues with no behavioral impact

---

## Section 4 — Relationship to Gate B

The QA session does NOT replace Gate B; it is a **post-Gate-B** review step.

```
Phase runs → Gate B passes (dotnet test green, acceptance tests green)
→ Developer produces the completion package
→ QA session runs (validates the package, not just the tests)
→ QA report → APPROVED or DEFECTS_FOUND
→ Luba reviews QA report + completion package → approves progression
```

QA is the cross-flow integration check that per-flow Gate B cannot provide. It
binds to the `SCENARIO-MANIFEST` produced by user-journey-testing.

---

## Anti-Patterns

1. **"Gate B passed — same as QA approved"** — Gate B is per-flow; QA checks
   cross-flow integration. Different scope.
2. **"QA found a non-blocking defect — session failed"** — NON-BLOCKING defects
   are tracked and may not block; BLOCKING defects block.
3. **"QA verdict is PARTIAL or CONDITIONAL"** — verdict is always APPROVED or
   DEFECTS_FOUND. No middle ground. Fix the defect, re-run QA, get APPROVED.
4. **"Build passed, so the page works"** — a green `ng build` is not a passed
   Playwright e2e; classify UI evidence (see test-integrity ui-evidence-scope).
