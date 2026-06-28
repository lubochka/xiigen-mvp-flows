# test-integrity-SKILL.md — xUnit test correctness guard (xiigen flows)
## v1.0: ported from llm_mvp_core test-integrity → .NET 10 per-flow / xUnit / Angular 21

## When to load
Load when a test fails after a guard condition is changed.
Load when a stub is modified (not just created new) to make a test pass.
Load when a phase ends with "all tests pass" and at least one test was modified.
Load when test count increases by more than the number of new behaviours introduced.
Load before declaring any per-flow `Xiigen.Flow.<Name>.Tests` slice complete.

---

## Stack context (xiigen flows)

- Each flow lives under `flows/FLOW-NN-<slug>/server/` with four projects:
  `Xiigen.Flow.<PascalName>.{Api,Application,Infrastructure,Tests}.csproj`
  (net10.0). `<PascalName>` is PascalCase of the slug, e.g. `BundleActivation`
  from `FLOW-00-bundle-activation`.
- The test project (`Xiigen.Flow.<PascalName>.Tests.csproj`) carries
  `<ProjectReference>` to `.Application` and `.Infrastructure` and uses
  `xunit` (2.9.3), `xunit.runner.visualstudio` (3.1.4), `Microsoft.NET.Test.Sdk`.
- Result wrapper is the flows-domain `DataProcessResult<T>` from `Xiigen.Kernel`
  — NOT `llm_mvp_core` `OperationResult<T>`. The read surface that tests assert
  on is `result.IsSuccess` / `result.IsFailure` / `result.Error?.Code` /
  `result.Error?.Message` / `result.Data`. Production code creates failures with
  the inherited service helper `Failure<T>("CODE", "message")` (code first) or
  the public factory `DataProcessResult<T>.Failure("CODE", "message")`.
- Fabrics are in-process .NET (`Xiigen.Fabrics.InMemory`): a test wires real
  services with `new InMemoryDocumentStore()`, `new InMemoryOutboxStore()`, etc.
  per `[Fact]` — no containers, no shared state.
- Gate B for a server slice = `dotnet test --filter "FullyQualifiedName~<Class>"`
  exits 0.
- UI clients are Angular 21 (`@angular/core ^21.2.0`) standalone apps under
  `flows/FLOW-NN-*/client/angular`, tested with `@playwright/test ^1.60.0`
  (e2e) and `vitest` (component). `ng build` / `ng test` are the client gates.

---

## Core principle

A passing test confirms an assertion holds. It does not confirm that the
code path you care about actually executed.

These two come apart whenever:
- A stub is changed to skip a `DataProcessResult.IsSuccess` check (the guard is
  never reached).
- Two conditions are stacked, making a downstream branch unreachable.
- A test adds assertions but the stubs bypass the real service contract.
- An `IsSuccess`/`IsFailure` guard exists in one per-flow service but is missing
  in its sibling (e.g. `BundleActivationService` vs `BundleStatusTrackerService`).

When they come apart, test count goes up and everything looks green — but a
code path is silently unguarded.

A test is allowed as fallback evidence when the real Angular UI path is not
available in the current slice. That fallback must be labelled with its exact
scope. A component (vitest) test, a server `[Fact]`, or a static scan must not
be reported as a full Playwright UI e2e run.

---

## The four failure modes (universal — portable to flows)

| Mode | Trigger | Symptom | Rule |
|------|---------|---------|------|
| FM-1: Stub-Fix Masking | Stub changed to skip `IsSuccess` check | Test passes, guard never runs | test-fix-or-code-fix |
| FM-2: Guard Stacking | Two conditions check the same input | Downstream branch unreachable | branch-reachability |
| FM-3: Coverage Inflation | Tests added but stubs bypass real contracts | Test count up, real path untested | coverage-vs-execution |
| FM-4: Sibling Guard Gap | Guard in `BundleActivationService`, missing in `BundleStatusTrackerService` | Same input, inconsistent protection | sibling-guard-parity |

### Boundary echelons added for flows

| Mode | Trigger | Symptom | Rule |
|------|---------|---------|------|
| FM-5: Test-Only Boundary Substitute | Tests pull production source through `Compile Include` / `Link` instead of a real `ProjectReference` | Tests pass, but the requested project boundary is missing | project-boundary-evidence |
| FM-6: UI Automation Scope Inflation | A vitest component test, server `[Fact]`, or `ng build` is reported as full UI e2e | Completion claims UI is automated while no Angular page/route was driven | ui-evidence-scope |
| FM-7: UX Automation Scope Inflation | Playwright clicks elements but never checks human labels, ARIA names/help text, examples, error clarity, layout/bounds | The client can be clicked but may still be confusing or inaccessible | ux-evidence-scope |

