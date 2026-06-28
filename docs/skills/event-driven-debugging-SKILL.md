# event-driven-debugging-SKILL.md — Define the chain, bisect, classify the break (A/B/C/D)

> **Stack adaptation (xiigen flows).** The core principle (a missing outcome
> means exactly one of: producer never published / no subscriber / wrong
> transformation / wrong read state), the "define the expected chain first" step,
> the binary-search-from-the-middle step, the four break classes A/B/C/D, the
> "verify the fix does not break adjacent chains" step, and the symptom→break-class
> table are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/event-driven-debugging-SKILL.md` (G06 debugging /
> root-cause). The transport is mapped to this repo:
> - flows uses the kernel **outbox** pattern (store-before-dispatch): events are
>   written to `IOutboxStore` (`Xiigen.Kernel`) inside the same operation that
>   mutates state, then dispatched; scheduled/recurring activation goes through
>   `ISchedulerService` (`Xiigen.Fabrics.Scheduler`). Some flows (e.g.
>   `FLOW-00-bundle-activation`) have explicit event activation; many do not.
> - Correlation is carried by `ICorrelationContextAccessor` (the
>   `correlationId` on the envelope), traced through `ILogger<T>`.
> - Class D ("read returns wrong state") = a read from `IDocumentStore` after the
>   write/dispatch — check the store, not the chain.
> - Chain tests are xUnit integration tests that capture published event types;
>   result type is `DataProcessResult<T>` (NOT core `OperationResult<T>`).
>
> **Applicability:** if a given flow has no event/outbox/scheduler activation,
> this skill is conditionally applicable — focus it on the outbox-dispatch and
> scheduler paths that the flow does have, or mark it NOT_APPLICABLE for that flow.

## What This Skill Prevents

Debugging a consumer when the event was never written to the outbox. Checking the
wrong handler when the real break is two hops downstream. Spending time on service
internals when the outbox/scheduler registration shows the break immediately.

---

## Core Principle

Every user-triggering action produces a predictable chain of events through the
outbox/scheduler. If the final outcome is missing, exactly one of these is true:

1. An event was never written to the outbox (producer bug)
2. An event was written but no subscriber/dispatch handler is registered (subscription bug)
3. A handler ran but produced the wrong downstream event/type (transformation bug)
4. All events dispatched but the read path returns the wrong state (read/query bug)

The debug session finds WHICH of the four is the case before touching any code.

---

## Step 1 — Define the Expected Chain

Before looking at any log or code, write out the expected chain:

```
User action: Marketplace checkout completed

Expected chain:
  1. CheckoutAsync writes a "PaymentCaptured" event to IOutboxStore (same op as the doc write)
  2. The outbox dispatcher delivers "PaymentCaptured" → escrow handler publishes "EscrowOpened"
  3. The escrow handler updates IDocumentStore → escrow document persisted
  4. ScheduledAutoRelease (ISchedulerService) fires after N days → "EscrowReleased"

