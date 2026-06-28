# architectural-decision-testing-SKILL.md — Load locked decisions, test over 3 horizons (xiigen flows)
## v1.0: ported from llm_mvp_core architectural-decision-testing → .NET 10 per-flow

## Core Problem

A planning session that proposes something contradicting a locked decision
wastes the session. A decision made once and never re-tested may have been right
at the time but wrong after 10 uses. This skill defines the decision loading
protocol (load before proposing), the three testing timeframes (immediate /
short-term / long-term), and the invalidation procedure (negative knowledge is
preserved, not deleted).

---

## When to Invoke

- At the start of every ARCHITECT session — before any proposal is made
- After implementing the 4th flow capability that uses a given decision
- During any architectural review after 10+ flow capabilities
- When a test reveals a decision was applied in the wrong context

---

## Section 1 — Decision Loading (Before Any Proposal)

Load prior decisions before proposing anything. A proposal that contradicts a
locked decision without knowing it exists creates planning waste.

### Step 1 — Find locked decisions for this domain

The flows decision source is the repo governance — `DECISIONS-LOCKED.md` /
`AGENTS.md` / `CLAUDE.md` if present, plus the active flow plan. Resolve the
**real flows source**, not .NET-package names from another repo.

```bash
# Read locked decisions (whichever governance file the flows repo carries)
cat docs/DECISIONS-LOCKED.md 2>/dev/null || cat AGENTS.md
# Find decisions relevant to this flow / capability
grep -i "<flow or capability name>" docs/DECISIONS-LOCKED.md AGENTS.md CLAUDE.md 2>/dev/null
```

### Step 2 — Classify each decision

| Status | Definition | Rule |
|---|---|---|
| LOCKED | Implemented, tested, or explicitly closed | Requires a decision-reopening challenge to change |
| OPEN | Stated as direction, not yet implemented/tested | Can change with evidence + rationale this session |
| PENDING | Flagged open question, unresolved | Must resolve before dependent work proceeds |

### Step 3 — Check every proposal against locked decisions

For every proposed solution: does it contradict any LOCKED decision?

**If yes: STOP.** Produce a DECISION_REOPENING challenge before proceeding:

```markdown
## DECISION_REOPENING Challenge
**Locked decision:** [exact text from DECISIONS-LOCKED.md / AGENTS.md]
**New evidence:** [what was found that warrants reconsideration]
**Proposed change:** [what would change if reopened]
**Blast radius:** [which flows / Api / Application / Infrastructure depend on this]
**Recommendation:** reopen | confirm locked | defer
```

Do not "work around" a LOCKED decision.

---

## Section 2 — Immediate Testing (Same Session as the Decision)

### For INTERFACE_INTRODUCTION decisions (fabric-first under .NET):

```
□ New interface defined and consumed via constructor injection
□ Fabric-first (no `new ConcreteType` for a contract that has an interface)
□ The per-flow service uses the interface, not the concrete type
□ Both the interface and at least one implementation exist before any consumer
```

```bash
# Fabric-first enforcement: concrete injections that bypass an interface should be 0
grep -rn "new ConcreteType" flows/FLOW-NN-*/server --include="*.cs" | grep -v obj | grep -v Tests
# Expected: 0 hits on the production path
```

### For DEPENDENCY_DIRECTION decisions:

```
□ No circular dependency among Api → Application → Infrastructure
  (Api depends on Application; Application must NOT depend on Api)
□ Consumer references the producer's interface, not its concrete type
□ A flow's .Tests references .Application + .Infrastructure (confirmed), never .Api-only shortcuts
```

```bash
# Circular-dependency guard: Application must not reference Api
grep -rn "Xiigen.Flow.<Name>.Api" flows/FLOW-NN-*/server/Xiigen.Flow.<Name>.Application --include="*.csproj"
# Expected: 0 hits
```

---

## Section 3 — Short-Term Testing (After First 3 Uses)

Run during the 4th flow capability that touches the same decision.

### For INTERFACE_INTRODUCTION decisions:

```bash
# Did the interface stay stable across 3+ implementors (flows)?
grep -rn "I<YourInterface>" flows --include="*.cs" | grep -v obj | grep -v Tests
# Expected: multiple implementors, same interface shape

# Concrete-type injections that bypass the interface
grep -rn "new <ConcreteType>" flows --include="*.cs" | grep -v obj | grep -v Tests
# Expected: 0 (fabric-first)
```

### For ARCHITECTURE_PATTERN decisions:

```bash
# Did the per-flow 4-project pattern (Api/Application/Infrastructure/Tests) hold across 3+ flows?
# Were there Gate-B failures attributable to the pattern? Expected: reinforced, not challenged.
```

---

## Section 4 — Long-Term Testing (After 10+ Capabilities)

Run during any architectural review.

```
HEALTHY decision:
  □ Same problem solved consistently across 5+ flows
  □ No flow needed to reopen the decision to proceed
  □ The decision is referenced, not re-derived

WEAKENING decision:
  □ 2+ flows deviated with justification
  □ The deviation pattern is the same (signal: too rigid)
  □ Consider updating the decision to include the deviation as a valid case

FAILED decision:
  □ Majority of flows worked around the decision to succeed
  □ The problem it was meant to solve still recurs
  □ Action: invalidate and reopen (Section 5)
```

---

## Section 5 — Decision Invalidation

If testing shows a decision was wrong:

**Step 1 — Record the invalidation in the governance file:**

```markdown
## D-NN — [Decision Name] [INVALIDATED 2026-MM-DD]
**Status:** INVALIDATED
**Original resolution:** [what was decided]
**Invalidation evidence:** [what testing revealed — specific, not vague]
**Negative teaching point:** [what NOT to do in similar situations]
```

The invalidated decision **stays** with INVALIDATED status. Future sessions know
it was tried and found wrong. Negative knowledge is valuable — do not delete it.

**Step 2 — Reopen the decision space** to design a replacement that accounts for
the invalidation evidence.

> **note-only (core_specific, do NOT port as a flows gate):** in `llm_mvp_core`
> an invalidation is also seeded as a DPO (Direct Preference Optimization) triple
> for `IncrementalTrainer`. Common/trainable models live in `llm_mvp_core`
> (mapping R5); flows do not produce DPO triples here. Keep this step as
> reference only — record the INVALIDATED note; do not build a DPO pipeline in
> flows.

---

## Anti-Patterns

1. **Proposing without loading the governance file first** — the proposal may
   contradict a locked decision already encoded in several flows.
2. **Short-term testing skipped because the decision "seems to work"** — the
   3-use threshold exists because one use may be the happy path; run the check at
   the 4th use.
3. **Invalidated decision deleted** — negative knowledge is valuable; mark
   INVALIDATED, do not delete.
4. **Long-term testing skipped because no review is scheduled** — run the bash
   checks during any session that touches a capability using the decision.
