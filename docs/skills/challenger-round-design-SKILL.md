# challenger-round-design-SKILL.md — Role panel, context isolation, genuine-disagreement, termination

> **Stack adaptation (xiigen flows).** Universal *challenger round design* ported
> from `llm_mvp_core/docs/skills/challenger-round-design-SKILL.md` (G08). Greenfield
> here — no prior author version. **Only the universal part is ported:** the
> challenger-role panel by capability archetype, per-role context isolation, the
> genuine-disagreement gate, and the three termination conditions (CONSENSUS /
> DEFERRED / STALLED). Run this before Gate B of any flow phase that uses
> multi-model design review. The stack details are adapted:
> - The unit reviewed is a `FLOW-NN-*` capability design.
> - The **DNA / rules challenger** checks fabric-first + `DataProcessResult<T>`
>   boundaries (XIIGen family wrapper — **NOT** core `OperationResult<T>`) + guard
>   clauses, per `docs/skills/DNA-DOTNET-TRANSLATION.md` — not core Rules 1–10
>   verbatim.
> - **Context packages** are: the contract in `contracts/` (Domain), the
>   `server/Application` signatures (Completeness), cross-flow dependencies from
>   `flow.manifest.json` (Business), and tenant/session data (Security — add only
>   when it actually exists). A **UI challenger** applies for Angular 21 standalone
>   surfaces (Playwright).
> - A CHALLENGE is genuine if it changes generated `.cs` / Angular code. Challenger
>   verify commands = `dotnet build` / `dotnet test --filter` (`net10.0`) +
>   Playwright. STALLED ⇒ escalate to Luba.
>
> **G12 / R5 boundary (NOT ported, stays in `llm_mvp_core`):** the DNA Compliance
> Arbiter hard-bound to core "Rules 1–10" numbering and `OperationResult<T>`
> wording; and the ML/arbiter-coupled tail — "convergence training signals feed
> into DomainModelRegistry via ContinuousLearningPipeline" and the couplings to
> `multi-reviewer-design` / `arbiter-panel-design` / `diagnostic-escalation` /
> `context-package-design`. Those are model internals; flows consumes the model
> through `.xiigen` manifests/locators only.

## Core Problem

A multi-model convergence session without defined challenger roles produces
inconsistent challenges — one reviewer checks intent, another checks security,
both check both, neither checks completeness. The challenge history is noise; the
final design carries unresolved ambiguity. This skill defines how to compose
challenger roles, what each receives (context isolation), and when to declare
consensus vs escalate.

---

## When to Invoke

- Before writing any multi-reviewer convergence session for a flow capability design
- When a phase plan review needs challenger prompt templates
- When a convergence session is STALLED and the escalation type needs diagnosis
- Before Gate B of any flow phase that uses multi-model design review

---

## Section 1 — Challenger Role Selection

### Minimum challenger set by capability archetype

| Archetype | Minimum challengers |
|---|---|
| ROUTING | Domain + DNA/Rules |
| DATA_PIPELINE | Domain + Security + DNA/Rules |
| VALIDATION | Domain + Completeness + DNA/Rules |
| SCORING | Domain + DNA/Rules + Guard-Clause |
| TRAINING (consumes model only) | Domain + Security + Business + Completeness + DNA/Rules |
| ORCHESTRATION | Domain + Security + Business + Completeness + DNA/Rules |

### Add optional challengers when

- Capability emits cross-flow events / crosses flow boundaries → add **Business**
- Capability involves session/tenant data or user context → add **Security**
- Capability ships an Angular surface → add **UI** (Playwright-backed)

### Never add challengers for noise

Do not add a Security challenger to a read-only query capability that touches no
session/tenant data. Do not add a Business challenger to a single-flow capability
with no cross-flow contracts. Noise challengers inflate rounds without adding signal.

---

## Section 2 — Challenger Prompt Templates

Every challenger receives a prompt defining its role, context package, and response
format. **Context isolation is mandatory:** each challenger receives ONLY its
defined subset.

