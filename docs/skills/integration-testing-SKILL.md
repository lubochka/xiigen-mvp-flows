# integration-testing-SKILL.md — In-process integration testing for .NET flows (xiigen flows)
## v1.0: ported from llm_mvp_core integration-testing → .NET 10 per-flow / xUnit

## When to load
Load before writing any test that exercises more than one class together.
Load when a unit test uses stubs for a per-flow `.Application`/`.Infrastructure` service.
Load when a phase gate requires fabric coverage.

---

## The problem this skill solves

xiigen mvp uses containers (Elasticsearch/Redis) plus a FastAPI RAG sidecar for
integration tests. xiigen flows has no container layer for server tests: every
fabric — `InMemoryDocumentStore`, `InMemoryOutboxStore`, `InMemoryIdempotencyStore`,
`InMemoryFlowBootstrapper`, the context accessors — is pure in-process .NET 10.

This means integration tests are simpler: create real instances directly in the
test body. No containers, no sidecar services, no network. Each `[Fact]` creates
its own fresh instances with no shared state. ".NET GC" replaces
`docker compose down`.

---

## What "integration test" means in xiigen flows

A test that wires two or more real implementations together and exercises a
cross-class code path, with no stubs on the interfaces directly involved in the
behaviour being tested.

The per-flow xUnit project (`Xiigen.Flow.<Name>.Tests.csproj`) is the test host.
Every integration test is a `[Fact]` or `[Theory]` in that project. The universal
rule is unchanged from core: ≥2 real implementations on the tested path, a real
input, a real output assertion, fresh instance per test, no shared/static state.

---

## Fabric coverage matrix

Every fabric / per-flow service must have at least one integration test that
exercises it with real inputs and verifies real output. This replaces the Docker
"container up" coverage check from mvp.

### Per-flow fabric (Xiigen.Fabrics.InMemory)

| Implementation | Must test | Assertion |
|----------------|-----------|-----------|
| `InMemoryDocumentStore` | Upsert then Get a document | `Assert.NotNull(get.Data)` |
| `InMemoryOutboxStore` | Append then List; event present | `Assert.Contains(..., r => r.Event.Type == "bundle.activated")` |
| `InMemoryIdempotencyStore` | Same key twice → replay | second call `HttpStatus == 200` |
| `InMemoryFlowBootstrapper` | Records DRY_RUN before FULL per flow | `Assert.Equal(expectedCalls, bootstrapper.Calls)` |

### Per-flow services (.Application)

| Implementation | Must test | Assertion |
|----------------|-----------|-----------|
| `<Flow>Service` happy path | Valid request → success outcome | `Assert.True(result.IsSuccess, result.Error?.Message)` |
| `<Flow>ValidatorService` | Invalid input → typed failure, no partial write | `Assert.Equal("VALIDATION_FAILED", result.Error?.Code)` |
| `<Flow>StatusTrackerService` | Fan-out over consumers | `Assert.Equal(expected, result.Data)` |

(`<Flow>` is the concrete flow, e.g. `BundleActivation`.)

---

## Test structure pattern

```csharp
// Integration test — no stubs on the services/fabric being tested
[Fact]
public async Task ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey()
{
    var fixture = await FlowFixture.CreateAsync();   // real fabric, seeded documents
    using var _ = fixture.PushContext();             // real tenant + correlation context
    var key = Guid.NewGuid().ToString("n");

    var first  = await fixture.Service.ActivateAsync(new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" }, "1", key);
    var second = await fixture.Service.ActivateAsync(new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" }, "1", key);

    Assert.True(first.IsSuccess, first.Error?.Message);
    Assert.True(second.IsSuccess, second.Error?.Message);
    Assert.Equal(201, first.Data?.HttpStatus);       // real fabric → real idempotency replay
    Assert.Equal(200, second.Data?.HttpStatus);
}
```

A reusable in-test fixture (see the FLOW-00 `FlowFixture.CreateAsync()` pattern)
constructs the real services with `new ...()`, seeds documents, and exposes a
`PushContext()` that pushes a real `TenantContext` + `CorrelationContext`.

---

## What replaces Docker Compose setup/teardown

| Docker concept | xiigen flows equivalent |
|----------------|-------------------------|
| `docker compose up -d` before test run | Each `[Fact]` constructs its own instances via `new ...()` / `FlowFixture.CreateAsync()` |
| Container isolation (clean state per test) | `new InMemoryDocumentStore()` per test — no shared state |
| `docker compose down` after run | .NET GC — no explicit teardown |
| `docker logs` for debugging | `result.Error?.Code` + `result.Error?.Message` from `DataProcessResult<T>` |
| Health check before tests | Build step: `dotnet build` must succeed before any test runs |

---

## Stubs that are allowed in integration tests

Stubs are allowed for components that are NOT part of the behaviour under test.

| Interface | Allowed stub | When |
|-----------|-------------|------|
| `ICodeReader` | in-memory reader | Testing a service without filesystem I/O |
| `ILlmClient` / RAG client | Mock | Testing flow orchestration, not model accuracy |
| External HTTP/3rd-party SDK | Mock/fake | When the third party is not the tested contract |

Stubs are NOT allowed for:
- The per-flow `.Application` service under test
- The per-flow `.Infrastructure` service under test
- The in-memory fabric (`InMemoryDocumentStore`/`InMemoryOutboxStore`/...) when it is the tested path

---

## Gate B format for integration tests

Every plan phase that includes integration tests must have a Gate B in the form:

```
Gate B: dotnet test --filter "FullyQualifiedName~<ClassName>" exits 0;
        <ClassName>.cs contains [Fact] covering <specific behaviour>
                (≥2 real services/fabric on the tested path);
        test count >= <N> (was <M>)
```

---

## Anti-patterns

### Anti-pattern 1: Stub replacing the thing under test
```
✗ Testing the activation service with a stub document store that always returns empty
  The service never exercises its store-then-outbox logic — test is vacuous

✓ Provide a real InMemoryDocumentStore + InMemoryOutboxStore
  Only stub ILlmClient / external SDKs if those are not under test
```

### Anti-pattern 2: Shared state between tests
```
✗ static InMemoryDocumentStore _shared = new();  // polluted by earlier tests
  Test results depend on run order — flaky

✓ Create a fresh store (or FlowFixture.CreateAsync()) per [Fact]
  No static fields in test classes
```

### Anti-pattern 3: Testing only the happy path
```
✗ Only test ActivateAsync succeeds — never test stale ifMatch or simulated timeout

✓ At least one [Fact] per service must exercise an error/empty path
  e.g. stale ifMatch → CONCURRENT_UPDATE; simulateFabricTimeout → TIMEOUT;
       empty bundleId → VALIDATION_FAILED
```
