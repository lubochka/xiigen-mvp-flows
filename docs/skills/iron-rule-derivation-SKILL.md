# Iron Rule Derivation (xiigen flows, .NET 10 / Angular 21)

> **Stack adaptation (xiigen flows).** Greenfield. Ported from
> `llm_mvp_core/docs/skills/iron-rule-derivation-SKILL.md` (G11). The universal part
> carries over: the **derivation sequence** (list failure modes → find the invariant
> → express as `Rule N: [subject] MUST [verb] [condition] — [consequence]` →
> classify MACHINE/FREEDOM), the **four-check quality test**, and the rule that DNA
> rules are referenced, not re-derived per capability. Adapted here:
> - Subjects = **per-flow Application services** (`Xiigen.Flow.<Name>.Application`),
>   not core algorithm classes.
> - FREEDOM values are read via the injected **`IFreedomConfigService.GetAsync(key, ct)`**
>   → typed value, **never** raw `IConfiguration`/`appsettings.json` (`IConfiguration`
>   is 0× in flows `src`).
> - Each iron rule produces an **xUnit `[Fact]`** in the per-flow
>   `Xiigen.Flow.<Name>.Tests` project; Gate-B = `dotnet test` (Failed 0).
> - The DNA "no exception for business logic, return an explicit result" rule is
>   cited in the live flows stack: the domain wrapper is **`DataProcessResult<T>`**
>   (core `OperationResult<T>` is 0× in flows `src`; see `DNA-DOTNET-TRANSLATION.md`).
> - For Angular 21 standalone components, the analogue of an iron rule is a
>   component invariant verified by **Playwright** (frontend equivalent of the
>   `[Fact]`).

## Core Problem

A test that catches the wrong violation wastes cycles. A test that passes because
the rule it checks is vague produces false confidence. Iron rules — domain-specific
behavioral invariants — are only valuable if they are derived from specific failure
modes, stated without implementation detail, and carry a testable failure
consequence. This skill defines the derivation sequence that produces rules meeting
that standard.

---

## When to Invoke

- Before writing any new test class for a flow capability.
- After a flow's failure modes have been identified.
- When a test is failing for "obvious reasons" that have never been written down.
- When a new `I*` contract is designed for a flow Application service.

Iron rules derived before implementation = tests that catch the actual failure mode,
not incidental surface behaviors.

---

## Section 1 — The Derivation Sequence

For each new flow capability, apply these steps exactly.

### Step 1: List failure modes

Start with the capability's stated contract. Then ask:

```
- What happens if this operation runs twice? (idempotency)
- What if the document store succeeds but the outbox emit does not? (atomicity)
- What if a resource is replaced without releasing the old one? (lifecycle race)
- What if a caller gets data from the wrong tenant/scope? (isolation)
- What if a FREEDOM config value changes at runtime? (MACHINE vs FREEDOM)
- What if the operation completes but no one finds out? (observability gap)
- What if two callers request the same resource simultaneously? (concurrency)
```

Write each failure mode as a sentence:
"Caller invokes `PlaceBidAsync` twice with the same bidId, double-charging the bid."

### Step 2: For each failure mode, find the invariant

Ask: "What condition, if always true, would prevent this failure?"

```
Failure:    PlaceBidAsync called twice → double bid lock
Invariant:  Bid placement is idempotent — same bidId is accepted at most once
```

The invariant must be implementation-neutral. No C# types, no class names. It
describes what must be true, not how to make it true.

### Step 3: Express as an iron rule

Format:

```
Rule N: [subject] MUST [verb] [condition] — [failure consequence if violated]
```

The consequence clause is mandatory. A rule without a stated consequence cannot be
evaluated — a test cannot detect whether the rule is violated.

Examples (flows):

```
Rule 1: PlaceBidAsync MUST be idempotent — same bidId at time T1 and T2 produces one bid lock
        — violation double-charges the advertiser
Rule 2: Every document query MUST include the tenant/scope key
        — violation returns cross-tenant data to callers
Rule 3: DataProcessResult.Fail MUST be returned on any validation failure — never throw
        — violation breaks the DNA explicit-result contract silently at call sites
Rule 4: The auction window MUST be read from IFreedomConfigService — never hardcoded
        — violation hardcodes a value operators cannot change without redeployment
```

### Step 4: Classify as MACHINE or FREEDOM

**MACHINE:** the invariant must always hold; no configuration can relax it.
Test for presence. Never tenant-configurable.

Example: "store before outbox emit on every state transition" — partial-failure
recovery requires the outbox pattern, so it cannot be configurable.

**FREEDOM:** the mechanism enforcing the invariant is configurable. Read from
`IFreedomConfigService` with a safe default.

Example: the rule "rate-limit bid placement" is MACHINE, but the specific window
(10 req/min vs 100) is FREEDOM:

```csharp
// Application service — _freedom is the injected IFreedomConfigService
var window = await _freedom.GetAsync("AdsPlatform:BidRateLimitWindow", cancellationToken)
                           .ConfigureAwait(false);
// window.Value with a safe default if absent — NEVER read IConfiguration / appsettings.json
```

Annotate each rule:

