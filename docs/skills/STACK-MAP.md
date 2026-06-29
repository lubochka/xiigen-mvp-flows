# STACK-MAP.md — Every core concept and its live equivalent in xiigen flows (.NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield bridge. Ported from the core
> "concept → equivalent → location" map `llm_mvp_core/docs/skills/REMOVED-FROM-XIIGEN.md`
> (G11). The universal part carries over: the map exists so that when a core
> abstraction is referenced, the porter can see **what replaces it and where**, and
> so nothing is silently "removed" — everything is either translated to a concrete
> flows class/file or explicitly marked as staying in `llm_mvp_core`. This map
> covers **two stacks**: backend (.NET 10, per-flow `Api/Application/Infrastructure/Tests`)
> and frontend (Angular 21 standalone components, Playwright e2e).
>
> Companion file: `DNA-DOTNET-TRANSLATION.md` translates the 10 DNA rules into the
> flows stack. STACK-MAP is the broader concept index that points at it.

Nothing was removed. Everything was translated, or it stays in `llm_mvp_core` and
is consumed through a manifest. This document tells you which.

---

## Backend stack map (.NET 10)

| Core concept (llm_mvp_core / xiigen) | Flows equivalent | Location |
|---|---|---|
| `OperationResult<T>` (core return wrapper) | `DataProcessResult<T>` (flows domain wrapper) — **core `OperationResult<T>` does not appear in flows `src`** (0×) | every flow `*.Application` / `*.Infrastructure` `.cs` (e.g. `…AdsPlatform.Application/AdsPlatformResults.cs`) |
| `Results.Ok` / `Results.Problem` / `ProblemDetails` | HTTP-layer helpers only — the minimal-API host, **not** the domain wrapper | `Xiigen.Flow.<Name>.Api` (`Program.cs` / endpoint mapping) |
| Fabric First (resolve through interface, not concrete type) | built-in .NET DI — constructor injection via interfaces | every flow service constructor (e.g. `AdsPlatformService(IDocumentStore, IFreedomConfigService, …)`) |
| FREEDOM config value access | `IFreedomConfigService.GetAsync(key, ct)` → typed value; **never raw `IConfiguration`/`appsettings.json`** (`IConfiguration` is 0× in flows `src`) | injected `_freedom` (e.g. `…AdsPlatform.Application/AdsPlatformService.cs:623`) |
| Redis / key-value session cache | `IDocumentStore` (document persistence fabric) | shared `Xiigen.Kernel` / `Xiigen.Fabrics.*` contract, injected per flow |
| Outbox / atomic store-before-emit | `IOutboxStore` | shared `Xiigen.Kernel` / `Xiigen.Fabrics.*` contract |
| Scheduler / timer fabric | `ISchedulerService` | shared `Xiigen.Fabrics.Scheduler` |
| `MicroserviceBase` | per-flow Application service class (constructor-injected fabrics) | `Xiigen.Flow.<Name>.Application/<Name>Service.cs` |
| Cross-flow governance / BFA rules | `<Name>BfaRuleRegistry` | `Xiigen.Flow.<Name>.Application/<Name>BfaRuleRegistry.cs` |
| `npm run build` | `dotnet build Xiigen.Flows.slnx` (0 Error, 0 Warning; `TreatWarningsAsErrors=true`) | — |
| `npx jest` / xUnit `[Fact]` (core) | xUnit `[Fact]` / `[Theory]` in `Xiigen.Flow.<Name>.Tests` → `dotnet test` (Failed 0) | `Xiigen.Flow.<Name>.Tests/*.cs` |
| Target framework | `net10.0`, `Nullable=enable`, `ImplicitUsings=enable` | `Directory.Build.props`, `global.json` (SDK 10.0.300) |

---

## Frontend stack map (Angular 21)

| Core / xiigen concept | Flows equivalent | Location |
|---|---|---|
| React / React Native UI | Angular 21 **standalone** components (no NgModule) | `flows/FLOW-NN-<name>/client/angular/src` |
| Component invariants / behavioral checks | Playwright e2e + visual evidence snapshots | per-flow client `scripts` + `docs/e2e-snapshots/`, `docs/visual-evidence` |
| `npm run build` (client) | `ng build` (`angular.json`) | per-flow `client/angular` |
| jest + @testing-library (UI) | `ng test --watch=false` (unit) + Playwright (e2e) | per-flow `client/angular` |

---

## What stays in llm_mvp_core (consumed, not ported)

Per R5/R6 the common models, their training, and the ML graph stay in
`llm_mvp_core`. Flows is a **consumer**: it reads the manifests shipped under
`.xiigen/`, it does not train or hold a local model.

| Core capability | Why it is not ported | How flows consumes it |
|---|---|---|
| `ILearningMachine<TDomain>` / DPO training / checkpoints | trainable model + training sets stay in `llm_mvp_core` (R5) | `.xiigen/wiki-domain/model-manifest.json` (manifest pointer) |
| `DomainModelRegistry` / `DynamicGraph` graph-sync engine | core ML graph (G12, `na — core-specific`) — no local registry in flows | `.xiigen/wiki-domain/relations/code-trace.json` is a **consumed** graph artifact, not a local `DomainModelRegistry` |
| DPO multi-generate topology / `meta-arbiter` escalation | core DPO loop (R6) — flows runs no DPO loop | n/a — note-only; see `prompt-patch-authoring-SKILL.md` Rule 5 (note-only here) |
| Skill graph-sync drift detector (`GraphDriftDetector`) | core ML drift engine (G12, `na`) | n/a — only the §4 authoring discipline is reusable, and that is `na` for flows until a local skill library needs compiler-parsing |

---

## Notes

- The token counts above (`OperationResult<` 0×, `IConfiguration` 0×,
  `DataProcessResult` present across `src`, `Results.*` HTTP-only) are verified
  against the live flows repo at port time; absolute counts drift as code grows,
  but the **direction** is stable: domain layer uses `DataProcessResult<T>`, HTTP
  layer uses `Results.*`/`ProblemDetails`, config comes from `IFreedomConfigService`.
- When porting any further core skill into flows, run it through this map first: if
  a core abstraction has no flows equivalent here, either add the equivalent row or
  mark it "stays in `llm_mvp_core`" — do not leave it implied.
