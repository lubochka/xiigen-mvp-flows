# Archetype Failure Map (xiigen flows, .NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield. Ported from
> `llm_mvp_core/docs/skills/archetype-failure-map-SKILL.md` (G11). The universal part
> carries over: the **score-bracket action table** (≥85% PASS; 70–84% NEAR-PASS +
> targeted patch; 60–69% PATCH; <60% STRUCTURAL_FAILURE → stop), the
> **per-capability-class failure map declared before implementation**, the
> **escalation triggers**, and the **inversion pattern** (score 0 on cycle 1 can be
> correct). Adapted here:
> - Capability classes = **per-flow Application logic** (`Xiigen.Flow.<Name>.Application`).
> - Score brackets are measured by **`dotnet test`** with an exact passed/Failed
>   count (Gate-B), and for client-bearing flows by a **Playwright** run.
> - "Fix action" = **add an iron rule** (see `iron-rule-derivation-SKILL.md`).
> - **ML archetypes are excluded.** HybridRetriever / IncrementalTrainer /
>   ContinuousLearningPipeline stay in `llm_mvp_core` (R5) and are consumed via the
>   `.xiigen/wiki-domain` manifest — they get a note-only row, never an
>   implementation phase here.

## Core Problem

A plan that says "implement the ads auction service and run tests" with no
prediction of where it will fail leaves the executor making ad-hoc decisions per
score bracket. When cycle 1 scores 0.68, should the response be a targeted patch, a
full regeneration, or an escalation? Without a per-archetype failure map, the answer
varies by whoever runs the cycle — not by the evidence. This skill produces a
failure map for each capability class before any implementation begins.

---

## When to Invoke

- Before writing any phase plan for a new flow capability class.
- When score interpretation requires a bracket → action decision.
- When a retrospective's prediction error is large — recalibrate the map.

---

## Section 1 — Score Bracket Action Table

Every capability class must declare its score-bracket actions before implementation:

| Score | Classification | Action |
|---|---|---|
| ≥ 85% | PASS | Gate B satisfied (`dotnet test` Failed 0 at the target count). Proceed to next phase. Store result. |
| 70–84% | NEAR-PASS | Add a targeted patch for the specific gap. One re-run. Escalate if still NEAR-PASS after patch. |
| 60–69% | PATCH | Identify the failing iron rule or missing behavioral invariant. Add a correction. Re-run. |
| < 60% | STRUCTURAL_FAILURE | Stop. Root cause required before cycling again. Do not retry blindly. |

**Exception — inversion patterns:** score 0 on cycle 1 is CORRECT when the
capability's iron rules require inverting the default (e.g. a moderation/safety
capability that must reject what the default accepts — FLOW-24). Verify the `[Fact]`
asserts the inversion. Proceed to cycle 2. Escalate only if cycle 2 also fails.

---

## Section 2 — Per-Capability-Class Failure Mode Templates

For each capability class built in a flow, declare its failure-map entry.

### AuctionScoring (FLOW-20, CapabilityClass)

```
Expected cycle 1 score: 70–85%
Common failure: bid weights summed incorrectly across ranking factors
Fix: add iron rule — "sum of ranking weights must equal 1.0 before applying to bids"
Escalate if: < 60% on cycle 1 (STRUCTURAL_FAILURE)
```

### IdempotentTransition (FLOW-09 / FLOW-19, CapabilityClass)

```
Expected cycle 1 score: 65–80%
Common failure: store succeeds but outbox emit is not atomic — partial state on retry
Fix: add iron rule — "state MUST be persisted before the outbox emit; retry is a no-op"
Escalate if: < 85% after cycle 2
```

### TenantScopedQuery (FLOW-15, CapabilityClass)

```
Expected cycle 1 score: 60–75%
Common failure: a query omits the scope key — cross-tenant data bleed
Fix: add iron rule — "every document query MUST include the tenant/scope key"
Escalate if: < 85% after cycle 3 (two surfaces: read path + write path)
```

