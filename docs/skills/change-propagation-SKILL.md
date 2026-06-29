# change-propagation-SKILL.md — Write the stale-file list before you start, not after (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/change-propagation-SKILL.md` (G09). Universal core
> carries over unchanged: ask the **prior question** ("what files will be stale the
> moment this change is made?") **before** starting; the **6 change types** (SKILL
> ADDED, SKILL UPDATED, INTERFACE, SUBPROJECT, PLAN, DECISION) each with a complete
> blast radius; **source-of-truth vs derived-views**; and the binding rule
> **"a derived view updates in the SAME commit as its source."** Stack mapping:
> - **SUBPROJECT ADDED** = a new `FLOW-NN-<name>` 4-project set
>   (`Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`) → add a node to
>   **`Xiigen.Flows.slnx`**, a row to the flow map doc, a `Add<Name>Flow()` DI
>   extension, and a `Program.cs` entry.
> - **INTERFACE change** = an `I*` contract change → update the flow interfaces doc
>   **and** the DI registration in `*ServiceCollectionExtensions` in the same commit.
> - **SKILL ADDED/UPDATED** = a `docs/skills/*.md` change → update the skills
>   index/README the repo keeps.
> - verify via `grep` (exclude `client/angular/node_modules`) + `dotnet build
>   Xiigen.Flows.slnx` (0/0) + `dotnet test` (Failed 0).
> - returns are `DataProcessResult<T>` (XIIGen flow wrapper — **NOT** core
>   `OperationResult<T>`).

## When to load

- At the START of any task that changes a contract, adds a flow, adds/updates a
  skill, or locks a decision — before writing the change.
- At the END of the task — to check off every file in the blast radius.

---

## The Prior Question

Before any task begins — not after it appears done — ask:

**"What are all the files that will be stale the moment this change is made?"**

That list is the blast radius. Writing it down before starting means nothing is
discovered reactively at the end. The task is not done until every item is checked.

---

## The Dependency Map — 6 change types

### Change Type 1: SKILL ADDED (new `docs/skills/*.md`)

```
Skills index / README the repo maintains
  - add the new skill to the index/triggers table
  - update any skill count / version field

CLAUDE.md / AGENTS.md (only if the skill changes agent behavior or load order)
flow interfaces doc (only if the skill introduces a new contract)
```

Before starting: note the current skills index location and its count field.

### Change Type 2: SKILL UPDATED (content change, no new file)

```
Skills index — only if the update changes WHEN the skill fires or WHAT it prevents.
```

```bash
grep -rn "<skill-file-name>-SKILL.md" docs/ | grep -v node_modules
# confirm the index entry reflects the new behavior
```

### Change Type 3: INTERFACE ADDED OR CHANGED (`I*` contract)

```
flow interfaces doc
  - add/update the contract signature block + its implementing class

flow map doc
  - update the flow's status / contract column

DI registration: *ServiceCollectionExtensions.cs in the flow's *.Infrastructure
  - services.AddSingleton<INew, Impl>() (or update the existing line)

CLAUDE.md (only if it is a foundational fabric contract worth a rule reference)
```

```bash
# Confirm the interfaces doc has the new contract
grep -rn "INewContract" docs/ | grep -v node_modules
# Confirm DI registration present
grep -rn "INewContract" --include="*.cs" flows/ | grep -v node_modules | grep -i "AddSingleton\|AddScoped\|AddTransient"
```

A **shared Kernel/Fabrics contract** change fans out to every flow that injects it —
walk all of them (see `blast-radius-SKILL.md`).

### Change Type 4: SUBPROJECT ADDED (new flow)

```
Xiigen.Flows.slnx — add the 4 new project nodes
flow map doc — add the new FLOW-NN-<name> row
docs/ARCHITECTURE-style doc — add the flow to the description (if one exists)
the new *.{Api,Application,Infrastructure,Tests}.csproj — created at flow start
Program.cs — wire Add<Name>Flow()
CLAUDE.md — update a test baseline count only if the repo tracks one
```

### Change Type 5: PLAN PRODUCED OR AMENDED

```
the plan / execution doc itself
a decisions doc — any new locked decisions from the plan
any state file — update if phase ordering changed
any other doc referencing the plan's phase count — update cross-references
```

### Change Type 6: DECISION LOCKED

```
the decisions doc — add the D-NN entry
flow interfaces doc — add "See D-NN" if the decision governs a contract
CLAUDE.md — reference the decision if it affects the rules / MicroserviceBase pattern
any execution doc drafted before the decision — update if the approach changed
```

---

## How to Use This Skill

### At task start (not end):

```
1. Identify the change type(s).
2. Write out the blast radius for that type.
3. Cross off files not applicable to this specific change.
4. What remains is the definition of "done."
```

### At task end:

```
For each file in the blast radius:
  □ Was it updated?
  □ Can the update be verified? (grep, dotnet build, dotnet test)
  □ If not updated: explicit reason why not needed for this change.
```

---

## The Structural Problem This Addresses

Information about contracts, skills, and flows is duplicated across files. Some are
sources of truth; most are derived views.

**Sources of truth** (one owner, one writer):
- each `*.cs` implementation file (owns its own code)
- each `docs/skills/*-SKILL.md` (owns its own content)
- the decisions doc (owns locked decisions)

**Derived views** (kept in sync):
- the skills index / README — aggregates from skill files
- the flow interfaces doc — aggregates from `I*` contract files
- the flow map doc — aggregates from flow status
- `Xiigen.Flows.slnx` — aggregates the project set
- `CLAUDE.md` — aggregates conventions

**The rule:** when a source changes, every derived view that references it updates in
the **same commit**. Not the next session. Not when noticed. Same commit. (See
`documentation-sync-SKILL.md`.)

---

## Anti-Patterns

1. **Closing a "skill added" task after updating only the skill file** — the skills
   index is then stale; the next session misses the trigger.
2. **"I'll update the other files next session"** — partial updates are the root
   cause of drift; the next session lacks the context to know what was deferred.
3. **Discovering the blast radius at the END** — it is identified at the START; it
   defines "done," not what was forgotten.
4. **Updating a derived view instead of the source** — if the interfaces doc is stale
   because the `I*` changed, fix the `I*` (source) and regenerate the doc entry; do
   not paper over a stale source by editing the doc.

---

## Integration

- `documentation-sync-SKILL.md` is the sync-before-commit discipline for derived views.
- `blast-radius-SKILL.md` rates the change; `blast-radius-tagger-SKILL.md` tags the files.
- `shared-service-design-SKILL.md` identifies fan-out (shared-contract) changes.
