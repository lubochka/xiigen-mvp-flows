# root-cause-tracing-SKILL.md — Trace backward to the original trigger, then defend every layer

> **Stack adaptation (xiigen flows).** The backward-trace process, the rule
> "NEVER fix only where the error appears", the defense-in-depth-at-every-layer
> requirement after a source fix, the polluter-test bisection, and "the stack
> trace line number is not the root cause" are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/root-cause-tracing-SKILL.md` (G06 debugging /
> root-cause). Adapted here:
> - **Caller / assignment / instantiation search** is `grep` over
>   `flows/**/*.cs` (per-flow `Api`/`Application`/`Infrastructure`/`Tests`).
> - **Instrumentation** uses `Microsoft.Extensions.Logging` (`ILogger<T>` already
>   injected through `MicroserviceBase<TSelf>`) or `System.Diagnostics.Debug.WriteLine`
>   inside `Application`/`Infrastructure`.
> - **Verbose run** is `dotnet test --filter "FullyQualifiedName~<Test>" --logger "console;verbosity=detailed"`;
>   for a UI symptom, `npx playwright test --trace on` in the flow's `client/angular`.
> - **Defense-in-depth layers** for this repo: source → per-flow entry
>   (`Api`/host) → `Infrastructure` (fabric access) → xUnit assert (and the
>   Angular component guard when the symptom is UI).
> - Return type is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`); failures are `Failure<T>("CODE","message")`.

## Overview

Bugs often manifest deep in the call stack. Fixing where the error appears treats
a symptom.

**Core principle:** Trace backward through the call chain until you find the
original trigger, then fix at the source.

## When to Use

- Error appears in a method that is not the entry point
- Stack trace shows more than 2 levels
- Unclear where invalid data originated
- A test fails but the test code looks correct

---

## The Tracing Process

### 1. Observe the Symptom

```
Error: KeyNotFoundException in MarketplacePaymentsService.BuildDashboardAsync()
  at flows/FLOW-16-marketplace-payments/server/...Application/MarketplacePaymentsService.cs:212
  key 'platformFeeBps' missing from the document body
```

### 2. Find Immediate Cause

What line directly causes this?

```csharp
// Line 212: reading a fee key from a document that did not contain it
var fee = (int)body["platformFeeBps"];   // ← body has no such key for legacy docs
```

### 3. Ask: What Called This?

```bash
grep -rn "BuildDashboardAsync\|platformFeeBps" flows/FLOW-16-marketplace-payments --include="*.cs"
```

```
BuildDashboardAsync() ← called by GetDashboardAsync()
GetDashboardAsync()   ← called by the Api endpoint in Program.cs
... ← document was written by CheckoutAsync() in an earlier run
```

### 4. Keep Tracing Up

What value was written at each level?

```csharp
// CheckoutAsync wrote the document WITHOUT platformFeeBps when freedom returned a default
// IFreedomConfigService.Get("platform-fee-bps") returned null → the key was never set
// → root is upstream in the write path, not the read in BuildDashboardAsync
```

### 5. Find Original Trigger

```
Root cause: CheckoutAsync persisted a payment document without platformFeeBps
  because IFreedomConfigService returned no value and the code skipped the field.

Fix location: Either:
  A. CheckoutAsync writes a resolved default fee (fail-safe at the source), OR
  B. BuildDashboardAsync treats a missing fee as the documented default
     (defense-in-depth, not the primary fix)
```

---

## Adding Instrumentation

When manual tracing fails, add diagnostic output before the failing operation:

```csharp
// In BuildDashboardAsync() — log inputs to trace which write produced the bad doc
_logger.LogDebug(
    "DEBUG dashboard build: docId={DocId}, hasFee={HasFee}, tenant={Tenant}",
    docId, body.ContainsKey("platformFeeBps"), TenantId);

// Defense-in-depth guard at this layer too
if (!body.ContainsKey("platformFeeBps"))
    return Failure<MarketplacePaymentsDashboard>("MISSING_FEE", "Document has no platformFeeBps");
```

Run the test with detailed logging:
```bash
dotnet test --filter "FullyQualifiedName~MarketplacePayments" \
  --logger "console;verbosity=detailed" 2>&1 | grep "DEBUG dashboard build"
```

The log line shows which document (and therefore which write path) triggered it.

---

## Finding Which Test Causes Pollution

If a test leaves side effects (shared in-memory fabric state, a file) that affect
other tests:

```bash
# Bisection: run the first half of a class, check for pollution, then narrow
dotnet test --filter "FullyQualifiedName~MarketplacePaymentsServiceTests" \
  --logger "console;verbosity=detailed"
```

If the pollution is shared fabric state, give each test a fresh
`Xiigen.Fabrics.InMemory` instance (construct per test, not static), or add
cleanup in the test's `Dispose()`.

---

## Defense-in-Depth After Root Cause Fix

After fixing the root cause, add validation at every layer in the call chain:

```
Layer 1 (source): CheckoutAsync writes a resolved default fee — never an absent key
Layer 2 (entry):  the Api endpoint / host validates the request before dispatch
Layer 3 (impl):   BuildDashboardAsync returns Failure<T> for a missing fee key
Layer 4 (test):   xUnit asserts the dashboard contains a non-null fee
(Layer 5, UI):    the Angular component guards against a null fee before rendering
```

Each layer catches the bug independently. If one layer is bypassed, the others
still catch it.

---

## Key Principle

**NEVER fix just where the error appears.** Trace back to find the original
trigger.

```
Found error at BuildDashboardAsync() → trace to GetDashboardAsync → CheckoutAsync write path
  → FOUND: fee key never written when freedom returned a default

Fix at: CheckoutAsync (write a resolved default fee at the source)
Also add: guard at BuildDashboardAsync (defense-in-depth, not primary fix)
```

---

## Real Tracing Commands for This Repo

```bash
# Find all callers of a method
grep -rn "MethodName\|IInterfaceName\.Method" flows --include="*.cs"

# Find where a field/key is written
grep -rn "\"platformFeeBps\"\|FieldName\s*=" flows --include="*.cs"

# Find all test files for a flow
grep -rln "MarketplacePayments" flows/FLOW-16-marketplace-payments/server/*.Tests --include="*.cs"

# Find all places a service/fabric is instantiated or resolved
grep -rn "new MarketplacePaymentsService\|GetRequiredService<" flows --include="*.cs"
```

---

## Anti-Patterns

1. **Fix at the symptom only** — the root cause remains, the bug recurs on the next run
2. **Single-layer defense** — if the source fix is bypassed, no other layer catches it
3. **Add a null check at the caller** — this silences the error without understanding why null was produced
4. **Trust the stack trace line number** — the crash site is not the root cause

---

## Integration

- `systematic-debugging-SKILL.md` Phase 1 step 5 invokes this skill
- `defense-in-depth-SKILL.md` — multi-layer validation after the source fix
- `debug-session-SKILL.md` — persist the trace hypotheses across sessions
- `mental-debug-SKILL.md` Rule 4 (boundary-data-lifecycle) for where the shape changed
