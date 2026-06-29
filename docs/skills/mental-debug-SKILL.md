# mental-debug-SKILL.md — Debug .NET flow problems systematically (8 rules + routing table)

> **Stack adaptation (xiigen flows).** The 8 diagnostic rules, the
> symptom→rule→next routing table, and the anti-pattern "the code looks correct
> so the test must be wrong → no" are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/mental-debug-SKILL.md` (G06 debugging / root-cause).
> The original used `llm_mvp_core` subjects (`ISemanticCompiler29`,
> `ProjectIndexer`, `OperationResult<T>`); those project-specific tables are
> rewritten here onto flows idioms:
> - Result boundary type is `DataProcessResult<T>` from `Xiigen.Kernel`
>   (`.IsSuccess` / `.ErrorCode` / `.Value`), produced by `MicroserviceBase<TSelf>`
>   helpers `Success<T>(...)` / `Failure<T>("CODE","message",details)` — **NOT**
>   core `OperationResult<T>`. Per-flow domain results are `sealed record`s in
>   `*.Application` (e.g. `MarketplacePaymentsOutcome` / `…Dashboard` in
>   `flows/FLOW-16-marketplace-payments/server/…Application/MarketplacePaymentsResults.cs`).
> - Service/subproject boundary = `Xiigen.Flow.<Name>.Application` ↔
>   `Xiigen.Flow.<Name>.Infrastructure`, per flow under `flows/FLOW-NN-*/server/`.
> - DI resolution is over shared kernel-fabric interfaces (`IDocumentStore`,
>   `IOutboxStore`, `IIdempotencyStore` from `Xiigen.Kernel`; `IFreedomConfigService`
>   from `Xiigen.Freedom`; `ISchedulerService` from `Xiigen.Fabrics.Scheduler`)
>   injected into the flow service ctor; in tests the concrete is usually
>   `Xiigen.Fabrics.InMemory`.
> - `interface-resolution-trace` here is the equivalent of the mvp
>   `fabric-resolution-trace`: confirm the correct concrete fabric was resolved.
> - For UI symptoms, Rule 4 also covers the Angular 21 standalone
>   component ↔ injectable service boundary.

## When to load

Load when a test fails unexpectedly or a `DataProcessResult<T>` value is wrong.

---

## The 8 Rules

| # | Rule | What it catches |
|---|------|----------------|
| 1 | pattern-recognition | Misidentifying what kind of failure this is |
| 2 | contextual-gap | Missing context the code assumes was injected (fabric / freedom / tenant) |
| 3 | mental-execution | Wrong mental model of how the code actually runs |
| 4 | boundary-data-lifecycle | Data shape changes at Application↔Infrastructure or API↔Angular boundaries |
| 5 | loop-off-by-one | Off-by-one in iteration, slicing, paging, or count checks |
| 6 | error-silent-swallow | `.IsSuccess` not checked — wrong data silently consumed |
| 7 | sibling-guard-parity | Guard exists in one flow service but missing in its symmetric sibling |
| 8 | interface-resolution-trace | Wrong concrete fabric resolved through a kernel interface |

---

## Diagnostic routing

| Symptom | Apply first | Then |
|---------|-------------|------|
| Wrong count (off by 1) | pattern-recognition → loop-off-by-one | mental-execution |
| All outputs collapse to one value/state | pattern-recognition → contextual-gap | boundary-data-lifecycle |
| Result is null or default unexpectedly | contextual-gap → error-silent-swallow | boundary-data-lifecycle |
| Test passes but output value is wrong | error-silent-swallow → mental-execution | sibling-guard-parity |
| DI resolves wrong type or NullReference | interface-resolution-trace → contextual-gap | mental-execution |
| `DataProcessResult.IsSuccess=true` but `.Value` wrong | error-silent-swallow → boundary-data-lifecycle | mental-execution |
| Guard in one flow service but not its sibling | sibling-guard-parity → contextual-gap | — |
| Angular renders empty/null but API returned data | boundary-data-lifecycle → interface-resolution-trace | mental-execution |

---

## Rule 1 — Pattern Recognition

Before debugging code, identify which class of failure you have.

| Pattern | Shape | Starting rule |
|---------|-------|---------------|
| Wrong count | actual=N, expected=M, diff=1 | loop-off-by-one |
| Silent wrong value | no exception, wrong output | error-silent-swallow |
| Null or default everywhere | all outputs identical | contextual-gap |
| Wrong status/branch taken | escrow released when it should hold | boundary-data-lifecycle |
| Works in one flow, broken in sibling | Checkout OK, Refund fails | sibling-guard-parity |

Anti-pattern: "The code looks correct so the failure must be in the test." The
failure is in the code until proven otherwise.

---

## Rule 2 — Contextual Gap

The code assumes something is injected or set that wasn't.

```
Checklist:
☐ Which kernel-fabric interface does this service need? Is the test providing the InMemory concrete?
☐ Does the ctor receive all required fabrics (IDocumentStore / IOutboxStore / IIdempotencyStore)?
☐ Is IFreedomConfigService injected (not reimplemented) where the flow reads config?
☐ Is ITenantContextAccessor / ICorrelationContextAccessor passed to MicroserviceBase<TSelf>?
☐ Is the IDocumentStore returning the seeded documents in this test path, or empty?
☐ If the test seeds a fixed tenant — does the assertion use that exact tenant id?
```

Common gaps in this repo:

| Missing context | Where needed | Symptom |
|----------------|-------------|---------|
| `IFreedomConfigService` not injected | flow service reading freedom keys | config default used → wrong fee/threshold |
| Tenant context not set | `MicroserviceBase<TSelf>` subclass | tenant-scoped read returns 0 documents |
| `IDocumentStore` empty for this tenant | flow read path | `IsSuccess=true` but empty payload |
| InMemory fabric not seeded | any per-flow test | domain assertion fails |

---

## Rule 3 — Mental Execution

Wrong mental model. Trace the actual execution path, not the path you assume.

**Protocol:**
1. Write the exact input to the method you are debugging
2. At every `if` / `switch`, check the ACTUAL condition value — do not assume
3. At every `await`, trace what actually returns
4. At every `DataProcessResult<T>` boundary, check: is `.IsSuccess` true or false here?
5. At every method call, check: does the return type match what the caller expects?

```csharp
// Trap: a switch over a status string that has a fall-through default
// changes the branch silently for an unknown status — check the ACTUAL value.

// Trap: DataProcessResult<bool>.Value is bool (value type) — cannot be null
// !(result.IsSuccess && result.Value)   ← correct
// result.Value ?? false                 ← WRONG — compiler error on value type
```

---

## Rule 4 — Boundary Data Lifecycle

Data changes shape or meaning as it crosses service/subproject and API↔client
boundaries.

```
Checklist:
☐ What type does the producer return at the boundary?
☐ What type does the consumer expect? Do they match exactly?
☐ Is it Xiigen.Kernel.DataProcessResult<T> or a per-flow sealed record (Outcome/Dashboard)?
☐ Does the Angular DTO/interface match the API contract record field-for-field?
☐ Does IReadOnlyList<T> need to be materialised before it is enumerated twice?
```

Common boundary mismatches:

| Boundary | Wrong assumption | Correct rule |
|----------|-----------------|-------------|
| Application → Infrastructure | both speak the same record | the record is owned by `*.Application`; Infrastructure maps to/from fabric documents |
| API → Angular client | field names identical | the Angular interface must mirror the contract DTO exactly (casing included) |
| flow A → flow B (cross-flow) | same payload shape | each flow owns its contract; cross-flow data goes through the manifest / outbox, not a shared type |

---

## Rule 5 — Loop Off-By-One

Off-by-one errors in indexing, paging, batch processing, or count assertions.

```
Checklist:
☐ What is the expected first iteration? Does the loop start at 0 or 1?
☐ Does < vs <= include or exclude the last element?
☐ For .Take(n) / .Skip(n) paging: is n the count or the last index?
☐ Is the loop modifying the collection it iterates?
☐ For count assertions: Assert.Equal(3, result.Count) — is 3 the right number?
```

Trace protocol: write out the first, second, and last iterations manually. The
bug is almost always at the last iteration (fencepost error).

---

## Rule 6 — Error Silent Swallow

`.IsSuccess` not checked — code reads `.Value` on a failed result.

```csharp
// Bug: reading .Value on a failed result → null payload consumed downstream
var result = await _documents.GetAsync(id, ct);
var doc = result.Value;            // if IsSuccess=false, Value is default → wrong data

// Fix: guard before access, propagate the failure
var result = await _documents.GetAsync(id, ct);
if (!result.IsSuccess)
    return Failure<MarketplacePaymentsOutcome>(result.ErrorCode!, result.ErrorMessage!);
var doc = result.Value!;
```

```
Checklist:
☐ Every DataProcessResult<T> consumer checks .IsSuccess before reading .Value
☐ No Failure<T>(...) returned with an empty ErrorCode string
☐ value-type T (bool, int): do NOT use ?? — read .Value directly after the guard
☐ Any catch block that only logs but returns Success = silent swallow → REJECT
☐ Any method returning Success(default) after a Failure path = silent swallow → REJECT
```

---

## Rule 7 — Sibling Guard Parity

An `.IsSuccess` check or validation exists in one flow service but not its sibling
that processes the same input type.

Common sibling pairs in this repo:

| Sibling A | Sibling B | Guard that must be in both |
|-----------|-----------|--------------------------|
| Checkout path | Refund / Payout path | `.IsSuccess` check on the fabric read result |
| Create / write path | Read / dashboard path | tenant-scope guard before access |
| ACCEPTED branch | REJECTED branch | outbox event written with the correct type |

```
Checklist:
☐ If service A checks .IsSuccess on fabric X, does service B also check it?
☐ If A validates the tenant/idempotency key, does B apply the same validation?
☐ If A returns a named ErrorCode on the empty case, does B return a parallel code?
```

---

## Rule 8 — Interface Resolution Trace

The wrong concrete type is resolved through a kernel-fabric interface — usually a
test wiring the wrong fabric, or a missing DI registration. (This is the flows
analogue of the mvp `fabric-resolution-trace`.)

```
Checklist:
☐ Which concrete does the test pass for IDocumentStore — the InMemory fabric or a stub?
☐ Is that fabric seeded with the documents the test asserts on?
☐ Does the registered concrete match the interface signature exactly?
☐ In the host: is each fabric registered before the flow service that injects it?
☐ Is there exactly one registration per interface (no duplicate AddSingleton shadowing)?
```

Anti-pattern: "The interface is correct — it must be the algorithm." Check the
resolved concrete first. In this repo most test failures are an unseeded or
wrong-concrete fabric, not the flow algorithm being wrong.
