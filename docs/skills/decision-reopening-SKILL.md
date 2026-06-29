# Decision Reopening Protocol

> **Stack adaptation (xiigen flows).** Universal 4-step ADR reopening gate ported from
> `llm_mvp_core/docs/skills/decision-reopening-SKILL.md`. Adapted to this repo:
> - the locked-decision register is **`docs/DECISIONS-LOCKED.md`** (greenfield — this repo
>   has no such file yet; create it the first time a decision is locked, see
>   `decision-capture-SKILL.md`). Until it exists, there are no locked decisions to reopen, so
>   most "reopening" requests are just normal free design choices;
> - **Step 4** scope-impact grep runs over `.cs` (per-flow `server/` projects), `.ts`
>   (per-flow `client/angular/src`), `.md` (docs), and `.json` (contracts/manifests);
> - the final verification run is **`dotnet build` + `dotnet test`** (xUnit) on the affected
>   `Xiigen.Flow.<Name>.Tests` project(s), plus Angular 21 Playwright when a client branch is
>   touched — NOT core `dotnet test LlmFromScratch.sln`;
> - the domain return type stays `DataProcessResult<T>` (XIIGen wrapper), never core
>   `OperationResult<T>`.

## Purpose

Locked decisions exist to prevent a recurring failure mode: every session
re-derives the same conclusions from first principles, wastes time, and sometimes
arrives at different answers due to context differences. But architecture must
evolve. This skill defines the narrow gate through which a decision legitimately
changes.

---

## When to Invoke

Any time a session or response challenges a locked decision:
- "I think we should reconsider D-NN"
- "This decision doesn't work for the new context"
- "The flow contract needs to change because..."
- Any suggestion that contradicts a D-series entry in `docs/DECISIONS-LOCKED.md`

---

## Four-Step Test

A decision may only be reopened if it passes all four steps.

### Step 1 — Is this decision in `docs/DECISIONS-LOCKED.md`?

```bash
grep -n "D-[0-9][0-9]" docs/DECISIONS-LOCKED.md | head -20
```

If the file does not exist or the decision is NOT in it: not a locked decision —
proceed freely (and capture the new decision per `decision-capture-SKILL.md`).
If IN the document: locked. Apply steps 2–4.

---

### Step 2 — What is the trigger for reopening?

**Valid triggers (any one is sufficient):**
```
□ New information that did not exist when the decision was made
  Example: "D-03 assumed the registration flow stored verification tokens in
   memory, but the new tenant-isolation requirement needs a persisted, scoped
   store — the schema must change"

□ The decision causes a concrete failure in the current plan
  Example: "D-05 says every flow shares one IDocumentStore registration, but
   FLOW-15 multi-tenancy needs a per-tenant store and there is no way to inject it"

□ The decision's rationale has been invalidated by subsequent decisions
  Example: "D-06 chose an in-memory IOutboxStore as default, but D-11 added a
   durable-saga requirement — in-memory is now the wrong default"
```

**Invalid triggers (do not reopen):**
```
✗ "I just think there's a better way"
✗ "This would be simpler if we changed it"
✗ "The other approach is more familiar"
✗ Re-examining without new information
```

---

### Step 3 — Write the ADR entry BEFORE changing anything

Append to the Decision Change Log section of `docs/DECISIONS-LOCKED.md`:

```markdown
## Decision Change Log

### D-NN Amendment — [today's date]
**Changed:** [what is changing]
**Rationale:** [the valid trigger from step 2 — exact new information or failure]
**Impact:** [which flows / layers / contracts / Angular clients need updating]
**Previous decision text:** [quote the original]
**New decision text:** [the replacement]
```

The ADR entry must exist as a written record **BEFORE** any file is edited
or any implementation is changed.

---

### Step 4 — Scope the impact and update all affected documents

After writing the ADR entry:
1. List every document/flow that references this decision
2. Update each one — no partial updates
3. Run `dotnet build` and `dotnet test` (and Angular 21 Playwright for client branches)
   after all updates

```bash
# Find all references to the decision number across server, client, docs, contracts
grep -rn "D-NN\|decisionNN" docs/ flows/ --include="*.md" --include="*.cs" --include="*.ts" --include="*.json"
# Update each file found
```

---

## Positive Example

```
Trigger: "D-05 says every flow shares one IDocumentStore registration, but
  FLOW-15 multi-tenancy needs a per-tenant store — we cannot inject it because
  the DI registration in the Api host always creates the shared store"

Step 1: D-05 is in docs/DECISIONS-LOCKED.md → locked decision

Step 2: Valid trigger — concrete failure in current plan. Multi-tenant flows
  cannot inject a per-tenant store without changing the DI registration.

Step 3: Write ADR entry:
  "D-05 amended: IDocumentStore implementations are registered per flow Api host
   (not globally). FLOW-15 registers a tenant-scoped store; other flows keep the
   shared store. Both implement IDocumentStore. Contract unchanged; binding is
   per-flow."

Step 4: Update docs/DECISIONS-LOCKED.md D-05. Update the FLOW-15 Api host DI
  registration comment. Run dotnet build + dotnet test for FLOW-15.Tests.
```

---

## Negative Example

```
Trigger: "I think we should consider a different result wrapper than DataProcessResult"

WRONG:
  → Re-debating a locked contract decision without new information
  → No concrete failure cited
  → No ADR entry written
  → Proposing a change that contradicts the locked decision in a comment
```

---

## Integration

```
Prerequisite:   verify D-NN exists in docs/DECISIONS-LOCKED.md before claiming it's locked
After reopening: assess blast radius (which flows/clients) before updating files
After updating:  update all derived views in the same commit;
                 sync any affected flow docs / contracts / manifests
Capture:         decision-capture-SKILL.md records the amendment as a new D-NN entry
```
