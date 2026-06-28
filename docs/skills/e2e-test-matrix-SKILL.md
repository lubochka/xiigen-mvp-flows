# e2e-test-matrix-SKILL.md — 8 mandatory scenario categories before code (xiigen flows)
## v1.0: ported from llm_mvp_core e2e-test-matrix → .NET 10 per-flow / xUnit + Angular 21

## Core Problem

A flow that has unit tests but no end-to-end scenarios has untested integration
paths. The E2E test matrix defines what the flow must do in concrete scenarios
**before code is written**. Without it, happy-path tests exist but idempotency,
compensation (LIFO), and time-based behaviour are uncovered.

---

## When to Invoke

- When designing tests for any flow phase
- When writing a phase plan for a new flow capability
- When a flow processes external events, timers, or multi-step operations
- Before Gate B is declared for any pipeline-stage capability

One test matrix before coding = no "tests pass but the time-based case is
untested" results.

---

## Section 1 — The 8 Mandatory Scenario Categories

Every flow capability needs all applicable categories. Mark N/A only with
justification.

### Category 1 — Happy Path (required, ≥1 per Api entry point)

Trigger → all expected outputs in sequence → terminal result.

```csharp
[Fact]
public async Task ActivateAsync_HappyPath_Returns201AndStores()
{
    var fixture = await FlowFixture.CreateAsync();
    using var _ = fixture.PushContext();
    var result = await fixture.Service.ActivateAsync(
        new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" }, "1", Guid.NewGuid().ToString("n"));
    Assert.True(result.IsSuccess, result.Error?.Message);
    Assert.Equal(201, result.Data?.HttpStatus);
    Assert.True(result.Data?.Stored);
}
```

### Category 2 — Idempotency (required for any capability with writes/state changes)

Same request + same idempotency key twice → result appears once, not doubled.

```csharp
[Fact]
public async Task ActivateAsync_SameKeyTwice_ReplaysNotDoubles()
{
    // second call returns the cached body and HttpStatus 200, store still has one activation
}
```

### Category 3 — Rate Limit / Quota (required for entry-point services with call limits)

(limit) requests succeed; (limit+1) returns a typed failure (e.g.
`result.Error?.Code == "RATE_LIMITED"`), not silent double-processing.

### Category 4 — Compensation Chain (required for multi-step operations with rollback)

Succeed through N steps → fail at step N+1 → assert steps 1…N-1 roll back in
**LIFO** order. Critical: LIFO order must be explicitly verified, not just
"rollback fired".

```csharp
[Fact]
public async Task Pipeline_FailsAtStep3_RollsBackStep2BeforeStep1()
{
    // Arrange: succeed steps 1-2, simulate step 3 failure
    // Assert: rollback fired for step 2 BEFORE step 1 (LIFO)
    //         result.IsFailure == true
}
```

### Category 5 — Time-Based / Delayed (REQUIRED for any capability with timers/TTLs)

When required: any timeout/TTL/SLA, any expiry check, any scheduled operation.
Inject a controllable clock — NEVER `Thread.Sleep`/`Task.Delay` with real time.

```csharp
[Fact]
public async Task Token_ExpiresAfterTtl_EmitsExpired()
{
    var clock = new MockDateTimeProvider();              // controllable
    // Arrange: create item with TTL = 1 hour using clock.UtcNow
    clock.AdvanceBy(TimeSpan.FromHours(1).Add(TimeSpan.FromSeconds(1)));
    // Act/Assert: DataProcessResult shows EXPIRED state
}
```

**Rule:** NEVER use `Thread.Sleep`/real-time delays. Always inject
`IDateTimeProvider`. (The FLOW-00 timeout case uses an explicit
`simulateFabricTimeout` flag rather than real waiting — same principle.)

### Category 6 — State Recovery (required for long-running / multi-step operations)

Execute through N steps → simulate interruption (new service instance, same
store) → resume → assert correct state, no double-execution.

```csharp
[Fact]
public async Task Flow_AfterInterruption_RestoresState()
{
    // Arrange: run N steps against a store, persist
    // Act: new service instance over the SAME InMemoryDocumentStore
    // Assert: recovered state correct; no step double-executed
}
```

### Category 7 — Boundary Assertions (required for cross-component contracts)

Invoke capability → read output → assert the consumer (downstream flow / Api
response) receives the exact expected data, not just `IsSuccess`.

```csharp
[Fact]
public async Task Output_MatchesConsumerExpectedShape()
{
    // Assert exact field values consumers read, e.g. result.Data?.Body["id"], HttpStatus
}
```

### Category 8 — Security Boundary (required for multi-tenant / access-controlled flows)

Request from the wrong tenant/context → typed failure
(`result.Error?.Code == "UNAUTHORIZED"`), no cross-tenant data leak. Flows push
a real `TenantContext`; assert isolation when the context differs.

```csharp
[Fact]
public async Task WrongTenant_ReturnsUnauthorized_NoLeak()
{
    // Assert result.IsFailure, result.Error?.Code == "UNAUTHORIZED"
    // Assert no data visible across the tenant boundary
}
```

---

## Section 2 — Test Matrix Format (write before any test)

```markdown
## Test Matrix — [FlowCapabilityName]

| Scenario | Category | virtualClock | Gate B |
|---|---|---|---|
| Happy path — valid bundleId | 1 (Happy Path) | No | dotnet test → 1 passed |
| Same key twice → single activation | 2 (Idempotency) | No | dotnet test → 1 passed |
| New instance, same store → resume | 6 (State Recovery) | No | dotnet test → 1 passed |
| Item expires after TTL | 5 (Time-Based) | Yes — inject MockDateTimeProvider | dotnet test → 1 passed |
| Wrong tenant context | 8 (Security) | No | dotnet test → 1 passed |

N/A categories with justification:
- Category 3 (Rate Limit): capability has no rate limit — N/A
- Category 4 (Compensation): single-step write — N/A
- Category 7 (Boundary): leaf node, no downstream consumer — N/A
```

For Angular 21 clients, the same matrix is realised as Playwright e2e specs:
Category 1 happy-path user flow, Category 8 auth boundary (redirected when
unauthenticated), and time-based UI via a mocked clock/network — never real
waits.

---

## Section 3 — Time-Based Test Protocol

```csharp
public interface IDateTimeProvider { DateTime UtcNow { get; } }

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;           // production: system clock
}

public sealed class MockDateTimeProvider : IDateTimeProvider
{
    private DateTime _current = DateTime.UtcNow;
    public DateTime UtcNow => _current;                  // tests: controllable
    public void AdvanceBy(TimeSpan duration) { _current += duration; }
}
```

---

## Anti-Patterns

1. **"Happy path passes, skip the rest"** — idempotency, compensation, and
   time-based failures are invisible from the happy path. Each category covers a
   distinct failure mode.
2. **"Use Thread.Sleep for time-based tests"** — real-time tests are slow,
   flaky, non-deterministic. Inject a controllable clock or an explicit
   simulate-flag.
3. **"Compensation is tested by the final state"** — LIFO order must be
   explicitly asserted; wrong order can corrupt data even if the final state
   looks right.
4. **"Category N/A without justification"** — every N/A needs a one-line reason;
   unlabeled N/A hides missing coverage.

---

## Integration

- user-journey-testing-SKILL.md provides scenario-first testing (scenarios before code)
- mvp-round-trip-verification-SKILL.md Step 5–6 verify Category 1, Step 7 verifies Category 7
- qa-session-type-SKILL.md uses this matrix as the acceptance baseline in its review
