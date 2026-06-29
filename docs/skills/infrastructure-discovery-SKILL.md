# infrastructure-discovery-SKILL.md — Gate 0 fresh reconnaissance before planning

> **Stack adaptation (xiigen flows).** Universal Gate-0 *read-before-build*
> protocol ported from `llm_mvp_core/docs/skills/infrastructure-discovery-SKILL.md`
> (G08). This repo is greenfield for author skills — no prior version existed
> here. The universal core kept verbatim in intent: a *fresh* (not cached)
> reconnaissance pass before the first line of code — pre-existing failure
> baseline, read the subproject/interface/architecture map, search the code
> structure, audit fabric violations, and rewrite every plan reference to
> `file:line`. The stack details are adapted:
> - **Baseline (Step 0)** is `dotnet build Xiigen.Flows.slnx` → `0 Error(s)` (and
>   because `Directory.Build.props` sets `TreatWarningsAsErrors=true`, **0
>   warnings**) plus `dotnet test` over the per-flow `Xiigen.Flow.<Name>.Tests`
>   (xUnit, `net10.0`). The "test suite" is per-flow, not one monolith.
> - **Structure discovery** enumerates `flows/FLOW-NN-<slug>/` — each flow =
>   `server` / `client` / `contracts` / `deployment` / `docker` / `docs` +
>   `flow.manifest.json`; server = `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
>   client = Angular 21 standalone (`client/angular`) + Playwright.
> - **`find Src/ -name "*.csproj"`** of core becomes
>   `flows/FLOW-NN-*/server/**/*.csproj`; the project anchor is `Xiigen.Flows.slnx`
>   and `Directory.Build.props`, not a `nest-cli.json`.
> - **Always read** `.xiigen/wiki-domain/{domain-manifest.json,model-manifest.json}`
>   as an already-consumed source — flows depends on the generated domain model;
>   discovery that ignores it can plan a change that breaks it (R5/G12 boundary:
>   the trained model itself stays in `llm_mvp_core`).
> - **Fabric check:** direct `new` of a dependency that should arrive through DI /
>   a `contracts/` interface. The domain return type is `DataProcessResult<T>`
>   (XIIGen family wrapper, `IsSuccess`/`ErrorCode`) — **NOT** core
>   `OperationResult<T>`, which must not leak into this repo.

## When to Run

Run before Gate 1 of `phase-planning-SKILL.md`. This is **Gate 0**. Discovery
must be fresh — not cached from a previous session. Code changes between
sessions; discover the current state every time.

---

## The 10-Step Protocol

### Step 0 — Pre-Existing Failure Audit

Build first, then run the affected flow tests, and record the baseline:

```bash
dotnet build Xiigen.Flows.slnx 2>&1 | tail -10
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests 2>&1 | tail -10
```

Record:

```
Pre-existing baseline: [timestamp]
  Build: [0 Error(s) / N Error(s)]   ← any error blocks; 0 warnings required (TreatWarningsAsErrors)
  Total: [N] tests
  Passed: [N]
  Failed: [N]  ← allowed floor. Any NEW failure introduced by this phase = regression.
  Skipped: [N]
```

Pre-existing failures are NOT this phase's regressions — but any new failure
introduced by this phase IS. A build error is never an acceptable floor.

### Step 1 — Live Artifact Boundaries

Read these completely (record one end-of-file fact as proof of reading each):

```
README.md                                   → repo orientation, run scripts
Directory.Build.props                       → net10.0, Nullable, TreatWarningsAsErrors
Xiigen.Flows.slnx                           → which flow projects exist
flows/FLOW-NN-<slug>/flow.manifest.json     → this flow's identity + cross-flow deps
flows/FLOW-NN-<slug>/docs/                   → flow intent / status
.xiigen/wiki-domain/domain-manifest.json    → generated domain model this flow consumes
.xiigen/wiki-domain/model-manifest.json     → model locator (model body lives in llm_mvp_core)
```

Do not rely on memory. These files may have changed since the last session.

### Step 2 — Code Structure Search

For each flow referenced in the plan, verify the directory structure:

```bash
# All flow server projects
find flows/ -name "*.csproj" | sort

# All public interfaces (contracts and Application boundaries)
grep -rn "public interface I" flows/FLOW-NN-<slug> --include="*.cs" | sort

# Application service classes (self-typed base + interface)
grep -rn "class .*: MicroserviceBase<\|class .*: I[A-Z]" flows/FLOW-NN-<slug>/server --include="*.cs"

