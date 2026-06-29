# data-flow-integrity-SKILL.md — Producer→consumer schema diff across the whole chain

> **Stack adaptation (xiigen flows).** The anti-pattern "all classes pass
> individually but the pipeline fails", the 5-step protocol (build the data-flow
> map → extract the REAL schema from live code, not fixtures → schema-diff per
> producer→consumer step → classify the gap → integrity report), the five gap
> classes (MISSING_FIELD / RENAMED_FIELD / TYPE_MISMATCH / NULLABLE_GAP /
> NESTED_MISMATCH), and the end-to-end integration test on real DI (no mocks of
> the chain itself) are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/data-flow-integrity-SKILL.md` (G06 debugging /
> root-cause). Adapted here:
> - **Schemas** are `sealed record`s / DTOs in `*.Application` and `*.Infrastructure`
>   (e.g. `MarketplacePaymentsOutcome` / `…Dashboard` in
>   `flows/FLOW-16-marketplace-payments/server/…Application/MarketplacePaymentsResults.cs`);
>   the result wrapper is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`).
> - **Chains** are producer→consumer between flow stages, between the flow and the
>   kernel-fabric document body, and between the `.xiigen` / `flow.manifest.json`
>   contract and its consumer.
> - **The API↔Angular boundary is a chain link too:** the Angular 21 standalone
>   client's TypeScript interface must mirror the API contract DTO field-for-field
>   (casing included).
> - Live-schema extraction and field-usage search are `grep` over `flows/**/*.cs`
>   (and `flows/**/client/angular/src/**/*.ts` for the client DTO).
> - The e2e test runs on real `Xiigen.Fabrics.InMemory` DI (xUnit), not mocks.

## What This Skill Prevents

The "all classes pass individually but the pipeline fails" failure mode. Each
class tests with its own fixtures. But in the live chain, if a producer drops a
field from its output, every downstream consumer receives null/absent where it
expects a value — and all of them produce wrong results while their individual
unit tests still pass.

---

## Step 1 — Build the Data Flow Map

For each processing chain in the flow, document the data contract:

```
Chain: CheckoutAsync → IDocumentStore (write) → BuildDashboardAsync (read)

CheckoutAsync OUTPUT (MarketplacePaymentsOutcome):
  {
    HttpStatus: int,
    Body: IReadOnlyDictionary<string, object?>,   ← persisted to the document store
    StateChanged: bool,                            ← consumed by the host response
    OutboxRetained: bool                           ← consumed by dispatch logic
  }

BuildDashboardAsync EXPECTS (from the stored document body):
  platformFeeBps: ⚠️ must have been written by CheckoutAsync
  escrowState:    ✅ present
```

For the client link:
```
Chain: Dashboard API (MarketplacePaymentsDashboard) → Angular DashboardComponent

API DTO field 'kpis' (IReadOnlyDictionary<string,object?>)  ↔  Angular interface 'kpis: Record<string, unknown>'
```

---

## Step 2 — Extract Actual Schemas from Live Code

Do not trust fixtures. Read the actual record / class / interface definitions:

```bash
# Find the actual output type of a stage
grep -rn "record MarketplacePaymentsOutcome\|record MarketplacePaymentsDashboard" flows --include="*.cs"
# Then read the file to see all fields

# Find what a consuming stage actually reads from its input / the document body
grep -rn "\[\"platformFeeBps\"\]\|\.Body\|\.Kpis" flows --include="*.cs" | grep -v ".Tests"

# Find the Angular interface that mirrors the DTO
grep -rn "interface .*Dashboard\|kpis" flows/FLOW-NN-<slug>/client/angular/src --include="*.ts"
```

---

## Step 3 — Run the Schema Diff Per Chain Step

For each (producer, consumer) pair in the chain:

```bash
# Fields the consumer uses
grep -rn "input\.\|result\.\|body\[\|\.Value\." flows/FLOW-NN-<slug>/server/*.Application --include="*.cs" | head -20

# Fields the producer actually provides
grep -rn "public .*{ get;\|public .*=>" flows/FLOW-NN-<slug>/server/*.Application/*Results.cs | head -20

# Cross-reference: is every consumed property present in the producer's output?
```

