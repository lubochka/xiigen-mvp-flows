# Decision Capture

> **Stack adaptation (xiigen flows).** Universal significant-decision capture format ported
> from `llm_mvp_core/docs/skills/decision-capture-SKILL.md`. This skill's **canonical owner is
> G02** (other groups consume it). Adapted to this repo:
> - decisions are written to **`docs/DECISIONS-LOCKED.md`** (greenfield — create the file the
>   first time a decision is locked; this repo has no canonical decisions doc yet);
> - the `Principle` field references this repo's invariants: `DataProcessResult<T>` envelope
>   (XIIGen wrapper, NOT core `OperationResult<T>`), `MicroserviceBase<TSelf>`
>   `Success<T>`/`Failure<T>`, Fabric First, tenant-scope isolation, No-Secrets, and the
>   per-flow layer dependency direction `Api → Application → Infrastructure`;
> - per-flow decision examples concern where a capability lives (`Application` for capability
>   vs `Infrastructure` for provider), dependency direction between flow layers, and which flow
>   owns a shared contract;
> - the `Question` field is the retrieval key; **≥2 significant decisions per planning session**;
>   Gate-B for any code that follows a decision = `dotnet build` + `dotnet test` (xUnit).

## Core Problem

A decision made in a planning session and not recorded is re-derived in the next
session. Re-derivation costs planning time and often produces a different answer —
not because the situation changed, but because the reasoning context was lost.
This skill defines what counts as a significant decision, how to record it in
the DESIGN_REASONING format, and when to write it to `docs/DECISIONS-LOCKED.md`
vs when to keep it as a session-level note.

---

## When to Invoke

- At Gate B of every phase — before declaring a phase complete
- During any planning session when a significant decision is made (capture immediately)
- When plan self-validation finds an undocumented design choice
- When a future session references "a prior decision" that is not in `docs/DECISIONS-LOCKED.md`

---

## Section 1 — What Counts as a Significant Decision

A decision is significant if a future session planning similar work would need to
know it to avoid rework.

**The one-sentence test:**
> "If a future session encountered a similar capability or flow, would knowing this
> decision save at least one phase of replanning?"

If YES → capture it.

**Decision types that are always significant:**

| Type | Description | Example (flows) |
|---|---|---|
| `CAPABILITY_CLASSIFICATION` | four-tier-decision result with non-obvious reasoning | FLOW-13 analytics ranking classified as LEARNABLE (consumed via manifest), not FREEDOM config |
| `INTERFACE_INTRODUCTION` | Why a new fabric interface was added vs extending an existing one | `IIdempotencyStore` added instead of overloading `IOutboxStore` — different contract, different lifecycle |
| `DEPENDENCY_DIRECTION` | Why a layer/flow depends on another in this direction | `Application` depends on `Infrastructure` abstraction, never the reverse — Infrastructure is the provider edge |
| `LAYER_OWNERSHIP` | Which flow layer owns a capability | Verification-token issuance lives in `Application`; the persisted store lives in `Infrastructure` |
| `ARCHITECTURE_PATTERN` | A general pattern for future flows | "A shared contract used by N>1 flows is owned by the lowest-numbered flow and referenced by the rest" |
| `LOCKED_DECISION_OVERRIDE` | Why a locked decision needed an ADR before changing | D-05 overridden for FLOW-15 per-tenant store — different binding semantics |

**What is NOT significant:**

- Decisions that follow directly from the project rules (no reasoning needed)
- File naming or test naming choices (implementation details)
- Test framework usage (xUnit `[Fact]`/`[Theory]` is decided — not a per-session decision)
- Choosing Angular standalone components (already the project default)

---

## Section 2 — The DESIGN_REASONING Format

```csharp
// This record maps directly to a docs/DECISIONS-LOCKED.md entry
public record DesignReasoning
{
    public string DecisionId { get; init; }    // "D-NN" — next sequential number
    public string Type { get; init; }          // capability-classification, interface-introduction, etc.
    public string Question { get; init; }      // what was decided? (the retrievable key)
    public string Reasoning { get; init; }     // why? what was considered and rejected?
    public string Principle { get; init; }     // which repo invariant or locked decision applies
    public string Outcome { get; init; }       // what is now true as a result
}
```

