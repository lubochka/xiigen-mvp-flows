# dna-compliance-guard-SKILL.md — Per-commit DNA compliance check for every .cs file

> **Stack adaptation (xiigen flows).** Per-commit DNA gate ported from
> `llm_mvp_core/docs/skills/dna-compliance-guard-SKILL.md` (G04) and translated
> through `docs/skills/DNA-DOTNET-TRANSLATION.md` (the NestJS→.NET seed). The
> original nine XIIGen DNA rules map onto this repo's real .NET idioms:
> - **Rule 3 (Result pattern)** → every `Application` public method returns
>   `DataProcessResult<T>` (the actual flow type, e.g.
>   `Task<DataProcessResult<OnboardingOutcome>>` in
>   `Xiigen.Flow.UserRegistration.Application/OnboardingDeliveryService.cs`), never
>   `throw` for a business failure. Return `Failure<T>("CODE","message",details)`.
>   This is the XIIGen family wrapper — **NOT** core `OperationResult<T>`.
> - **Rule 4 (interface/base contracts)** → fabric/external dependencies of a flow
>   resolve through interfaces (real names named by the plan:
>   `IPaymentFabricService`, `ICanvaWriter`, `IMiroWriter`,
>   `IClientPushConnectionPool`, `IOssCurriculumQueueBroker`); services inherit
>   `MicroserviceBase<TSelf>`; no orphan concrete classes.
> - **Rule 1 (no hardcoded config)** → parameters/settings come from config/options
>   objects, not inline magic constants buried in logic.
> - **Rule 5 (scope isolation / thread-safe)** → no per-call static mutable state;
>   handlers are thread-safe; tenant/correlation context comes from the ambient
>   accessor, not a passed parameter.
> - **Rule 7 (idempotent atomic writes)** → checkpoint/export writes are temp+rename,
>   idempotent on retry.
> - Detection greps run against `*.cs` under
>   `flows/FLOW-NN-*/server/Xiigen.Flow.<Name>.{Application,Infrastructure}`.
> - Gate-B for any generation/codegen phase = `dotnet build Xiigen.Flows.slnx`
>   (0 errors AND 0 warnings, `TreatWarningsAsErrors=true`) + the generator's
>   `[Fact]` returns `IsSuccess=true` for a known input.
> - ML / DPO / trainable-unit logic stays in `llm_mvp_core` (R5 / G12). "No external
>   ML frameworks" applies as a boundary check (this repo must not pull in
>   TorchSharp/ML.NET), not as a here-trained-model rule.

## When to load

Load before committing any new or modified `.cs` file under a flow's server
projects (not in a `.Tests` project).
Load during EXECUTOR sessions when a new service class is created.
Load after any refactor that touches a public method signature.

---

## The DNA Guard Patterns (flows mapping)

| Rule | Check | Detection command (run per flow) |
|------|-------|----------------------------------|
| Rule 1 | No hardcoded config constants buried in logic | `grep -rn "= 0\.[0-9]" --include="*.cs" flows/FLOW-NN-*/server | grep -v "readonly\|const\|//\|Tests"` |
| Rule 2 | Dynamic filters, not hardcoded selectors | review `.Where(e => e.X == "literal")` for missing filter parameter |
| Rule 3 | Every Application public method returns DataProcessResult<T> | see below |
| Rule 4 | Service inherits MicroserviceBase<TSelf>; deps via interface | see below |
| Rule 5 | No per-call static mutable state; context from accessor | `grep -rn "static.*=" --include="*.cs" flows/FLOW-NN-*/server | grep -v "readonly\|const\|//\|Tests"` |
| Rule 7 | Idempotent atomic checkpoint/export writes (temp+rename) | review `File.WriteAllBytes(path,...)` without `.tmp`+`Move` |
| Rule 4b | `dotnet test` green before commit | `dotnet test ... 2>&1 \| tail -3` |
| Boundary | No external ML framework / no core OperationResult leak | `grep -rn "using TorchSharp\|using Microsoft\.ML\|OperationResult<" --include="*.cs" flows/FLOW-NN-*/server` |

---

## Pre-commit gate — run on every modified file

```bash
FILE="<path-to-modified-file>"

# Rule 3: Application public methods must return DataProcessResult<T> (not void/raw)
grep "public .*void " "$FILE" | grep -v "//\|override void Dispose"
# Expected: no output (void only for Dispose)

grep "public .*Task " "$FILE" | grep -v "DataProcessResult\|//\|override"
# Expected: no output (all async Application methods wrap in DataProcessResult<T>)

# Rule 3: no throw for business logic — use Failure<T>("CODE","message")
grep -n "throw new" "$FILE" | grep -v "// infrastructure\|ArgumentNullException.ThrowIfNull"
# Review: any business-failure throw is a violation → return Failure<T>(...)

# Rule 4: no concrete instantiation of a dependency in a constructor
grep "new [A-Z][a-z]" "$FILE" | grep -v "new List\|new Dictionary\|new string\|new [A-Z].*Options\|new [A-Z].*Request\|new [A-Z].*Outcome"
# Any new XyzService()/new XyzClient() in a ctor is a Rule 4 violation → inject the interface

# Rule 4: service must inherit MicroserviceBase<TSelf>
grep "class .*Service" "$FILE" | grep -v "MicroserviceBase"
# Review: any service not inheriting MicroserviceBase<TSelf> needs investigation

# Boundary: no core OperationResult<T> leak, no ML framework
grep -n "OperationResult<\|using TorchSharp\|using Microsoft\.ML" "$FILE"
# Expected: no output
```

