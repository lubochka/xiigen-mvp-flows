# user-journey-testing-SKILL.md — Scenario-first testing (xiigen flows)
## v1.0: ported from llm_mvp_core user-journey-testing → .NET 10 per-flow / xUnit + Angular 21

## What This Skill Prevents

The "all unit tests pass but the feature doesn't work" failure. Individual class
tests validate classes. This skill validates the user's experience of a complete
flow. The scenarios must be written **BEFORE** the implementation, not after, and
they become the Gate-B acceptance criteria.

---

## When to Run

At the start of any phase that implements a user-visible flow feature.
Produce test stubs BEFORE writing production code. The stubs become acceptance
criteria that constrain what the implementation must produce.

This is a backend discipline as well as a UI one: the canonical example is a
server `[Fact]` asserting a specific `DataProcessError` code (e.g.
`VALIDATION_FAILED`, `DIMENSION_MISMATCH`-style domain codes), not only a UI
click-path. The 8-category matrix (e2e-test-matrix) and the post-Gate-B verdict
(qa-session-type) do NOT carry "scenario-as-acceptance-before-code" + manifest —
this skill does.

---

## Step 1 — Extract Scenarios from the Feature Description

Every feature description contains the happy path, guard conditions, and edge
cases. Map each to a scenario:

```
Feature: Bundle activation (FLOW-00)

HAPPY PATH:
  S-01: Activate a known bundle with current version → 201, stored, outbox appended
    Expected: result.IsSuccess, result.Data?.HttpStatus == 201

GUARD CONDITIONS:
  S-02: Empty bundleId
    Expected: result.IsFailure, result.Error?.Code == "VALIDATION_FAILED", no partial write
  S-03: Stale ifMatch version (concurrent)
    Expected: result.IsFailure, result.Error?.Code == "CONCURRENT_UPDATE"

EDGE CASES:
  S-04: Same idempotency key twice
    Expected: second call HttpStatus 200, same body id (replay, not double)
  S-05: Fabric timeout during activation
    Expected: result.IsFailure, result.Error?.Code == "TIMEOUT", outbox retains timeout record
```

---

## Step 2 — Write Test Stubs Before Implementation

Each scenario becomes a `[Fact]` stub written before the production class. These
stubs fail (Class 1 in test-failure-triage-SKILL.md) until the production class
exists — that is correct; they are acceptance criteria.

```csharp
// S-01: Happy path — acceptance criteria (fails until BundleActivationService exists)
[Fact]
public void Activate_ValidBundle_Returns201()
{
    Assert.Fail("TODO: implement after BundleActivationService is written — S-01 acceptance criteria");
}

// S-02: Empty bundleId guard
[Fact]
public void Activate_EmptyBundleId_ReturnsValidationFailed()
{
    Assert.Fail("TODO: implement — S-02 acceptance criteria");
}
```

---

## Step 3 — Write Guard Condition Tests

Guard conditions are the most valuable tests — they are the failure modes that
matter most to users. Each guard test must (1) set up the guard condition,
(2) execute the action, (3) verify the **SPECIFIC** expected code — not just
"returns an error".

```csharp
// S-02: Guard condition — empty bundleId → specific code, no partial write
[Fact]
public async Task Activate_EmptyBundleId_ReturnsValidationFailed_NoWrite()
{
    var fixture = await FlowFixture.CreateAsync();
    using var _ = fixture.PushContext();

    var result = await fixture.Service.ActivateAsync(
        new Dictionary<string, object?> { ["bundleId"] = string.Empty }, "1", "bad-input");

    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_FAILED", result.Error?.Code);   // specific code, not just IsFailure
    var activation = await fixture.Documents.GetAsync(BundleActivationConstants.BundleActivations, "BDL-core-platform-v2");
    Assert.Null(activation.Data);                            // no partial write
}
```

---

## Step 4 — Attach to Phase Gate B

The scenarios become the Gate-B conditions for the phase:

```
Gate B for FLOW-00 activation:
  dotnet test --filter "FullyQualifiedName~BundleActivationServiceTests" shows:
  S-01 (valid bundle → 201): PASS
  S-02 (empty bundleId → VALIDATION_FAILED): PASS
  S-03 (stale ifMatch → CONCURRENT_UPDATE): PASS
  S-04 (idempotent replay): PASS
  S-05 (timeout → TIMEOUT + outbox record): PASS
  Total: Failed: 0, Passed: ≥ 5
```

---

## Scenario File Structure

One file per flow feature, in the per-flow Tests project, plus a manifest:

```
flows/FLOW-00-bundle-activation/server/Xiigen.Flow.BundleActivation.Tests/
  BundleActivationServiceTests.cs   ← happy path + guard conditions + edge cases
  SCENARIO-MANIFEST.md              ← lists all scenarios with status
```

```markdown
## FLOW-00 Bundle Activation — Scenario Manifest

| Scenario | Test | Status |
|---------|------|--------|
| S-01: Valid bundle → 201 | Activate_ValidBundle_Returns201 | ✅ PASS |
| S-02: Empty bundleId → VALIDATION_FAILED | Activate_EmptyBundleId_ReturnsValidationFailed_NoWrite | ✅ PASS |
| S-03: Stale ifMatch → CONCURRENT_UPDATE | Activate_StaleVersion_ReturnsConcurrentUpdate | ✅ PASS |
| S-04: Idempotent replay | Activate_SameKeyTwice_Replays | ✅ PASS |
| S-05: Timeout → TIMEOUT | Activate_Timeout_RetainsOutbox | ✅ PASS |
```

## Angular 21 client journeys

For user-visible client flows, the same scenario-first discipline produces
Playwright e2e specs written before the standalone component: happy path
(`getByRole`/`getByLabel` → submit → assert visible result), guard (invalid
input → visible specific error), edge (empty list / max size). The
SCENARIO-MANIFEST lists both server `[Fact]` and client Playwright rows.

---

## Anti-Patterns

1. **Writing acceptance tests after implementation** — "tests to match the code"
   validates nothing.
2. **Guard tests that only check the return is a failure, not the code** —
   specificity is the point (`result.Error?.Code == "CONCURRENT_UPDATE"`).
3. **Happy path only** — guard conditions are where most bugs live.
4. **Asserting only `result.IsFailure` without checking `Error?.Code`** — too
   weak; failures are not interchangeable. (Ties to the behavioral-assertion
   discipline in test-integrity.)
5. **Not linking scenarios to Gate B** — a scenario not in Gate B can be forgotten.