Any property accessed by the consumer but not present in the producer's output =
DATA INTEGRITY GAP.

---

## Step 4 — Classify Each Gap

| Gap Type | Description | Fix |
|----------|-------------|-----|
| MISSING_FIELD | Producer omits a field the consumer requires | Add it to the producer's output record / document body |
| RENAMED_FIELD | Producer uses `Count`, consumer expects `TotalCount` | Align naming in one record |
| TYPE_MISMATCH | Producer emits `string`, consumer expects `int` | Fix in the producer (or the Angular interface type) |
| NULLABLE_GAP | Field present sometimes (nullable) but consumer expects non-null | Add a null guard in the consumer + document the contract |
| NESTED_MISMATCH | Producer flattens, consumer expects nested (or vice-versa) | Align structure |

---

## Step 5 — Produce the Integrity Report

```markdown
## Data Flow Integrity Report — [date] — FLOW-NN-<slug>

Chain: CheckoutAsync → IDocumentStore → BuildDashboardAsync → Dashboard API → Angular

| Link | Producer Field | Consumer Expects | Status |
|------|---------------|-----------------|--------|
| Checkout→store | Body["platformFeeBps"] | body["platformFeeBps"] | ❌ MISSING (legacy docs) |
| store→dashboard | escrowState | escrowState | ✅ MATCH |
| API→Angular | kpis (Record) | kpis: Record<string,unknown> | ✅ MATCH |

Gaps Found: 1 (MISSING_FIELD)
Fix Required: CheckoutAsync writes a resolved default platformFeeBps so the read path never sees an absent key

Verified By:
  grep -rn "\"platformFeeBps\"" flows/FLOW-16-marketplace-payments/server/*.Application → write-site absent on the default-fee branch (gap confirmed)
  grep -rn "body\[\"platformFeeBps\"\]" .../MarketplacePaymentsService.cs → read-site at line 212 (usage confirmed)
```

---

## Integration Test for Data Flow

After fixing any integrity gap, write an integration test that verifies the chain
end-to-end on real DI (no mocks of the chain itself):

```csharp
[Fact]
public async Task Checkout_Then_Dashboard_AllFieldsPropagate()
{
    // Arrange: real fabrics, real flow service — Xiigen.Fabrics.InMemory, no chain mocks
    var documents = new InMemoryDocumentStore();
    var service = BuildServiceWithRealFabrics(documents);

    // Act: run the full chain
    var checkout = await service.CheckoutAsync(request, ifMatch: null, idempotencyKey: "k1");
    var dashboard = await service.GetDashboardAsync(tenantId);

    // Assert: data made it through — no missing fee, no null propagation
    Assert.True(checkout.IsSuccess);
    Assert.True(dashboard.IsSuccess);
    Assert.True(dashboard.Value!.Kpis.ContainsKey("platformFeeBps")); // field propagated
    Assert.DoesNotContain(dashboard.Value!.Payments, p => p is null);  // no null propagation
}
```

---

## Anti-Patterns

1. **Trusting unit tests as proof of chain integrity** — fixtures/mocks hide the gaps
2. **Running the integrity check only on the first stage** — the gap is always downstream
3. **Treating nullable fields as "low priority"** — a null in a KPI/fee calculation produces 0 or NaN
4. **Running after the chain is in use** — run before any stage that depends on the chain is written
5. **Skipping the API↔Angular link** — a DTO/interface field-casing mismatch silently nulls the client view

---

## Integration

- `root-cause-ladder-SKILL.md` — a missing contract field is a Level 3 architectural gap
- `event-driven-debugging-SKILL.md` — event payload schema diff (Break Class C/D)
- `mental-debug-SKILL.md` Rule 4 (boundary-data-lifecycle)
- `data-connection-classification` / flow contract docs — the source of the declared schema
