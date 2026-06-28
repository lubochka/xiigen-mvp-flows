# DNA Compliance — .NET Translation for xiigen flows

> **Stack adaptation (xiigen flows).** Seed translation ported from
> `llm_mvp_core/.skills/DNA-DOTNET-TRANSLATION.md` (G04). This is **not a separate
> target skill** — it is the seed artifact that `dna-compliance-guard-SKILL.md` and
> `dev-safety-SKILL.md` reuse for the .NET 10 flows stack. Key difference from the
> core bridge: the core bridge proposes a generic `Result<T>`, but this repo already
> implements **`DataProcessResult<T>`** (the same NestJS-derived idiom as XIIGen mvp),
> so every rule below uses the real `DataProcessResult<T>` + `MicroserviceBase<TSelf>`
> + `Success<T>(...)` / `Failure<T>("CODE","message",details)` family — **never** core
> `OperationResult<T>` (which stays in `llm_mvp_core`).

## Context

The original nine DNA coding rules were written for XIIGen's NestJS/TypeScript SaaS
platform. This document translates each rule to the .NET 10 / per-flow context so the
same compliance gate can run here against
`flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}`.

`Directory.Build.props` already provides the .NET enforcement scaffolding:
`TargetFramework=net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`,
`RestorePackagesWithLockFile=true`. So "0 errors" in the Dev-Safety Gate is really
"0 errors AND 0 warnings".

---

## Rule 1 — No hardcoded config / freedom data buried in logic

**Original:** All business data uses `Record<string, unknown>`. Never create typed
model classes for freely-overridable values.

**In flows:** Per-flow contracts and outcomes (`OnboardingOutcome`, request/response
records) are typed because they ARE the machine contracts. But tunable
parameters/thresholds a caller might override must NOT be hardcoded as
`private const` deep in logic — they belong in config/options objects passed in.

```csharp
// BAD — hidden magic constant buried in a handler
var retryWindow = 0.75f;

// GOOD — comes from an options object
var retryWindow = options.RetryWindow;
```

**Detection:**
```bash
grep -rn "= 0\.[0-9]" --include="*.cs" flows/FLOW-NN-*/server | grep -v "readonly\|const\|//\|Tests"
```

---

## Rule 2 — Dynamic filter building (no hardcoded selectors)

**Original:** All searches use a dynamic filter builder; empty fields auto-excluded.

**In flows:** Query/lookup methods must accept filter parameters, not hardcode a
single field check. A query that only matches `Status == "active"` is hardcoded.

```csharp
// BAD
records.Where(r => r.Status == "active")

// GOOD
records.Where(r => filter.Status == null || r.Status == filter.Status)
```

---

## Rule 3 — Result pattern, never throw for business logic

**Original:** Every service method returns `DataProcessResult<T>`. Exceptions for
infrastructure failures only.

**In flows:** Every public `Application` method that handles expected failure cases
returns `DataProcessResult<T>`. Never `throw` for an expected input/business case —
return `Failure<T>("CODE","message")`. Throw only for genuine programming errors.

```csharp
// BAD
if (string.IsNullOrEmpty(userId)) throw new InvalidOperationException("no user");

// GOOD (real flow shape — UserRegistration.OnboardingDeliveryService)
if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
    return Failure<OnboardingOutcome>("VALIDATION_FAILED", "userId and email are required");
```

**Note:** Propagate inner-service errors instead of re-wording them:
```csharp
if (!stored.IsSuccess)
    return Failure<OnboardingOutcome>(stored.Error?.Code ?? "STORE_FAILED",
                                      stored.Error?.Message ?? "Completion store failed");
```

---

## Rule 4 — Base class / interface contracts

**Original:** All services extend `MicroserviceBase`.

**In flows:** All `Application` services inherit `MicroserviceBase<TSelf>` (the
self-typed base that exposes `Success<T>`/`Failure<T>`). All external/fabric
dependencies of a flow resolve through interfaces (`IPaymentFabricService`,
`ICanvaWriter`, `IMiroWriter`, `IClientPushConnectionPool`,
`IOssCurriculumQueueBroker`, per-flow document/outbox/idempotency stores). No
orphan concrete class with no interface.

