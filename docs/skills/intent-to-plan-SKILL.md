# Intent to Plan

> **Stack adaptation (xiigen flows).** Universal intent→clause contract ported from
> `llm_mvp_core/docs/skills/intent-to-plan-SKILL.md`. Parse the request into a clause
> table BEFORE choosing ".NET 10 per-flow vs Angular 21" implementation. Technology
> qualifiers ("in C#", "standalone component", "minimal-API endpoint", "xUnit") are
> **context, not clauses**. Split "and"-steps so each has a single measurable Gate B,
> and that Gate B is a real command for this repo: `dotnet build` / `dotnet test`
> (xUnit) for backend, Angular 21 Playwright for client.

## What This Skill Prevents

1. **Technology names in plan steps.** When planning "add invitation onboarding to the
   user-registration flow", the natural default is to write "add `DeliverInvitationAsync`
   to `OnboardingDeliveryService` and a `MapPost(.../onboarding/invitation)` route." That
   is a technology-scoped step, not a plain-language step. The initial plan
   (REFERENCE-PLAN) must be infrastructure-neutral.
2. **Scope drift from sentence to implementation.** The INTENT field must be the user's
   request verbatim. Every paraphrase changes scope.
3. **Steps that combine responsibilities.** A step containing "and" usually contains two
   steps. Combined responsibilities make Gate B ambiguous — a step covering two things has
   no single measurable Gate B.

---

## When to Invoke

Load before writing any REFERENCE-PLAN or EXECUTION phase descriptions. Use alongside
`phase-planning-SKILL.md` — this skill governs WHAT to put in each plan step;
`phase-planning-SKILL.md` governs the gate validation.

---

## Section 1 — Parsing the User's Request

An **intent clause** is one distinct capability the user is requesting. Parse the user
sentence to identify all intent clauses before writing anything.

### Parsing rules

| Signal | What it means |
|--------|--------------|
| Every noun phrase naming a capability | One clause |
| Technology qualifiers ("in C#", "standalone Angular component", "minimal-API", "xUnit") | Context — not a clause |
| Multiple verbs describing distinct actions | One clause per verb |
| "and" connecting two capabilities | Two clauses |
| Ambiguous phrase that could mean two things | Flag as ambiguous — ask |

### Clause table (fill before writing the plan)

| # | Clause | Source phrase | Ambiguous? | Question if ambiguous |
|---|--------|---------------|-----------|----------------------|
| 1 | | | | |

### Example — onboarding completion

Request: "Add onboarding completion so a verified user can finish onboarding and get a tutorial."

| # | Clause | Source phrase | Ambiguous? | Question |
|---|--------|---------------|-----------|---------|
| 1 | onboarding completion | "onboarding completion" | No | — |
| 2 | deliver tutorial | "get a tutorial" | No | — |

Result: 2 clauses → minimum phases max(2, complexity); state the reasoning, do not use
default ranges without justification.

---

## Section 2 — What a Valid Plan Phase Looks Like

Every phase must satisfy all 3 properties.

### Property 1 — PLAIN LANGUAGE

The phase names a capability, not a technology implementation.

```
Good: "Build onboarding completion for a verified user"
Bad:  "Add CompleteAsync(userId, email, existingCount) to OnboardingDeliveryService and MapPost .../onboarding/complete"
      — contains class/method/route names
```

The technology-level detail (class names, method signatures, route templates,
`DataProcessResult<T>` returns) belongs in the EXECUTION file, not the REFERENCE-PLAN.

### Property 2 — SINGLE RESPONSIBILITY

One phase = one distinct capability. "And", "also", "as well as" is a candidate split point.

```
Good: "Define onboarding completion"
Bad:  "Define onboarding completion and wire the tutorial delivery route"
      — two capabilities (define + wire)
Split rule: if you can draw a line between two parts and each makes sense alone — split.
```

### Property 3 — GATE B DECLARED

Every phase has a measurable Gate B (Rule 8). A phase without Gate B is not a valid plan
phase. Gate B must be a real command for this repo:

```
Good: "Gate B: dotnet test --filter FullyQualifiedName~Onboarding → 6+ tests pass, 0 fail"
Good: "Gate B: dotnet build flows/FLOW-01-user-registration → 0 Error(s)"
Good: "Gate B: Playwright spec onboarding.spec.ts → green"
Bad:  "Gate B: onboarding works correctly"  — not measurable

Gate B format: [command] → [exact expected value or comparison]
```

---

## Section 3 — Plan Reviewer Protocol

After writing a plan, apply these checks to each phase.

### Check 1 — COVERAGE
For each intent clause: COVERED | PARTIAL | MISSING (the clause is the primary subject of
at least one phase = COVERED).

### Check 2 — ABSTRACTION
For each phase: does it contain a class name, method signature, route template, or C#
keyword? Derive the technology list per plan, e.g.:
```
Class names: OnboardingDeliveryService, EmailVerificationService
Method names: CompleteAsync(), DeliverTutorialAsync(), MapPost()
C# keywords / types: async, Task<DataProcessResult<T>>, sealed record
```
Plan fails abstraction if ANY REFERENCE-PLAN phase has a VIOLATION. (Technology detail
belongs in EXECUTION files.)

### Check 3 — SINGLE RESPONSIBILITY
Signal words "and", "also", "as well as" → flag as compound. Plan fails if >20% of phases
are compound.

### Check 4 — GATE B COMPLETENESS
Verdict per phase: PRESENT | MISSING | VAGUE.
- PRESENT: "[dotnet/Playwright command] → [exact value]".
- VAGUE: "works correctly", "tests pass", "implementation complete" → must be replaced
  with exact command + expected output.

---

## Anti-Patterns

1. **Technology names in REFERENCE-PLAN** — REFERENCE-PLAN describes capabilities;
   EXECUTION files describe steps with class names, methods, routes, and `DataProcessResult<T>` returns.
2. **100% first-run coverage** — suspicious; check whether the plan addresses the user's
   actual clauses, not a standard flow template.
3. **"While I'm here" phases** — adding phases not requested. The plan must address the
   user's clauses and no more.
4. **Vague Gate B** — "tests pass" is not Gate B. "`dotnet test --filter ~Onboarding` →
   6+ tests pass, 0 fail" is Gate B.

## Integration

Load alongside: `phase-planning-SKILL.md` (gate validation), `plan-self-validation-SKILL.md`
(Phase 0). Required by: `planning-session-startup-SKILL.md`.
