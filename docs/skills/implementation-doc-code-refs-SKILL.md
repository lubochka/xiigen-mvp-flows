# Skill: Implementation Documentation — Code References with Explanations (§3)

> **Stack adaptation (xiigen flows).** Universal Part-A §3 contract ported from
> `llm_mvp_core/docs/skills/implementation-doc-code-refs-SKILL.md`. Citations are C# on
> .NET 10 per-flow layers with `path:line`, e.g.
> `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs:44`.
> HOW-layer reveals the internals of called `Application`/`Infrastructure` methods and
> `Xiigen.Kernel` fabric stores; thresholds/constants (e.g. reservation TTL, status-code
> mapping) get a WHY blockquote. The domain wrapper is `DataProcessResult<T>` (NOT core
> `OperationResult<T>`). Gate-B = `dotnet build` + `dotnet test`.

## Purpose

Code reference sections must explain WHY and HOW, not just WHAT. A code block that only
restates what the syntax says is useless documentation. Citations must reflect the code
**as actually implemented** (real `path:line`), not as planned.

## Three layers every code block must have

### Layer 1 — WHY (mandatory, most important)
Explain why this condition, threshold, or decision exists:
- ✅ "> **Why `X-Idempotency-Key` is required:** without it, a repeated POST could create
  a second user. The key is reserved in `IIdempotencyStore` for 24 hours, making
  a network retry safe."
- ❌ `// idempotency check`

### Layer 2 — HOW (mandatory when calling another component)
When a code block calls a method, explain what happens INSIDE it:
- ✅ "> **What happens inside `TryReserveAsync`, step by step:** 1. Build the key
  `flow:register:tenant:key`. 2. Atomically try to record a reservation. 3. If the key is already taken,
  return `IsFailure` with code `CONCURRENT_UPDATE`."
- ❌ `// calls _idempotency`

### Layer 3 — WHAT (secondary, the code shows this)
Comments inside code blocks focus on values and outcomes:
- ✅ `// result.IsSuccess = true, Data.HttpStatus = 201 → Results.Json(body, 201)`
- ❌ `// returns result`

## Format rules

1. Use `>` blockquote blocks for WHY/HOW explanations — they visually separate
   explanation from code.
2. Inside code blocks: show concrete example values in comments, this stack:
   ```csharp
   // flows/.../UserRegistrationService.cs:44
   // idempotencyKey == null → cannot guarantee a single registration
   return Failure<UserRegistrationOutcome>(
       "BAD_REQUEST",                                 // → ErrorResult → HTTP 400
       "X-Idempotency-Key header is required",
       new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
   ```
3. When a condition blocks something, show ALL blocking conditions with reasons, e.g.
   the status-code mapping in `Program.cs:247` (`ErrorResult`):
   ```csharp
   // Map domain Error.Code → HTTP status (Program.cs:247):
   // "CONCURRENT_UPDATE" / "DUPLICATE_EMAIL" → 409
   // "VALIDATION_FAILED" / "BAD_REQUEST" / "INVALID_TOKEN" → 400
   // "EXPIRED_TOKEN" → 410; "RATE_LIMIT_EXCEEDED" → 429; "TIMEOUT" → 504
   // default → 500
   ```

For any architecture/audit-style §3 quote, use this adjacent structure (concrete solution
immediately under the quote, never an appendix-only reference):

```text
file + line + short quote:
current behavior:
why this is wrong/acceptable:
required behavior:
affected files/classes/records/methods/data rows:
how it works:
affected datasets/tests:
affected tests/e2e/qa:
lifecycle:
```

## What NOT to do

- ❌ Code block + comment that just renames the method call: `// the service registers the user`.
- ❌ `// returns result` — what result? show `IsSuccess`/`Data.HttpStatus`/`Error.Code`.
- ❌ Calling a method and explaining nothing about its non-obvious internals.
- ❌ Threshold/constant without explaining why that value was chosen (e.g. the 24h
  reservation TTL, the `acme-corp` default tenant, the status-code map).
- ❌ Citing a `path:line` that does not exist or references code not found in the current repo.
- ❌ schema-only or prompt/spec text such as "create schema" or "wrap it" without
  concrete files/classes/records/methods/tests/data rows.

## Validation gate

For every code block in §3, verify:
- [ ] Has at least one WHY explanation (blockquote or comment)
- [ ] If it calls another method → internals explained or linked to §2
- [ ] All thresholds/constants have a "why X" explanation
- [ ] All conditions show ALL branches with reasons (e.g. full `Error.Code` → status map)
- [ ] Example values shown in comments (`IsSuccess`/`Data`/`Error.Code`, not just type names)
- [ ] Every citation is a real `path:line` in this repo reflecting code as implemented

## Integration

Load alongside: `implementation-doc-uml-SKILL.md`, `implementation-doc-algorithms-SKILL.md`.
Required by: `planning-session-startup-SKILL.md` (pre-execution doc gate),
`plan-review-SKILL.md` (FC-16).