```
Rule 4: Auction window MUST be read from IFreedomConfigService        [MACHINE: rule]
        Key: "AdsPlatform:AuctionWindowSeconds" default 30            [FREEDOM: value]
```

---

## Section 2 — The Quality Test

A well-derived iron rule passes all four checks:

```
□ Can this rule be violated without the system throwing an exception?
  If YES → the rule is important (silent violations are the dangerous ones)
  If NO  → it is already enforced structurally; does not need an iron rule

□ Is the rule expressed without C# class names or implementation detail?
  If NO  → rewrite: "store before emit" not "await _documents.SetAsync() before _outbox.EnqueueAsync()"

□ Does the rule have a testable failure consequence?
  If NO  → add the consequence: what breaks if the rule is violated?

□ Does the rule derive from a specific failure mode?
  If NO  → trace it back: which failure does this prevent? If unanswerable,
           the rule may not apply to this flow
```

---

## Section 3 — Rules That Come From DNA

The DNA rules apply universally (see `DNA-DOTNET-TRANSLATION.md`). Do not repeat them
as per-capability iron rules. Reference them instead:

```
DNA "explicit result" applies: return DataProcessResult.Fail, never throw for business logic
→ State in test: "verifies DNA explicit-result compliance"
→ Do NOT write a new flow-specific iron rule for this — it is already universal

DNA "fabric first" applies: resolve through interface, not concrete type
→ State in constructor parameter type: verify via test that the injected type is an interface
```

Domain-specific rules are the ones that require derivation. Universal rules are DNA.

---

## Section 4 — Example Derivation

**Capability:** `AdsPlatformService.PlaceBidAsync` (FLOW-20)

| Failure mode | Invariant | Iron rule | Class |
|---|---|---|---|
| Same bidId placed twice → double lock | Placement is idempotent | Rule 1: same bidId accepted at most once — violation double-charges | MACHINE |
| Window changes without a config key | Window is configurable | Rule 2: auction window MUST come from `IFreedomConfigService` — hardcoding breaks operator tuning | MACHINE: rule, FREEDOM: value |
| Cross-tenant bids returned | Tenant scope on every call | Rule 3: every document query MUST include the scope key — violation returns cross-tenant data | MACHINE |
| Empty bid list throws | Graceful empty handling | Rule 4: empty input MUST return `DataProcessResult.Ok` with an empty result — never throw | MACHINE |

**Result: 4 iron rules from 4 distinct failure modes.** None copied from a template.

---

## Section 5 — Iron Rule Test Pattern

Each iron rule produces a `[Fact]` test in `Xiigen.Flow.<Name>.Tests`:

```csharp
[Fact]
public async Task PlaceBidAsync_SameBidIdTwice_LocksOnce()
{
    // Verifies Rule 1: idempotency
    var first  = await _service.PlaceBidAsync(SameBid, CancellationToken.None);
    var second = await _service.PlaceBidAsync(SameBid, CancellationToken.None);

    Assert.True(first.IsSuccess);                 // first accepted
    Assert.True(second.IsSuccess);                // second is a no-op, not a failure
    Assert.Equal(1, await CountBidLocksAsync(SameBid.BidId)); // exactly one lock
}

[Fact]
public async Task AuctionWindow_FromFreedom_NotHardcoded()
{
    // Verifies Rule 2: FREEDOM config via IFreedomConfigService (never IConfiguration)
    _freedom.Set("AdsPlatform:AuctionWindowSeconds", "90");
    var result = await _service.GetAuctionWindowAsync(CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(90, result.Value); // configurable value is honored
}
```

Frontend analogue (Angular 21 standalone component invariant), verified by Playwright:

```
Invariant: the bid form MUST disable Submit while a placement is in flight
           — violation lets a double-click create two bids
Playwright: click Submit, assert button is disabled until the response resolves
```

---

## Anti-Patterns

1. **"Must handle errors correctly"** — not a rule. What specific error? What handling?
   Correct: "MUST return `DataProcessResult.Fail('EMPTY_INPUT')` when the input list is empty."
2. **Copying iron rules from another flow** — different flows have different failure
   modes. Derive from this flow's failures.
3. **Listing DNA rules as iron rules** — "MUST return `DataProcessResult`" is universal.
   Reference DNA instead.
4. **Iron rules without failure consequence** — every rule must state what breaks.
5. **Implementation-specific rules** — "MUST call `_documents.SetAsync()` before
   `_outbox.EnqueueAsync()`" is a C# detail. Correct: "MUST persist state before emitting events."
6. **Reading config from `IConfiguration`/`appsettings.json`** — flows reads FREEDOM
   via `IFreedomConfigService.GetAsync` (raw `IConfiguration` is 0× in flows `src`).

---

## Integration

- `archetype-failure-map-SKILL.md` produces the failure maps whose PATCH-class
  failures are fixed by iron rules derived here.
- `test-integrity-SKILL.md` ensures the `[Fact]` asserts a domain outcome, not just
  `result.IsSuccess`.
- `dna-compliance-guard-SKILL.md` / `DNA-DOTNET-TRANSLATION.md` own the universal DNA
  rules referenced (not re-derived) here.
- `audit-protocol-SKILL.md` Point 7 checks that each iron-rule Gate-B has an exact
  `dotnet test` count.
