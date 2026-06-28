# service-boundary-design-SKILL.md — Where one service ends and the next begins (.NET 10 per-flow)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/service-boundary-design-SKILL.md` (G09). The four
> boundary tests, the Decision Table, and the "method vs class" threshold are
> universal and carry over unchanged. The stack mapping is:
> - **class boundary** = a C# class behind an `I*` interface (e.g.
>   `IOssCurriculumQueueBroker` in
>   `flows/FLOW-39-oss-curriculum/server/Xiigen.Flow.OssCurriculum.Application/OssCurriculumResults.cs`,
>   implemented in `*.Infrastructure`). A flow service inherits
>   `MicroserviceBase<TSelf>` and returns `DataProcessResult<T>` (the XIIGen flow
>   wrapper — **NOT** core `OperationResult<T>`).
> - **subproject boundary** = the per-flow 4-project set
>   `flows/FLOW-NN-<name>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   (confirmed on disk for FLOW-01-user-registration). Clean-Architecture layers
>   already encode the domain boundary: **Domain-Ownership ⇒ business logic in
>   `*.Application`, I/O in `*.Infrastructure`, HTTP entry in `*.Api`.**
> - **Gate-B** for any boundary decision that changes code =
>   `dotnet build Xiigen.Flows.slnx` (0 errors, 0 warnings) +
>   `dotnet test` of the affected `*.Tests` project (Failed 0).
> - the `.csproj` boundary in core maps to the per-flow `*.{Api,Application,Infrastructure}`
>   project split; the `Xiigen.Flows.slnx` solution lists every project node.
> - ML / trainable boundaries stay in `llm_mvp_core` (R5 / G12); flows hosts only
>   inference/consumer boundaries.

## When to load

- Before splitting a flow's logic into multiple services or merging two.
- When a UML sequence maps to task-types and you must decide class vs method.
- When a capability appears to span two flows (then load
  `shared-service-design-SKILL.md` first).
- Before adding a new `I*` contract to a flow's `*.Application` project.

---

## What This Skill Prevents

**Over-splitting:** one algorithm per class where a cohesive service is correct.
Tests are shallow; each class is trivially simple with no meaningful behavior.
In flows this shows up as five `*.Application` classes that only exist to forward
one call each to `IDocumentStore`.

**Under-splitting:** one `MicroserviceBase<TSelf>` class that does registration,
email verification, AND onboarding delivery. Untestable in isolation; a change to
one concern breaks unrelated `[Fact]` tests.

---

## The Four Boundary Tests

Apply all four. The boundary that passes the most tests is correct.

### Test 1 — Transaction Boundary Test

Operations that must succeed or fail atomically belong in the same class.

```
EmailVerificationService.IssueToken() + IOutboxStore.AppendAsync(verification-email-requested)
  → If the outbox append fails after the token document is written, the flow is
    in an inconsistent state (token exists, no email will be sent).
  → They share a consistency boundary → keep in one service, store-before-dispatch
    (Upsert the document, THEN AppendAsync the event — DNA-8 order).

OnboardingDeliveryService.CompleteAsync() + a downstream analytics read
  → The analytics read happens independently of whether onboarding completed.
  → No shared transaction boundary → SEPARATE service / separate fabric call.
```

### Test 2 — Lifecycle Test

Classes with different lifetimes, caching, or update frequency belong apart.

```
UserRegistrationBfaRuleRegistry — static rule set, built once, never mutated → its own type
OnboardingDeliveryService       — singleton, holds no per-call mutable state → separate service
A per-request token record      — created and expired per registration → a record, not a service
```

In flows, every service registered with `services.AddSingleton<...>()` in the
`*ServiceCollectionExtensions` is a singleton; per-call mutable state is therefore
forbidden (it would leak across tenants — see `dna-compliance-guard-SKILL.md`
Rule 5). Different lifetime = different type.

### Test 3 — Consumer Independence Test

If service A's output is consumed by code that does not need service B's output,
A and B belong in different classes behind different interfaces.

```
IOssCurriculumQueueBroker.PublishAsync(...) → consumed by the curriculum dispatch path
IDocumentStore.UpsertAsync(...)             → consumed by every flow service
IFreedomConfigService.GetAsync(...)         → consumed by config-reading services only