```csharp
// BAD — standalone class, no base, no interface
public class OnboardingDeliveryService { ... }

// GOOD
public sealed class OnboardingDeliveryService
    : MicroserviceBase<OnboardingDeliveryService>, IOnboardingDelivery { ... }
```

---

## Rule 5 — Scope isolation

**Original:** Every query scoped to tenant via `AsyncLocalStorage`.

**In flows:** Every request handler is stateless with respect to other requests.
No static mutable fields in a service; tenant/correlation context comes from the
ambient accessor, not a passed parameter. Handlers must be thread-safe (read-only
configuration, no shared mutable per-call state).

**Detection:**
```bash
grep -rn "static.*=" --include="*.cs" flows/FLOW-NN-*/server | grep -v "readonly\|const\|//\|Tests"
```

---

## Rule 6 — Generic controllers (mostly N/A)

The `Api` project exposes minimal-API/controller endpoints, but there are no
generic entity controllers; endpoints are thin and delegate straight to an
`Application` service. Keep controllers thin — no business logic in the `Api` layer.

---

## Rule 7 — Idempotency for checkpoints / exports

**Original:** All queue consumers deduplicate using idempotency keys.

**In flows:** Checkpoint/export writes must be atomic and idempotent — write to a
temp file first, then rename. Calling the write twice with the same key must not
corrupt the previous artifact. Queue/outbox consumers dedupe by idempotency key.

```csharp
// BAD — direct overwrite, crash mid-write = corrupt artifact
File.WriteAllBytes(path, data);

// GOOD — atomic rename
File.WriteAllBytes(path + ".tmp", data);
File.Move(path + ".tmp", path, overwrite: true);
```

---

## Rule 8 — Persist state before dispatching work (outbox pattern)

**Original:** `storeDocument()` MUST happen before `enqueue()`.

**In flows:** In a flow handler, persist the record/checkpoint BEFORE dispatching
the next unit of work or enqueuing the next event. If the process crashes, the
dispatch must not be ahead of the last persisted state.

---

## Rule 9 — CloudEvents / event envelope

**Original:** Inter-service messages are CloudEvents-wrapped.

**In flows:** Where a flow emits domain events across a boundary, wrap them in the
standard envelope used by the flow's messaging fabric; do not emit bare payloads.
For a flow with no cross-service messaging, this rule does not apply.

---

## Dev Safety Gate (.NET / flows)

Run at start and end of every phase:

```bash
dotnet build Xiigen.Flows.slnx
# Expected: 0 errors, 0 warnings (TreatWarningsAsErrors=true → a warning fails the build)

dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --logger "console;verbosity=minimal"
# Expected: 0 failed, all passed, total >= baseline
```

**Blocked condition:** any build error/warning or test failure blocks the phase
from closing.

---

## Test Integrity Rules (.NET / flows)

- Tests must assert domain outcomes, not just "ran without exception".
- Assert the `DataProcessResult<T>` payload field, the `ErrorCode`, a count, or a
  shape — `Assert.True(result.IsSuccess)` alone is a stub test.
- Value-range / known-property assertions are behavioral assertions — include them.
- Zero failures = zero failures. No "pre-existing" carve-outs. With
  `TreatWarningsAsErrors=true`, "zero" also covers build warnings.

---

## Applicable Rules Summary

| Rule | Applies | How (flows) |
|------|---------|-------------|
| 1 — no hardcoded config | Partial | Tunable values come from config/options objects, not inline constants |
| 2 — dynamic filters | Yes | Query/lookup methods take filter parameters |
| 3 — Result pattern | Yes | All `Application` public methods return `DataProcessResult<T>`; `Failure<T>` not throw |
| 4 — base/interface contracts | Yes | `MicroserviceBase<TSelf>` + injected fabric interfaces |
| 5 — scope isolation | Yes | Thread-safe handlers, context from ambient accessor, no per-call static mutable state |
| 6 — generic controllers | Mostly No | Thin `Api` endpoints only; no business logic in controllers |
| 7 — idempotent writes | Yes | Checkpoint/export temp+rename; consumer dedup |
| 8 — persist before dispatch | Yes | Store record before enqueue/dispatch |
| 9 — event envelope | Conditional | Wrap cross-boundary events; N/A if no messaging |
