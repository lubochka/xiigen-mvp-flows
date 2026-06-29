# domain-event-design-SKILL.md — Event naming, payload, and additive evolution (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/domain-event-design-SKILL.md` (G09) — the **native .NET
> variant** of the core skill (core is already C#). Universal core carries over
> unchanged: events name the **entity + past-tense verb**, payloads carry **only
> stable identifiers** (not internal DB ids), payloads are **immutable**, evolution
> is **additive-only** (version when breaking), and the **schema freezes once a
> subscriber exists**. Stack mapping (verified against real flow code):
> - flows emit events as a `CloudEventEnvelope` whose **`type`** is the canonical
>   name, e.g. `"xiigen.user-registration.onboarding-completed.v1"` — a dotted,
>   versioned string. The "entity + past verb" rule applies to the type segment
>   (`onboarding-completed`, `verification-email-requested`, `charge-captured`).
> - payload (`data`) and any C# event/outcome record are **immutable `record`** with
>   `IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>`; producing methods return
>   `DataProcessResult<T>` (XIIGen flow wrapper — **NOT** core `OperationResult<T>`).
> - cross-flow contracts are documented in the flow interfaces doc (the flows
>   analogue of `COMPONENT-INTERFACES.md`); a breaking change bumps the version
>   segment (`.v1` → `.v2`), never a silent reshape.
> - the transport is `IOutboxStore.AppendAsync` with store-before-dispatch (DNA-8);
>   see `event-contract-design-SKILL.md` for the correlation-field schema.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` (Failed 0).

## When to load

- Before defining any new event a flow emits.
- When naming an event or designing its payload.
- When an event begins crossing a flow boundary (then also load
  `event-contract-design-SKILL.md`).

---

## What This Skill Prevents

Event names that do not form a coherent vocabulary. Payloads that drop fields across
refactors, silently breaking downstream consumers. Consumers coupled to internal
state that should not cross a flow boundary.

---

## Rule 1 — Naming Convention

The canonical name is the **`type`** segment of the `CloudEventEnvelope`:
`xiigen.<flow>.<entity>-<past-tense-verb>.v<N>`.

```
✅ Correct
"xiigen.user-registration.onboarding-completed.v1"
"xiigen.user-registration.verification-email-requested.v1"
"xiigen.payments.charge-captured.v1"

❌ Wrong
"xiigen.user-registration.onboarding.v1"        // no verb — what happened?
"xiigen.user-registration.onboarding-event.v1"  // "event" suffix — name the transition
"xiigen.user-registration.onboarding-update.v1" // "update" — ambiguous, which update?
"xiigen.user-registration.onboarding-was-completed.v1" // "was" — use simple past
```

For C# event/notification record types (when a flow uses an in-process notification),
name them `[Entity][PastTenseVerb]Message`:

```csharp
public sealed record OnboardingCompletedMessage(string UserId, string TenantId, DateTimeOffset CompletedAt);
```

State-machine transitions use the specific state, not a generic "updated":
```
TokenIssuedMessage → TokenSupersededMessage → TokenExpiredMessage
NOT: TokenUpdatedMessage (ambiguous)
```

---

## Rule 2 — Payload Design

Every payload carries only what consumers NEED:

```csharp
// ✅ stable identifiers, no internal state, enough to act without a lookup
public sealed record OnboardingCompletedMessage(
    string UserId,           // stable reference — consumers can look up details
    string TenantId,         // scope — which tenant
    DateTimeOffset CompletedAt);

// ❌ internal state, mutable objects, raw encodings
public sealed record OnboardingCompletedMessage(
    int DocumentRowId,       // internal — unstable if storage changes
    List<string> Materials,  // mutable — a consumer can mutate shared state
    byte[] SerializedState); // implementation coupling
```

**Payload rules:**
1. **Stable identifiers, not internal ids.** Carry `UserId`, not a storage row key.
2. **Never mutable objects.** Use `IReadOnlyList<T>`/`IReadOnlyDictionary<K,V>` and
   `record`, never `List<T>` or a class with setters.
3. **Enough to act without a lookup.** If most consumers need `TenantId` alongside
   `UserId`, include both; do not force a synchronous callback to fetch context.
4. **Additive evolution.** Future versions add fields; they never remove or rename
   existing fields — that breaks existing consumers.

The real flow event body follows this: it carries `userId`, `tenantId`,
`onboardingCompletedAt` — stable ids and a timestamp, no internal row id, no raw
object.

---

## Rule 3 — Subscriber Count Awareness

When an event has more than 5 consumers it is shared infrastructure and needs
careful design — consider whether a more specific event is better:

```
xiigen.user-registration.onboarding-completed.v1
  → consumed by: analytics relay, welcome-email relay, gamification seed, ...

At 5+ consumers, ask: should this be split (e.g. ...onboarding-completed-for-analytics.v1
vs ...onboarding-completed-for-gamification.v1)?
```

---

## Rule 4 — Cross-Flow Event Contracts

When an event crosses a flow boundary (FLOW-A emits → FLOW-B/platform consumes), it
is a cross-flow contract with stricter rules:

1. **Schema freezes once a consumer exists.** After a consumer subscribes to
   `…charge-captured.v1`, the payload cannot change without a version bump.
2. **Document it in the flow interfaces doc** (the `COMPONENT-INTERFACES.md`
   analogue). Cross-flow events are part of the producing flow's public interface.
3. **Include the minimum viable payload for all current consumers** — not the lowest
   common denominator; include what every current consumer needs.
4. **Version on breaking change** — `…charge-captured.v2`, never a silent reshape of
   `.v1`.

---

## Step-by-Step: Design an Event

```
1. Name entity + verb: onboarding + completed → "…onboarding-completed.v1"
2. Naming check: past tense? clear entity? versioned? ✅
3. Who emits: OnboardingDeliveryService.CompleteAsync via IOutboxStore.AppendAsync
4. Who consumes: list every relay/flow that subscribes
5. Consumer count ≤ 5? ✅ (else reconsider granularity)
6. Payload: what does each consumer NEED? start minimal, add only required fields
7. Cross-flow check: crosses a flow boundary? YES → document + frozen-schema rule
8. Immutability: record + IReadOnly collections? ✅
9. Register: document in the flow interfaces doc if cross-flow
10. Verify naming consistency against existing event types
```

---

## The Event Vocabulary

Maintain a consistent vocabulary. Before adding a new event type, verify:
- No existing event covers the same state transition.
- The type follows `xiigen.<flow>.<entity>-<pastVerb>.v<N>`.
- Granularity is consistent with similar events.

```
Current vocabulary (example):
  xiigen.user-registration.verification-email-requested.v1
  xiigen.user-registration.onboarding-completed.v1
```

---

## Anti-Patterns

1. **Generic names** — `…status-changed.v1` covers nothing specifically.
2. **Mutable payloads** — `List<T>` in an event record can be mutated by consumers.
3. **Internal ids in the payload** — a storage row key crosses an internal boundary.
4. **Cross-flow events undocumented** — they are public contracts; document them.
5. **Removing/renaming a field in `.v1`** — always additive; bump to `.v2` for
   breaking changes.

---

## Integration

- `event-contract-design-SKILL.md` adds the required correlation-field schema,
  compensation, SLA, and INJECTABLE/PLATFORM_ONLY boundary for these events.
- `change-propagation-SKILL.md` propagates an event-schema change to the interfaces
  doc and every consumer in the same commit.
- `IOutboxStore` (`Xiigen.Kernel`) carries these events; DNA-8 store-before-dispatch.