### Domain Challenger

```
ROLE:   Domain Challenger in a flow-capability design convergence session.
EXPERTISE: Intent correctness and invariant completeness for this capability class.

RECEIVES:
- The proposed capability design
- The capability's invariants / guard clauses
- The domain context (what this capability does for the flow; flow.manifest.json intent)

DOES NOT RECEIVE: security patterns, DNA text, other challenger verdicts.

For each challenge, provide:
1. Which field in the design is incorrect or incomplete
2. Why (trace to a specific domain invariant)
3. What the correct representation should be

RESPONSE FORMAT:
{
  "verdict": "APPROVED" | "CHALLENGE" | "CONTEXT_INSUFFICIENT",
  "challenges": [ { "field": "intent.failureModes", "reason": "...", "correction": "..." } ],
  "contextRequests": [ { "type": "DOWNSTREAM_CONTRACT | CONTRACT_SCHEMA | ...", "target": "...", "question": "..." } ]
}

A challenge is GENUINE if removing it would change what code gets generated.
A challenge is NOISE if the correction would produce functionally identical code.
Raise only genuine challenges.
```

### Security Challenger

```
ROLE:   Security Challenger.
EXPERTISE: fabric-first compliance, tenant/correlation isolation, failure-mode coverage.

RECEIVES:
- The proposed capability design
- The DataProcessResult<T> + fabric-first rules from DNA-DOTNET-TRANSLATION.md
- The capability's declared failure modes + tenant scoping (ITenantContextAccessor)

DOES NOT RECEIVE: domain context, other challenger verdicts.

Check:
- Do failure modes cover all DataProcessResult<T> error paths (no throw-for-business-logic)?
- Are there missing failure modes around tenant isolation / contract violations?
- Do invariants enforce the critical boundary properties (tenant scope, no cross-tenant leak)?

RESPONSE FORMAT: same as Domain Challenger
```

### Business Challenger

```
ROLE:   Business Challenger.
EXPERTISE: Cross-flow consistency — contracts between this capability and other flows.

RECEIVES:
- The proposed capability design
- The events/fields this capability emits and consumes
- Upstream flow contracts (from flow.manifest.json) earlier in the same pipeline

DOES NOT RECEIVE: DNA text, security patterns.

Looking for:
- Field values a prior flow may transform before reaching this one
- Cross-flow schema fields downstream consumers may interpret differently
- A dependency on an assumption about an upstream flow

RESPONSE FORMAT: same as Domain Challenger
```

### DNA / Rules Challenger (context-isolated — mandatory)

```
ROLE:   DNA / Rules Arbiter.
EXPERTISE: Alignment with docs/skills/DNA-DOTNET-TRANSLATION.md (DataProcessResult<T>,
           fabric-first, comments, MicroserviceBase<TSelf>, no core OperationResult<T> leakage).

RECEIVES (ONLY these — isolation is the enforcement mechanism):
- Full text of the flows DNA rules with concrete violation examples
- The proposed capability design

DOES NOT RECEIVE: domain context, security patterns, other challenger verdicts.

Check: does the design, as written, violate any flows DNA rule?

BLOCK threshold: ANY DNA violation is automatic BLOCK. A CHALLENGE from this role means
the design cannot be accepted until resolved.

RESPONSE FORMAT: same as Domain Challenger
```

### Completeness Challenger

```
ROLE:   Completeness Challenger.
EXPERTISE: Verifying nothing essential is missing.

RECEIVES:
- The proposed capability design
- The capability's structure fields (inputShape, outputShape, emits)

Check:
- Are all declared outputs reachable from the declared inputs?
- Are all declared emits traceable to a condition in invariants or failure modes?
- Is the purpose specific enough that a second implementor could build the same thing?

RESPONSE FORMAT: same as Domain Challenger
```

### UI Challenger (client-bearing flows only)

