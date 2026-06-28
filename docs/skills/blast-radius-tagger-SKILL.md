# blast-radius-tagger-SKILL.md — Tag every planned file before writing it (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/blast-radius-tagger-SKILL.md` (G09). Universal core
> carries over unchanged: tag every planned deliverable file **before** writing it
> as **TEST-ONLY / GOVERNANCE / PRODUCTION / INTERFACE / DOCS**, apply the per-tag
> extra gate, and the commit rules (GOVERNANCE committed separately and first in a
> mixed session; DOCS in the **same** commit as the code; INTERFACE triggers the
> Assessor). Stack mapping:
> - **PRODUCTION** = `flows/FLOW-NN-<name>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}`
>   `*.cs` and `.csproj` (also `Xiigen.Kernel`/`Xiigen.Fabrics.*` when touched).
> - **INTERFACE** = an `I*.cs` contract (per-flow in `*.Application`, or a shared
>   Kernel/Fabrics contract).
> - **TEST-ONLY** = `*.Tests` `*.cs`.
> - **GOVERNANCE** = `docs/skills/*.md`, `CLAUDE.md`, `AGENTS.md`.
> - **DOCS** = flow interfaces doc / `docs/*.md`.
> - PRODUCTION gate = `dotnet build Xiigen.Flows.slnx` (0 errors, 0 warnings) +
>   `dotnet test` (Failed 0); INTERFACE gate adds `blast-radius-SKILL.md` (Assessor)
>   + all-caller check.
> - production returns are `DataProcessResult<T>` (XIIGen flow wrapper — **NOT** core
>   `OperationResult<T>`).

## When to load

- At the start of any phase — tag every planned deliverable before writing.
- At session end — verify PRODUCTION/INTERFACE files were properly validated.
- In any session that mixes file types (tests + production + docs + skills).

---

## Blast Radius Categories

| Tag | Definition | Examples (flows) | Additional Gates Required |
|-----|-----------|------------------|---------------------------|
| TEST-ONLY  | Only affects the test suite | `*.Tests/**/*.cs`, test fixtures | Standard gate only (`dotnet test`) |
| GOVERNANCE | Affects session behavior | `docs/skills/*.md`, `CLAUDE.md`, `AGENTS.md` | Commit BEFORE code in a mixed session |
| PRODUCTION | Affects runtime behavior | `Xiigen.Flow.<Name>.{Api,Application,Infrastructure}/*.cs`, `*.csproj` | `dotnet build` + `dotnet test` after every change |
| INTERFACE  | Changes a public contract | a flow `*.Application` `I*.cs`; `Xiigen.Kernel`/`Xiigen.Fabrics.*` contract | Blast-radius assessment + all-caller check |
| DOCS       | Documentation update | flow interfaces doc, `docs/*.md` | Verify accuracy against code state |

---

## Rules

1. **Tag first** — assign a tag to every deliverable before writing any file.
2. **INTERFACE** files require a blast-radius assessment (`blast-radius-SKILL.md`)
   before any edit — list all implementations and callers first.
3. **GOVERNANCE** files in a mixed session are committed **separately, before** code.
4. **PRODUCTION** files must pass `dotnet build` (0 errors, 0 warnings) before the
   session is declared done.
5. **Never mix** TEST-ONLY and INTERFACE in the same commit (different blast radii).
6. **DOCS** go in the **same** commit as the code they describe.

---

## Tagging Example

Session: "Add `ListPendingAsync` to `IOssCurriculumQueueBroker` and implement it."

| File | Tag | Extra Gates |
|------|-----|-------------|
| `.../OssCurriculum.Application/OssCurriculumResults.cs` (the `I*`) | INTERFACE | Blast-radius assessment first — list implementations + callers |
| `.../OssCurriculum.Infrastructure/OssCurriculumQueueBroker.cs` | PRODUCTION | `dotnet build` after edit |
| `.../OssCurriculum.Tests/OssCurriculumQueueBrokerTests.cs` | TEST-ONLY | `dotnet test` after edit |
| flow interfaces doc | DOCS | Must match the final `I*` signature |
| `docs/skills/blast-radius-SKILL.md` | GOVERNANCE | Separate commit if mixed with code |

Commit sequence:
1. GOVERNANCE changes (if any) — separate commit.
2. INTERFACE + PRODUCTION + TEST-ONLY + DOCS — one commit (Rule 6: one commit per phase).

---

## INTERFACE Validation Template

When writing to or changing an `I*.cs`:

```bash
# 1. List all implementations before changing (exclude node_modules)
grep -rn "class.*:.*IOssCurriculumQueueBroker" --include="*.cs" flows/ | grep -v node_modules
# → add the new method to EVERY implementation before committing

# 2. List all callers before changing
grep -rn "IOssCurriculumQueueBroker" --include="*.cs" flows/ | grep -v node_modules
# → verify callers still compile after the change

# 3. Build verification after change
dotnet build Xiigen.Flows.slnx
# → 0 Error(s), 0 Warning(s)

# 4. Note how to revert before editing (git stash if needed)
```

For a **shared Kernel/Fabrics contract** (`IDocumentStore`, `IOutboxStore`,
`ISchedulerService`, `IFreedomConfigService`), the caller grep spans **every flow** —
this is a fan-out CRITICAL change (see `blast-radius-SKILL.md`).

---

## PRODUCTION Validation Template

When writing to `Xiigen.Flow.<Name>.{Api,Application,Infrastructure}/*.cs`:

```bash
# After editing, before committing:
dotnet build Xiigen.Flows.slnx
# → 0 Error(s), 0 Warning(s)  (TreatWarningsAsErrors)

dotnet test 2>&1 | tail -5
# → Failed: 0

# Rule 3 check: every production line carries a plain-language comment (manual review)
# Boundary check: no OperationResult<T> leak, no ML framework import
grep -n "OperationResult<\|using Microsoft\.ML\|using TorchSharp" "<changed-file>"
# → no output
```

---

## Anti-Patterns

1. **Editing a public `I*` alongside 5 test files without tagging** — the interface
   edit then gets no blast-radius check.
2. **Committing GOVERNANCE files together with code in a blocked code session** —
   skill files and code should be separable; commit governance first.
3. **Tagging TEST-ONLY and then asserting on call counts** — fragility not caught by
   tagging; also load `test-integrity-SKILL.md`.
4. **Treating DOCS as a follow-up commit** — docs go in the same commit as the code.

---

## Integration

- `blast-radius-SKILL.md` (Assessor) runs whenever a file is tagged INTERFACE.
- `change-propagation-SKILL.md` defines which DOCS/DI must travel in the same commit.
- `phase-git-report-SKILL.md` / commit boundary rules enforce the commit sequence.
