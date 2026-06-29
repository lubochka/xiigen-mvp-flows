# work-scope-inventory-SKILL.md — Establish the denominator before any work begins

> **Stack adaptation (xiigen flows).** Universal *work scope inventory* ported from
> `llm_mvp_core/docs/skills/work-scope-inventory-SKILL.md` (G08). Greenfield here —
> no prior author version. The universal core kept: install the denominator before
> the first line of code — a scope tracking record (total / complete / partial /
> blocked / not-started + denominator + thisSessionCovers), a 6-count protocol, and
> a Claim Authority table (no scope record → only per-unit claims; ≥20% eligible →
> pipeline-level with a coverage check). The stack details are adapted:
> - The **natural unit of the denominator** is a `FLOW-NN-*` (total = count of
>   `flows/FLOW-NN-*/` directories, or the flow projects in `Xiigen.Flows.slnx`).
> - **Complete** = `dotnet test` over the flow's `Xiigen.Flow.<Name>.Tests`
>   (`net10.0`, xUnit) → `0 failed` + DNA evidence (and, for client-bearing flows,
>   `ng build` clean + Playwright green).
> - **Blocked** = a missing `contracts/` interface / `flow.manifest.json`
>   prerequisite (via `prerequisite-chain-SKILL.md`). The domain wrapper is
>   `DataProcessResult<T>` (XIIGen family) — **NOT** core `OperationResult<T>`.

## Core Problem

A session that improves 3 flows without knowing there are 47 has not made
pipeline-level progress. It made local progress on an unknown fraction of the
work. This skill installs the denominator before any work begins.

---

## When to Invoke

- Before any session that touches multiple flows
- Before any pipeline-level improvement claim
- Before any STOP that reports a percentage reduction in errors or failures
- At session start for any session spanning more than one flow

One run of this skill before work begins = no overclaimed pipeline-level improvement.

---

## Section 1 — The Scope Tracking Record

Before any flow is touched, produce a scope tracking record:

```json
{
  "scope": {
    "totalFlows": 47,
    "completeCount": 0,
    "partialCount": 0,
    "blockedCount": 0,
    "notStartedCount": 0,
    "thisSessionCovers": [],
    "denominator": "N of 47 flows (M blocked → N of (47-M) eligible)",
    "capturedAt": "session-start"
  }
}
```

**Rule:** A session without a scope tracking record may only claim improvement at
the individual flow level for flows it fully tested in that session.

---

## Section 2 — The Count Protocol

Before the first file is touched, establish all counts.

### Count 1 — Total flow count

```bash
ls -d flows/FLOW-* | wc -l
# → total count of flows
```

Or for a specific pipeline (e.g. the social cluster):
"This session covers the social cluster: FLOW-06, FLOW-07, FLOW-10 (3 flows)."
The full project total remains the denominator for any project-level claim.

### Count 2 — Complete count

A flow is complete when all its tests pass AND it has DNA evidence.

```bash
# Per-flow:
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --no-build 2>&1 | tail -3
# Client-bearing flow also: cd client/angular && npx ng build && npx playwright test
```

### Count 3 — Partial count

A flow is partial when started but has failing tests or missing DNA checks.

### Count 4 — Blocked count

A flow is blocked when a prerequisite (contract, dependency, or decision) is not
yet in place. Check `prerequisite-chain-SKILL.md` / `flow.manifest.json` for the chain.

### Count 5 — Not-started count

```
not_started = total - complete - partial - blocked
```

### Count 6 — The denominator

```
Denominator: [sessionFlowCount] of [total - blockedCount] eligible flows
 ([sessionFlowCount / (total - blockedCount) × 100]% of eligible)
```

---

## Section 3 — Scope Tracking Record Template

```markdown
## Work Scope Inventory (work-scope-inventory-SKILL)

Project total: [N] flows
Complete (all tests pass, DNA checked): [N] flows
Partial: [N] flows
Blocked (missing prerequisites): [N] flows — [list names]
Not started: [N] flows

This session covers: [list flows or cluster name]
Session count: [N] of [total - blocked] eligible flows
Denominator: [N / (total - blocked) × 100]% of eligible

Claim authority:
  Project-level: NOT ESTABLISHED (session covers < 20% of eligible)
  OR
  Project-level: ESTABLISHED (session covers ≥ 20% of eligible)
  Flow-level: ESTABLISHED for [list flows fully tested this session]
```

---

## Section 4 — Claim Authority Rules

| Session covers | Permitted claim |
|---|---|
| 1 flow, fully tested | "FLOW-NN improved" |
| N flows, all fully tested | "These N flows improved" |
| ≥ 20% of eligible, all fully tested | "Pipeline is improving" (with coverage-completeness-gate check) |
| < 20% of eligible | Flow-level claims only — no pipeline-level claims |
| Any session without scope tracking record | No improvement claims at any level |

**"Fully tested" means:** all tests pass for that flow + DNA compliance verified
(+ client build/Playwright green where applicable).

---

## Anti-Patterns

1. **"I'll establish scope after I see what there is"** — scope is established before work begins, not discovered during it.
2. **"I only need the denominator if I'm making a pipeline claim"** — establish it regardless; sessions that don't plan to make pipeline claims often end up making them when work goes well.
3. **"I improved 3 of 47 flows — meaningful progress"** — meaningful at the flow level; at the pipeline level 3/47 ≈ 6%, below the 20% threshold. State the denominator explicitly.
4. **"Partial progress counts as complete"** — complete means all tests pass with DNA compliance. Partial is partial.

---

## Integration

- `coverage-completeness-gate-SKILL.md` reads the denominator to verify claim scope before every STOP.
- `improvement-measurement-SKILL.md` / `mvp-round-trip-verification-SKILL.md` use the record to decide whether a behavioral delta is required.
- `output-skepticism-SKILL.md` scope question checks the same thing: "does my evidence cover my scope?" If the record is absent, the answer is "unknown".
