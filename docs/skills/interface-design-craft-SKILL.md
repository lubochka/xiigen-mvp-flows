# interface-design-craft-SKILL.md — Every contract is a deliberate decision (.NET 10 contract craft)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/interface-design-craft-SKILL.md` (G09) — **contract
> part only.** The core skill is written for Windows Forms visual craft
> (SystemColors, Segoe UI, panel layout); that visual material is **out of scope
> here** and belongs to the Angular 21 client UI group (standalone components under
> `flows/FLOW-NN-<name>/client/angular`), not to this `.cs`-contract skill.
> What ports is the universal contract-craft core: **intent-first** (who/what/why
> for each contract), **one coherent responsibility per contract**, and the
> **swap test** for replaceability behind an interface. Stack mapping:
> - a contract = `public interface I*` in a flow's `*.Application`, methods
>   returning `Task<DataProcessResult<T>>` (XIIGen flow wrapper — **NOT** core
>   `OperationResult<T>`); real example: `IOssCurriculumQueueBroker` in
>   `flows/FLOW-39-oss-curriculum/server/Xiigen.Flow.OssCurriculum.Application/OssCurriculumResults.cs`.
> - DTOs are **immutable `record`** with `IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>`
>   (e.g. `OssCurriculumOutcome`, `QueueDispatchRecord`).
> - the "fabric" = DI registration in `*ServiceCollectionExtensions`
>   (`services.AddSingleton<...>()`); swapping an implementation = swapping the DI
>   line, callers unchanged.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` of the
>   owning flow (Failed 0).

## When to load

- Before adding or changing any `public interface I*` in a flow's `*.Application`.
- When a contract "works" but feels like a default (a CRUD wrapper over the store).
- Before designing the contract surface a new flow exposes to its `*.Api`.
- Pair with `service-boundary-design-SKILL.md` (where the boundary is) and
  `api-shape-verification-SKILL.md` (what the real signature is).

---

## Core Problem

A contract designed without intentional craft produces the same shape as every
other generated interface: a thin pass-through over `IDocumentStore` with
`GetAsync`/`SaveAsync`/`DeleteAsync` and no coherent responsibility. This happens
because defaults are invisible — they feel like infrastructure rather than design
decisions. This skill ensures every contract is a genuine choice with a stated
reason. (Visual craft for the Angular client is governed by the UI group, not here.)

---

## Section 1 — Intent First (for the contract)

Before declaring any `I*` method, answer three questions explicitly.

```
Question 1: WHO calls this contract?
  Not "the system" — the actual caller. A flow's *.Api endpoint? Another flow's
  service? A scheduler callback? The caller's needs shape the method set.

Question 2: WHAT must the caller accomplish in one call?
  Not "use the broker" — the specific outcome. "Publish one dispatch envelope and
  get back the dispatched record." The answer determines the method, its inputs,
  and its DataProcessResult<T> payload.

Question 3: WHAT is the ONE responsibility of this contract?
  Say it in a sentence. "Owns publishing curriculum messages to the queue and
  listing what was dispatched." If you need "and" twice, you have two contracts.
```

If you cannot answer all three with specifics, stop — you are about to default to a
generic store wrapper.

---

## Section 2 — One Coherent Responsibility

A contract owns exactly one responsibility. The real flow contract demonstrates it:

```csharp
public interface IOssCurriculumQueueBroker
{
    // Responsibility: dispatch curriculum messages and report what was dispatched.
    Task<DataProcessResult<QueueDispatchRecord>> PublishAsync(CloudEventEnvelope envelope, CancellationToken cancellationToken = default);
    Task<DataProcessResult<IReadOnlyList<QueueDispatchRecord>>> ListAsync(CancellationToken cancellationToken = default);
}
```

Both methods serve "queue dispatch." A method like `RecalculateUserScore(...)` would
not belong — that is a different responsibility and a different contract.

**Method-set test:** read the method names without the interface name. Do they all
describe one job? If one method is an outlier, split it into its own contract.

---

## Section 3 — Per-Contract Checkpoint

Before writing the `I*`, state the following:

```
Caller:        [who calls it + what they are doing — from Section 1]
Responsibility:[the single sentence — from Section 2]
Inputs:        [immutable record/value types; no mutable List<T>/Dictionary in/out]
Return:        [Task<DataProcessResult<T>> for async I/O; DataProcessResult<T> for sync compute]
Errors:        [which Failure<T>("CODE", ...) codes the implementation returns]
Swap:          [what a second implementation would look like — proves replaceability]
```

This checkpoint is mandatory before writing the contract. If you cannot fill the
`Swap` row, the contract is probably leaking an implementation detail.

---

## Section 4 — Contract Foundations (flows)

### Result type

Every async I/O method returns `Task<DataProcessResult<T>>`. Synchronous compute
returns `DataProcessResult<T>`. Never `void`, never a raw `T`, never a `bool`
without context, never `throw` for a business failure — return
`Failure<T>("CODE","message")` (the `MicroserviceBase<TSelf>` helpers `Success`/
`Failure<T>` are how implementations build the result). Core `OperationResult<T>`
must never appear in flows.

### Immutable DTOs

Inputs and outputs are `record` types with read-only collections:

```csharp
public sealed record QueueDispatchRecord(string MessageType, CloudEventEnvelope Event, DateTimeOffset DispatchedAt);
```

No setters, no `List<T>`/`Dictionary<,>` exposed across the boundary — use
`IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>` so a caller cannot mutate shared state.

### DI tokens as the fabric

The contract is the swap seam. Registration lives in the flow's
`*ServiceCollectionExtensions`:

```csharp
public static IServiceCollection AddOssCurriculumFlow(this IServiceCollection services)
{
    services.AddSingleton<IOssCurriculumQueueBroker, OssCurriculumQueueBroker>(); // swap impl here only
    return services;
}
```

A second implementation (e.g. a real-bus broker) replaces only this line; no caller
changes. That is the swap test made concrete.

---

## Section 5 — The Swap Test

Before presenting any contract:

```
The swap test: Could a different implementation satisfy this exact interface
  without changing a single caller? If a method exposes a store key, a SQL column,
  or a concrete type, the answer is no — that is a leak. Fix it before shipping.

The responsibility test: Name the one job in a sentence with no "and …and". If you
  cannot, split the contract.

The result test: Does every method return DataProcessResult<T> (or
  Task<DataProcessResult<T>>)? Any void / raw T / bool is a defaulted method.
```

If any test fails, iterate before writing the implementation.

---

## Anti-Patterns

1. **"A CRUD wrapper over `IDocumentStore` is fine"** — `Get/Save/Delete` with no
   coherent responsibility is the generic default. State the single responsibility
   first, then design the methods that serve it.
2. **"Return the entity directly"** — every method returns `DataProcessResult<T>`;
   a raw return type hides failure and breaks the flow error convention.
3. **"Expose `List<T>` so callers can append"** — mutable payloads let consumers
   corrupt shared state. Use `IReadOnlyList<T>` and records.
4. **"The interface can mention the store key/column"** — that fails the swap test.
   The contract must not encode the implementation's storage shape.
5. **(Out of scope here) visual styling** — Angular client craft is the UI group's
   skill, not this `.cs`-contract skill.

---

## Integration

- `service-boundary-design-SKILL.md` decides whether a responsibility is its own
  contract/class at all.
- `api-shape-verification-SKILL.md` verifies the real signature before tests call it.
- `domain-event-design-SKILL.md` governs the event records a contract emits.
- Angular client/UI craft is governed by the UI group (out of scope here).
