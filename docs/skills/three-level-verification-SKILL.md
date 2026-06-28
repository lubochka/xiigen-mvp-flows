# three-level-verification-SKILL.md — Verify a fix at three levels before claiming done (xiigen flows)
## v1.0: ported from llm_mvp_core three-level-verification → .NET 10 per-flow / xUnit

## When to load
Load after any fix to a per-flow `.Application`/`.Infrastructure` service or algorithm.
Load before declaring any bug fix complete.
Load before a per-flow phase-end Gate B.

---

## The problem this skill solves

A fix can pass all unit tests and still break at a higher level. This happens
because unit tests use stubs that don't exercise the real code paths.

Three failure scenarios:

1. **Unit passes, integration fails**: The fix works in isolation but breaks
   when the real `.Application` + `.Infrastructure` services are wired together.
   The stub was too permissive and accepted inputs the real fabric never produces.

2. **Integration passes, suite fails**: The integration test used specific
   inputs. The full per-flow suite covers edge cases (empty store, stale
   `ifMatch`, simulated timeout) that the narrow integration test missed.

3. **All pass locally but the wrong thing was fixed**: The fix corrects the
   test-visible output but leaves the root cause in place. A different test
   added later will expose it.

A fix is done when all three levels confirm it — not when the unit test alone
goes green.

---

## The 3-Level Protocol

Run in sequence. Stop at the first level that fails — do not skip ahead.

### Level 1 — Unit Test (xUnit `[Fact]` with stubs)

**Command:**
```bash
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/Xiigen.Flow.<Name>.Tests.csproj --filter "FullyQualifiedName~<ClassName>"
```

Allowed Level 1 stubs: `ICodeReader` → in-memory reader, `ILlmClient` → mock,
or any fabric NOT on the fixed path.

**What passes Level 1:**
- The specific `[Fact]` for the fixed behaviour
- 0 test failures
- The assertion covers the specific output path that was fixed
  (e.g. `result.Data?.HttpStatus`), not just `result.IsSuccess`

**What fails Level 1:**
- Any test failure in the specific test class
- Test passes only because a stub is intercepting (bypassing) the fixed path

**Stop rule:** If Level 1 fails, fix the unit. Re-run Level 1. Do not proceed.

---

### Level 2 — Integration Test (real `.Application` + `.Infrastructure`, no stubs on the fixed path)

Write a `[Fact]` or `[Theory]` that wires real per-flow services together for
the fixed code path. No stubs on the interfaces involved in the fix — use the
real in-memory fabric.

```csharp
// Level 2 integration test — real services + real in-memory fabric (no stubs on the fixed path)
[Fact]
public async Task ActivateAsync_RealFabric_StoresAndAppendsOutbox()
{
    var documents = new InMemoryDocumentStore();            // real
    var idempotency = new InMemoryIdempotencyStore();        // real
    var outbox = new InMemoryOutboxStore();                  // real
    var bootstrapper = new InMemoryFlowBootstrapper();       // real
    var tenant = new AsyncLocalTenantContextAccessor();
    var correlation = new AsyncLocalCorrelationContextAccessor();
    var validator = new BundleValidatorService(documents, outbox, tenant, correlation, NullLogger<BundleValidatorService>.Instance);
    var service = new BundleActivationService(documents, idempotency, outbox, bootstrapper, validator, tenant, correlation, NullLogger<BundleActivationService>.Instance);
    // ... seed documents, push context ...

    var result = await service.ActivateAsync(
        new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" }, "1", Guid.NewGuid().ToString("n"));

    Assert.True(result.IsSuccess, result.Error?.Message);   // real services, real outcome
    Assert.True(result.Data?.OutboxAppended);
}
```

**What passes Level 2:**
- Real services wired together for the fixed path
- The fix holds when services receive real-shaped fabric inputs (not stub-shaped)
- `DataProcessResult<T>` shape is correct throughout the chain

**What fails Level 2:**
- Test fails with real inputs even though the unit stub test passed
- `IsFailure` is returned on a path that should succeed
- Field-name mismatch between what one service produces and what the next expects

**Stop rule:** A Level 2 failure after a Level 1 pass = the stub was too permissive.

---

### Level 3 — Full Suite (all tests in the per-flow Tests project)

**Command:**
```bash
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/Xiigen.Flow.<Name>.Tests.csproj 2>&1 | tail -5
```

**What passes Level 3:**
- 0 failures
- Count >= the per-flow session-start baseline (record the baseline before the fix)
- No test that was passing before the fix is now failing

**What fails Level 3:**
- Any test failure
- Count decrease (a test was accidentally broken or deleted)

**Stop rule:** If Level 3 fails after Levels 1 and 2 passed, there is a
side-effect from the fix. Find the newly failing test and fix the root cause.

---

## When to invoke

Mandatory:
- After any fix to a service class (any file in a flow's `.Application` or `.Infrastructure`)
- After any change to a service constructor (DI wiring in `.Api/Program.cs`)
- After any `DataProcessResult<T>` change that affects a public method signature
- Before declaring any bug fix complete
- Before a per-flow phase-end Gate B / commit

Optional but recommended:
- After adding new test stubs to a test file
- After changing any `DataProcessError` code string (verify no test asserts on the old code)
- After modifying a record/DTO that crosses the Api↔Application↔Infrastructure boundary

---

## Quick routing table

| What was fixed | Level 1 spec | Level 2 spec | Level 3 command |
|---------------|-------------|-------------|-----------------|
| Activation guard logic | `BundleActivationService` `[Fact]` | Real service + real in-memory fabric | `dotnet test <Flow>.Tests.csproj` |
| Validator error code | `BundleValidatorService` `[Fact]` | Real validator + real document store | `dotnet test <Flow>.Tests.csproj` |
| Status tracker fan-out | `BundleStatusTrackerService` `[Fact]` | Real tracker + seeded bundles | `dotnet test <Flow>.Tests.csproj` |
| `DataProcessResult<T>` signature | All `[Fact]`s for that service | Caller + callee wired together | `dotnet test <Flow>.Tests.csproj` |
| DI registration in `.Api` | Registration `[Fact]` / smoke | `WebApplicationFactory`-style host | `dotnet test <Flow>.Tests.csproj` |

---

## Anti-patterns

### Anti-pattern 1: Declaring done after Level 1 only
```
✗ "The specific [Fact] passes" → phase complete
✓ Run Level 2 (real services) before declaring done — stubs may be too permissive
```

### Anti-pattern 2: Skipping Level 2 because "it takes longer"
```
✗ Level 1 passes → skip to Level 3
✓ Level 2 catches stub-masking failures that Level 3 cannot diagnose as quickly
```

### Anti-pattern 3: Level 2 with the same stubs as Level 1
```
✗ Level 2 test reuses the same canned stub validator and never tests the fixed path
✓ Level 2 uses real .Application + .Infrastructure services for the components in the fix
```

### Anti-pattern 4: Trusting Level 3 without Level 2
```
✗ Full suite passes → fix must be correct
✓ Full suite passing only means no regression — Level 2 proves the fix is correct
   for the specific scenario
```
