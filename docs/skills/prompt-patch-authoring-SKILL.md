# Prompt Patch Authoring (xiigen flows, .NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield. Ported from
> `llm_mvp_core/docs/skills/prompt-patch-authoring-SKILL.md` (G11). Only the **non-ML
> governance face** is ported: **Rules 0–4 and Rule 6** — the discipline for patching
> a context package when generating code/context for flow services. The universal
> part carries over: Rule 0 (one bottleneck per patch), Rule 1 (NAMED_CHECK +
> literal WRONG/CORRECT pair), Rule 2 (target zone 0.80–0.90, >0.90 = over-specified),
> Rule 3 (regression-prevention checklist against existing checks), Rule 4
> (patch-vs-rewrite thresholds), Rule 6 (patch-history record; REGRESSED is valuable
> data). Adapted here:
> - WRONG/CORRECT patterns are written on **C# (.NET 10 per-flow services)** and/or
>   **TypeScript/Angular 21** for frontend generation.
> - The CORRECT pattern for a domain method returns **`DataProcessResult<T>`** (the
>   flows wrapper), never core `OperationResult<T>` (0× in flows `src`).
> - **Rule 5 (multi-generate verification) is note-only here.** It is bound to the
>   core multi-generate topology (Pattern F) and the DPO arbiter, which stay in
>   `llm_mvp_core` (R5/R6). Flows runs no DPO loop and consumes models through the
>   `.xiigen/wiki-domain` manifest, so Rule 5 does not apply — it is recorded for
>   completeness, not executed.

## Core Problem

A context package that produces a score in the 0.65–0.89 range has a fixable gap —
but applying the wrong patch type, patching the wrong bottleneck, or introducing a
conflict with an existing named check will produce a regression in a different
capability class while appearing to fix the current one. This skill defines the
rules that govern when and how to patch a context package, what the patch must
contain, and how to prevent it from breaking what was already working.

---

## When to Invoke

- When a capability class scores in DETAIL_GAP (0.65–0.84) and a targeted patch is
  the right intervention (not a full rewrite).
- When score interpretation routes to "apply PATCH".
- When the escalation flow selects a PATCH cycle before escalating.

---

## Section 1 — The Rules

### Rule 0 — One Bottleneck Per Patch

A patch targets exactly one bottleneck per cycle. Never combine two independent gap
fixes in the same patch. If two gaps exist:

```
Identify the higher-impact bottleneck.
Patch it. Re-score.
If score still insufficient: identify the next bottleneck. Patch separately.
```

Combining two fixes makes it impossible to determine which fix resolved the gap — or
which introduced a regression. One patch = one learning signal.

### Rule 1 — NAMED CHECK + WRONG/CORRECT Pair

Every patch must contain:

```
NAMED_CHECK: CF-[N] — [descriptive name]
WRONG:   [exact pattern that fails — the literal failure mode, not a generalization]
CORRECT: [exact pattern that passes — the replacement that satisfies the iron rule]
```

