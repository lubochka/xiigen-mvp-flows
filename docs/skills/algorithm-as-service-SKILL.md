# algorithm-as-service-SKILL.md — No hardcoded weights; every algorithm is config-driven (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/algorithm-as-service-SKILL.md` (G09). Universal core
> carries over unchanged: the **5 algorithm classes** (Weighted Scoring, Temporal
> Decay, Recommendation, Matching, Gamification), the **iron rules** (violations
> score 0), and the **MACHINE constant test** (a config weight that differs from
> the default must change the output). Stack mapping:
> - weights/thresholds are read from **FREEDOM config via `IFreedomConfigService.GetAsync(key, ct)`**
>   (the flows analogue of core `IConfiguration.GetValue<double>`), seen in
>   `OnboardingDeliveryService.GetStringAsync` →
>   `_freedom.GetAsync(key, ct)`. `IConfiguration`/`IOptions<T>` from
>   `appsettings.json` is the acceptable alternative for non-FREEDOM static knobs.
> - compute methods return `DataProcessResult<T>` (XIIGen flow wrapper — **NOT**
>   core `OperationResult<T>`).
> - the MACHINE test is an **xUnit `[Fact]`**: set the FREEDOM key to a non-default
>   value through the flow's `FlowFixture`, assert the result differs from the
>   default-value result.
> - **Class 3 — Recommendation (adaptive ML) is NOT implemented in flows.** Training
>   and the common model stay in `llm_mvp_core` (R5/G12); a flow consumes a model
>   through a manifest/locator. A flow that trains locally fails this skill.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` of the
>   owning flow (Failed 0).

## When to load

- When designing any capability that computes a composite score, ranking, decay,
  similarity, or points/level.
- When a capability has `if/else` over numeric thresholds.
- Before writing any class with weights, decay rates, similarity scores, or point
  values.

One classification before implementation = no hardcoded weights, no R5 violation.

---

## Core Problem

Algorithms that hardcode scoring weights as `const` are not configurable by
operators. Algorithms that bypass the fabric (writing storage directly, or training
a model inside a flow) violate Fabric First and R5. This skill defines the 5
algorithm classes and the required iron rules, fabric usage, and test strategy for
each — adapted to the flows stack.

---

## Section 1 — The 5 Algorithm Classes

### Class 1 — Weighted Scoring (most common)

**Pattern:** N input signals × configurable weights → composite score in [0.0, 1.0].

**Fabric:** none (pure computation). **Config:** all weights via `IFreedomConfigService`.

**Iron rules (violations score 0):**
1. `weights_from_config` — every weight reads from FREEDOM config, never a literal decimal.
2. `weights_sum_to_one` — verify sum ≈ 1.0 at construction (±0.05 tolerance).
3. `score_range_zero_to_one` — output clamped to [0.0, 1.0].
4. `score_denominator_not_zero` — guard division when all signals are 0.

**FREEDOM config pattern (flows):**
```csharp
// ✅ CORRECT: weight from FREEDOM config
var matchWeight = Convert.ToDouble((await _freedom.GetAsync("scoring.search.matchWeight", ct)).Data?.Value ?? 0.25);

