# Skill: Implementation Documentation — Algorithm Descriptions (§1/§2)

> **Stack adaptation (xiigen flows).** Universal Part-A §1/§2 contract ported from
> `llm_mvp_core/docs/skills/implementation-doc-algorithms-SKILL.md`. Method/type names
> are C# on .NET 10 per-flow layers: `Application` services inherit
> `MicroserviceBase<TSelf>` and return `Task<DataProcessResult<T>>` (XIIGen family
> wrapper, NOT core `OperationResult<T>`); example values come from real domain records
> such as `UserRegistrationOutcome` / `EmailVerificationOutcome` (see
> `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationResults.cs`).
> The §2 signature block uses C# signatures. Gate-B = `dotnet build` + `dotnet test`.

## Purpose

Every component in an implementation document must have a human-language algorithm
description (§1 names class + method + parameters + step-by-step; §2 expands it) that
explains HOW and WHY, not just WHAT. A programmer must understand the component without
reading the source code. A functional spec ("what it should do") and a class name alone
are NOT sufficient — name the exact **class AND the implementing method/function**, the
**parameters** it takes, and the **actual step-by-step algorithm**.

This is a no-code algorithm section. It explains what happens, where, and in what order,
with class/method refs, but it does not dump code. Default language is English unless
the user requests another language.

## Required structure for each component description

### 1. Human analogy first (mandatory)
Start with an analogy that a non-programmer can understand:
- ✅ "Imagine a hotel receptionist: they receive a form, check that the required
  fields are filled in, prevent two guests from using the same key for one room (idempotency),
  and only then create a record."
- ❌ "This service calls `_idempotency.TryReserveAsync` and writes to `IDocumentStore`."

### 2. What happens during initialization / construction
Concrete description of what the component holds and how it is wired:
- Name the constructor dependencies (fabric interfaces from `Xiigen.Kernel`), e.g.
  `IDocumentStore`, `IIdempotencyStore`, `IOutboxStore`, `IPasswordHasherService`,
  `ITenantContextAccessor`, `ICorrelationContextAccessor`, `ILogger<T>`.
- Name the `ServiceDescriptor` identity the service registers under (e.g.
  `new ServiceDescriptor("T47", nameof(UserRegistrationService), FlowId)`).
- These are registered through the flow's DI extension (`AddUserRegistrationFlow()`),
  not constructed with `new`.

### 3. What happens during the request (inference / execution)

Step-by-step numbered list of what the method does:
1. What comes in (with a real example value, not just type name) — e.g.
   `request = { ["email"]="Luba@Example.COM", ["password"]="..." }`, `idempotencyKey="register-1"`.
2. What transformation happens — normalize email, read password, build operation key.
3. What decision is made and why (threshold/condition) — e.g. missing idempotency key →
   `Failure("BAD_REQUEST", ...)`; reservation already taken → `CONCURRENT_UPDATE`.
4. What comes out (with real example value) — `DataProcessResult.Ok` carrying
   `UserRegistrationOutcome(HttpStatus: 201, Body, Stored: true, OutboxRecorded: true)`.

### 4. "What it does NOT do" section (mandatory)
Explicitly state what the component does NOT do:
- ❌ It does NOT throw for business failures — every failure is `Failure<T>(code, message, details)`.
- ❌ It does NOT instantiate fabric stores directly — all come through DI (Fabric First).
- ❌ It does NOT trust client-supplied tenant — tenant comes from `ITenantContextAccessor`.

### 5. Parameter signatures with examples
After the text description, add a `**Signatures and data types:**` block with C# signatures
and real field values:

**Example of correct parameter block (this stack):**
```csharp
// Application method for the registration flow:
Task<DataProcessResult<UserRegistrationOutcome>> RegisterAsync(
    IReadOnlyDictionary<string, object?> request, // { "email": "...", "password": "..." }
    string? ifMatch,                              // ETag from the If-Match header; may be null
    string? idempotencyKey,                       // "register-1" — required
    CancellationToken cancellationToken = default)

// Contents of a successful result:
UserRegistrationOutcome(
    HttpStatus: 201,                              // status code for Results.Json
    Body: { "accountStatus": "PENDING_VERIFICATION" },
    Stored: true,                                 // record created in IDocumentStore
    OutboxRecorded: true)                         // event recorded in IOutboxStore

// Domain result wrapper (NOT core OperationResult<T>):
// DataProcessResult<T> { bool IsSuccess; bool IsFailure; T? Data; DataProcessError? Error; }
// DataProcessError(string Code, string Message, IReadOnlyDictionary<string,object?>? Details)
```

## Status annotation

Every component description must end with its implementation status:
- **✅ Complete** — all public methods implemented and covered by `[Fact]`/`[Theory]` tests.
- **⚠️ Partial** — state exactly which methods work and which are stubs (e.g.
  `RegisterAsync` works; `ExportFlowDataAsync` always returns `Failure("NOT_READY", ...)`).
- **🔶 Skeleton** — state what is saved (contract fields/record shape), what always returns
  a stub `Failure`/empty outcome, and WHY.

> **Note.** Do NOT use the core `✅ Bootstrap` symbol — it marks a deterministic
> Confidence=1.0 wrapper awaiting a trained ILearningMachine model and is core-specific
> (G12). For a static wrapper in this repo use `✅ Hardcoded`.

## Validation gate

Before submitting §1/§2, verify each component description:
- [ ] Names the exact class AND implementing method, its parameters, and a step-by-step algorithm (not a functional summary)
- [ ] Has a human analogy (not a technical summary)
- [ ] Shows concrete example values (not just type names), drawn from real domain records
- [ ] Has "What it does NOT do" section (includes the no-throw / Fabric-First / tenant-from-accessor facts)
- [ ] Has `**Signatures and data types:**` block with real C# signatures returning `Task<DataProcessResult<T>>`
- [ ] States implementation status (no core `Bootstrap` symbol)

## Integration

Load alongside: `implementation-doc-uml-SKILL.md`, `implementation-doc-code-refs-SKILL.md`.
Required by: `planning-session-startup-SKILL.md` (pre-execution doc gate),
`plan-review-SKILL.md` (FC-15).
