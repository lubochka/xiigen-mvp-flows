# Multi-Reviewer Design

> **Stack adaptation (xiigen flows).** Universal multi-perspective review framework ported from
> `llm_mvp_core/docs/skills/multi-reviewer-design-SKILL.md`. Adapted to this repo:
> - **Evidence** = `file:line` over `.cs` for each per-flow layer
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}` (+ `Tests`)
>   and over `.ts` for `client/angular/src`; plus `dotnet build Xiigen.Flows.slnx` /
>   `dotnet test` (xUnit) per `Xiigen.Flow.<Name>.Tests`; for UI branches, Angular 21 Playwright;
> - **"public interface"** = a per-flow ASP.NET Core (Web API) endpoint/DTO contract PLUS an
>   Angular 21 standalone component's public inputs;
> - the **Security reviewer** checks this repo's invariants: Fabric First (no `new`
>   ConcreteStore/Provider), tenant-scope isolation, No-Secrets-in-repo, and the
>   `DataProcessResult<T>` envelope (XIIGen wrapper, NOT core `OperationResult<T>`); the
>   **Principles reviewer** checks `docs/DECISIONS-LOCKED.md` and the per-flow layer dependency
>   direction; the **Domain reviewer** uses flow invariants from
>   `.xiigen/wiki-domain/domain-manifest.json` (evidence_only — there is no own plan source yet);
> - an accepted BLOCK/CONCERN becomes a binding row in the local Authority Requirements Ledger
>   (`source_authority=reviewer_gate`) — see `authority-requirement-binding-SKILL.md`;
> - **NB (R5/G12):** the core "10 Review Cycle Gate Compatibility" (Claude/Cursor/Codex
>   three-extensions, `requested_model=gpt-5.5`/`xhigh`, Sub-Agent Visibility Ledger) and the
>   ML-eval/arbiter trainable hook are **note-only** in G12 and are NOT ported here.

## Core Problem

A design review from a single perspective misses the failure modes visible from
other domains. A security reviewer who doesn't see the domain constraints and a
domain reviewer who doesn't see security requirements each miss legitimate gaps.
This skill defines a structured multi-perspective review where each reviewer
receives only their domain's context and produces independent verdicts.

Accepted reviewer gates are active task authority for downstream repair. Once a
BLOCK or accepted CONCERN is adopted by the governance loop, it must be entered
into the Authority Requirements Ledger with `source_authority=reviewer_gate`. A
downstream architect or executor must not treat it as a note, preference, or
optional wishlist item.

---

## When to Invoke

- Before any new interface or architecture decision is finalized
- When a design proposal touches multiple domains (security + business + principles)
- When a plan has more than 3 phases and needs multi-stakeholder validation
- When Luba asks for a thorough review before a major implementation starts

---

## Section 1 — The 5 Reviewer Roles

Select reviewers based on the capability's domain and risk level.

| Role | Context package (what they see) | What they check |
|---|---|---|
| **Domain Reviewer** | Capability spec, flow invariants (`domain-manifest.json`), flow context | Intent correctness, flow invariant completeness |
| **Security Reviewer** | Capability spec, Fabric First, tenant-scope, No-Secrets, failure modes | Failure-mode coverage, Fabric-First compliance, tenant isolation, no secrets |
| **Business Reviewer** | Capability spec, downstream flow contracts, event/outbox schemas | Cross-flow consistency, consumer impact |
| **Principles Reviewer** | Capability spec, `docs/DECISIONS-LOCKED.md`, layer dependency rules, `DataProcessResult<T>` | Decision adherence, envelope + layer-direction compliance |
| **Completeness Reviewer** | Capability spec, contract/DTO fields | All outputs reachable from inputs, no missing paths |

**Mandatory by risk level:**
| Capability type | Minimum reviewer set |
|---|---|
| Internal utility (single flow layer) | Domain + Principles |
| Public interface (Web API endpoint/DTO, Angular inputs) | Domain + Security + Principles |
| Cross-flow contract | Domain + Security + Business + Principles |
| Architecture decision | All 5 reviewers |

**Never add reviewers for appearance.** A security reviewer on a pure-compute helper that touches no storage and no tenant data adds noise without signal.

---

## Section 2 — Context Isolation Rule

Each reviewer receives ONLY their defined context package.

- Domain Reviewer: does NOT see security patterns or principles text
- Security Reviewer: does NOT see flow invariants or domain context
- Principles Reviewer: does NOT see domain context or security patterns — only the capability spec and DECISIONS/layer rules

**Context isolation ensures independent verdicts.** A reviewer who has seen another reviewer's verdict will anchor to it rather than finding independent issues.

---

## Section 3 — Verdict Format

Each reviewer produces a structured verdict:

```markdown
## [Role] Reviewer Verdict

Verdict: APPROVED | CONCERN | BLOCK

BLOCK items (plan cannot proceed without resolving these):
- [specific issue] — [which rule or principle is violated] — [what must change]

CONCERN items (plan may proceed but author must respond in writing):
- [issue] — [what this risks if unaddressed]

