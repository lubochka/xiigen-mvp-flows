# Document Hierarchy

> **Stack adaptation (xiigen flows).** Universal document-routing + tier hierarchy ported from
> `llm_mvp_core/docs/skills/document-hierarchy-SKILL.md`. Adapted to this repo:
> - this is a **greenfield** docs set — you must *create* the canonical set as decisions accrue:
>   `docs/DECISIONS-LOCKED.md` (locked decisions) and an architecture/flow-map doc. The
>   `.xiigen/wiki-domain/*manifest.json` files (`domain-manifest.json`, `model-manifest.json`)
>   are the Tier source for the consumed-domain facts;
> - **Tier 0 (supreme)** = `AGENTS.md` / `CLAUDE.md` if/when present at repo root; otherwise the
>   active Luba instruction + the current bounded work order;
> - the live **test baseline** is a fresh `dotnet test` run (xUnit) per affected flow — never
>   memory, never a stale log;
> - canonical project facts are keyed off the per-flow layout
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` and
>   `flows/FLOW-NN-<slug>/client/angular/src`;
> - the domain return type is `DataProcessResult<T>` (XIIGen wrapper), not core
>   `OperationResult<T>`.

## Core Problem

Two documents with conflicting information about the same fact cause planning
errors. One doc says a flow stores tokens in memory; another says in a persisted
store. One note says capability X lives in `Application`; another says
`Infrastructure`. The hierarchy navigator prevents new documents from being
produced when an authoritative one already exists, and routes every question to
the single correct source.

---

## When to Invoke

- Before producing any new `.md` or `.json` document
- Before answering an architectural question that could reference stale data
- When two documents appear to conflict
- When deciding "where does this information live?"
- When adding a new entry to `docs/` and unsure which canonical file to update

---

## Section 1 — Three Checks Before Producing Any Document

### Check 1: Does a canonical document already exist for this concern?

```
Read: the docs/ index (DECISIONS-LOCKED.md + the architecture/flow-map doc) and
      the .xiigen/wiki-domain manifests.

If a Tier 1, 2, or 3 document already governs this concern:
  → Do NOT produce a new document
  → Instead: update the existing canonical document
  → Add a section, extend it, version it
  → Never create a competing parallel document

Only create a new document if:
  □ No existing document covers this concern (greenfield — likely at first)
  □ The document is a new plan / session log
  □ The document is a session state checkpoint
```

### Check 2: Is the information consistent with existing documents?

```
Any new document must not contradict:
  □ docs/DECISIONS-LOCKED.md (any locked decision D-01..D-NN), once it exists
  □ The architecture/flow-map doc (current flow statuses)
  □ The Tier 0 documents (AGENTS.md / CLAUDE.md, when present)
  □ The verified per-flow code layout

If a conflict is found: the existing canonical document is right.
Investigate why the new information is different before writing anything.
```

### Check 3: Is a superseded document being accidentally referenced?

```
Superseded documents (do NOT use for new planning):
  Any file with -v1, -v2, -v3 suffix where a -v4 or higher exists
  → Use the highest version
  → Do not propagate superseded content
```

---

## Section 2 — Routing Table

"Where is the answer to this question?"

```
Architectural decision rationale     → docs/DECISIONS-LOCKED.md
Current test baseline count          → dotnet test (live run per flow, not memory)
Next available decision number       → read docs/DECISIONS-LOCKED.md last D-NN + 1
Flow current status / map            → the architecture/flow-map doc (create if absent)
Flow public contract / DTO shape     → flows/FLOW-NN-<slug>/contracts + the Api project
Consumed-domain facts (model/wiki)   → .xiigen/wiki-domain/domain-manifest.json, model-manifest.json
Where a capability lives in a flow   → flows/FLOW-NN-<slug>/server/...{Application|Infrastructure}
Agent execution rules                → AGENTS.md (if present) / current work order
Core rules + result envelope         → CLAUDE.md (if present) / DataProcessResult contract
```

---

## Section 3 — Document Tier Hierarchy

When two documents conflict, the higher tier wins:

```
Tier 0 (supreme):   AGENTS.md + CLAUDE.md (when present)
                    → Override everything else. Always current.
                    → If absent: the active Luba instruction + current work order.

Tier 1 (primary):   docs/DECISIONS-LOCKED.md
                    the architecture/flow-map doc
                    → Maintained after every session.

Tier 2 (reference): per-flow contracts (flows/FLOW-NN-<slug>/contracts)
                    .xiigen/wiki-domain manifests (consumed-domain source)
                    → Updated when contracts or the consumed domain change.

Tier 3 (results):   evaluation/evidence artifacts (.xiigen/wiki-domain/eval, evidence)
                    → Updated when evals complete.

Tier 4 (session):   plans, execution logs, state checkpoints
                    → Superseded by the corresponding Tier 1 updates.
```

**Resolution protocol when documents conflict:**
```
Step 1: Identify which documents conflict on which specific value
Step 2: Apply tier hierarchy — higher tier wins
Step 3: Update the lower-tier document to match the higher-tier document
Step 4: Note the reconciliation in docs/DECISIONS-LOCKED.md if architectural
```

---

## Section 4 — What Goes in Which Document

| Information type | Goes in |
|---|---|
| Locked architectural decisions (D-01..D-NN) | docs/DECISIONS-LOCKED.md |
| Current flow status and map | architecture/flow-map doc |
| Per-flow public contracts / DTO shapes | flows/FLOW-NN-<slug>/contracts + Api project |
| Consumed-domain facts (model, wiki, question seeds) | .xiigen/wiki-domain manifests |
| Evaluation hypotheses and results | .xiigen/wiki-domain/eval + evidence |
| Rules that govern agent behavior | AGENTS.md (when present) / work order |
| Flow execution details (what was done) | session execution log files |

---

## Anti-Patterns

1. **"I'll create FLOW-01-ARCHITECTURE.md to document this flow's decisions"** —
   architecture decisions belong in `docs/DECISIONS-LOCKED.md`. Create a D-NN entry, not a competing file.

2. **"The baseline was N tests last session"** — session state is ephemeral. The
   live `dotnet test` run per flow is the baseline. Memory is not a baseline.

3. **"According to the old plan's baseline..."** — plans are Tier 4 documents.
   The live test run or the Tier 1 flow-map is authoritative, not a plan file.

4. **"Let me document this decision in the plan"** — decisions that govern future
   sessions belong in `docs/DECISIONS-LOCKED.md`. Plans are Tier 4 and will be superseded.

---

## Integration

```
- planning-session-startup-SKILL.md (governing documents) uses this tier hierarchy
- decision-capture-SKILL.md routes decisions to docs/DECISIONS-LOCKED.md
- decision-reopening-SKILL.md reads docs/DECISIONS-LOCKED.md for the next D-NN number
- session-scope-resolution-SKILL.md uses this routing table to pick minimum documents per session type
```
