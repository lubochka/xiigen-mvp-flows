# investigation-handoff-SKILL.md — Persist discoveries continuously, write a handoff at session end

> **Stack adaptation (xiigen flows).** The handoff format (CONTEXT, DISCOVERIES
> with fact / verified_by / implication / action, REJECTED CLAIMS, OPEN QUESTIONS
> requiring Luba's decision, NUMBERS ESTABLISHED, WHAT COMES NEXT) and the rule
> "write discoveries to the state file DURING the session, not only at the end"
> are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/investigation-handoff-SKILL.md` (G06 debugging /
> root-cause). Adapted here:
> - Handoff file lives in `docs/investigations/` in this repo:
>   `docs/investigations/INVESTIGATION-HANDOFF-<topic>-<YYYY-MM-DD>.md`.
> - `verified_by` is a `dotnet test --filter "FullyQualifiedName~<Test>"` command
>   or a `file:line` read over `flows/**/*.cs` (or `**/*.ts` for the Angular
>   client) — NOT a core `OperationResult<T>`-flavoured command.
> - The NUMBERS table holds xUnit per-flow test counts and flow counts.
> - The continuous `discoveries[]` log binds to the flow's own STATE artifact /
>   `.xiigen` records (the flows analogue of `STATE-Pn.json`).
> - Result type is `DataProcessResult<T>` from `Xiigen.Kernel`.

## When to Invoke

At the end of any session that:
- Analyzed the codebase to understand current state (e.g. a multi-flow audit)
- Compared two approaches or designs
- Debugged a complex issue across multiple flows / kernel fabrics
- Discovered facts not in the flow's STATE / docs

Write to `docs/investigations/` AND update the flow's STATE `discoveries[]` field.

**Write discoveries to STATE during the session — not just at the end.** A session
that resets before writing the handoff loses everything discovered in the last
hour.

---

## Required Output Format

File: `docs/investigations/INVESTIGATION-HANDOFF-[topic]-[YYYY-MM-DD].md`

```markdown
# INVESTIGATION-HANDOFF — [topic] — [YYYY-MM-DD]
## Session type: [CODE | PLANNING | ANALYSIS | DEBUG]
## For: [next session / Luba]

## CONTEXT
[2-3 sentences: what problem was investigated, what approach, what starting state]

---

## DISCOVERIES

DISCOVERY-1:
  fact:        [precise factual statement — no hedging, no "probably"]
  verified_by: [exact command or file:line that confirmed it]
  implication: [what this means for any session that touches this flow/fabric]
  action:      RESOLVED: [what was done]
             | DEFERRED: [why and what triggers resolution]
             | OPEN: [what needs to happen next]

DISCOVERY-2: ...

---

## REJECTED CLAIMS

REJECTED-1:
  claim:       [exact wrong statement that was assumed]
  source:      [where the assumption came from]
  truth:       [what is actually true]
  verified_by: [command or file:line that disproved it]
  risk:        [what would have gone wrong if accepted]

---

## OPEN QUESTIONS REQUIRING LUBA'S DECISION

OPEN-1:
  question:  [precise question requiring Luba's decision]
  context:   [why this cannot be decided automatically]
  options:   [2-3 concrete options with tradeoffs]
  blocked:   [what cannot proceed until this is answered]

---

## NUMBERS ESTABLISHED THIS SESSION

| Artifact | Verified Value | Verified By |
|----------|---------------|-------------|
| FLOW-16 tests at session start | 24 | dotnet test flows/FLOW-16-marketplace-payments/server/*.Tests \| tail -3 |
| FLOW-16 tests at session end | 27 | dotnet test flows/FLOW-16-marketplace-payments/server/*.Tests \| tail -3 |
| Total flow count | 48 | ls flows \| grep -c "FLOW-" |

---

## WHAT COMES NEXT

1. [task] — [CODE SESSION | PLANNING SESSION | ANALYSIS]
2. ...
```

---

## discoveries[] in the flow STATE

Write discoveries to the STATE artifact continuously during investigation:

```json
{
  "flow": "FLOW-16",
  "status": "IN_PROGRESS",
  "discoveries": [
    {
      "foundAt": "step-3",
      "fact": "CheckoutAsync omits platformFeeBps from the document body when IFreedomConfigService returns null",
      "verifiedBy": "grep -n \"platformFeeBps\" flows/FLOW-16-marketplace-payments/server/*.Application/MarketplacePaymentsService.cs",
      "implication": "BuildDashboardAsync throws KeyNotFoundException on legacy documents — write a resolved default at the source",
      "actionTaken": "OPEN — fix in CheckoutAsync write path"
    }
  ],
  "rejectedClaims": [
    {
      "claim": "BuildDashboardAsync handles a missing fee gracefully",
      "source": "assumption from reading the read path",
      "truth": "It indexes body[\"platformFeeBps\"] directly with no guard",
      "verifiedBy": "Read MarketplacePaymentsService.cs:212 — no ContainsKey guard",
      "risk": "Would have relied on graceful degradation that does not exist"
    }
  ]
}
```

---

## Real Example

```
DISCOVERY-1:
  fact:        Every FLOW-NN Application method returns DataProcessResult<T> — confirmed across 48 flows
  verified_by: per-entity-examination audit; grep -rn "public.*DataProcessResult" flows/**/*.Application
               INCONSISTENT count: 0
  implication: The flows DNA Rule 1 holds repo-wide — no void/raw-bool business methods
  action:      RESOLVED — recorded in the audit handoff

REJECTED-1:
  claim:       "All flows register fabrics by interface in Program.cs"
  source:      assumption from the dev-safety skill
  truth:       Some flows register via a *ServiceCollectionExtensions.cs in the Infrastructure project, not Program.cs
  verified_by: grep -rn "AddSingleton\|AddScoped" flows/**/*.Infrastructure → registrations found there
  risk:        Would have grepped only Program.cs and falsely reported missing registrations
```

---

## Anti-Patterns

1. **Writing the handoff only at session end** — a context reset destroys discoveries made hours earlier
2. **"See above" as verified_by** — not a command; not reproducible; not useful
3. **Discoveries without implications** — "FLOW-16 test count is 27" with no implication = incomplete
4. **Omitting rejected claims** — wrong assumptions resurface in future sessions if not recorded
5. **Vague facts** — "FLOW-16 might have issues with missing keys" is not a discovery; "BuildDashboardAsync throws on a missing platformFeeBps key" is

---

## Integration

- `per-entity-examination-SKILL.md` — the Freeze-Gate HANDOFF uses this format
- `debug-session-SKILL.md` — the active-session.md feeds discoveries into the handoff
- `root-cause-ladder-SKILL.md` — OPEN questions become Level 2/3 roots
- The flow STATE / `.xiigen` records are the continuous discoveries store
