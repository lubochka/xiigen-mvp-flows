# api-shape-verification-SKILL.md — Verify the real signature before you call it (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/api-shape-verification-SKILL.md` (G09). Universal core
> carries over unchanged: before writing any test or call, verify the symbol exists
> with the exact signature (sync/async, parameter count and order, return-type
> wrapper, property vs method); **if the symbol is not found → STOP.** Stack mapping:
> - the return wrapper that actually exists in flows is **`DataProcessResult<T>`**
>   (and `Task<DataProcessResult<T>>` for async) — the XIIGen flow wrapper, **NOT**
>   core `OperationResult<T>`. Verify against the wrapper that exists here, not the
>   core one.
> - verification tool = `grep`/IDE "go to definition" over `*.cs` under
>   `flows/FLOW-NN-<name>/server/**` (exclude `client/angular/node_modules`).
> - **Real-API warning carried from core:** the scheduler method is
>   `ISchedulerService.ScheduleDelayedAsync(string action, TimeSpan delay,
>   IReadOnlyDictionary<string,object?> payload, string idempotencyKey,
>   CancellationToken)` (confirmed in
>   `EmailVerificationService.cs:198`). A method named `ScheduleAt(...)` does **NOT**
>   exist — this is exactly why api-shape verification is mandatory before tests.
> - run this at the phase's Gate-0 (infrastructure discovery), before the first test
>   line.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` (Failed 0).

## When to load

- At infrastructure discovery (Gate 0 of phase planning) — verify every class/
  interface before writing tests for it.
- Before writing any test that calls a method — verify its signature and return type.
- After any phase whose write-time discovery rate was high (assumptions kept being wrong).

---

## Why This Exists

Most write-time fixes are API-shape discoveries:
- Assumed a method takes 2 parameters → it takes 5 (the scheduler case above).
- Assumed a method is async → it is synchronous (or vice versa).
- Assumed a property → it is a method with different semantics.
- Assumed an interface has method X → it was renamed or never existed.

Verifying a symbol at Gate 0 costs ~30 seconds. Discovering the mismatch
mid-implementation costs a blocked session.

---

## Verification Protocol

For each class or interface the plan will use:

```bash
# Step 1: Verify the type exists (exclude angular node_modules false positives)
grep -rn "public.*class <ClassName>\|public.*interface I<Name>" \
  --include="*.cs" flows/FLOW-NN-<name>/server | grep -v node_modules

# Step 2: Verify the method exists with its signature
grep -rn "public.*<MethodName>\s*(" \
  --include="*.cs" flows/FLOW-NN-<name>/server | grep -v node_modules | head -5

# Step 3: async vs sync + return wrapper — confirm DataProcessResult<T> / Task<DataProcessResult<T>>
grep -rn "Task<DataProcessResult\|DataProcessResult<" \
  --include="*.cs" flows/FLOW-NN-<name>/server | grep -v node_modules | head -10

# Step 4: property vs method — is it { get; } or ()?
grep -rn "public.*\b<MemberName>\b" \
  --include="*.cs" flows/FLOW-NN-<name>/server | grep -v node_modules

# Step 5: confirm the core wrapper did NOT leak in (must be empty)
grep -rn "OperationResult<" --include="*.cs" flows/FLOW-NN-<name>/server | grep -v node_modules
```

---

## Verification Table

Produce this before writing any test code that uses a class:

| Symbol | Source File | Type | Signature | Return Type | Notes | Verified |
|--------|-------------|------|-----------|-------------|-------|----------|
| `OnboardingDeliveryService` | `.../UserRegistration.Application/OnboardingDeliveryService.cs` | class | ctor: `(IDocumentStore, IOutboxStore, IFreedomConfigService, ITenantContextAccessor, ICorrelationContextAccessor, ILogger<…>)` | instance | `MicroserviceBase<TSelf>` | ☐ |
| `CompleteAsync` | method on `OnboardingDeliveryService` | method | `CompleteAsync(string userId, string email, int existingOnboardingMaterialsCount = 0, CancellationToken = default)` | `Task<DataProcessResult<OnboardingOutcome>>` | async | ☐ |
| `ISchedulerService` | `Xiigen.Fabrics.Scheduler` | interface | `ScheduleDelayedAsync(string action, TimeSpan delay, IReadOnlyDictionary<string,object?> payload, string idempotencyKey, CancellationToken)` | `Task<DataProcessResult<…>>` | **no `ScheduleAt`** | ☐ |
| `IOssCurriculumQueueBroker` | `.../OssCurriculum.Application/OssCurriculumResults.cs` | interface | `PublishAsync(CloudEventEnvelope, CancellationToken = default)` | `Task<DataProcessResult<QueueDispatchRecord>>` | async | ☐ |

---

## Rules

1. **Never assume** — even "obvious" APIs may have changed since the last session.
2. **Check the return wrapper** — `DataProcessResult<T>` vs `T` vs `Task<DataProcessResult<T>>`
   are all different; do not assume the core `OperationResult<T>` shape.
3. **Check sync vs async** — `await` on a sync method will not compile; calling an
   async method without `await` loses the result.
4. **Check parameter order and count** — `ScheduleDelayedAsync(action, delay, payload, key, ct)`,
   not a guessed 2-argument shape.
5. **If the symbol is not found → STOP.** Do not write the test. Report it.
6. **Complete the verification table before the first test line.**

---

## Examples from this repo

```bash
# Confirm DataProcessResult<T> usage (not OperationResult<T>)
grep -rn "DataProcessResult<" --include="*.cs" \
  flows/FLOW-01-user-registration/server | grep -v node_modules | head

# Confirm the real scheduler method name (ScheduleDelayedAsync) and prove ScheduleAt is absent
grep -rn "ScheduleDelayedAsync\|ScheduleAt" --include="*.cs" flows/ | grep -v node_modules
# Expected: ScheduleDelayedAsync present; ScheduleAt: no output

# Confirm constructor dependency list before instantiating in a test
grep -rn "public OnboardingDeliveryService(" --include="*.cs" \
  flows/FLOW-01-user-registration/server | grep -v node_modules
```

---

## Integration with Plan-Execution Feedback

After each phase, record any shape mismatches discovered during implementation:

```
API shape discovery (write-time fix):
  Planned: _scheduler.ScheduleAt(when, payload)
  Actual:  _scheduler.ScheduleDelayedAsync("action", delay, payload, idempotencyKey, ct)
  Fix: rewrite test/setup to the real 5-parameter delayed-schedule signature.
```

If the discovery rate stays high across phases, the next phase must complete this
full verification table at Gate 0 before any test is written.
