# pipeline-position-check-SKILL.md — Q0 stage contract + CONTEXT_INSUFFICIENT before generation

> **Stack adaptation (xiigen flows).** Universal *pipeline position check* ported
> from `llm_mvp_core/docs/skills/pipeline-position-check-SKILL.md` (G08). Greenfield
> here — no prior author version. The universal core kept: a Q0 stage-contract
> discipline — before any implementation phase, declare what this stage RECEIVES,
> what it PRODUCES, which component CONSUMES it, and what the consumer NEEDS; if the
> consumer requirement (Q0d) cannot be answered with specificity, emit
> `CONTEXT_INSUFFICIENT` and halt rather than synthesize blindly. This is general
> planning discipline, NOT core ML-pipeline topology. The stack details are adapted:
> - A **stage** is the input/output of a per-flow `Api` / `Application` /
>   `Infrastructure` boundary (Api → Application → Infrastructure; or upstream-flow
>   → this-flow), not core engine nodes.
> - A **missing `contracts/` interface or `.xiigen` manifest** ⇒
>   `CONTEXT_INSUFFICIENT` stop.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` / `dotnet test` (+ Playwright for
>   UI). The contracts referenced produce `DataProcessResult<T>` (XIIGen family) —
>   **NOT** core `OperationResult<T>`.

## Core Problem

A phase that implements a component without knowing what its downstream consumer
needs produces locally correct output that is globally useless. The component
compiles, passes tests, and still fails to serve its purpose because the wrong
shape was produced. This skill defines the four pre-phase questions answered
before any file is read or any code is written.

---

## When to Invoke

- Before Q1 of any implementation session
- Before Phase A of any phase whose output is consumed by a downstream phase or flow
- When session scope is "fix the interface between Phase N and Phase N+1" (or between two flows)
- When a component passes Gate B but the downstream phase/flow still fails

---

## Section 1 — The 4-Part Pipeline Position Declaration

Answer all four explicitly before any file is read or code written. If Q0d cannot
be answered with specificity, emit `CONTEXT_INSUFFICIENT` (Section 2) instead of
proceeding.

### Q0a — What does this stage RECEIVE as input?

State which upstream phase/component/flow produces the input, field names, required shape.

```
Example: "OnboardingDeliveryService.DeliverAsync() receives a RegistrationOutcome
          from UserRegistrationService after RegisterAsync().
          Required fields: UserId, TenantId, VerifiedEmail"
```

### Q0b — What does this stage PRODUCE as output?

State field names, required shape, persistence destination.

```
Example: "DeliverAsync() produces a DataProcessResult<OnboardingOutcome>.
          OnboardingOutcome fields: DeliveryChannel, DeliveredAtUtc, CorrelationId"
```

### Q0c — Which component CONSUMES this output?

State which downstream component/flow reads this and what it does with it.

```
Example: "The registration endpoint (Program.cs MapPost) returns OnboardingOutcome to the
          Angular client; FLOW-02 profile-enrichment later reads DeliveredAtUtc to gate enrichment."
```

### Q0d — What does the consumer NEED from this output?

Answer from the consumer's perspective, not the producer's.

```
BAD:  "FLOW-02 needs the OnboardingOutcome"  ← restatement
GOOD: "FLOW-02 needs DeliveredAtUtc to be a UTC instant (not local time and not null)
      because enrichment eligibility is computed as (now - DeliveredAtUtc) > 24h.
      If it is local time or null, eligibility is wrong across tenants in other time zones."
```

**Gate rule:** If Q0d produces "I'm not sure" or a generic restatement of the
output fields, the session is not ready. Emit `CONTEXT_INSUFFICIENT`.

---

## Section 2 — CONTEXT_INSUFFICIENT Signal

When Q0d cannot be answered with specificity, emit this signal. This is correct
behavior, not a failure. The session halts and waits.

```
CONTEXT_INSUFFICIENT

phase:    [which phase this session is implementing]
gap:      [what specific information is missing — not vague, not "more context"]
consumer: [which downstream phase / component / flow is affected]
impact:   [what quality failure will occur if this proceeds — be specific]
request:  [3-6 concrete questions Luba can answer in one sentence each]
```

Example — consumer requirement unknown:

```
CONTEXT_INSUFFICIENT

