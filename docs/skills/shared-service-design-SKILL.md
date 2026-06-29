# shared-service-design-SKILL.md — When a capability is shared infrastructure (.NET 10 per-flow)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/shared-service-design-SKILL.md` (G09). The universal
> core carries over unchanged: count how many subprojects use a capability, apply
> the **≥3 ⇒ SHARED INFRASTRUCTURE** threshold, classify by sharing pattern
> (A: write+read, B: shared-read, C: event bus), and build the contract BEFORE the
> first consumer. The stack mapping:
> - **"appears in N subprojects"** = referenced by N per-flow `*.Application`
>   projects (`flows/FLOW-NN-<name>/server/Xiigen.Flow.<Name>.Application`).
> - a **shared service** becomes a dedicated `Shared.*` / `Common.*` `.csproj` (or
>   lives in the existing `Xiigen.Kernel`/`Xiigen.Fabrics.*` fabric layer) with an
>   `I*` contract, registered **once** in the host (`builder.Services.AddSingleton<...>()`
>   inside a `*ServiceCollectionExtensions` like
>   `UserRegistrationServiceCollectionExtensions.AddUserRegistrationFlow`).
> - **Pattern C (event bus)** in flows is the outbox transport: `IOutboxStore.AppendAsync(CloudEventEnvelope)`
>   with store-before-dispatch (DNA-8), not a per-flow bus. A flow that needs an
>   in-process notification fan-out introduces a single `IEventBus`/MediatR
>   notification contract — **never** a second bus implementation per flow.
> - shared services return `DataProcessResult<T>` (XIIGen flow wrapper — **NOT**
>   core `OperationResult<T>`).
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + the consuming flows'
>   `dotnet test` (Failed 0).
> - trainable/common-model shared services stay in `llm_mvp_core` (R5/G12); flows
>   consumes them through a manifest/locator, it does not host the common model.

## When to load

- Before any multi-flow planning where a capability name appears in 2+ flows.
- During `subproject-examination-SKILL.md` when a capability recurs across flows.
- Before `service-boundary-design-SKILL.md` when a boundary spans flows.

---

## Core Problem

Building the same capability twice — once inside FLOW-A's `*.Application` and again
inside FLOW-B's — so the two implementations diverge and require double maintenance.
The same capability written twice in different flows is the most expensive
architectural mistake in a multi-flow system. This skill decides when a capability
is shared infrastructure (needs a single `I*` contract registered once) vs
per-flow, and produces the **Shared Services Map** before any multi-flow design.

---

## Section 1 — Count Service Appearances Across Flows

For each capability mentioned in any flow plan, count how many flow plans reference it:

```
IDocumentStore:        ████████  N of N flows   ← CRITICAL: shared infrastructure (Kernel)
IOutboxStore:          ███████   shared          ← shared infrastructure (Kernel, Pattern C)
IFreedomConfigService: ███████   shared          ← shared infrastructure (Xiigen.Freedom)
ISchedulerService:     █████     shared          ← shared infrastructure (Xiigen.Fabrics.Scheduler)
IOssCurriculumQueueBroker: █     1 of N flows    ← per-flow (only FLOW-39 publishes)
```

**Classification threshold (binding):**
- Appears in **≥ 3 flows** → SHARED INFRASTRUCTURE: a single `I*` contract, built
  once, registered once.
- Appears in **2 flows** → REVIEW: may be shared depending on usage type.
- Appears in **1 flow** → PER-FLOW: build inside that flow's `*.Infrastructure`.

---

## Section 2 — Classify Each Shared Service

**Pattern A — Shared Write + Shared Read (most complex).**
Multiple flows write to it AND multiple flows read from it.
Example: `IDocumentStore` — every flow upserts and searches documents.
Resolution: a fabric `I*` with write AND read methods, in `Xiigen.Kernel`, built as
standalone infrastructure BEFORE the first writing flow.

**Pattern B — Shared Read Only.**
One flow builds it; later flows only read.
Resolution: build it in the first flow's `*.Infrastructure`, document the read
interface as a cross-flow dependency, and have later flows reference the same `I*`
— never rebuild it.

**Pattern C — Shared Notification / Event Delivery.**
Multiple flows publish events; multiple flows (or a platform consumer) subscribe.
Example: `IOutboxStore` — FLOW-01 appends `verification-email-requested.v1`,
FLOW-05 appends gamification events; a downstream relay consumes all.
Resolution: one `IOutboxStore` fabric in `Xiigen.Kernel`; each flow appends its own
`CloudEventEnvelope` types. Never build a per-flow outbox/bus implementation.

---

## Section 3 — Order the Build Sequence

Shared infrastructure must be built (or at least stubbed as `I*` contracts) BEFORE
the first flow that uses it:

```
Before FLOW-01-user-registration can be built, these must already exist as contracts:
  IDocumentStore         ← stores user/token/onboarding records (Pattern A)
  IOutboxStore           ← emits verification + onboarding events (Pattern C)
  IFreedomConfigService  ← reads invitation copy / thresholds (config)
  ISchedulerService      ← schedules token expiry (ScheduleDelayedAsync)

These are registered together via services.AddXiigenInMemoryFabrics() +
AddXiigenFreedomConfig() + AddXiigenInMemoryScheduler() in the flow's DI extension.
```

---

## Section 4 — Shared Services Map Format

Before any multi-flow design session, produce:

```markdown
## Shared Services Map

| Service | Appearances | Pattern | Built in | Contract (I*) |
|---|---|---|---|---|
| IDocumentStore        | N flows | A: write+read | Xiigen.Kernel / Xiigen.Fabrics.InMemory | IDocumentStore |
| IOutboxStore          | N flows | C: event delivery | Xiigen.Kernel / Xiigen.Fabrics.InMemory | IOutboxStore |
| IFreedomConfigService | N flows | B: shared read | Xiigen.Freedom | IFreedomConfigService |
| ISchedulerService     | M flows | C: delayed action | Xiigen.Fabrics.Scheduler | ISchedulerService |

## Per-Flow Services (build inside the flow, share only the read contract)

| Service | Flow | Not shared because |
|---|---|---|
| IOssCurriculumQueueBroker | FLOW-39 | Only the curriculum flow publishes dispatch records |
```

`COMPONENT-INTERFACES`-style docs (or the flow interfaces doc) are the persistence
layer for this map.

---

## Anti-Patterns

1. **"Build a `TokenStore` in FLOW-01 and a `DocumentStore` in FLOW-02 — same idea,
   different names"** — same capability, two implementations. Identify the shared
   service first; one `IDocumentStore` covers both.
2. **"Build a per-flow outbox in each flow"** — every flow needs event delivery.
   Use the one `IOutboxStore`, registered once; each flow appends its own envelopes.
3. **"Discover in FLOW-09 that the shared store needs a new method, forcing a
   retroactive change to FLOW-01's already-shipped code"** — the canonical
   cross-cutting mistake. The Shared Services Map declares the full contract before
   any flow builds against it.
4. **"The contract can be built when the first flow needs it"** — too late. Build
   the stub `I*` and DI registration before the first dependent flow starts.

---

## Integration

- `service-boundary-design-SKILL.md` uses this classification to decide whether a
  boundary is a flow boundary or a class boundary.
- `change-propagation-SKILL.md` propagates a shared-contract change to every flow.
- `blast-radius-SKILL.md` rates a shared `I*` contract change as CRITICAL.
