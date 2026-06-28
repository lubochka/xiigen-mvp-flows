# event-contract-design-SKILL.md — The minimum required schema for every server event (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/event-contract-design-SKILL.md` (G09). Universal core
> carries over unchanged: every server event needs **correlation fields (MACHINE,
> always present)**, **no PII in the payload**, a **compensation rule** (forward →
> `{Event}Reverted`/`{Event}RolledBack` + `RollbackReason`; terminal events have no
> compensation), an **SLA annotation** (timeout/retry), an **INJECTABLE vs
> PLATFORM_ONLY** boundary, and **string event-type names, not class names**. Stack
> mapping (verified against real flow code):
> - the transport is **`IOutboxStore.AppendAsync(CloudEventEnvelope)`** with
>   **store-before-dispatch (DNA-8)**: the flow `UpsertAsync`-es the document, then
>   `AppendAsync`-es the event (see `OnboardingDeliveryService.CompleteAsync`).
>   Direct queue/bus enqueue bypassing the outbox is forbidden.
> - an event is a `CloudEventEnvelope` (id, source, **type** as a versioned string
>   like `"xiigen.user-registration.onboarding-completed.v1"`, specversion,
>   timestamp, subject, datacontenttype, **data**, **attributes**). The correlation
>   fields ride in `attributes`: `correlationid` (from `CurrentCorrelationId`),
>   `xiigentenantid`, `xiigenflowid` — these are the MACHINE correlation fields here.
> - **no PII** in `data` or `attributes` (no external email/userId, no display name,
>   no IP unless needed for routing). A downstream consumer fetches user context
>   from its own store via the IDs, not from the event.
> - event records and outcome records are **immutable `record`** types (XIIGen flow
>   wrapper `DataProcessResult<T>` on the producing method — **NOT** core
>   `OperationResult<T>`).
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` of the
>   producing flow (Failed 0), asserting the envelope `type`, the correlation
>   `attributes`, and store-before-dispatch order.

## When to load

- Before adding any new event a flow appends to `IOutboxStore`.
- When `domain-event-design-SKILL.md` hits an event that needs compensation.
- Before wiring any compensation (LIFO rollback) chain.
- When designing events consumed across flow / platform boundaries.

---

## Core Problem

An event without correlation fields cannot be traced to a flow execution. An event
with PII leaks user data across boundaries. A forward event without a compensation
schema cannot be rolled back. An event modeled as HTTP coupling cannot be consumed
without coupling to the producer. This skill defines the minimum required schema for
every `CloudEventEnvelope` a flow emits through the outbox.

---

## Section 1 — Server Event Schema (required structure)

Every outbox event is a `CloudEventEnvelope`. The producer builds it like the real
flow does:

```csharp
private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
    new(
        Guid.NewGuid().ToString("n"),               // id
        "xiigen://flow/user-registration",          // source
        type,                                        // TYPE — versioned string constant, e.g. ".v1"
        "1.0",                                       // specversion
        DateTimeOffset.UtcNow,                       // timestamp
        subject,                                     // subject (stable id of the entity)
        "application/json",                          // datacontenttype
        data,                                        // DATA — typed payload, no PII
        new Dictionary<string, string>
        {
            ["xiigenflowid"]   = UserRegistrationConstants.FlowId,  // MACHINE
            ["xiigentenantid"] = CurrentTenant.TenantId,            // MACHINE (scope isolation)
            ["correlationid"]  = CurrentCorrelationId               // MACHINE (ties the execution chain)
        });
```

**Three correlation fields are MACHINE (always present, always named the same):**
- `correlationid` — ties every event in one execution chain.
- `xiigentenantid` — tenant scope isolation (Fabric First applies to tenant context).
- `xiigenflowid` — which flow produced the event.

(A `traceparent` attribute may be added for W3C distributed tracing; include it when
the flow participates in a cross-service trace.)

**No PII in `data` or `attributes`. Ever.** PII = external email, external userId,
display names, IP (unless required for routing). If a consumer needs user context,
it reads from its own store using the IDs the event carries.

---

## Section 2 — Compensation Event Rule

For every forward event, define a compensation event:

```
Forward:      {EventName}.v1   e.g. "xiigen.payments.charge-captured.v1"
Compensation: {EventName}-reverted.v1 | {EventName}-rolledback.v1

The compensation payload is the forward payload plus one field in data:
  ["rollbackReason"] = "<why>"

Terminal events (no compensation possible):
  *-onboarding-completed   → terminal success
  *-session-completed      → terminal state
  *-promoted-to-proven     → can only be demoted by new evidence, not compensated
