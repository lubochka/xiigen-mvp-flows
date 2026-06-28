# Solution Scope Gate

> **Stack adaptation (xiigen flows).** Universal 5-level scope ladder + reframe tests + Scope
> Gate Record ported from `llm_mvp_core/docs/skills/solution-scope-gate-SKILL.md`. Adapted to
> this repo:
> - **Level 2 ADAPTATION** = a wrapper around a .NET BCL/NuGet utility behind `IConfiguration`,
>   or an Angular wrapper around an npm utility;
> - **Level 4 NEW COMPONENT / Level 5 NEW INTERFACE** map onto the per-flow layers:
>   capability work lands in `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application`,
>   provider work in `...Infrastructure`, and DI registration in the flow `Api` host. UI
>   capability lands in an Angular 21 standalone component / provider in
>   `client/angular/src`;
> - every Level 5 interface = a new `I`-prefixed C# interface + concrete class + DI registration
>   in the flow `Api` host + xUnit tests in `...Tests`. (On the client: a new Angular service +
>   `InjectionToken`/provider in `app.config.ts`.) The domain return type is `DataProcessResult<T>`
>   (XIIGen wrapper), never core `OperationResult<T>`;
> - **Gate-B** for any produced level = `dotnet build` + `dotnet test` (xUnit), plus Angular 21
>   Playwright for client branches.

## Core Problem

A session that proposes a new infrastructure component when a convention change would
suffice has over-engineered the solution. A session that proposes a convention when
new infrastructure is genuinely required has under-specified it. This skill installs
the correct scope level before any proposal is written.

---

## When to Invoke

- Before proposing any solution to a gap or missing capability
- Before writing a new plan that adds components to a flow
- When gap-to-proposal work identifies a gap that needs resolution
- Before classifying any change as "new interface required"

One run of this skill before proposal = no over-engineered solutions, no
under-specified ones.

---

## Section 1 — The 5-Level Scope Ladder

Every solution proposal occupies exactly one level. Start at Level 1 and
move up only when the lower level is genuinely insufficient.

| Level | Name | What It Produces | When It Applies |
|-------|------|-----------------|-----------------|
| **1** | CONVENTION | Entry in a skill file, `docs/DECISIONS-LOCKED.md`, or AGENTS.md/CLAUDE.md | Behavior that must be consistent but requires no code |
| **2** | ADAPTATION | Wrapper around a .NET BCL/NuGet utility (server) or npm utility (Angular) | External tool exists; it needs a project-conformant interface |
| **3** | EXTENSION | New method/capability on an existing class or interface in a flow layer | The class already exists; it is missing one thing |
| **4** | NEW COMPONENT | New class / pipeline step / xUnit fixture in `Application` or `Infrastructure`; new Angular standalone component | Net-new behavior with no existing attachment point |
| **5** | NEW INTERFACE | New `I`-prefixed interface + concrete class + DI registration in the flow `Api` host + xUnit tests | Net-new abstraction boundary required; Level 4 is insufficient |

**Level 5 is the last resort.** Every interface addition requires:
- A new `I`-prefixed interface file (server) or Angular service + `InjectionToken` (client)
- A concrete implementation
- Registration in the flow `Api` host DI (server) or `app.config.ts` providers (client)
- xUnit tests for the concrete implementation (server) / Playwright for the client branch
- A compliance check for all touched files (Fabric First, `DataProcessResult<T>`, tenant-scope)

---

## Section 2 — Gap Classification Table

When a gap is identified, classify it on two axes before assigning a scope level.

### Axis 1 — Gap Category

| Category | Meaning |
|----------|---------|
| **TOOL** | A utility is missing (formatter, parser, mapper) |
| **SKILL** | A skill document is missing or incomplete |
| **ORCHESTRATION** | A coordination step between existing flow components is missing |
| **ARCHITECTURE** | A structural boundary or abstraction is missing |

### Axis 2 — Fix Class

| Fix Class | Meaning | Max Scope Level |
|-----------|---------|-----------------|
| **CONTENT** | Missing documentation, comment, or convention entry | Level 1 |
| **INTERFACE** | Missing `I`-prefix interface definition | Level 5 |
| **EXTENSION** | Existing class needs a new method | Level 3 |
| **NEW_HANDLER** | New class needed, but no new interface boundary | Level 4 |
| **INFRASTRUCTURE** | New external dependency or storage layer (a flow `Infrastructure` provider) | Level 5 |

