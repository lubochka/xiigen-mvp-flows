# Phase Preflight

> **Stack adaptation (xiigen flows).** Universal Assumption-Registry + Fix-Patterns
> contract ported from `llm_mvp_core/docs/skills/phase-preflight-SKILL.md`. Verify-steps
> read per-flow service constructors and DI registration in
> `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api/Program.cs` (real pattern:
> `builder.Services.AddUserRegistrationFlow();` + minimal-API `app.MapPost(...)`, NOT MVC
> controllers) and method signatures `Task<DataProcessResult<T>>`. Example Pattern A =
> adding a dependency to an existing `Application` service. Files are `.cs`.

## When to Run

Run this skill **before writing any code in a phase**. It catches wrong assumptions before
they are baked into implementation.

---

## Assumption Registry

Before writing the first line, record every assumption the plan makes about the codebase:

```
ASSUMPTION-1: UserRegistrationService inherits MicroserviceBase<UserRegistrationService>
  Verify: grep -rn "class UserRegistrationService" flows/FLOW-01-user-registration/server --include="*.cs"
  Expected: "class UserRegistrationService : MicroserviceBase<UserRegistrationService>"
  Status: [ ] PENDING

ASSUMPTION-2: UserRegistrationService takes its fabric stores via constructor injection
  Verify: Read flows/.../Application/UserRegistrationService.cs constructor
  Expected: ctor(IDocumentStore, IIdempotencyStore, IOutboxStore, IPasswordHasherService,
            ITenantContextAccessor, ICorrelationContextAccessor, ILogger<UserRegistrationService>)
  Status: [ ] PENDING

ASSUMPTION-3: RegisterAsync returns Task<DataProcessResult<UserRegistrationOutcome>>
  Verify: Read flows/.../Application/UserRegistrationService.cs:38
  Expected: Task<DataProcessResult<UserRegistrationOutcome>> RegisterAsync(...)
  Status: [ ] PENDING

ASSUMPTION-4: the flow registers itself via AddUserRegistrationFlow() in Program.cs
  Verify: grep -rn "AddUserRegistrationFlow" flows/FLOW-01-user-registration/server --include="*.cs"
  Expected: "builder.Services.AddUserRegistrationFlow();" in Api/Program.cs
  Status: [ ] PENDING
```

Verify every assumption before writing code. Change the plan if any assumption is wrong.

---

## Fix Patterns by Type

### Pattern A — Handler Injection (adding a new dependency to an existing service)

```
Verify before touching the file:
1. Read the service constructor completely (Rule 5)
2. Record current constructor signature
3. Find all callers / registration:
   grep -rn "new <Service>\|Add<Name>Flow\|AddSingleton<<Service>>\|AddScoped<<Service>>" --include="*.cs"
4. Confirm the DI registration location (the AddXxxFlow() extension in Infrastructure)
5. Plan: changes to constructor + DI registration + any direct callers
```

Example (this stack):
```csharp
// Current: ctor(IDocumentStore documents, IIdempotencyStore idempotency, IOutboxStore outbox, IPasswordHasherService passwordHasher, ...)
// Plan: add a new fabric dependency, e.g. INotificationStore
// → Must update the AddUserRegistrationFlow() registration so the new dependency resolves
// → Must check the xUnit FlowFixture provides the new fabric (Xiigen.Fabrics.InMemory)
```

### Pattern B — Contract Field Access (adding a new method to a flow interface)

```
Verify before changing the interface:
1. Read the interface completely (Rule 5)
2. Find all implementations:
   grep -rn "class.*: I<InterfaceName>" flows/ --include="*.cs"
3. Find all callers (Program.cs routes + tests):
   grep -rn "I<InterfaceName>\.\|<MethodName>" flows/ --include="*.cs"
4. Plan: new method signature (Task<DataProcessResult<T>>) + implementation + route + tests
```
If there are more than 3 implementations, escalate to Luba — interface changes have wide blast radius.

### Pattern C — Module Registration (adding a new service to the flow DI container)

```
Verify before writing the class:
1. Locate the flow's DI extension (the Add<Name>Flow() method, usually in Infrastructure)
   grep -rn "AddSingleton\|AddScoped\|AddTransient\|public static IServiceCollection Add" flows/.../Infrastructure --include="*.cs" -l
2. Confirm the contract/record is already defined
3. Confirm the concrete class name doesn't already exist:
   grep -rn "class <NewClassName>" flows/ --include="*.cs"
4. Plan: new class + registration entry in Add<Name>Flow() + minimal-API route in Program.cs
```

---

## Escalation Format

If preflight discovers a blocking issue:

```
PREFLIGHT BLOCKER: [one-line description]

Assumption: [what the plan assumed]
Reality: [what the code actually shows]
Evidence: [path:line where reality is visible]

Options:
A. [option A — impact]
B. [option B — impact]

Recommended: [A or B and brief why]

Awaiting direction before proceeding.
```

Do not choose an option unilaterally unless it is clearly non-architectural. If in doubt, escalate.

---

## Issue Inventory Integration

As you run preflight, record out-of-scope issues:

```
OUT-OF-SCOPE-1: a service constructs a fabric with "new InMemoryDocumentStore()" directly
  — Fabric First violation, not in scope for this phase
  — Record for a future fabric-first audit pass
  — File: flows/.../Application/<Service>.cs:NN
```

These become inputs for a fabric-first audit. Do not fix them in this phase unless fixing
is required for the current task to compile and test.

---

## Preflight Completion Criteria

```
□ All assumptions verified against live code (not from memory)
□ All failed assumptions have plan amendments
□ Fix pattern identified (Handler Injection / Contract Field / Module Registration)
□ All callers of any changed component identified (Program.cs routes + tests)
□ DI registration location confirmed (Add<Name>Flow())
□ No undocumented blockers remain
□ Out-of-scope issues recorded for future
```

Only then: begin writing code. First Gate B after the code = `dotnet build` 0 Error(s) +
`dotnet test` (xUnit) green.

## Integration

Load alongside: `design-artifact-completeness-SKILL.md`, `phase-planning-SKILL.md`.
Required by: `plan-execution-feedback-SKILL.md` (write-time fixes are failed assumptions).