### core_specific failure modes — note-only for flows (do NOT port as gates)

These exist in `llm_mvp_core` for trainable models and host extensions and are
out of scope for flows (per mapping R5/R6). Keep them as reference only:

- FM-8 Pseudo-Trainable (`ILearningMachine`/`ILogical*` lifecycle) — common
  models live in `llm_mvp_core`; flows consume via manifest/locator.
- FM-16 Dual-Path host UI / FM-17 Host-Session-Auth (Cursor/Claude/Codex).
- FM-9 Sub-Agent firewall, FM-10/FM-11 full-extension / model-downgrade.

If a flows report names one of these as a passed gate, downgrade the claim:
it is core territory, not a flows test-integrity gate.

---

## Quick routing table

| What just happened | Apply first | Then |
|-------------------|-------------|------|
| Test fails after guard tightened | test-fix-or-code-fix | branch-reachability |
| Stub changed to skip `IsSuccess`/`IsFailure` | test-fix-or-code-fix | coverage-vs-execution |
| Two guards check same input path | branch-reachability | coverage-vs-execution |
| Test count up but stubs bypass service contract | coverage-vs-execution | branch-reachability |
| New flow/Tests project passes through linked production source | project-boundary-evidence | coverage-vs-execution |
| Angular UI e2e requested, but only vitest/`[Fact]`/build evidence exists | ui-evidence-scope | coverage-vs-execution |
| Playwright clicks controls but asserts no human labels/ARIA | ux-evidence-scope | coverage-vs-execution |
| Guard in one per-flow service, missing in sibling | branch-reachability (sibling audit) | — |
| Test uses hand-crafted fake data | contract-driven-testing | — |

---

## Rule: test-fix-or-code-fix

Before changing any stub or assertion to make a test pass, answer:

```
Diagnosis question: WHY is this path failing?

Option A: The code is wrong — the stub is strict and correctly rejecting bad output.
  → Fix the .Application/.Infrastructure code. Keep the stub strict.

Option B: The stub is too strict for a valid code path.
  → Update the stub to match the real service/fabric contract. Do not weaken it.

Option C: The assertion tests the wrong thing.
  → Only change the assertion if Luba explicitly approves the behaviour change
    (Iron Rule — see bug-to-tests-SKILL.md).
```

```
Anti-pattern:
  Test fails because DataProcessResult.IsSuccess == false
  → Stub changed to always return a success result
  → Test passes
  → IsSuccess guard in the per-flow service is now unreachable

Correct:
  Diagnose WHY IsSuccess == false is returned
  Fix the .Application service to return success on the happy path
  Keep the stub strict — it should still fail for the error path
```

---

## Rule: branch-reachability

After adding or changing a guard condition, verify that every branch has
at least one test that reaches it.

```csharp
// Example from a per-flow service: stacked guards
if (input.IsFailure) return Failure<TOut>("UPSTREAM_FAILURE", "no input");   // guard 1
if (input.Data is null) return Failure<TOut>("BUNDLE_NOT_FOUND", "missing"); // guard 2 — reachable?

// Checklist:
// [ ] Is there a [Fact] that yields IsSuccess == true but Data == null?
// [ ] If guard 1 rejects everything that reaches guard 2, guard 2 is dead code
```

Sibling audit: after adding a guard to any per-flow service, check every sibling
service in the same flow that processes the same input type.

```
Per-flow sibling pairs (example — FLOW-00):
  BundleActivationService ↔ BundleStatusTrackerService  (both read SolutionBundles)
  BundleValidatorService ACCEPT ↔ REJECT paths          (both produce DataProcessError)
Cross-layer sibling check:
  Api endpoint guard ↔ Application service guard (same request shape)
```

---

## Rule: coverage-vs-execution

Count tests that execute real code paths, not tests that merely pass.

```
Checklist:
[ ] Does the test exercise the real InMemoryDocumentStore.UpsertAsync/GetAsync, or a stub returning fixed data?
[ ] Does the test exercise the real .Application service method, or a stub with a canned outcome?
[ ] Is there at least one [Fact] per service that uses a non-trivial fabric (real-shaped documents)?
[ ] Does the test assert on the specific output field the behaviour change affects (e.g. result.Data?.HttpStatus), not just IsSuccess?
[ ] After FM-3 is suspected: count tests that set up real-shaped input vs tests that use empty stores
```

---

## Rule: contract-driven-testing

Use real flow domain shapes as test inputs — not synthetic hand-crafted
dictionaries that the production path never produces.

