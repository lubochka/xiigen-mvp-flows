# defense-in-depth-SKILL.md — 4 validation echelons, return Fail not throw (xiigen flows)
## v1.0: ported from llm_mvp_core defense-in-depth → .NET 10 per-flow / xUnit

## Core Principle

When you fix a bug caused by invalid data, adding validation at one place feels
sufficient. But that single check can be bypassed by different code paths,
refactoring, or test mocks.

**Single validation: "We fixed the bug"**
**Multiple layers: "We made the bug structurally impossible"**

In flows, every echelon returns a flows-domain `DataProcessResult<T>` failure —
never throws (Rule 1). A validation layer that throws instead of returning a
failure is a Rule 1 violation.

---

## When to Invoke

- After tracing the source of invalid data in a flow
- When a bug was caught only at a deep call site (symptom far from cause)
- After any fix to a method that receives untrusted / externally-provided input
- When adding a new service that crosses a system boundary (Api request, file I/O, IPC)

---

## The Four Layers

### Layer 1: Entry Point Validation (the `.Api` boundary of the flow)

Reject obviously invalid input at the first point it enters the flow — the Api
endpoint / the `.Application` entry method.

```csharp
// ✅ Layer 1: reject at the flow boundary (DataProcessResult, not throw)
public async Task<DataProcessResult<BundleActivationOutcome>> ActivateAsync(
    IReadOnlyDictionary<string, object?> request, string ifMatch, string idempotencyKey)
{
    if (request is null || !request.TryGetValue("bundleId", out var raw) || raw is not string bundleId || string.IsNullOrWhiteSpace(bundleId))
        return DataProcessResult<BundleActivationOutcome>.Failure("VALIDATION_FAILED", "bundleId is required");
    if (string.IsNullOrWhiteSpace(idempotencyKey))
        return DataProcessResult<BundleActivationOutcome>.Failure("VALIDATION_FAILED", "idempotencyKey is required");
    // ... proceed
}
```

(Production also uses the inherited service helper form `Failure<TOut>("CODE",
"message")`; both produce the same `DataProcessResult` failure.)

### Layer 2: Business Logic Validation (the `.Application` invariants)

Ensure the data makes sense for the specific operation.

```csharp
// ✅ Layer 2: validate domain invariants in .Application
if (bundle.IsFailure || bundle.Data is null)
    return DataProcessResult<BundleActivationOutcome>.Failure("BUNDLE_NOT_FOUND", $"Bundle {bundleId} not found");

if (!VersionMatches(bundle.Data, ifMatch))
    return DataProcessResult<BundleActivationOutcome>.Failure("CONCURRENT_UPDATE", "bundle version changed");
```

### Layer 3: Environment Guards (test vs production)

Prevent dangerous operations in specific contexts.

```csharp
// ✅ Layer 3: refuse destructive operations outside the test temp root during testing
public DataProcessResult<bool> ClearFlowState(string storagePath)
{
    var testRoot = Environment.GetEnvironmentVariable("TEST_TEMP_ROOT");
    if (testRoot != null && !storagePath.StartsWith(testRoot, StringComparison.Ordinal))
        return DataProcessResult<bool>.Failure("UNSAFE_TEST_OPERATION",
            $"Refusing to clear flow state outside test temp root during testing: {storagePath}");
    // ... proceed
}
```

### Layer 4: Debug Instrumentation

Capture context for forensics when other layers fail (structured logging, never
secrets).

```csharp
// ✅ Layer 4: log context at the call site for forensic analysis
_logger.LogDebug("ActivateAsync called: bundleId={BundleId}, idempotencyKey={Key}, correlation={Corr}",
    bundleId, idempotencyKey, _correlationContextAccessor.Current?.CorrelationId);
```

---

## Applying the Pattern

When you find a bug that reached a deep call site:

1. **Trace the data flow** — where did the bad value originate? Where was it used?
2. **Map all checkpoints** — list every point the data passes through (Api → Application → Infrastructure → fabric).
3. **Add validation at each layer** — entry, business logic, environment, debug.
4. **Test each layer independently** — verify that bypassing Layer 1 is caught by Layer 2.

---

## Flows-Specific Example

Bug: empty `bundleId` produced a partial activation document deep in the store call.

**Data flow:**
1. Api request → `bundleId = ""`
2. `BundleActivationService.ActivateAsync(...)`
3. `InMemoryDocumentStore.UpsertAsync(BundleActivations, "", ...)` → partial write

**Four layers added:**
- Layer 1: `ActivateAsync` validates `bundleId` not null/empty → `VALIDATION_FAILED` before any store call.
- Layer 2: `.Application` confirms the bundle exists and the version matches (`BUNDLE_NOT_FOUND` / `CONCURRENT_UPDATE`) before mutation.
- Layer 3: if `TEST_TEMP_ROOT` is set, refuse destructive flow-state operations outside it.
- Layer 4: log `bundleId`, `idempotencyKey`, and correlation id at entry.

**Result:** the bug is structurally impossible — it is caught at Layer 1 before
reaching the store.

---

## DataProcessResult Pattern Enforcement

Every layer returns `DataProcessResult<T>` — never throws (Rule 1). The check at
each layer is:

```csharp
// ✅ validation returns a failure, never throws
if (invalidCondition)
    return DataProcessResult<T>.Failure("ERROR_CODE", "reason");
```

A validation layer that throws instead of returning a failure is a Rule 1
violation.

---

## Integration

- Load after the source of the bug is traced.
- Find all callers across flows before adding Layer 1 validation (blast radius — retroactive-development-SKILL.md).
- Each validation layer produces one test case (bug-to-tests-SKILL.md: one failing
  `[Fact]` per layer before adding the check).
