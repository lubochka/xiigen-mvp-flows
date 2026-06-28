# Context Package Design

> **Stack adaptation (xiigen flows).** The instruction-file vs context-package
> distinction and the 5-field structure ported from
> `llm_mvp_core/docs/skills/context-package-design-SKILL.md` (G07). The universal
> part (an instruction file says WHAT to do; a context package says WHAT IT NEEDS
> TO KNOW to decide; exactly 5 fields — INTENT verbatim / DOMAIN without
> technologies / CONSTRAINTS only machine-enforced invariants / PRIOR_ART as a
> query string or explicit `NO_PRIOR_ART` / SUCCESS as output shape not content;
> the 8 verify checks; the runtime failure modes) is taken verbatim from core.
> Adapted here:
> - **The core ML examples are NOT ported (G12).** `ContinuousLearningPipeline`,
>   `IncrementalTrainer`, and `DomainModelRegistry` belong to `llm_mvp_core`; this
>   repo's flows consume `.xiigen/wiki-domain` manifests from core and train no
>   models of their own. Context packages here feed flow design decisions
>   (server services and Angular client behavior), not a training pipeline.
> - **CONSTRAINTS** = `DataProcessResult<T>` discipline (`IsSuccess`/`ErrorCode`/
>   `Data`, from `Xiigen.Kernel`; never throw for business logic) plus the flow's
>   locked decisions and the contracts in `flows/FLOW-NN-<slug>/contracts`. This is
>   the XIIGen family wrapper — **not** core `OperationResult<T>`.
> - **DOMAIN** describes flow behavior with no technology names (no `net10.0`, no
>   `Angular`, no class names).
> - **SUCCESS** = the shape of the proof: xUnit `[Fact]` test categories and the
>   Gate-B condition (`dotnet test (per-flow .Tests)` / `ng test`).

## Core Problem

A document that tells a system what steps to execute is an instruction file.
A document that tells a system what it needs to know to make a decision is a
context package. Confusing the two produces decisions that either have too little
context (wrong decisions) or are over-constrained by instructions (no genuine
decision-making). This skill defines the 5-field structure of a valid context
package and how to verify it before use.

---

## When to Invoke

- Before writing the context input for a flow design decision (which server
  service to add, how an Angular component should behave end-to-end)
- Before authoring the brief a planning/architect session will decide from
- When reviewing another actor's context input and verifying it is not an instruction file

---

## Section 1 — The Fundamental Distinction

| Instruction file | Context package |
|---|---|
| Tells the system: do step 1, then step 2 | Tells the system: here is the intent, constraints, context, and success criteria |
| Output is prescribed — the actor executes it | Output is decided by the actor — context enables the decision |
| Fails if a step is ambiguous | Fails if a field is missing or mis-filled |
| The actor is a transcriber | The actor is a decision-maker |
| Self-containment check: can the actor run this alone? | Self-containment check: can the actor make a defensible decision from this alone? |

**The one-sentence test:**
If the document tells the actor WHAT to produce — it is an instruction file.
If the document tells the actor WHAT IT NEEDS TO KNOW to decide — it is a context package.

Practical test: remove all field labels. Read the document. Does it sound like a
recipe (do this, then do that) or a briefing (here is the situation, constraints,
what success looks like)? A recipe is an instruction file. A briefing is a context package.

---

## Section 2 — The 5-Field Structure

Every context package has exactly these 5 fields. No more. No fewer.
If a field is absent, the package is incomplete and must not be used.

### INTENT

| | |
|--|--|
| **Definition** | The exact thing this decision is trying to understand or settle |
| **What belongs** | The user's request or flow goal verbatim — no rephrasing |
| **What does NOT belong** | Interpretation, clarification, scope expansion |
| **Empty fails because** | The decision solves what is written. Empty = invented problem. Rephrased = different problem. |

### DOMAIN

| | |
|--|--|
| **Definition** | What this flow does, in 2–3 sentences |
| **What belongs** | Flow behaviors, user-facing outcomes, business rules |
| **What does NOT belong** | Technology names (`net10.0`, Angular, xUnit), class names, method signatures |
| **Empty fails because** | Without domain context, the actor produces generic output unconstrained by what the flow actually does. |

### CONSTRAINTS

| | |
|--|--|
| **Definition** | Machine-enforced invariants — things that must hold regardless of what is decided |
| **What belongs** | DNA rules; the `DataProcessResult<T>` contract (never throw for business logic); the flow's locked decisions; the contracts in `flows/FLOW-NN-<slug>/contracts` |
| **What does NOT belong** | Preferences, quality goals, technology choices |
| **Empty fails because** | The actor produces unconstrained output that may violate rules the flow must always enforce. |

### PRIOR_ART