**The Question field is the retrieval key.** Future sessions match against `Question`
when searching prior decisions. A vague question produces no match.

```
BAD:  Question = "FLOW-01 design"
GOOD: Question = "Should FLOW-01 store verification tokens in Application memory or an Infrastructure store?"

BAD:  Question = "Where does idempotency live"
GOOD: Question = "Should idempotency be a method on IOutboxStore or a separate IIdempotencyStore?"
```

**The Reasoning field is what teaches future sessions.** A decision that says
"we decided X" teaches nothing. A decision that says "we considered A and B;
A was rejected because it puts a provider concern in the Application layer;
B was chosen because it keeps the layer dependency direction intact..." gives
future sessions the classification logic, not just the classification.

---

## Section 3 — `docs/DECISIONS-LOCKED.md` Entry Format

```markdown
## D-NN: [One-sentence decision statement]

**Type:** CAPABILITY_CLASSIFICATION | INTERFACE_INTRODUCTION | DEPENDENCY_DIRECTION |
          LAYER_OWNERSHIP | ARCHITECTURE_PATTERN | LOCKED_DECISION_OVERRIDE

**Question:** [Exact retrievable question — stated as a question]

**Reasoning:** [What was considered and rejected. Why this option was chosen.
               Which failure mode this prevents. Two to five sentences.]

**Principle:** [Which repo invariant (DataProcessResult, Fabric First, tenant-scope,
               No-Secrets, layer direction), prior locked decision, or pattern applies]

**Outcome:** [What is now true. What this decision enables or prevents.]

**NEVER reopen without Luba approval.** [Add this line if the decision has
high blast radius — i.e., 3+ flows depend on it]
```

**Minimum 2 decisions per planning session.** A session with fewer than 2 significant
decisions either had trivial scope or decisions were missed.

---

## Section 4 — When to Write to `docs/DECISIONS-LOCKED.md`

Not every significant decision belongs in the locked file. The criteria:

| Write to `docs/DECISIONS-LOCKED.md` when | Keep as session note when |
|---|---|
| Decision affects ≥ 2 flows or capability classes | Decision is specific to one flow's internal implementation |
| Decision sets a pattern for future flows | Decision is about current session scope only |
| Decision would be challenged without documentation | Decision is obvious from the code |
| Decision requires "NEVER reopen without Luba" | Decision can safely be revisited |
| Decision overrides a prior locked decision | Decision supplements but does not override |

---

## Section 5 — Retroactive Capture

For flows that were built before this skill existed, capture decisions retroactively from:
- Git commit messages (flow commit messages often contain reasoning)
- `.xiigen/wiki-domain` manifests and any flow `docs/` notes (settled context)
- Code comments and the per-flow layer layout (often capture the why)

Retroactive capture is less valuable than real-time capture (some context is lost),
but it is far better than nothing. Every captured decision reduces future replanning.

---

## Anti-Patterns

1. **"The decision is obvious from the code"** — code shows what was decided.
   `docs/DECISIONS-LOCKED.md` records why. Future sessions need the why.

2. **"I'll capture it at the end of the session"** — decisions captured from memory
   at session end are thinner than decisions captured immediately. Record as you go.

3. **"Decision type = GENERAL"** — every significant decision fits one of the 6 types.
   A decision typed GENERAL was not classified. Type forces the reasoning structure.

4. **"Question field written as a statement"** — `Question = "The FLOW-01 design"`
   is not retrievable by a future session searching for the decision. Ask it as a question.

5. **"Fewer than 2 decisions per session"** — a planning session with fewer than 2 significant
   decisions either had trivial scope or decisions were missed. Re-read the session.

---

## Integration

```
- docs/DECISIONS-LOCKED.md is the persistence layer for decisions captured by this skill
- decision-reopening-SKILL.md governs when a locked decision must be revisited
- document-hierarchy-SKILL.md routes decisions to docs/DECISIONS-LOCKED.md (Tier 1)
- four-tier-decision-classification-SKILL.md produces CAPABILITY_CLASSIFICATION decisions
- planning-session-startup-SKILL.md reads docs/DECISIONS-LOCKED.md before any plan is written
```