Different consumers → different interfaces → separate classes.
```

### Test 4 — Domain Ownership Test

Classes in different conceptual domains belong apart even if they run sequentially.

```
EmailVerificationService (verification domain) → owns: tokens, TTL, supersede-active
OnboardingDeliveryService (onboarding domain)  → owns: workspace/tutorial/invitation delivery

Different domains → different classes? YES.
But the flow boundary (FLOW-01) is the subproject; the class boundary inside it is
single coherent responsibility. Both services live in
Xiigen.Flow.UserRegistration.Application — same subproject, different classes.
```

**Layer corollary (Clean Architecture, flows-specific):** the Domain-Ownership
test also decides the *layer*. Pure business logic → `*.Application`. Anything
that talks to storage, scheduler, HTTP, or the file system → `*.Infrastructure`
behind an `I*` contract. The HTTP surface → `*.Api`. A class that mixes business
rules with `HttpClient`/`File.*` calls has failed the layer boundary.

---

## The Decision Table

| Transaction shared? | Lifecycle shared? | Consumers shared? | Decision |
|---------------------|-------------------|-------------------|----------|
| YES | YES | YES | MERGE — one class |
| YES | YES | NO  | MERGE — same class, callers call it separately |
| YES | NO  | any | SPLIT — different classes, same flow project |
| NO  | YES | YES | MERGE if domains are compatible |
| NO  | YES | NO  | SPLIT |
| NO  | NO  | any | SPLIT — definitely separate |

This table is a **binding rule**, not advice. Record the YES/NO row that produced
the decision in the plan's §1 algorithm note.

---

## Method vs Class Boundary

Not everything needs its own class. Some things are methods within a class.

**Becomes its own CLASS when:**
- It owns state (fields) managed independently.
- It has its own lifecycle (constructed once vs per-call).
- It can be swapped at runtime behind an `I*` interface (Fabric First).
- It has 3+ callers across different flows.

**Stays a METHOD within a class when:**
- It is stateless and called synchronously.
- Its failure does not change the calling class's state.
- No other class needs it independently.

```
OnboardingDeliveryService.StoreDeliveryAsync(...) → private METHOD
  - Called only by the three Deliver* methods of the same service.
  - No other flow needs it independently. No state to manage.

IOssCurriculumQueueBroker (PublishAsync/ListAsync) → SEPARATE CLASS behind an interface
  - Swappable Infrastructure implementation, registered in DI.
  - Different consumers, owns the dispatch records list.
```

---

## Subproject (per-flow project) Boundary Design

The `*.{Api,Application,Infrastructure,Tests}` split has coarser granularity than
a class.

**New flow project set when:**
- Genuinely different flow domain with its own task-types and BFA rules.
- A clear new `FLOW-NN-<name>` row in the flow map.
- Its own `Add<Name>Flow()` DI extension and `Program.cs` entry.

**Same flow, same project when:**
- Classes are closely related (two services of the same flow domain).
- They share the flow's fabrics, constants, and outcome records.
- No meaningful reason to separate them into another flow.

---

## Recording Boundary Decisions

For each significant boundary decision, record it where the flow's decisions live
(flow README / decisions doc), in this shape:

```
D-NN: EmailVerificationService and OnboardingDeliveryService are separate services
Date: YYYY-MM-DD
Decision: two services in Xiigen.Flow.UserRegistration.Application, not one
Alternatives: one UserRegistrationService doing verification + onboarding
Rationale: different domains (Test 4), different consumers (Test 3); merging would
           produce a class whose [Fact] tests cross unrelated concerns
Affected: Xiigen.Flow.UserRegistration.{Application,Infrastructure,Tests}, Program.cs DI
Gate-B: dotnet build Xiigen.Flows.slnx (0/0) + dotnet test UserRegistration.Tests (Failed 0)
```

---

## Anti-Patterns

1. **One UML box = one class** — boxes describe interactions, not implementation units.
2. **Splitting on technical lines** — `DocumentWriter` and `DocumentReader` as
   separate classes when they share `IDocumentStore` and a transaction boundary.
3. **Every stateless helper as its own class** — a format helper is a private
   method, not a service.
4. **Under-splitting for "simplicity"** — a 500-line `MicroserviceBase<TSelf>` with
   8 unrelated responsibilities is not simple; it is untestable.
5. **Business logic in `*.Infrastructure` or I/O in `*.Application`** — the layer
   boundary IS the Domain-Ownership test in this repo. Move the code to the layer
   that owns it.