phase:    OnboardingDeliveryService.DeliverAsync() in FLOW-01 Phase 3
gap:      Unknown whether DeliveredAtUtc must be UTC or tenant-local. contracts/schemas
          and the flow docs are silent on the time-zone semantics of this field.
consumer: FLOW-02 profile-enrichment eligibility gate
impact:   If tenant-local time is stored, the 24h enrichment gate fires early/late for
          tenants outside the server time zone; cross-tenant behavior becomes inconsistent.
request:
  1. Must DeliveredAtUtc be a UTC instant, or the tenant's local time?
  2. Does FLOW-02 expect to compare it against UTC now, or tenant-local now?
  3. Is there a decision artifact / flow.manifest.json note I should read first?
```

---

## Section 3 — Resolution Protocol

**Step 1 — Session halts.** After emitting CONTEXT_INSUFFICIENT, halt completely.
No files read, no code written. State: "Session halted. Awaiting context resolution."

**Step 2 — Luba provides resolution.** One sentence per question is sufficient.

**Step 3 — Resumption gate:**

```
□ Each question in `request` has a specific answer (not "use your judgment")
□ The answers together close the `gap`
□ Q0d can now be answered with specificity using the answers
```

If any item fails → re-emit CONTEXT_INSUFFICIENT with the remaining gap.

**Step 4 — Update design artifacts.** Before writing code, update the relevant
artifacts (`contracts/schemas`, the flow docs, `flow.manifest.json`) so the same
gap does not recur.

---

## Section 4 — Pipeline Contract Check (Before Gate B)

After implementation, before declaring Gate B passed:

```
□ Q0a verified: input shape received matches what was declared
□ Q0b verified: output shape produced matches what was declared
□ Q0c verified: consumer can receive this output (no contract mismatch)
□ Q0d verified: consumer requirement is met by what was produced
               If not → surface gap, resolve, then proceed to Gate B
```

Passing test count is necessary but not sufficient. A phase that produces locally
correct output failing Q0d has produced noise, not a deliverable.

---

## Section 5 — Flow Pipeline Contracts Reference

Consumer requirements at common flow boundaries:

```
STAGE BOUNDARY                 CONSUMER REQUIREMENT
─────────────────────────────────────────────────────────────────────────────
Application → Api (endpoint):
  The endpoint needs a DataProcessResult<T> whose ErrorCode maps to a stable HTTP
  status. A bare value or a thrown exception breaks the response contract.

Api (this flow) → Angular client:
  The client needs the exact field names/types declared in contracts/schemas; an
  additive-only change is safe, a rename is a breaking contract change.

This flow → downstream flow (flow.manifest.json edge):
  The downstream flow needs the cross-flow field with the agreed semantics
  (time zone, scope, units). Same name, different meaning = silent corruption.

Server → .xiigen/wiki-domain consumer:
  Anything contributed to the generated domain model must match the manifest shape;
  the model body lives in llm_mvp_core (R5) — do not redefine it here.
```

---

## Anti-Patterns

1. **"Implementing a phase without stating Q0d first"** — if Q0d is unknown, emit CONTEXT_INSUFFICIENT before touching any file.
2. **"Q0d answered as a restatement: 'the consumer needs the output'"** — not an answer. State the specific property the consumer depends on.
3. **"'Looks correct' means done"** — output that looks well-formed but fails Q0d's consumer requirement is incorrect. Verify Q0d before claiming Gate B.
4. **"Vague CONTEXT_INSUFFICIENT requests"** — "please provide more context" shifts work back to Luba without narrowing the gap. Name the specific field and why it matters.
5. **"Resuming without updating design artifacts"** — the resolution closes a gap; record it so the question doesn't recur.

---

## Integration

- `planning-session-startup-SKILL.md` blockers question produces the prerequisite context for Q0a.
- `contracts/schemas` is the canonical source for the shapes needed in Q0b–Q0c.
- `implementation-integrity-SKILL.md` / `api-shape-verification-SKILL.md` run the Q0c contract-mismatch check.
- `system-intake-SKILL.md` / `context-package-design-SKILL.md` structure maps to Q0a/Q0b/Q0c/Q0d fields.