Evidence:
- [exact file:line over .cs/.ts, grep result, or dotnet test / Playwright output]
```

**BLOCK class:** Business-logic violations, Security violations (Fabric-First bypass, cross-tenant access, a secret in the repo), Principles violations (`DataProcessResult<T>` bypass, reversed layer dependency, contradicting a locked decision), flow-invariant violations.

**A single BLOCK overrides all APPROVED verdicts.** BLOCK cannot be diluted by averaging.

---

## Section 4 — The Convergence Protocol

After all reviewers produce verdicts:

### Round 1: Collect Verdicts

All reviewers produce verdicts independently. Record:
```
Domain Reviewer:      [APPROVED | CONCERN | BLOCK]
Security Reviewer:    [APPROVED | CONCERN | BLOCK]
Business Reviewer:    [APPROVED | CONCERN | BLOCK]
Principles Reviewer:  [APPROVED | CONCERN | BLOCK]
Completeness Reviewer:[APPROVED | CONCERN | BLOCK]
```

### Decision Rules (applied in order — stop at first match)

**Rule 1: Any BLOCK → Reject**
If any reviewer returns BLOCK:
- The blocked item is identified and must be resolved before proceeding
- Author addresses the BLOCK specifically; generic rewrites are not sufficient
- The BLOCK becomes a binding Authority Requirements Ledger row for repair
- After addressing: only the blocking reviewer re-evaluates (not a full restart)

**Rule 2: All APPROVED (0 BLOCKs) with 0–2 CONCERNs → ACCEPTED**
Plan may proceed. Author responds to CONCERNs in writing before Phase 1.

**Rule 3: All APPROVED with 3+ CONCERNs → Targeted cycle**
Author addresses specific CONCERNs. Affected reviewers re-evaluate targeted sections only.

**Rule 4: Same BLOCK persists after 2 address cycles → Escalate to Luba**
If the author has addressed a BLOCK twice and the reviewer still finds it unresolved:
- This is a genuine architectural disagreement
- Escalate to Luba with full context: original BLOCK + both address attempts + reviewer response

**Rule 5: All outputs blocked (no path forward) → Redesign required**
If all reviewer paths produce BLOCKs that cannot be reconciled:
- Plan is not viable as stated
- Produce an ESCALATION report: what specifically blocks each path
- Redesign before a new review cycle

### Maximum 3 cycles
After 3 cycles without full approval: escalate. Three cycles of BLOCK without resolution = the problem is architectural, not editorial.

> **NB (R5/G12) — not ported:** the core "10 Review Cycle Gate Compatibility" block
> (Claude/Cursor/Codex three-extensions, `requested_model=gpt-5.5`/`xhigh`, runtime-metadata
> handling, Sub-Agent Visibility Ledger) and the ML-eval/arbiter trainable-gate hook are
> note-only in G12 and are intentionally NOT part of this flows skill.

---

## Section 5 — Review Output Format

```markdown
## Multi-Reviewer Design Review (multi-reviewer-design-SKILL)

Design: [capability or interface name + target FLOW-NN / layer]
Date: [date]
Reviewers used: [list roles]

Round [N] results:
| Reviewer | Verdict | BLOCK count | CONCERN count |
|---|---|---|---|
| Domain | APPROVED | 0 | 1 |
| Security | CONCERN | 0 | 2 |
| Principles | APPROVED | 0 | 0 |

Overall verdict: APPROVED_WITH_CONCERNS
Concerns to address: [list]
Next step: author responds to concerns → plan proceeds to Phase 1
```

---

## Section 6 — Escalation Report Format

When escalation to Luba is required:

```markdown
## Design Review Escalation

Capability: [name + FLOW-NN / layer]
Cycle count: [N]
Still blocked by: [role] — [exact BLOCK text]

Address attempts:
  Attempt 1: [what was changed]
  Reviewer response: [still blocked — specific remaining issue]
  Attempt 2: [what was changed]
  Reviewer response: [still blocked — specific remaining issue]

Root cause assessment: [architectural disagreement / specification ambiguity / missing decision]

Question for Luba: [one specific decision needed to unblock]
```

---

## Anti-Patterns

1. **"One thorough review is enough"** — a single reviewer's blind spots are invisible to them. Multiple independent perspectives are the point.

2. **"Show all reviewers each other's verdicts before they finish"** — anchoring to prior verdicts eliminates the independence that makes multi-reviewer review valuable.

3. **"Average the BLOCKs against the APPROVEDs"** — a BLOCK from one reviewer means the design has a genuine problem. Averaging hides it.

4. **"Skip the CONCERNs because the plan is proceeding"** — CONCERNs not addressed in writing become implicit technical debt. The author responds; the concern is not dropped.

---

## Integration

- `plan-review-SKILL.md` is the detailed gate list; this skill provides the multi-perspective structure
- `architect-behavior-classifier-SKILL.md` FAIL-5 (claims without evidence) applies to every reviewer verdict
- `decision-reopening-SKILL.md` handles BLOCK items that conflict with locked decisions
- `authority-requirement-binding-SKILL.md` ingests each accepted BLOCK/CONCERN as a binding `source_authority=reviewer_gate` row
