# feature-prioritization-SKILL.md — 3-axis score card before sequencing what to build

> **Stack adaptation (xiigen flows).** Universal *feature prioritization* ported
> from `llm_mvp_core/docs/skills/feature-prioritization-SKILL.md` (G08). Greenfield
> here — no prior author version. The universal core kept: a 3-axis score
> (Business Value 1–5 × Dependency Multiplier 1–3 × Infrastructure Cost⁻¹ 1–3),
> a score card before execution, an override only with a recorded decision, and an
> invariant dependency lock. The stack details are adapted:
> - The **unit of prioritization** is a `FLOW-NN-*` or a capability inside a flow.
> - **Axis 2 (deps)** is read from `flow.manifest.json` / cross-flow contracts.
> - **Axis 3 (cost⁻¹)** measures how much new `contracts/` or fabric must be built
>   before the capability can be implemented (all required contracts already exist
>   = highest score).
> - **Overrides** are recorded in a decision artifact next to the plan /
>   `flow.manifest.json`, not in a core `DECISIONS-LOCKED.md`. The domain wrapper
>   is `DataProcessResult<T>` (XIIGen family) — **NOT** core `OperationResult<T>`.

## Core Problem

When multiple flows or capabilities are buildable at the same time, choosing the
wrong one to build first delays user-visible value and wastes infrastructure
investment. This skill makes the ordering decision explicit and auditable, and
records any override so priority decisions are not invisible.

---

## When to Invoke

- When a plan has 2+ candidate flows/capabilities to sequence
- When Luba asks "what should we build next?"
- When a new flow is proposed and dependencies are not yet mapped
- When session-start reconnaissance finds multiple incomplete flows

One score card per session = zero "we built the wrong thing first" retrospectives.

---

## Section 1 — The 3-Axis Score

### Axis 1 — Business Value Unlock (1–5)

Does completing this flow/capability let users DO something new?

| Score | Meaning |
|---|---|
| 5 | Users can take a new action that was impossible before |
| 4 | Substantially improves a current action (speed, quality, reach) |
| 3 | Makes the system more complete, but no new user action is unlocked |
| 2 | Operational improvement (monitoring, diagnostics) |
| 1 | Backend-only: infrastructure/plumbing, no user-visible change |

### Axis 2 — Dependency Multiplier (1–3)

How many other flows are blocked until this ships? (read `flow.manifest.json`)

| Score | Meaning |
|---|---|
| 3 | 3 or more flows cannot start until this ships |
| 2 | 1–2 flows are blocked |
| 1 | Nothing else is blocked by this |

### Axis 3 — Infrastructure Cost (1–3, INVERTED — lower cost = higher score)

How much new `contracts/` or fabric must exist before this can be implemented?

| Score | Meaning |
|---|---|
| 3 | All required contracts already exist (in `contracts/` or shared Xiigen.Kernel) |
| 2 | One new contract required |
| 1 | Two or more new contracts/fabric pieces required |

INVERTED because high infrastructure cost means the capability cannot be built
until that infrastructure exists — building the infrastructure first is the only
correct sequence, and a low score captures that friction correctly.

### Priority Score Formula

```
Priority Score = Axis1 × Axis2 × Axis3
```

Maximum 5 × 3 × 3 = 45. Minimum 1 × 1 × 1 = 1.

---

## Section 2 — Scoring Example

| Capability | Axis 1 | Axis 2 | Axis 3 | Score | Rationale |
|---|---|---|---|---|---|
| User registration core | 5 | 3 | 3 | 45 | Users can register; 3 downstream flows depend on it; contracts exist |
| Onboarding delivery | 4 | 2 | 3 | 24 | Improves registration; 2 flows blocked; contracts exist |
| Moderation decision | 3 | 1 | 2 | 6 | More complete; nothing blocked; 1 new contract needed |
| Analytics export | 3 | 1 | 1 | 3 | Backend-only; nothing blocked; 2+ new contracts needed |

**Build order:** registration core → onboarding delivery → moderation → analytics export.

---

## Section 3 — Score Card Format

```markdown
## Feature Priority Score Card — [Session Date]

| Capability/Flow | Axis1 (value) | Axis2 (deps) | Axis3 (cost⁻¹) | Score | Verdict |
|---|---|---|---|---|---|
| [name] | [1-5] | [1-3] | [1-3] | [A1×A2×A3] | BUILD_NEXT / QUEUE / BLOCKED |

Selected build order: [ranked list]
Override: [NONE / Decision (artifact next to plan) — reason]
```

Verdicts:
- **BUILD_NEXT** — highest score in current set; no blocking prerequisites
- **QUEUE** — lower score; prerequisites met; sequence after BUILD_NEXT
- **BLOCKED** — hard dependency on an unshipped capability; score irrelevant until unblocked

---

## Section 4 — Override Rule

A priority score can be overridden when:
- Luba explicitly names a different capability as the priority
- A stakeholder commitment requires a specific delivery order

**Override is not free.** Record every override as a decision in the artifact next
to the plan (and reference it from `flow.manifest.json` if it changes ordering):

```markdown
D-XX — Priority Override: [capability] promoted over [score-winner]
Reason: [stakeholder reason / commitment]
Score at override time: [score-winner]=N, [promoted]=M
Recorded: [date]
```

**What override cannot do:** override a hard dependency lock. If Capability B
requires Capability A's output, B cannot be built first regardless of score. The
dependency graph is invariant; only the ordering of non-blocked capabilities is
flexible.

---

## Section 5 — Dependency Lock Rule

Before scoring, classify each candidate:

```markdown
Dependency check: [Capability/Flow]
  Requires: [contracts/flows that must exist first — from flow.manifest.json]
  Status of each: [SHIPPED / IN_PROGRESS / MISSING]
  Can start now: YES / NO — blocked by [name]
```

Candidates with any MISSING prerequisite are marked BLOCKED and removed from the
score calculation. They are re-evaluated after their prerequisites ship.

---

## Anti-Patterns

1. **"We built what was easiest"** — low infrastructure cost (Axis 3 = 3) alone does not justify building first if Axis 1 = 1. Score all three axes.
2. **"We built what felt most important"** — feeling is not a score. Produce the card before building.
3. **"Override is fine, we don't need to record it"** — an unrecorded override is an invisible architectural decision. Record it.
4. **"Dependency locks are guidelines"** — they are not. If Capability B reads Capability A's output, B cannot run before A writes it. This is a code constraint, not a preference.

---

## Integration

- `prerequisite-chain-SKILL.md` classifies each dependency as PASS/FAIL/BLOCKING.
- `solution-scope-gate-SKILL.md` classifies each capability's fix class before scoring.
- `plan-self-validation-SKILL.md` verifies the selected build order delivers the stated goals.
- The decision artifact next to the plan receives every priority override.
