# bug-to-tests-SKILL.md — 3 failing tests before any fix (xiigen flows)
## v1.0: ported from llm_mvp_core bug-to-tests → .NET 10 per-flow / xUnit

## The Iron Rule

**Test assertion change = Luba approval required.**

If you change a test to make it pass instead of fixing the engine, that is not a
fix. Changing the assertion, relaxing a comparison, or removing a test all
require explicit written approval.

## Bug ID Format

Every bug gets an ID before work starts (write `BUG-NN` to the flow's STATE /
`BUG-LOG.md`):

```
BUG-NN: [one-line description]
Component: [flow + service/class name]
Symptom: [what was observed]
Hypothesis: [suspected root cause — one sentence]
```

Examples:
```
BUG-01: BundleActivationService writes activation before validating empty bundleId
Component: FLOW-00 / BundleActivationService
Symptom: empty bundleId leaves a partial activation document
Hypothesis: validation runs after the store call instead of before

BUG-02: BundleStatusTrackerService misses bundles for a regenerated flow
Component: FLOW-00 / BundleStatusTrackerService
Symptom: OnFlowRegeneratedAsync updates 1 bundle when 2 contain the flow
Hypothesis: fan-out query filters on the wrong field
```

---

## The 6-Step Protocol

### Step 1 — Assign Bug ID
Write `BUG-NN` to the flow STATE file (or a dedicated `BUG-LOG.md`).

### Step 2 — Write 3 FAILING Tests
Write tests at all three levels before touching any production code.

```csharp
// Unit test — pure logic, minimal wiring
[Fact]
public async Task Activate_EmptyBundleId_NoPartialWrite()
{
    var fixture = await FlowFixture.CreateAsync();
    using var _ = fixture.PushContext();
    var result = await fixture.Service.ActivateAsync(
        new Dictionary<string, object?> { ["bundleId"] = string.Empty }, "1", "bad-input");
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
    var activation = await fixture.Documents.GetAsync(BundleActivationConstants.BundleActivations, "BDL-core-platform-v2");
    Assert.Null(activation.Data);                 // proves no partial write
}

// Integration test — real services via ServiceCollection DI, in-process fabric
[Fact]
public async Task Activate_EmptyBundleId_Integration_NoOutbox()
{
    var services = new ServiceCollection();
    services.AddSingleton<InMemoryDocumentStore>();
    services.AddSingleton<InMemoryOutboxStore>();
    // ... register real .Application services ...
    var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<BundleActivationService>();
    // ... push context, act with empty bundleId ...
    // Assert: result.IsFailure && outbox is empty (no event leaked)
}

// Full-suite gate (the gate is the dotnet test run, not this assertion)
[Fact]
public void AllFlow00Tests_PassAfterFix()
{
    // Gate B: dotnet test --filter "FullyQualifiedName~BundleActivation" shows Failed: 0
    Assert.True(true);
}
```

### Step 3 — Confirm All Tests FAIL
Run before any fix:
```
dotnet test --filter "FullyQualifiedName~Activate_EmptyBundleId"
→ Must show: Failed: 2, Passed: 0
```
**If the tests pass before the fix, the tests are wrong. Rewrite them.**

### Step 4 — Fix the Engine, Not the Tests
Fix the per-flow service that produces the wrong output. Never change the test
assertions to make them pass.
```
Fix location: flows/FLOW-00-bundle-activation/server/Xiigen.Flow.BundleActivation.Application/BundleActivationService.cs
Fix: run validation (and return VALIDATION_FAILED) BEFORE the store/outbox call
```

### Step 5 — Confirm All Tests PASS
```
dotnet test --filter "FullyQualifiedName~Activate_EmptyBundleId"
→ Must show: Passed: 2, Failed: 0
```

### Step 6 — Zero-Regression Gate
```
dotnet test flows/FLOW-00-.../Xiigen.Flow.BundleActivation.Tests.csproj --no-build 2>&1 | tail -10
→ Failed: 0
→ Total count ≥ session-start baseline
```
**If total count is LOWER than baseline, a test was accidentally deleted. Find it.**

---

## Test Level Reference

| Level | xUnit Pattern | What It Tests |
|-------|--------------|---------------|
| Unit | `[Fact]` + `FlowFixture` / `new ...()` | Service logic on the in-memory fabric |
| Integration | `[Fact]` + `ServiceCollection` DI | Component wiring, in-process, real services |
| Regression gate | `dotnet test --filter "FullyQualifiedName~<Flow>"` | Full per-flow test suite |

---

## What "Fix the Engine" Means

Fix the class that produces the wrong output, not the code that consumes it.
This is the retroactive-development discipline.

```
❌ Wrong: Change the test to Assert.True(result.IsFailure || result.IsSuccess) to tolerate the bug
❌ Wrong: Add a null check in the Api endpoint that calls the service
✅ Right: Fix the ordering/validation inside BundleActivationService itself
```

If fixing the engine would require changing a test assertion (because the
original assertion was wrong), that requires Luba's written approval first.

---

## Anti-Patterns

1. **Fix first, write tests after** — post-hoc tests are justifications, not guards.
2. **Write tests that already pass** — Step 3 must show FAIL; immediate pass = wrong test.
3. **Change the assertion to match broken output** — documents the bug, does not fix it.
4. **One test for a bug** — bugs need unit + integration minimum.
5. **Skip the zero-regression gate** — a fix that introduces a new failure is not a fix.