Expected for a clean file: all sections return no violations.

---

## Quick verdicts

| Observation | Rule | Action |
|-------------|------|--------|
| `public void Process(...)` in Application | Rule 3 | Change to `public DataProcessResult<bool>` |
| `public Task<string> DeliverAsync(...)` | Rule 3 | Change to `public Task<DataProcessResult<string>>` |
| `throw new InvalidOperationException("not found")` for a business case | Rule 3 | `return Failure<T>("NOT_FOUND", "…")` |
| `_payments = new PaymentFabricService()` in a ctor | Rule 4 | Inject `IPaymentFabricService payments` |
| `class OnboardingDeliveryService` (no `: MicroserviceBase<...>`) | Rule 4 | Add `: MicroserviceBase<OnboardingDeliveryService>` |
| Line with no `//` comment | Rule 3-comment | Add a plain-English comment explaining the line |
| `OperationResult<T>` referenced here | Boundary | Replace with `DataProcessResult<T>` (core type must not leak into flows) |
| `using Microsoft.ML;` | Boundary | Remove — trainable/ML logic belongs in `llm_mvp_core` (G12) |

---

## When a violation is found

1. Do NOT commit the violating file
2. Fix the violation in the production code (not in the test)
3. Re-run `dotnet build` — must be 0 errors AND 0 warnings
4. Re-run `dotnet test` — must be 0 failures
5. Re-run the pre-commit gate — must show no violations
6. Then commit

---

## Rule 4 — Fabric First expanded

The most common Rule 4 violation is calling `new ConcreteService()` instead of
accepting an interface:

```csharp
// VIOLATION: concrete instantiation
public OnboardingDeliveryService()
{
    _push    = new ClientPushConnectionPool();   // ← VIOLATION
    _queue   = new OssCurriculumQueueBroker();    // ← VIOLATION
}

// CORRECT: inject through interfaces, self-typed base
public sealed class OnboardingDeliveryService : MicroserviceBase<OnboardingDeliveryService>, IOnboardingDelivery
{
    public OnboardingDeliveryService(IClientPushConnectionPool push, IOssCurriculumQueueBroker queue)
    {
        _push  = push;   // injected
        _queue = queue;  // injected
    }
}
```

Real flow interfaces to inject (never `new` directly): `IPaymentFabricService`,
`ICanvaWriter`, `IMiroWriter`, `IClientPushConnectionPool`,
`IOssCurriculumQueueBroker`, and the per-flow document/outbox/idempotency stores.

---

## Rule 3 — DataProcessResult<T> return pattern

```csharp
// VIOLATION: raw return types / throw for business logic
public string GetStatus(string id) { ... }
public bool IsComplete(Delivery d) { ... }
throw new InvalidOperationException("delivery incomplete");   // ← VIOLATION

// CORRECT: always wrapped, Failure for business errors
public DataProcessResult<string> GetStatus(string id) => Success(status);
public DataProcessResult<bool>   IsComplete(Delivery d) => Success(complete);
return Failure<OnboardingOutcome>("DELIVERY_INCOMPLETE", "Not all onboarding materials are present");
```

`Success<T>(...)` / `Failure<T>("CODE","message",details)` are the
`MicroserviceBase<TSelf>` helpers. Propagate inner errors with
`stored.Error?.Code` / `stored.Error?.Message` as the real flow code does.

---

## Rule 7 — Idempotent atomic writes

```csharp
// BAD — direct overwrite, crash mid-write = corrupt checkpoint/export
File.WriteAllBytes(path, data);

// GOOD — atomic temp + rename, idempotent on retry
File.WriteAllBytes(path + ".tmp", data);
File.Move(path + ".tmp", path, overwrite: true);
```

Calling an export/checkpoint write twice with the same key must not corrupt the
previous artifact. Persist state before dispatching the next unit (outbox order:
`storeDocument()` before `enqueue()`).

---

## Generated output audit

Any flow that generates code/contracts must pass this audit before commit:

```bash
GENERATED_FILE="<path-to-generated-output>"

# 1. Does the generated output build without errors/warnings?
dotnet build Xiigen.Flows.slnx 2>&1 | grep -E "error|warning"
# Expected: 0 errors, 0 warnings

# 2. Does the generator return DataProcessResult<T> on all paths?
grep "public .*void .*Generate\|public .*Task .*Generate" <generator-file> | grep -v "DataProcessResult"
# Expected: no output

# 3. Any hardcoded absolute paths in generated output?
grep -n "C:\\\\\|/home/\|/Users/" "$GENERATED_FILE"
# Expected: no output
```

### Gate B format for generation phases

```
Gate B: dotnet build Xiigen.Flows.slnx exits 0 with 0 warnings after generation;
        <GeneratorClass>.Generate(<sample input>) returns IsSuccess=true;
        generated output contains expected token/contract count (was 0)
```
