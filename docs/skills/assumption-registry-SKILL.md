# assumption-registry-SKILL.md — One testable command per plan assumption, before EXECUTION files

> **Stack adaptation (xiigen flows).** Universal *assumption registry* ported from
> `llm_mvp_core/docs/skills/assumption-registry-SKILL.md` (G08). Greenfield here —
> no prior author version. The universal core kept: for every plan, register
> assumptions in three classes — INFRASTRUCTURE / BEHAVIORAL / OPERATIONAL — where
> each row is one testable claim + a literal command + expected output +
> fallback/BLOCKING; and the registry is produced BEFORE session/EXECUTION files,
> placed in STEP 0 (PRE-FLIGHT). The stack details are adapted:
> - **INFRA** verify = `grep "interface I..."` in `contracts/` (or the Application
>   boundary) and `grep "class ... : MicroserviceBase<"` for the implementation.
> - **BEHAVIORAL** verify = a method signature in `server/Application` returning
>   `DataProcessResult<T>` (XIIGen family wrapper — **NOT** core
>   `OperationResult<T>`).
> - **OPERATIONAL** baseline = `dotnet build Xiigen.Flows.slnx` → `0 Error(s)`
>   (0 warnings, `TreatWarningsAsErrors=true`) and `dotnet test --no-build` →
>   `Failed: 0` (`net10.0`, xUnit). Client-bearing flows add a `provideHttpClient`
>   / standalone-component presence assumption.

## When to Invoke

After a plan is produced, before session or EXECUTION files are written. The
assumption registry feeds `phase-preflight-SKILL.md`, which runs the verification
commands at the start of the execution session.

---

## Why This Exists

The classic blocking issues appear in planning reviews:
1. A method assumed to exist in a service — it didn't (renamed last phase).
2. A DI registration assumed to be in `Program.cs` — it wasn't (the `Add<Name>Flow()`
   extension was never called).
3. A field assumed to be a property — it was a method (wrong access pattern).

All three are discoverable in 30 seconds with a grep. None were checked at plan
authoring time. Cost of a wrong assumption found in review: 1 review cycle. Found
at execution: blocked session + re-plan + re-implementation.

---

## The Three Assumption Classes

### Class 1 — INFRASTRUCTURE (verifiable by grep or file existence)

Something must exist in the repo that the plan depends on.

```
Example:  "IOnboardingDelivery contract exists with DeliverAsync()"
Verify:   grep -rn "interface IOnboardingDelivery" flows/FLOW-01-user-registration --include="*.cs"
Expected: 1+ hits
Fallback: If 0 hits — BLOCKING: define the contract before this phase
```

### Class 2 — BEHAVIORAL (verifiable by reading the code)

Something must behave a certain way — a method signature, field, return type.

```
Example:  "UserRegistrationService.RegisterAsync() returns DataProcessResult<RegistrationOutcome>"
Verify:   grep -rn "DataProcessResult<RegistrationOutcome>.*RegisterAsync\|RegisterAsync.*DataProcessResult" \
            flows/FLOW-01-user-registration/server --include="*.cs"
Expected: DataProcessResult<RegistrationOutcome> wrapping present
Fallback: If unwrapped — update callers to unwrap, or change the method
```

### Class 3 — OPERATIONAL (verifiable by running a command)

Something must build/compile/test green for the plan to be executable.

```
Example:  "All existing flow tests still pass before adding new tests"
Verify:   dotnet test flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Tests --no-build 2>&1 | tail -3
Expected: Failed: 0
Fallback: If failures exist — BLOCKING: fix pre-existing failures before this phase
```

---

## The Assumption Registry Format

Produce an `ASSUMPTIONS` block for every EXECUTION file in STEP 0 (PRE-FLIGHT):

```markdown
## STEP 0 — PRE-FLIGHT: ASSUMPTION REGISTRY

Before executing any step, verify all assumptions. A failed assumption is BLOCKING
unless a fallback is specified. If BLOCKING and no fallback: STOP. Escalate to Luba.

| # | Assumption | Class | Verification | Expected | Fallback |
|---|-----------|-------|--------------|----------|---------|
| A1 | IOnboardingDelivery contract exists | INFRA | `grep -rn "interface IOnboardingDelivery" flows/FLOW-01-user-registration --include="*.cs"` | 1+ hits | BLOCKING — define contract |
| A2 | OnboardingDeliveryService : MicroserviceBase + IOnboardingDelivery | INFRA | `grep -rn "class OnboardingDeliveryService.*MicroserviceBase<" .../server --include="*.cs"` | 1 hit | BLOCKING — implement |
| A3 | RegisterAsync() returns DataProcessResult<T> | BEHAVIORAL | `grep -rn "DataProcessResult<.*RegisterAsync" .../server --include="*.cs"` | 1+ hits | BLOCKING — update callers |
| A4 | AddUserRegistrationFlow() registered in Program.cs | INFRA | `grep -rn "AddUserRegistrationFlow" .../Xiigen.Flow.UserRegistration.Api/Program.cs` | 1 hit | BLOCKING — register DI |
| A5 | Test baseline 0 failures | OPERATIONAL | `dotnet test .../Xiigen.Flow.UserRegistration.Tests --no-build 2>&1 \| tail -3` | Failed: 0 | BLOCKING — fix pre-existing |
```

**Each row must have:** one testable claim (not "everything is set up"), a literal
command, the expected output, and a fallback OR "BLOCKING".

---

## How to Identify Assumptions From a Plan

For every step, ask:

```
1. What file/contract does this step touch?
   → Does it exist? (Class 1: INFRASTRUCTURE)

2. What method or field does this step call or read?
   → Does it have the expected signature / DataProcessResult<T> wrapping? (Class 2: BEHAVIORAL)
   → Will it still be there when this session runs?

3. What state must the repo be in for this step to succeed?
   → Are all prior phases complete? (Class 1)
   → Does build + test gate pass? (Class 3: OPERATIONAL)
```

For every YES answer, the thing in question is an assumption. Register it and
write the verification command.

---

## Assumption Registry vs. Simple File Check

Weak:

```bash
ls flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs
# Expected: file exists
```

Strong (assumption registry):

```bash
# A1: RegisterAsync() exists and returns the expected wrapper
grep -rn "public.*RegisterAsync.*DataProcessResult<" \
  flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs
# Expected: 1+ hits — method present with correct return wrapper
# Fallback: If 0 hits — signature changed; update callers before Phase 2
```

The `ls` confirms the file exists. The registry confirms the thing the plan
depends on exists INSIDE the file and has the right shape.

---

## Anti-Patterns

1. **Assumptions stated in prose** ("we assume the service works") — every assumption needs a verification command.
2. **Registering only obvious assumptions** — the non-obvious ones (signature changed, field renamed, DI extension not wired) cause blocked sessions.
3. **Fallback of "check manually"** — not a fallback. Write the command, or mark BLOCKING and escalate at planning time.
4. **Registry at the end of the plan** — it is produced BEFORE session files; a failed assumption may change the session file content.

---

## Integration With Phase-Preflight

This skill produces the registry at planning time; `phase-preflight-SKILL.md`
executes it at session start.

```
Plan authoring:  assumption-registry-SKILL → ASSUMPTIONS registered → EXECUTION STEP 0 populated
Session start:   phase-preflight-SKILL → reads STEP 0 → runs each command → PASS/FAIL/FALLBACK
                 → any BLOCKING fail: STOP, escalate to Luba
                 → fallback available: apply, note in session, continue
```

The two skills are a pair. Without the registry, preflight may not cover the
right assumptions; without preflight, the registered assumptions are never
actually verified at execution time.