```csharp
// Wrong — synthetic fixture that bypasses the real document shape
var result = await service.ActivateAsync(new Dictionary<string, object?>(), "1", "k");

// Correct — real-shaped fixture matching the seeded SolutionBundles document
var fixture = await FlowFixture.CreateAsync();      // seeds BDL-core-platform-v2 + FLOW-01/02 lifecycle
using var _ = fixture.PushContext();                // real tenant + correlation context
var result = await fixture.Service.ActivateAsync(
    new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
    ifMatch: "1",
    idempotencyKey: Guid.NewGuid().ToString("n"));
Assert.True(result.IsSuccess, result.Error?.Message);
Assert.Equal(201, result.Data?.HttpStatus);
```

---

## Rule: project-boundary-evidence

For a new flow, Api surface, or Tests project, tests must exercise the artifact
boundary requested by the work order through a real reference — not a source link.

```
[ ] The implementation project files exist under flows/FLOW-NN-<slug>/server/
[ ] Xiigen.Flow.<Name>.Tests.csproj references .Application and .Infrastructure
    via <ProjectReference> (confirmed in the real csproj), not <Compile Include> / <Link>
[ ] The Tests project does NOT pull .Application/.Infrastructure .cs files as the main path
[ ] dotnet build / dotnet test proves the real project boundary compiles/runs
[ ] contracts/{auth,endpoints,schemas} are consumed through the package/reference, not copied across the boundary
[ ] The report states whether this is unit, integration, component (vitest), or full UI (Playwright) evidence
```

If the real Angular UI path is not available, write that as an honest gap and
keep the fallback evidence narrow. Do not rename a server `[Fact]` as UI e2e.

---

## Rule: ui-evidence-scope

When the plan asks for Angular UI e2e, classify UI evidence before accepting a
completion claim:

```text
full_interactive_ui:
  Playwright drives and observes a running Angular 21 page/route in flows/FLOW-NN-*/client.

intermediate_ui:
  vitest component test, server [Fact], `ng build` success, or static scan.
```

Intermediate evidence is useful but cannot close a final UI e2e row. If the
Playwright path is unavailable in this slice, add the honest intermediate
artifact, record the remaining full-UI gap, and continue independent
plan-derived work.

## Rule: ux-evidence-scope

When UI/UX automation is requested, classify UX evidence separately from UI
execution evidence:

```text
ux_automation:
  Drives the real Angular surface AND asserts human-readable labels, accessible
  names/help text (aria-label, aria-describedby), examples, visible errors,
  layout/bounds, and no internal jargon on the primary path.

click_only_ui:
  Selects, types, clicks, and reads values without checking whether a human can
  understand the surface.
```

A Playwright test that only clicks is `click_only_ui`. To close a UI/UX row it
must also assert accessible names/roles (`getByRole`, `aria-label`) and visible
help/error text for the primary path.

---

## Anti-patterns

### Anti-pattern 1: Stub-fix masking
```
✗ Test fails because BundleValidatorService returns Failure("VALIDATION_FAILED", ...)
  → Stub validator changed to always return a success result
  → Test passes
  → VALIDATION_FAILED guard now never reached in the activation path

✓ Diagnose WHY VALIDATION_FAILED fires (empty bundleId)
  Provide a valid bundleId in the test setup
  Keep the validator strict — it should still fail empty input
```

### Anti-pattern 2: Guard stacking
```
✗ ActivateAsync checks reservation.IsFailure, then checks bundle.IsFailure
  on a path the first guard already rejected — second guard never reached

✓ Run branch-reachability for the combined guard chain
  Ensure each branch has at least one [Fact] input that reaches it
```

### Anti-pattern 3: Coverage inflation
```
✗ 5 new [Fact]s added, all using an empty InMemoryDocumentStore that yields no documents
  The CONCURRENT_UPDATE / TIMEOUT error paths are never exercised

✓ At least one [Fact] per service must drive an error path
  (stale ifMatch → CONCURRENT_UPDATE; simulateFabricTimeout → TIMEOUT)
```

### Anti-pattern 4: Sibling guard gap
```
✗ IsFailure guard added to BundleActivationService after a fabric call
  BundleStatusTrackerService calls the same store with no guard
  Bug: status tracker silently processes a failed read

✓ After adding a guard to any per-flow service:
  Audit all siblings in the flow that touch the same store/contract
  Apply the same guard consistently
```

### Anti-pattern 5: Component test reported as full UI automation
```
✗ vitest component spec passes
  → final report says full Angular UI e2e is complete
  → no running page/route was driven by Playwright

✓ Report vitest specs as intermediate component evidence
  Keep full-interactive UI automation open until a Playwright test drives and
  observes the real page/route
```