Total: 3 events, 2 handlers, 1 scheduled job
```

---

## Step 2 — Find the Break Point (Binary Search)

Do not start at step 1. Start in the middle of the chain.

```csharp
// Add diagnostic tracing to the midpoint handler
public async Task<DataProcessResult<Unit>> HandleEscrowOpened(EscrowOpenedEvent evt, CancellationToken ct)
{
    // DEBUG: confirm this handler is reached, with the envelope correlation id
    _logger.LogDebug("DEBUG escrow handler: correlationId={Cid}, paymentId={Pid}",
        evt.CorrelationId, evt.PaymentId);
    // ... rest of handler
}
```

Run the scenario via an integration test:
```bash
dotnet test --filter "FullyQualifiedName~CheckoutChain" --logger "console;verbosity=detailed" 2>&1 | grep "DEBUG"
```

- If the midpoint debug line appears → the break is in steps 3–4 (after midpoint)
- If the midpoint debug line is absent → the break is in steps 1–2 (before midpoint)

Apply binary search recursively until the break is isolated to a single step.

---

## Step 3 — Classify the Break

Once isolated to one step (event N written, event N+1 absent):

### Break Class A — Event Written, No Subscriber/Dispatch Registered

The event exists in the outbox. No handler is registered for its type.

```bash
# Find handler/dispatch registrations for the event type
grep -rn "EscrowOpened\|AddOutboxHandler\|Subscribe\|services\." \
    flows/FLOW-NN-<slug>/server/*.Infrastructure flows/FLOW-NN-<slug>/server/*.Api --include="*.cs" | grep -i escrow
# Empty result = no handler registered in DI
```

Fix: register the handler/dispatch in the host / service-collection extension.

### Break Class B — Handler Registered, Throws Before Publishing

The subscription exists; the handler receives the event but fails before writing
the next event.

```csharp
public async Task<DataProcessResult<Unit>> HandleEscrowOpened(EscrowOpenedEvent evt, CancellationToken ct)
{
    try
    {
        // ... handler body
        return Success(Unit.Value);
    }
    catch (Exception ex)
    {
        // this catch reveals the throw we're hunting (do not leave it swallowing — Rule 6)
        _logger.LogError(ex, "DEBUG handler threw");
        return Failure<Unit>("HANDLER_EXCEPTION", ex.Message);
    }
}
```

Fix: `root-cause-tracing-SKILL.md` — find the throw, fix at the source.

### Break Class C — Handler Runs, Publishes the Wrong Event Type

The handler runs successfully; the next event appears but with the wrong type
name.

```bash
# Compare published type name to the subscribed type name
grep -rn "new .*Event\|WriteAsync\|EnqueueAsync" flows/FLOW-NN-<slug>/server --include="*.cs" | grep -i escrow
# Case / name mismatch between producer and consumer = Class C
```

Fix: align the published event type name with the subscriber's expected type.

### Break Class D — All Events Dispatched, Read Returns Wrong State

All events dispatched; the document write happened; but the read returns
incorrect data.

```csharp
// Verify the store state directly after the write/dispatch
var load = await _documents.GetAsync(escrowId, ct);
_logger.LogDebug("DEBUG state after dispatch: success={Ok}, body={Body}",
    load.IsSuccess, System.Text.Json.JsonSerializer.Serialize(load.Value));
// If the state is correct here but the caller sees stale data: cache or query filter issue
```

Fix: not a chain problem. Check the tenant/query filter or a caching layer.

---

## Step 4 — Verify Fix Does Not Break Adjacent Chains

After fixing the identified break, run the full chain test AND a second scenario:

```csharp
[Fact]
public async Task CheckoutChain_FullFlow_AllEventsDispatched()
{
    // Arrange: fresh in-process DI with real fabrics (Xiigen.Fabrics.InMemory)
    var sp = BuildRealServiceProvider();
    var service = sp.GetRequiredService<MarketplacePaymentsService>();
    var capture = new OutboxCapture(sp); // capture every written event type

    // Act
    var result = await service.CheckoutAsync(request, ifMatch: null, idempotencyKey: "k1");

    // Assert: all expected events were written (chain verification)
    Assert.True(result.IsSuccess);
    Assert.Contains("PaymentCaptured", capture.EventTypes);
    Assert.Contains("EscrowOpened", capture.EventTypes);
}
```

---

## Quick Reference: Break Class by Symptom

| Symptom | Break Class | First Check |
|---------|------------|-------------|
| Event N+1 absent, event N present in outbox | A or B | grep for handler/dispatch registration |
| Handler registered, event N+1 still absent | B | add log in handler, find the throw |
| Event N+1 present but wrong type | C | compare published vs subscribed type names |
| All events present but final read state wrong | D | log store state directly after dispatch |
| Event N itself absent from the outbox | Producer bug | add log in the producer before the outbox write |

---

## Anti-Patterns

1. **Starting by reading service internals before checking the chain** — check the chain first
2. **Checking a consumer when the producer never wrote to the outbox** — binary search before blame
3. **Adding more tests before isolating which step fails** — classification first, tests after
4. **Fixing the wrong handler because it's "nearby"** — the binary search locates the exact step

---

## Integration

- `root-cause-tracing-SKILL.md` — find the throw inside a Break-Class-B handler
- `data-flow-integrity-SKILL.md` — the event payload schema diff (Class C/D)
- `debug-session-SKILL.md` — persist the chain hypotheses across sessions
- `mental-debug-SKILL.md` Rule 6 (error-silent-swallow) for swallowed handler failures