```
ROLE:   UI Challenger for an Angular 21 standalone surface.
EXPERTISE: Component/route contract, accessible names/labels, error clarity, and Playwright-verifiable behavior.

RECEIVES:
- The proposed UI design + the standalone component/route contract
- The server endpoint contract it calls (contracts/schemas)

DOES NOT RECEIVE: DNA text, other challenger verdicts.

Check:
- Does the component consume exactly the declared response schema (no rename drift)?
- Are user-facing labels/help present and free of internal jargon on the primary path?
- Is there a Playwright spec that asserts the user-visible outcome?

RESPONSE FORMAT: same as Domain Challenger
```

---

## Section 3 — Termination Conditions

### CONSENSUS_REACHED (happy path)

All challengers return APPROVED in the same round.

```
□ Every challenger returned APPROVED (not just a majority)
□ Final design validated against the last round's challenges
□ No pending CONTEXT_INSUFFICIENT requests
□ convergenceHistory.consensusReachedAt is set
```

### DEFERRED_CONSTRAINT (acceptable provisional)

A CONTEXT_INSUFFICIENT request could not be resolved (needs human judgment or
external info not available in the session). Mark the design provisional:

```markdown
## DEFERRED_CONSTRAINT
Request type: DOWNSTREAM_CONTRACT
Target: FLOW-02 enrichment eligibility — time-zone semantics of DeliveredAtUtc
Question: Is DeliveredAtUtc UTC or tenant-local?
Deferred at: [date]
Unblocks: the invariant "compare against UTC now" cannot be finalized
```

Implementation proceeds with the provisional design. Generated code is marked with
the DEFERRED_CONSTRAINT and may need revision when resolved.

### STALLED (escalation required)

Max rounds exceeded without consensus. Escalate to a planning session / Luba.
Stalling usually means:
- Genuine architectural ambiguity → escalate to Luba
- Circular challenges (challenger scope too broad) → narrow the challenger's context package
- Missing upstream design (convergence order violated) → check the `flow.manifest.json` dependency graph

---

## Section 4 — Genuine Disagreement Requirement

A convergence session produces learning signal only when challengers genuinely
disagree. The session can produce syntactically valid designs that teach nothing.

**Genuine disagreement requires:**
```
1. At least one CHALLENGE verdict from a challenger
   (all APPROVED on round 1 = either the design is trivial or challengers didn't try)
2. The CHALLENGE traces to a specific field and specific invariant
   (vague challenges like "needs more detail" = noise)
3. The correction, if applied, changes what code gets generated
   (cosmetic challenges = noise)
```

**Pre-convergence gate — run before starting rounds:**
```
□ Design has at least one flow-specific constraint
  (generic designs give challengers nothing to challenge)
□ At least one legitimate implementation path exists that a challenger might object to
  (if there is only one correct approach, disagreement is impossible)
□ Purpose is specific enough to distinguish from similar capabilities
```

---

## Anti-Patterns

1. **"Challenger prompt without context isolation"** — a DNA challenger receiving domain context is contamination. Each challenger receives ONLY its defined package.
2. **"Adding challengers to appear thorough"** — a Security challenger on a read-only query adds noise. Minimum panel by archetype; add only when justified.
3. **"APPROVED after round 1 with no challenges"** — either the design is trivial or the challengers didn't try. Zero challenges in round 1 warrants re-running with more specific criteria.
4. **"STALLED treated as equivalent to CONSENSUS"** — STALLED means genuine disagreement exists. Escalate it. A STALLED design used for implementation carries unresolved ambiguity into the code.

---

## Integration

- `plan-review-SKILL.md` (Gate B AI cross-review + per-element arbiters) is where these challenger roles run for flow phase plan reviews — extend it, do not duplicate its FC classes.
- `context-package-design-SKILL.md` governs the structure of each challenger's context package.
- `diagnostic-escalation-SKILL.md` governs escalation when STALLED is reached.
- Model-internal convergence-training coupling stays in `llm_mvp_core` (G12 / R5); flows consumes the model via `.xiigen` manifests only.
