# documentation-sync-SKILL.md — Sync docs before the commit, not after (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/documentation-sync-SKILL.md` (G09). Universal core
> carries over unchanged: **synchronize docs BEFORE the commit, never after**; keep
> a **File → Canonical-Docs map**; the definition of **"synced"** = docs match the
> code (no description of anything that does not exist, no hole for anything that
> does), counts/versions accurate; and the **ban on "I'll update it in the next
> commit / TODO."** Stack mapping:
> - the File → Docs map points at flows artifacts: a flow's
>   `*.{Api,Application,Infrastructure}/*.cs` → the flow interfaces doc / flow map /
>   ARCHITECTURE-style doc; a new `*.csproj` → `Xiigen.Flows.slnx` + the map; a new
>   `docs/skills/*.md` → the skills index/README.
> - a Web API surface's human-readable contract = the per-flow `*.Api` OpenAPI /
>   endpoint doc: each endpoint has a human name, example, and parameter description
>   (no internal jargon on the primary path).
> - the test-count field comes from a **fresh `dotnet test`**, not memory.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` (Failed 0);
>   the doc sync is part of the same phase commit (Rule 6).
> - returns are `DataProcessResult<T>` (XIIGen flow wrapper — **NOT** core
>   `OperationResult<T>`); docs must describe that wrapper, not the core one.

## When to load

- At session end, before the phase commit, whenever code changed behavior,
  contracts, events, DI, or the flow set.
- Whenever a new skill, contract, or flow was added.

---

## Core Rule

**Synchronize documentation BEFORE the commit, not after.** A code change that
alters behavior, a contract, lifecycle, events, DI registration, or the flow set
must update the matching doc in the **same** commit. "Synced" means the docs match
the code: nothing described that does not exist, no hole for something that does,
and every count/version accurate. "I'll update it next commit / TODO" is forbidden.

---

## File → Canonical-Docs Map (flows)

| Changed file | Canonical doc(s) that must update in the same commit |
|--------------|------------------------------------------------------|
| a flow `*.Application` `I*.cs` (contract added/changed) | flow interfaces doc (signature + implementing class); flow map (contract column) |
| a flow `*.Application` / `*.Infrastructure` service (`.cs`) | flow map status; ARCHITECTURE-style doc if structure changed |
| a flow `*.Api` endpoint | the flow's OpenAPI / endpoint doc — human name, example, parameters |
| a new event type (`xiigen.<flow>.<entity>-<verb>.v1`) | flow interfaces doc events section (producer + consumers) |
| a new `*.csproj` (new flow) | `Xiigen.Flows.slnx`; flow map; ARCHITECTURE-style doc |
| a new `docs/skills/*.md` | skills index / README (triggers + index + count) |
| DI change in `*ServiceCollectionExtensions.cs` | flow interfaces doc if the registered contract changed |
| test count changed | the doc field that records it — from a **fresh `dotnet test`** |

---

## Definition of "Synced"

```
A doc is SYNCED when, for the changed area:
  □ Every contract/method described EXISTS in code with that exact signature
    (DataProcessResult<T> / Task<DataProcessResult<T>>, real parameter list).
  □ Every contract/method/event that EXISTS in code is described (no hole).
  □ Counts are exact (test count from a fresh `dotnet test`; flow count from the slnx).
  □ Versions/event-version segments (.v1/.v2) are accurate.
  □ No description references a removed or renamed symbol.
```

Verification:
```bash
# A method the doc claims must exist in code (exclude node_modules)
grep -rn "PublishAsync" --include="*.cs" flows/ | grep -v node_modules
# Fresh test count for the count field
dotnet test 2>&1 | tail -5
# A skill the index claims must have a file
ls docs/skills/<skill-file-name>-SKILL.md
```

---

## API Surface Documentation (Web API, flows-specific)

For each `*.Api` endpoint, the doc/OpenAPI entry must carry human-readable content
on the primary path:

```
□ A human name for the operation (not just the route).
□ At least one request/response example with realistic values.
□ A description per parameter (what it is, allowed values).
□ Error responses named by their DataProcessResult Failure code.
□ No internal jargon (store keys, table names, DNA codes) on the user-facing path.
```

This is the UX-readability layer for the API and is checked alongside the contract
signature.

---

## Anti-Patterns

1. **"Update the docs in a follow-up commit"** — partial sync is the root cause of
   drift; docs travel in the same commit as the code (Rule 6).
2. **Describing a method that no longer exists** — a stale signature in the
   interfaces doc misleads the next session; verify with grep.
3. **A hole for a shipped contract/event** — if `PublishAsync` exists but the doc
   omits it, the doc is not synced.
4. **A test count from memory** — always re-run `dotnet test` for the number.
5. **Documenting the core `OperationResult<T>`** — flows return
   `DataProcessResult<T>`; the doc must reflect the real wrapper.

---

## Integration

- `change-propagation-SKILL.md` produces the blast-radius list of which docs must
  update; this skill is the discipline that they are actually synced before commit.
- `blast-radius-tagger-SKILL.md` tags DOCS files (same commit as code).
- `output-readability-gate-SKILL.md` governs the human-readability of the API docs.