Example (C# / .NET 10 flow service):

```
NAMED_CHECK: CF-12 — DataProcessResult return on a public Application method
WRONG:   public void ProcessBids(List<Bid> bids) { ... }
CORRECT: public Task<DataProcessResult<BidBatch>> ProcessBidsAsync(IReadOnlyList<Bid> bids, CancellationToken ct) { ... }
```

Example (TypeScript / Angular 21 standalone component):

```
NAMED_CHECK: CF-21 — standalone component, no NgModule
WRONG:   @NgModule({ declarations: [BidFormComponent] })
CORRECT: @Component({ selector: 'app-bid-form', standalone: true, imports: [ReactiveFormsModule], ... })
```

The WRONG pattern must match the literal failure mode observed. A generalized
description ("missing return type") is insufficient — the validator needs to match
the pattern against generated code.

### Rule 2 — Target Zone 0.80–0.90

```
< 0.80:     Patch is attempting too much — narrow the scope further
  0.80–0.90: Target zone — single focused fix
> 0.90:     Over-specified — the patch adds detail the iron rule doesn't require
```

Over-specification at > 0.90 is a signal to loosen, not to celebrate. A patch that
constrains generated code beyond what the iron rule requires will block valid
implementations that satisfy the rule through different but correct means.

### Rule 3 — Regression Prevention Checklist

Before submitting any patch:

```
□ Load all existing NAMED CHECKs for this capability class
□ Verify: does the new WRONG pattern conflict with any existing CORRECT pattern?
□ Verify: does the new CORRECT pattern match any existing WRONG pattern?
□ If either conflict exists: the bottleneck is not a patch target — escalate to a
  contract revision instead of a patch
```

A patch that passes score for the current capability class while conflicting with an
existing check is worse than no patch — it introduces a silent failure in another
capability class.

### Rule 4 — Patch vs Rewrite Thresholds

```
Score 0.65–0.84 (DETAIL_GAP):        Targeted patch (this skill)
Score 0.50–0.64 (PATTERN_MISSING):   Section rewrite (all checks in one sub-section)
Score < 0.50 (STRUCTURAL):           Full rewrite (reconstruct from iron rules)
```

A DETAIL_GAP means the model understands the pattern — it missed a specific detail.
A PATTERN_MISSING means the wrong mental model of one sub-section. A STRUCTURAL score
means the context package itself needs rebuilding. Never apply a targeted patch to a
STRUCTURAL-class failure — it appears to help locally while the structural problem
compounds.

### Rule 5 — Multi-Generate Verification (NOTE-ONLY in flows)

In core, when the target topology uses multi-generate (Pattern F), both generators
must avoid the WRONG pattern and a DPO arbiter judges agreement. **Flows does not run
a DPO loop** and consumes models through the `.xiigen/wiki-domain` manifest (R5/R6),
so there is no second generator to verify and no arbiter to satisfy. Record
`MultiGenerateVerified: N/A (no DPO topology in flows — model consumed via manifest)`
and move on. This rule is documented for parity, not executed here.

### Rule 6 — Patch History Record

Every applied patch is recorded:

```csharp
public record PatchHistoryEntry
{
    public string CapabilityClass { get; init; }
    public string FlowId          { get; init; }   // e.g. "FLOW-20-ads-platform"
    public string NamedCheck      { get; init; }   // CF-N identifier
    public string WrongPattern    { get; init; }
    public string CorrectPattern  { get; init; }
    public double ScoreBefore     { get; init; }
    public double ScoreAfter      { get; init; }
    public string PatchOutcome    { get; init; }   // RESOLVED / PARTIAL / REGRESSED
}
```

A REGRESSED outcome is high-value data. Do not delete it — it is evidence that the
named check over-fires or the contract is wrong.

---

## Section 2 — Patch Format Template

```
CONTEXT_PATCH:
  CapabilityClass: [capability class name]
  FlowId:          [FLOW-NN-<name>]
  IronRuleTarget:  CF-[N]
  ScoreBefore:     [current score]
  TargetScore:     0.85 (PASS minimum)

  NAMED_CHECK: CF-[N] — [check name]
  WRONG:   [exact failure pattern — literal C# or TypeScript]
  CORRECT: [exact passing pattern — literal C# or TypeScript]

  RegressionCheck:
    - Conflicts with existing WRONG patterns: NONE / [list]
    - Conflicts with existing CORRECT patterns: NONE / [list]

  MultiGenerateVerified: N/A (no DPO topology in flows)
```

---

## Anti-Patterns

1. **"Patching two bottlenecks in one cycle"** — produces two learning signals at
   once, making attribution impossible. One bottleneck per patch.
2. **"WRONG pattern is a generalization"** — "missing DataProcessResult" is not
   matchable. Write the actual code that fails, not a description of why.
3. **"Target score set at 1.00"** — 1.00 means only one valid implementation. 0.85–0.90
   is the target; variation above that produces valid code through different routes.
4. **"Skipping Rule 3 regression check"** — a patch that conflicts with an existing
   check creates a silent failure in a different capability class. Not optional.
5. **"Applying a patch to a STRUCTURAL failure"** — a package scoring < 0.50 needs
   reconstruction, not a targeted addition.
6. **"Running a multi-generate / DPO verification in flows"** — there is no DPO loop
   here; Rule 5 is note-only. Models are consumed via manifest (see STACK-MAP.md).

---

## Integration

- `archetype-failure-map-SKILL.md` routes a DETAIL_GAP bracket to this skill.
- `iron-rule-derivation-SKILL.md` supplies the iron rule (`CF-[N]`) the patch targets.
- `audit-protocol-SKILL.md` Point 3 checks the CORRECT pattern returns
  `DataProcessResult<T>`, not core `OperationResult<T>`.
- `STACK-MAP.md` records that the DPO topology / arbiter stay in `llm_mvp_core`
  (why Rule 5 is note-only).