# Angular client surface (if the flow ships a UI)
ls flows/FLOW-NN-<slug>/client/angular/src 2>/dev/null
```

### Step 3 — History / Decision Docs

Read this flow's `docs/` and any decision artifact recorded next to the plan.
Check whether the plan's approach conflicts with a recorded flow decision or with
the consumed `.xiigen/wiki-domain` model.

### Step 4 — Architecture Map

Identify every dependency edge the plan will create or modify, per flow:

```
New edge:      [ServiceA in Xiigen.Flow.<Name>.Application] → [IContractB in contracts/]
Existing edge: [Xiigen.Flow.<Name>.Api Program.cs] → [Add<Name>Flow() DI extension]
Removed edge:  [ServiceE] no longer depends on [IContractF]
Cross-flow:    [FLOW-NN] depends on [FLOW-MM] (must be declared in flow.manifest.json)
```

Verify edges are consistent with the per-flow `Api/Application/Infrastructure`
boundary (Api references Application + Infrastructure; Application owns domain
logic; Infrastructure owns fabric implementations).

### Step 5 — Read Key Files in Full

Read every file the plan will change, completely. Record proof of reading — one
specific fact from the end of the file:

```
Read: flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs
Proof: Last public method is RegisterAsync() at line NN, returns DataProcessResult<RegistrationOutcome>
```

The end of the file often holds the facts that matter most. Do not skim.

### Step 6 — Completeness Map

List every deliverable in the plan and verify it can be found or placed:

```
DELIVERABLE-1: flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<Class>.cs
  Status: Does not exist yet (NEW)
  Directory exists: YES (server/Xiigen.Flow.<Name>.Application/)
  Contract to implement: I<Class> (confirmed in contracts/ or Application boundary)

DELIVERABLE-2: contracts/schemas/<name>.schema.json (modify)
  Status: EXISTS
  Current shape: [recorded in Step 5]
  Plan change: add field X
```

### Step 7 — Data Format Audit

For any data structures the plan creates or modifies:

```
Verify:
- DataProcessResult<T> wraps every Application public return type (NOT core OperationResult<T>)
- Concrete C# records / DTOs used for contracts (not anonymous types)
- Nullable reference types handled explicitly (Nullable is enabled repo-wide)
- contracts/schemas/*.json stays in sync with the C# DTO it describes
```

### Step 8 — Fabric-First Dependency Check

For every Application/Infrastructure class with dependencies:

```bash
# Direct instantiation of a dependency that should arrive through DI
grep -rn "= new [A-Z]" flows/FLOW-NN-<slug>/server --include="*.cs" | grep -v "// Fabric:" | grep -v Tests
```

A direct `new` of a fabric/contract dependency in non-test code bypasses DI.
Record it in the Out-of-Scope Issues list for the preflight.

### Step 9 — Rewrite Plan Elements with Real References

After Steps 0–8, update every plan reference to include `file:line`:

```
Before: "Add registration method to UserRegistrationService"
After:  "Add RegisterAsync() to flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs:NN"

Before: "Register the flow in DI"
After:  "Call AddUserRegistrationFlow() in .../Xiigen.Flow.UserRegistration.Api/Program.cs:NN"
```

No plan element should reference a file without a verified path.

---

## Discovery Completion Checklist

```
□ Step 0: Build 0 errors/0 warnings recorded; per-flow test failure floor + timestamp
□ Step 1: README, Directory.Build.props, slnx, flow.manifest.json, flow docs, .xiigen manifests read
□ Step 2: Directory structure verified — all referenced paths exist
□ Step 3: Flow decisions / consumed domain model checked — no conflicts
□ Step 4: New dependency edges documented; per-flow + cross-flow boundaries consistent
□ Step 5: Every file to be changed read completely with proof
□ Step 6: Every deliverable verified as placeable (NEW) or locatable (MODIFY)
□ Step 7: Data formats use DataProcessResult<T>; schemas in sync
□ Step 8: No unrecorded direct-new fabric violations in scope
□ Step 9: All plan references updated with real file:line citations
```

Only after this checklist is complete: proceed to Gate 1 (DNA check).

---

## Anti-Patterns

1. **Reading only the relevant section** — read the whole file; the end carries the load-bearing facts.
2. **Trusting the plan's file paths** — verify every path with a directory check.
3. **Skipping Step 0** — pre-existing failures (and the build state) must be recorded to distinguish new regressions.
4. **Reusing discovery from last session** — code changes between sessions; discover fresh.
5. **Ignoring `.xiigen/wiki-domain`** — flows consumes the generated domain model; a change that contradicts it is globally wrong even if it compiles.
