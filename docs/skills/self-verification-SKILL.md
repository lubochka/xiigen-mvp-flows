# self-verification-SKILL.md — Six rules for verifying your own work before claiming correct

> **Stack adaptation (xiigen flows).** Universal six rules ported from
> `llm_mvp_core/docs/skills/self-verification-SKILL.md`. Adapted to this repo:
> - fresh fixtures use real flow services through DI (`ServiceCollection` / `WebApplicationFactory`),
>   not a mock of the service under test; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` on
>   `net10.0`;
> - cascade/caller discovery uses `rg` over the flow's projects and `dotnet build`;
> - generated/non-renderable C# is verified by parse (Roslyn / `dotnet build`); generated
>   Angular 21 output by `ng test` / render assertion;
> - the domain wrapper is `DataProcessResult<T>` via `MicroserviceBase<TSelf>` (`Success`/`Failure`)
>   — NOT core `OperationResult<T>`;
> - core ML/`ILearningMachine`/DPO specifics are core-only (G12) and out of scope here.

Applied after writing code, before claiming completion.

## Rule 1 — Fresh fixtures

Tests use real contracts, not hand-crafted fakes that happen to pass.

```csharp
// ✅ Real fixture — uses the actual Application service through DI with a real (or in-memory) store
var services = new ServiceCollection();
services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();   // real implementation, not a mock of the SUT
services.AddSingleton<UserRegistrationService>();
var sut = services.BuildServiceProvider().GetRequiredService<UserRegistrationService>();
var result = await sut.RegisterAsync(request, ifMatch: null, idempotencyKey: "k1");
Assert.True(result.IsSuccess);

// ❌ Hand-crafted fake of the thing under test — proves nothing
var mock = new Mock<UserRegistrationService>();
mock.Setup(x => x.RegisterAsync(...)).ReturnsAsync(DataProcessResult<...>);  // mocking the SUT
```

When in doubt, use the real implementation. Mocks are for testing callers, not for verifying
that the thing being called works. For Api-level coverage, use `WebApplicationFactory<TEntry>`
against the real pipeline.

## Rule 2 — Categorize the change

| Category | Definition | Verification required |
|----------|-----------|----------------------|
| `CONTRACT_CHANGE` | Changed a fixed interface/contract/endpoint shape | All callers + integration tests + contract update |
| `IMPLEMENTATION_CHANGE` | Changed a swappable implementation | That implementation's tests only |
| `BACKWARD_COMPAT` | Changed without breaking the contract | Contract test + regression suite |
| `NEW_ARTIFACT` | Added a new class/interface/component | Unit + integration + doc/contract sync |

Categorize first, then choose the right verification scope. Over-verifying wastes time;
under-verifying ships bugs.

## Rule 3 — Right level

| Change blast radius | Test level |
|--------------------|-----------|
| Single method, no callers changed | Unit test (`[Fact]` + direct construction) |
| Class with DI dependencies | Integration test (`ServiceCollection` + real DI) |
| Api endpoint behaviour | `WebApplicationFactory` integration test |
| Multiple projects in the flow affected | The flow's full Tests project (`dotnet test ...Tests`) |
| Public contract changed | All callers tested + the flow's full suite |
| Angular client behaviour | `ng test` (component) + Playwright (user journey) |

Do not run the whole flow suite to verify a private method; do not run only a unit test to
verify a contract change.

## Rule 4 — Cascade

Trace the full effect chain before declaring a fix complete.

```
Changed: UserRegistrationService.RegisterAsync()
  -> Used by: the Api registration endpoint (Program.cs / controller)
    -> Used by: the Angular client registration form (via the endpoint)
```

Find callers:
```bash
rg -rn "UserRegistrationService|RegisterAsync" flows/FLOW-NN-<slug>/server
dotnet build flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api   # confirms callers compile
```

Verify each caller's tests still pass after the change.

## Rule 5 — Non-renderable

Generated code cannot be verified by inspection alone.

For generated C#:
```csharp
var tree = CSharpSyntaxTree.ParseText(generatedCode);
Assert.Empty(tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
// or: dotnet build the project that contains it -> 0 Error(s)
```

For generated Angular templates/components: run `ng test` with a render assertion, e.g. assert
the expected text/marker appears and no unresolved `{{ }}` placeholder remains.

## Rule 6 — Predict, then verify

Before a change, state a prediction; after, verify it.

```
Before touching flows/FLOW-NN-<slug>/server/.../<Name>Service.cs:
1. Current test count: dotnet test ...Tests --filter "<X>" -> record N
2. Predicted outcome: the conflict-path test goes FAIL -> PASS
3. Predicted side effects: none — this method's only caller is the registration endpoint
4. Blast radius check: rg confirms callers = {registration endpoint only}
5. Risk: idempotency change may affect the happy path -> Mitigation: run the full flow suite

After the change:
1. Actual test count: dotnet test ...Tests -> N+1 or same
2. Prediction matched? YES / NO — if NO, explain delta
3. Side effects observed: none / [describe]
```

If prediction does not match outcome: investigate before claiming complete.

## Verification quick checklist

```
□ Categorized the change (CONTRACT/IMPLEMENTATION/BACKWARD_COMPAT/NEW_ARTIFACT)?
□ Identified blast radius — all callers in the chain (rg + dotnet build)?
□ Test level matches blast radius?
□ Fresh fixtures used (real service via DI, not a mock of the SUT)?
□ Generated code verified by parse / dotnet build / ng test, not just inspection?
□ Domain methods return DataProcessResult<T> (not OperationResult<T>) — verified by rg?
□ Pre-change prediction recorded?
□ Post-change outcome matches prediction?
□ dotnet test ...Tests shows Failed: 0 (and ng test / playwright where applicable)?
```