// ❌ WRONG: hardcoded weight (score-0 violation)
const double matchWeight = 0.25;
```

**MACHINE constant test (xUnit):**
```csharp
[Fact]
public async Task WeightedScorer_UsesConfigWeight_NotHardcodedValue()
{
    var fixture = FlowFixture.Create();
    fixture.Freedom.Set("scoring.search.matchWeight", 0.40); // default would be 0.25
    var score = (await fixture.Scorer.ComputeAsync(signals)).Data!.Value;
    // hardcoded ⇒ score uses 0.25 regardless; config-driven ⇒ score uses 0.40
    Assert.NotEqual(expectedWithDefault025, score);
}
```

### Class 2 — Temporal Decay

**Pattern:** score decreases as time since an anchor event grows.

**Fabric:** none (pure computation over timestamps). **Config:** decay rate / floor
/ boost via FREEDOM. **See also** `temporal-behavior-SKILL.md`.

**Iron rules:**
1. `decay_rate_from_config` — half-life/rate from FREEDOM config.
2. `decay_never_below_floor` — configurable floor enforced.
3. `decay_idempotent` — same inputs twice = same result.
4. `timestamp_not_datetime_now` — use the event's `DateTimeOffset` from the payload,
   never `DateTimeOffset.UtcNow` alone inside the decay formula.

### Class 3 — Recommendation (adaptive ML) — **consumed from core, not built in flows**

**Pattern:** weights UPDATE from feedback. **This is R5 territory.** Training, the
common model, and continue-training live in `llm_mvp_core`. A flow may only
**consume** a published model through a manifest/locator and apply its inference.

**Iron rules (flows side):**
1. `no_local_training` — no training loop, optimizer, or weight-update code in any
   `Xiigen.Flow.*` project.
2. `model_via_manifest` — the model is loaded through a manifest/locator boundary,
   not retrained here.
3. `no_ml_framework_import` — no `using Microsoft.ML;` / `using TorchSharp;` in flows.

If a flow plan says "the flow learns/adapts/trains," that is a hard fail — route it
to `llm_mvp_core` (G12).

### Class 4 — Matching (multi-criteria similarity)

**Pattern:** two entities → similarity across N dimensions.

**Fabric:** vector/term-frequency matching is consumed from core where it is a
trained capability; pure deterministic similarity (e.g. Jaccard over tags) may live
in a flow's `*.Application`.

**Iron rules:**
1. `match_score_symmetric` — `score(A,B) == score(B,A)`.
2. `match_score_bounded` — score ∈ [0.0, 1.0].
3. `match_weights_from_config` — dimension weights from FREEDOM config.
4. `match_never_self` — if `A.Id == B.Id`, return 0.0.

**Symmetry test (required, xUnit):**
```csharp
[Fact]
public void MatchingAlgorithm_IsSymmetric()
{
    var ab = _matcher.Compute(entityA, entityB).Data!.Value;
    var ba = _matcher.Compute(entityB, entityA).Data!.Value;
    Assert.InRange(Math.Abs(ab - ba), 0.0, 0.001);
}
```

### Class 5 — Gamification (points, levels, achievements)

**Pattern:** award points for events, derive level from totals, unlock achievements.
(FLOW-05-completion-gamification is the home flow.)

**Fabric:** points written through the store fabric (`IDocumentStore`) behind the
flow service; never a raw storage write from a static helper.

**Iron rules:**
1. `points_via_fabric` — points awarded through the service/store, not a direct write.
2. `points_idempotent` — same event + idempotency key = same points, never doubled
   (flows already carry idempotency keys, e.g. `idempotencyKey: "register-1"`).
3. `achievement_check_after_award` — achievement check runs after every award.
4. `level_derived_not_stored` — level computed from total points, never stored
   independently.

**FREEDOM config for point values:**
```csharp
var points = Convert.ToInt32((await _freedom.GetAsync($"gamification.points.{actionName}", ct)).Data?.Value ?? 10);
```

---

## Section 2 — Design Checklist for Any Algorithm Capability

Before the first line of implementation:

```
1. Classify the algorithm (Class 1–5).
2. Register all weight/rate keys in FREEDOM config (or appsettings.json for static knobs).
3. Add the class-appropriate iron rules.
4. Write the MACHINE constant test BEFORE the implementation
   (config-driven weight differs from the hardcoded default).
5. Define the success signal (what indicates a correct output).
6. If it looks like Class 3 (adaptive ML) → STOP, route to llm_mvp_core (R5/G12).
7. Verify weights sum to 1.0 for Class 1 at construction.
```

---

## Section 3 — FREEDOM Config Key Convention

Weights/rates use stable dotted keys read via `IFreedomConfigService.GetAsync(key, ct)`:

```
scoring.<domain>.<signal>Weight
decay.<domain>.halfLifeHours
decay.<domain>.floor
gamification.points.<action>
```

For static, non-tenant-tunable knobs, `appsettings.json` bound through
`IConfiguration`/`IOptions<T>` is acceptable; tenant-tunable weights belong in
FREEDOM.

---

## Anti-Patterns

1. **"Weight is a constant — it's the right value"** — today's right value is
   tomorrow's tuning request. All weights are FREEDOM config.
2. **"This flow can just learn from usage"** — any weight-updating/training code in
   a flow violates R5. Recommendation/ML stays in `llm_mvp_core`.
3. **"Matching is deterministic, no test needed"** — symmetry and self-match tests
   are structural and required.
4. **"The algorithm outputs the right value, so DNA is fine"** — every compute
   method still returns `DataProcessResult<T>`; raw returns fail Rule 3.

---

## Integration

- `temporal-behavior-SKILL.md` covers Class 2 phase/anchor design in depth.
- `freedom-machine-SKILL.md` verifies FREEDOM vs MACHINE per parameter.
- `dna-compliance-guard-SKILL.md` enforces no-hardcoded-config and the
  `DataProcessResult<T>` return at commit time.