| | |
|--|--|
| **Definition** | A retrieval query that fetches relevant prior work — NOT the prior work itself |
| **What belongs** | A query string: "prior implementations of [capability] in [flow]" |
| **What does NOT belong** | Copied documents, pasted prior implementations, inline examples |
| **Empty is acceptable ONLY when** | Explicitly stated as `NO_PRIOR_ART` with reasoning. Silent empty field is a failure. |
| **Empty fails because** | The actor cannot learn from prior work if it cannot retrieve it. |

### SUCCESS

| | |
|--|--|
| **Definition** | What the output must look like — format and structure, not content |
| **What belongs** | Output shape, required `[Fact]` test categories, Gate-B condition format (`dotnet test (per-flow .Tests)` / `ng test`) |
| **What does NOT belong** | Expected content, prescribed answers, example outputs that bound what the actor can produce |
| **Empty fails because** | Without a success definition, the actor cannot self-evaluate. Gate B cannot be verified. |

---

## Section 3 — Verification Checklist

Run before using any context package. All 8 must be TRUE:

```
□ INTENT contains the exact source text — no paraphrasing, no added scope

□ DOMAIN has no technology names (net10.0, Angular, xUnit, class names, etc.)

□ CONSTRAINTS lists only machine-enforced invariants — not preferences or goals

□ Each constraint is stated as a verifiable condition — not as a description

□ PRIOR_ART is a query string OR explicitly states NO_PRIOR_ART with reasoning
  (copying prior implementations inline fails this check)

□ SUCCESS defines output FORMAT — not output content or expected answers

□ No field references another document ("see X", "per Y", "as in Z")

□ No field contains a step or instruction ("first do X", "then check Y")
```

If any check returns FALSE: fix the field before using the package.
A failing context package produces unverifiable output.

---

## Section 4 — Runtime Failure Modes

### INTENT_REPHRASED

```
In package INTENT: "The system should handle marketplace settlement."
User said:         "I need idempotent settlement for the marketplace checkout flow."
Effect:            The actor produces a generic settlement design, not an idempotent one.
Fix:               Copy the request verbatim into INTENT. Never paraphrase.
```

### DOMAIN_SCOPES_THE_STACK

```
In package DOMAIN: "An ASP.NET Core minimal-API project targeting net10.0 with xUnit."
Effect:            The actor optimizes for framework specifics, not flow behavior.
Fix:               Describe what the flow does for users (a buyer pays, the seller is
                   settled, retries never double-charge). Remove all technology names.
```

### CONSTRAINTS_CONTAIN_PREFERENCES

```
In package CONSTRAINTS: "Code should be clean and maintainable."
Effect:            "Clean" is unverifiable. Any output can claim to be clean.
Fix:               Replace with: "Every public service method returns DataProcessResult<T>
                   and never throws for business logic." — a failing [Fact] test exists if violated.
```

### PRIOR_ART_INLINE

```
In package PRIOR_ART: [copied 50 lines from another flow's settlement service]
Effect:            The actor reproduces the prior implementation instead of deciding.
Fix:               Replace with query: "prior settlement implementations in FLOW-09 / FLOW-16"
                   Or: "NO_PRIOR_ART — first settlement service in this flow"
```

### SUCCESS_PRESCRIBES_CONTENT

```
In package SUCCESS: "The implementation should match the FLOW-09 settlement service structure."
Effect:            The actor copies the reference instead of deciding independently.
Fix:               Replace with: "Each public method returns DataProcessResult<T>.
                   One [Fact] test per invariant. Gate B: dotnet test (per-flow .Tests) -> N passing."
```

---

## Anti-Patterns

1. **"I'll fill in PRIOR_ART later"** — an empty PRIOR_ART field cannot be distinguished
   from a forgotten one. Fill it now or mark `NO_PRIOR_ART: first instance in this flow`.

2. **"CONSTRAINTS repeats what's in the DNA doc"** — yes, it should. The package makes
   constraints explicit so the actor does not need to load external documents.
   If this feels redundant, the constraints section is correctly authored.

3. **"I added instructions to SUCCESS to be helpful"** — SUCCESS defines what the output
   looks like, not how to produce it. Remove all procedural language.

4. **"One context package covers all decisions in this phase"** — each decision is a
   separate point. A second-decision package is NOT the same as the first.
   The INTENT field changes per decision. Write a template per decision with parameterized INTENT.

---

## Integration

- `output-readability-gate-SKILL.md` governs the prose sections of a context package
- `session-output-contract-SKILL.md` produces the deliverables list a context package decides toward
- `plan-self-validation-SKILL.md` consumes the verified package when scoping a multi-phase plan
- Rule 5 (read before touching) applies when a context package updates an existing brief