```

Compensation events wire into the LIFO rollback chain (durable-saga flows such as
FLOW-19 are the canonical home). Each compensation event carries the same
correlation `attributes` as its forward event so the chain stays traceable.

---

## Section 3 — SLA Annotation

Every event gate the flow waits on must declare its timing:

```csharp
public sealed record EventGateConfig(
    string EventType,            // the event to wait for
    string? TimeoutDuration,     // null = synchronous, no timeout
    string? TimeoutEvent,        // emitted on timeout
    RetryPolicy? RetryPolicy);

public sealed record RetryPolicy(
    string Owner,                // "execution-unit" | "caller"
    int MaxAttempts,             // default 3
    string Strategy,             // "exponential_backoff" | "linear"
    string OnMaxRetries);        // "compensate" | "fail" | "escalate"
```

For a synchronous, sub-millisecond event set `TimeoutDuration = null` explicitly —
empty implies "unknown," which cannot be verified. Delayed/timeout transitions are
scheduled via `ISchedulerService.ScheduleDelayedAsync(...)` (the same primitive the
token-expiry path uses), never by polling `DateTimeOffset.UtcNow`.

---

## Section 4 — Fabric Boundary Annotation (INJECTABLE vs PLATFORM_ONLY)

For each event handler, declare whether it is customizable:

```
INJECTABLE:    an experiment/tenant may provide a different handler implementation.
PLATFORM_ONLY: DNA-invariant or security-critical — no customization.
```

```csharp
// IOnboardingDeliveredHandler: INJECTABLE — a tenant may customize onboarding side effects
// IChargeCapturedHandler:      PLATFORM_ONLY — payment-compensation coordination is invariant
```

Default for unmarked handlers: PLATFORM_ONLY.

---

## Section 5 — Stack-Neutral Event Contract Check

After designing CONSUMES/EMITS/BOUNDARY for a flow capability, verify:

```
□ Every event-type name is a versioned string constant — not a C# type name.
  CORRECT: "xiigen.user-registration.onboarding-completed.v1"
  WRONG:   typeof(OnboardingCompletedEvent).Name

□ CONSUMES references only outbox/bus events — no HTTP calls.
  CORRECT: "consumes xiigen.payments.charge-captured.v1 (IOutboxStore relay)"
  WRONG:   "POST /api/payments/charge"

□ EMITS contains only CloudEvents-compatible string types appended via IOutboxStore.
□ BOUNDARY uses only "INJECTABLE" or "PLATFORM_ONLY".
```

If any check fails, produce a gap report before implementation.

---

## Section 6 — Event Contract Document Format

Before any handler implementation, produce:

```markdown
## Event Contract — UserRegistration / Onboarding

### Server events emitted (via IOutboxStore.AppendAsync):
- `xiigen.user-registration.onboarding-completed.v1` — after CompleteAsync succeeds
  Required attributes: correlationid, xiigentenantid, xiigenflowid
  Data: { userId, email-less profile keys, onboardingCompletedAt }   ← no external PII
  Compensation: none (terminal success)

- `xiigen.user-registration.verification-email-requested.v1` — when a token is issued
  Required attributes: [same correlation attributes]
  Data: { tokenId, userId, expiresAt }
  Compensation: `…-verification-superseded.v1` with rollbackReason

### SLA:
  onboarding-completed: synchronous, no timeout (TimeoutDuration = null)
  verification token expiry: ScheduleDelayedAsync(TTL) emits the expiry event

### Boundary:
  IOnboardingDeliveredHandler: INJECTABLE
```

---

## Anti-Patterns

1. **"Put the user's email in `data`"** — PII violation. Carry the stable id; the
   consumer looks up details in its own store.
2. **"No compensation for a state-changing event"** — forward events that mutate
   state need a compensation event before implementation.
3. **"Event type = `typeof(X).Name`"** — event types are versioned string constants
   (`".v1"`), stack-neutral.
4. **"Enqueue straight to the bus"** — store-before-dispatch (DNA-8): `Upsert` the
   document, then `AppendAsync`. Bypassing the outbox is forbidden.
5. **"Leave the SLA timeout empty"** — set `TimeoutDuration = null` for synchronous
   events so the contract is verifiable.

---

## Integration

- `domain-event-design-SKILL.md` governs the naming of the events defined here.
- `temporal-behavior-SKILL.md` covers scheduled/timeout transitions via the scheduler.
- `dna-compliance-guard-SKILL.md` enforces store-before-dispatch and no-PII at commit.
- `IOutboxStore` (`Xiigen.Kernel`) is the fabric that carries these events.