Classification record:
```
Gap: [description]
Category:  [TOOL | SKILL | ORCHESTRATION | ARCHITECTURE]
Fix class: [CONTENT | INTERFACE | EXTENSION | NEW_HANDLER | INFRASTRUCTURE]
Assigned scope level: [1-5]
Justification: [one sentence — why this level and not one lower]
```

---

## Section 3 — The 3 Reframe Tests

Before finalizing a scope level, run all 3 reframe tests.
Each test that triggers a downgrade must be recorded.

### Reframe Test 1 — Next Instance Test

> "What happens at the next instance of this same need?"

If the answer is "same convention applies" → Level 1.
If the answer is "same wrapper applies" → Level 2.
If "each instance (each flow) needs its own class" → Level 4 or 5.

A solution that scales to the next flow without code change belongs at Level 1 or 2.

### Reframe Test 2 — Provider vs Capability Test

> "Is this a provider (external tool, service, data source) or a capability
> (algorithm, decision, transformation)?"

Provider → Level 2 (ADAPTATION: wrap behind `IConfiguration` / DI) — and it belongs in
the flow's `Infrastructure` layer.
Capability → Level 3, 4, or 5 depending on whether an attachment point exists — and it
belongs in the flow's `Application` layer.

### Reframe Test 3 — Backward Compatibility Test

> "Does the existing design survive intact inside the new design?"

If yes: the new design is an extension → maximum Level 3.
If no: the new design replaces something → validate with `decision-reopening-SKILL.md`
before proceeding to Level 4 or 5.

---

## Section 4 — Scope Gate Record

Produce this record before writing any proposal:

```markdown
## Solution Scope Gate (solution-scope-gate-SKILL)

Gap statement: [one sentence — what is missing]
Category: [TOOL | SKILL | ORCHESTRATION | ARCHITECTURE]
Fix class: [CONTENT | INTERFACE | EXTENSION | NEW_HANDLER | INFRASTRUCTURE]

Reframe tests:
  Test 1 (next instance): [result]
  Test 2 (provider vs capability): [result → Application or Infrastructure]
  Test 3 (backward compatibility): [result]

Assigned scope level: [1 | 2 | 3 | 4 | 5]
Level name: [CONVENTION | ADAPTATION | EXTENSION | NEW COMPONENT | NEW INTERFACE]
Target flow + layer: [FLOW-NN-<slug> / Application | Infrastructure | Api | client/angular]
Justification: [one sentence — why this level and not one lower]

Proposal permitted: YES
```

If any reframe test indicates a lower level is sufficient, the scope is downgraded
before the record is finalized. Do not propose at a higher level than the record permits.

---

## Anti-Patterns

1. **"Start at Level 5 to be safe"** — Level 5 creates DI/wiring debt that outlasts
   the feature. Justify each level from Level 1 up.

2. **"Skip classification, just write the code"** — unclassified gaps produce
   solutions at the wrong scope level. Classification takes 2 minutes; rework takes hours.

3. **"Reframe tests don't apply here"** — they always apply. A test that returns
   "not applicable" is a signal that the gap was not stated precisely enough.

4. **"Convention is too weak for this"** — if a convention entry in a skill file or
   `docs/DECISIONS-LOCKED.md` would solve the problem, that is the right answer.
   Code is not stronger than a rule — it is more expensive.

5. **"Put the provider in Application"** — a provider (external dependency/storage) is
   Level 2/5 and belongs in `Infrastructure`. A capability belongs in `Application`.

---

## Integration

- gap-to-proposal work calls this skill to assign a scope level to each gap
- `decision-reopening-SKILL.md` is required before any Level 5 proposal that
  replaces an existing interface (Backward Compatibility Test 3 failure)
- `level-correction-response-SKILL.md` detects when a proposal sits at the wrong level on this ladder
- `freedom-machine-SKILL.md` / `four-tier-decision-classification-SKILL.md` classify the values inside the chosen level
