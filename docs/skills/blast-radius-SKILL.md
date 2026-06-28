# blast-radius-SKILL.md — Assess how far a change propagates before touching a file (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/blast-radius-SKILL.md` (G09, the Assessor — distinct
> from the tagger). Universal core carries over unchanged: classify every change as
> **CRITICAL / HIGH / MEDIUM / LOW**, find **all** dependents with grep before
> claiming "isolated," run **protection gates before and after**, and write an
> **Impact Summary** before editing. Stack mapping:
> - **`I*` interface signature change = always CRITICAL.** In flows that means a
>   per-flow contract (e.g. `IOssCurriculumQueueBroker`) or a shared Kernel/Fabrics
>   contract (`IDocumentStore`, `IOutboxStore`, `ISchedulerService`,
>   `IFreedomConfigService`). A shared Kernel contract change fans out to every flow.
> - **constructor / DI change = HIGH** (every `services.Add…<…>()` registration in
>   the affected `*ServiceCollectionExtensions` plus every test `new …()`/fixture
>   must update).
> - **implementation change inside a class = MEDIUM**; **new file / new method / docs
>   / new skill = LOW**.
> - gates = `dotnet build Xiigen.Flows.slnx` (0 errors, 0 warnings) +
>   `dotnet test` of the affected `*.Tests` project(s), recorded **before and after**.
> - returns are `DataProcessResult<T>` (XIIGen flow wrapper — **NOT** core
>   `OperationResult<T>`); a change to that wrapper's shape would be CRITICAL.

## When to load

- Before editing any `I*` interface file in a flow or in the Kernel/Fabrics layer.
- Before a constructor / DI-registration change.
- Whenever you are tempted to call a change "small" or "isolated."
- Triggered by `blast-radius-tagger-SKILL.md` when a file is tagged INTERFACE.

---

## Classification Levels

```
CRITICAL — affects all callers across the solution
  Triggers: public I* signature change; a change to the DataProcessResult<T> wrapper;
            a shared Kernel/Fabrics contract change (IDocumentStore/IOutboxStore/
            ISchedulerService/IFreedomConfigService); a DNA-rule or CLAUDE/AGENTS change.
  Impact: every flow/class that calls the contract must be checked.
  Protocol: full baseline before & after; read every caller; human review before commit.

HIGH — affects all callers in one flow / one project / one assembly
  Triggers: constructor signature change; new required DI dependency; shared base
            (MicroserviceBase<TSelf>) change; cross-project type change.
  Impact: all callers in the affected flow project + its tests + its DI extension.
  Protocol: grep all callers; run the affected flow's full test project before & after.

MEDIUM — affects one class or one method
  Triggers: method signature change inside a class; new private helper; implementation
            change (not interface change).
  Impact: only the changed class and its direct tests.
  Protocol: run the changed class's tests; check its direct callers.

LOW — affects only future code, not existing callers
  Triggers: new file; new method added to a class; new test; docs update; new skill file.
  Impact: no existing code affected.
  Protocol: standard gate (build + tests).
```

---

## Assessment Procedure

### Step 1 — Identify Change Type and Location

| Location | Default Blast Radius |
|----------|---------------------|
| `docs/skills/` | LOW |
| flow interfaces doc / `docs/*.md` | MEDIUM (doc change) |
| `*.Tests/` only | LOW |
| a flow's `*.Application` `I*.cs` (per-flow contract) | CRITICAL |
| `Xiigen.Kernel` / `Xiigen.Fabrics.*` contract (`IDocumentStore`, etc.) | CRITICAL (fans out to every flow) |
| `MicroserviceBase<TSelf>` (shared base) | HIGH |
| a flow's `*.Application` / `*.Infrastructure` concrete `.cs` | MEDIUM |
| `*ServiceCollectionExtensions.cs` (DI) | HIGH |
| `*.csproj` / `Xiigen.Flows.slnx` | HIGH (project/assembly changes) |
| `CLAUDE.md` / `AGENTS.md` | HIGH (governs all sessions) |

