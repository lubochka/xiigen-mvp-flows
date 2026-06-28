# prerequisite-chain-SKILL.md — Verify prerequisites with commands before a phase starts

> **Stack adaptation (xiigen flows).** Universal *prerequisite chain* ported from
> `llm_mvp_core/docs/skills/prerequisite-chain-SKILL.md` (G08). Greenfield here —
> no prior author version. The universal core kept: before any phase, list every
> prerequisite, write a verification command for each, classify PASS / FAIL /
> BLOCKING, order the chain, and never mock a missing prerequisite. The stack
> details are adapted:
> - **Session-start checklist** = `dotnet build Xiigen.Flows.slnx` → `0 Error(s)`
>   (and 0 warnings, since `TreatWarningsAsErrors=true`) and
>   `dotnet test --no-build` over the affected `Xiigen.Flow.<Name>.Tests` →
>   `0 failed` (`net10.0`, xUnit).
> - **Prerequisites** = a contract present in `contracts/`, the DI registration
>   present in `server/Xiigen.Flow.<Name>.Api/Program.cs` (the `Add<Name>Flow()`
>   extension), and — for client-bearing flows — a standalone component +
>   `provideHttpClient` in `client/angular`. Cross-flow prerequisites come from
>   `flow.manifest.json`.
> - Each flow is its own `.Api` / `.Application` / `.Infrastructure` / `.Tests`
>   set, so prerequisite verification is per-flow, not over one monolith.
> - The domain wrapper is `DataProcessResult<T>` (XIIGen family) — **NOT** core
>   `OperationResult<T>`.

## Purpose

Before starting any phase or session, verify all prerequisites are met. A phase
with unmet prerequisites blocks mid-session and wastes the setup time.

---

## The Prerequisite Check Protocol

### Step 1 — List All Prerequisites

For each planned deliverable, ask:
- What contract, service, or DI registration does it depend on?
- Does that thing currently exist?
- Does it have the methods/shape the deliverable needs?

```
Deliverable: UserRegistration onboarding integration test

Prerequisites:
  1. IOnboardingDelivery contract must exist (contracts/ or Application boundary) with DeliverAsync()
  2. OnboardingDeliveryService must implement IOnboardingDelivery and return DataProcessResult<T>
  3. UserRegistrationService must exist with RegisterAsync()
  4. AddUserRegistrationFlow() must register all of the above in Program.cs
  5. Tenant/correlation accessors (ITenantContextAccessor / ICorrelationContextAccessor) resolvable
```

### Step 2 — Write Verification Commands

```bash
# Prereq 1: contract exists with the method
grep -rn "interface IOnboardingDelivery" flows/FLOW-01-user-registration --include="*.cs"
grep -rn "DeliverAsync" flows/FLOW-01-user-registration --include="*.cs"
# Expected: 1+ matches each

# Prereq 2: implementation returns DataProcessResult<T>, inherits MicroserviceBase
grep -rn "class OnboardingDeliveryService.*MicroserviceBase<\|: IOnboardingDelivery" \
  flows/FLOW-01-user-registration/server --include="*.cs"
# Expected: 1 match

# Prereq 4: DI registration present
grep -rn "AddUserRegistrationFlow" \
  flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Api/Program.cs
# Expected: 1 match

# Prereq 5: accessors resolvable (referenced through Xiigen.Kernel)
grep -rn "ITenantContextAccessor\|ICorrelationContextAccessor" \
  flows/FLOW-01-user-registration/server --include="*.cs"
# Expected: 1+ matches
```

### Step 3 — Classify Each Prerequisite

| Prerequisite | Verification | Status | Action if Unmet |
|-------------|--------------|--------|-----------------|
| IOnboardingDelivery contract | grep returns 1+ | PASS / FAIL | BLOCKING — define contract before this phase |
| OnboardingDeliveryService impl | grep returns 1 | PASS / FAIL | BLOCKING — implement + return DataProcessResult<T> |
| RegisterAsync() exists | grep returns match | PASS / FAIL | BLOCKING — check Application service |
| AddUserRegistrationFlow() DI | grep returns 1 | PASS / FAIL | BLOCKING — register in Program.cs |
| Build + test baseline | build 0 err / tests 0 failed | PASS / FAIL | BLOCKING — fix before this phase |

---

## Chain Ordering

When multiple phases have prerequisites, map the full chain:

```
Phase 3 (onboarding integration test)
  ← requires Phase 2 (OnboardingDeliveryService implementation)
     ← requires Phase 1 (IOnboardingDelivery contract + DTO in contracts/)
        ← requires: NONE (starts here)

Execution order: Phase 1 → Phase 2 → Phase 3
```

Do not start Phase 3 until Phase 2 tests show `0 failed`.

Cross-flow chains: if `FLOW-NN` depends on `FLOW-MM`'s contract, that dependency
is declared in `flow.manifest.json` and `FLOW-MM`'s contract must exist and build
before `FLOW-NN`'s phase starts.

---

## Prerequisite Block Protocol

If any BLOCKING prerequisite fails at session start:

```
PREREQUISITE BLOCK: [what failed]

Verification run: [command that showed the gap]
Output: [actual vs expected]

Cannot proceed with: [which deliverable is blocked]

Options:
A. Build the missing prerequisite now (estimated: N steps)
B. Stop and wait for Luba to assign the prerequisite phase

Recommendation: [A or B with reasoning]
```

Do not work around a missing prerequisite by mocking it in tests or adding `TODO`
comments. Either fix the prerequisite or stop.

---

## Session Start Prerequisite Checklist

```
□ Build: dotnet build Xiigen.Flows.slnx → 0 Error(s), 0 warnings
□ Test baseline: dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --no-build → N passed, 0 failed
□ Confirm the flow's prior phase Gate B passed (per its plan/state)
□ Verify top-3 prerequisites for this phase with grep commands
□ Client-bearing flow: cd client/angular && npx ng build → clean
□ No unmet prerequisites → begin work
□ Any unmet prerequisite → apply block protocol above
```

---

## Anti-Patterns

1. **"I'll discover prerequisites as I go"** — blocked sessions have high overhead; verify first.
2. **Mocking a missing prerequisite to unblock a test** — the mock hides the gap; the real integration will fail.
3. **Assuming a prior phase is complete without verifying** — re-run the prior phase's Gate B (`dotnet test --filter`).
4. **Not listing prerequisites at all** — implicit prerequisites are exactly the ones that cause blocked sessions.
5. **Ignoring cross-flow prerequisites** — a missing upstream-flow contract blocks just as hard as a missing local one; read `flow.manifest.json`.