### SafetyModeration (FLOW-24, CapabilityClass — inversion)

```
Expected cycle 1 score: 0% (inversion — must reject default-accepted content)
Common failure: accepts content the default pipeline would pass
Fix cycle 1: verify the [Fact] asserts rejection; this is PASS-CONDITIONAL, not failure
Escalate if: cycle 2 still fails after the inversion is asserted
```

### (note-only) ML archetypes — NOT implemented in flows

```
HybridRetriever / IncrementalTrainer / ContinuousLearningPipeline / DPO arbiter
→ stay in llm_mvp_core (R5). Consumed via .xiigen/wiki-domain manifest.
→ No score bracket, no cycle budget, no implementation phase here. Note-only.
```

---

## Section 3 — Cycle-Specific Escalation Triggers

**Always escalate:**

- STRUCTURAL_FAILURE on any cycle.
- Score decreases between consecutive cycles (regression).
- Same failure mode recurs after a targeted patch was applied.
- Expected-failure capability class still failing after its full cycle budget.

**Never escalate before budget exhausted for these:**

- Multi-surface capabilities (TenantScopedQuery read+write): do not escalate before cycle 3.
- First instance of any capability class in this flow repo: cycle 1 < 85% is expected.
- NEAR-PASS on cycle 1 for an established capability class: one targeted patch, do not escalate.

---

## Section 4 — Failure Map Table Format

Produce this for every capability class before any implementation phase:

```markdown
## Failure Map — [CapabilityClass] (FLOW-NN)

| Capability class | Archetype | Cycle 1 expected | Known failure | Fix action | Escalate trigger |
|---|---|---|---|---|---|
| AuctionScoring | SCORING | 70–85% | Weight sum ≠ 1.0 | Add iron rule: weight normalization | < 60% cycle 1 |
| IdempotentTransition | OUTBOX | 65–80% | Non-atomic emit | Add iron rule: persist before emit | < 85% cycle 2 |
| TenantScopedQuery | ISOLATION | 60–75% | Missing scope key | Add iron rule: scope on every query | < 85% cycle 3 |
```

This table belongs in the phase plan's adaptation-map section. It is not optional
even when cycle counts are low — it determines what happens per score bracket
without ad-hoc diagnosis each cycle. For client-bearing flows, add a parallel
**UI archetype** row (forms/tables/Angular 21 standalone components) whose bracket
is measured by a Playwright run instead of `dotnet test`.

---

## Anti-Patterns

1. **"Score 68% is fine, it's close to NEAR-PASS"** — 60–69% is PATCH, not NEAR-PASS.
   PATCH requires identifying a specific failing invariant before re-running.
2. **"I'll escalate as soon as cycle 1 misses 85%"** — NEAR-PASS and PATCH are not
   escalation triggers. Only STRUCTURAL_FAILURE or budget exhaustion triggers escalation.
3. **"The failure map is for simple capabilities only"** — it is most valuable for
   complex capabilities. Pre-declaring where it fails gives the executor a response
   plan, not just a diagnosis task.
4. **"Score 0 = failure"** — for inversion-pattern capabilities (FLOW-24 moderation),
   score 0 on cycle 1 verifies the inversion. Treat as PASS-CONDITIONAL.
5. **"Add a score bracket for the retriever"** — ML archetypes are not implemented in
   flows; they stay in `llm_mvp_core` and are consumed via manifest. Note-only.

---

## Integration

- `iron-rule-derivation-SKILL.md` produces the iron rules that fix PATCH-class failures.
- `audit-protocol-SKILL.md` Point 7 checks the bracket thresholds are exact
  `dotnet test` / Playwright counts.
- `test-integrity-SKILL.md` ensures the `[Fact]` behind each bracket asserts a domain
  outcome, not just `result.IsSuccess`.
- `STACK-MAP.md` records which archetypes stay in `llm_mvp_core` (the note-only rows).