### Step 2 — Find All Dependents

```bash
# For CRITICAL/HIGH: find every caller of the changed identifier (exclude node_modules)
grep -rn "IOssCurriculumQueueBroker\|IDocumentStore" \
  --include="*.cs" flows/ | grep -v node_modules | sort

# For an interface change: find all implementations
grep -rn "class.*:.*IOssCurriculumQueueBroker" --include="*.cs" flows/ | grep -v node_modules

# For a MicroserviceBase change: find all subclasses
grep -rn "class.*:.*MicroserviceBase" --include="*.cs" flows/ | grep -v node_modules

# For a constructor/DI change: find all registrations and test instantiations
grep -rn "AddSingleton<\|AddScoped<\|AddTransient<" --include="*.cs" flows/ | grep -v node_modules
```

Never claim "this is isolated" without running the grep.

### Step 3 — Identify Protection Gates

```
CRITICAL:
  □ dotnet test (full affected scope) BEFORE — record N
  □ Read every caller in full
  □ dotnet build Xiigen.Flows.slnx (0 errors, 0 warnings) AFTER
  □ dotnet test AFTER — count must be ≥ N (+ new tests)

HIGH:
  □ Record the affected flow's test count BEFORE
  □ Read all callers in the affected flow + its DI extension
  □ Run the affected flow's tests AFTER

MEDIUM:
  □ Run the changed class's tests BEFORE and AFTER
  □ Check direct callers of the changed method

LOW:
  □ dotnet build: 0 errors, 0 warnings
  □ dotnet test: Failed 0
```

### Step 4 — Write Impact Summary

Before touching any file:

```
Change: Add ListPendingAsync(CancellationToken) to IOssCurriculumQueueBroker

Blast radius: CRITICAL
  Reason: public I* signature change — every implementation and caller affected.

Dependents found:
  Implementations: OssCurriculumQueueBroker (.../OssCurriculum.Infrastructure/...)
  Callers:        OssCurriculum.Application services that inject the broker
  DI:             AddOssCurriculumFlow() in OssCurriculum.Infrastructure

Protection gates:
  □ dotnet test OssCurriculum.Tests BEFORE — record N
  □ Read the implementation + all callers in full
  □ Add ListPendingAsync to the implementation
  □ dotnet build Xiigen.Flows.slnx (0/0); dotnet test AFTER ≥ N (+ new test)

Estimated impact: 1 implementation + injecting services + the flow's tests.
```

---

## Special Cases

### Public Interface Change (CRITICAL)

```
Adding a method to an existing interface:
  → every implementation must add it; every caller may need updating.
  → never add a method to an interface with 3+ implementations without listing them first.

Removing a method:
  → even higher radius — all callers fail to compile. Requires explicit approval.
```

### Shared Kernel/Fabrics Contract Change (CRITICAL, fans out)

```
Changing IDocumentStore / IOutboxStore / ISchedulerService / IFreedomConfigService:
  → walk every flow that injects it (grep across flows/), plus the Fabrics
    implementation(s).
  → gate = dotnet build of the whole solution + dotnet test of every affected flow.
```

### Constructor / DI Signature Change (HIGH)

```
Adding a required constructor parameter:
  → every services.Add…<…>() registration and every test fixture/new …() must update.
  → grep all instantiation + registration sites before changing.
```

---

## Anti-Patterns

1. **"This is just a small change"** — run the grep before assuming small.
2. **Changing an `I*` without listing all implementations first** — list before touching.
3. **Claiming LOW on an interface change** — interface changes are always CRITICAL.
4. **Not running the affected tests before the change** — no baseline = no delta.

---

## Integration

- `blast-radius-tagger-SKILL.md` tags a file INTERFACE → triggers this assessment.
- `change-propagation-SKILL.md` lists the derived docs/DI that a CRITICAL change must
  update in the same commit.
- `shared-service-design-SKILL.md` identifies which contracts are shared (and thus
  fan-out CRITICAL).
