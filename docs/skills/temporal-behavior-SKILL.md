# temporal-behavior-SKILL.md — One handler parameterized by phase, not a handler per phase (.NET 10)

> **Stack adaptation (xiigen flows).** Ported from
> `llm_mvp_core/docs/skills/temporal-behavior-SKILL.md` (G09). Universal core
> carries over unchanged: a service whose output changes with time-distance from an
> anchor event is **one handler parameterized by phase** (anchor → phase → weights →
> formula), **not** a separate handler per phase; **phase thresholds are FREEDOM
> config**, **formula structure is MACHINE**; and the **three iron rules**
> (`temporal_threshold_from_config`, `anchor_event_timestamp_present`,
> `phase_transition_uses_scheduler`). Stack mapping (verified against real flow code):
> - thresholds via **`IFreedomConfigService.GetAsync(key, ct)`** (FREEDOM); static
>   knobs via `IConfiguration`/`IOptions<T>` from `appsettings.json`.
> - phase transitions are scheduled via **`ISchedulerService.ScheduleDelayedAsync(string action,
>   TimeSpan delay, IReadOnlyDictionary<string,object?> payload, string idempotencyKey,
>   CancellationToken)`** — the real flow method (confirmed in
>   `EmailVerificationService.cs:198`). **The core skill's `ScheduleAt(...)` does NOT
>   exist in this stack** — use `ScheduleDelayedAsync` (or a hosted `BackgroundService`/
>   Hangfire) instead.
> - the anchor timestamp is a **`DateTimeOffset`** taken from the event/record
>   payload, never `DateTimeOffset.UtcNow` alone inside the formula.
> - compute returns `DataProcessResult<TemporalScore>` (XIIGen flow wrapper — **NOT**
>   core `OperationResult<T>`).
> - the IR-TEMPORAL-2 test is an **xUnit `[Fact]`**: two calls at different wall-clock
>   times with the same anchor ⇒ the same result.
> - **Gate-B** = `dotnet build Xiigen.Flows.slnx` (0/0) + `dotnet test` (Failed 0).

## When to load

- Before designing any scoring/ranking service whose behavior changes with time
  since a reference event.
- When a handler reads the current time but has no anchor-event timestamp.
- When separate task types are being created for "early" vs "approaching" vs "active"
  phases of the same concept.
- When phase thresholds are hardcoded instead of read from FREEDOM config.

---

## Core Problem

A service that computes different outputs at different times relative to an anchor is
not a SCHEDULED archetype (run once at a set time). It is one service whose formula
is parameterized by time-delta. Without explicit design these conflate: one approach
creates a separate task type per phase (over-decomposition); another polls
`DateTimeOffset.UtcNow` per request (non-reproducible, slow). This skill defines the
anchor → phase → weights → formula chain so one handler is correct across all phases.

---

## Section 1 — Identify the Anchor Event and Phases

```
Anchor event: the fixed reference point time is measured from.
  Examples in flows: an event's StartsAt; a registration CompletedAt; a token's
  IssuedAt; a subscription RenewsAt.

Phases: time ranges where behavior is qualitatively different.
  Phase A [creation → T-N]:   baseline
  Phase B [T-N → T-M]:        approach
  Phase C [T-M → event]:      imminent
  Phase D [event → end]:      active
  Phase E [end → T+K]:        decay
  Phase F [T+K → ∞]:          archive
```

Define only the phases that are qualitatively different — not merely a weight change
(that is a weight tweak, not a phase change).

---

## Section 2 — Classify Phase Behavior

For each phase, three entries:

```
Phase A (baseline):
  MACHINE constants: formula structure (which factors combine)
  FREEDOM config:    weighting.phaseA.matchWeight       (via IFreedomConfigService)
  Trigger: default when timeDelta > the Phase-B threshold from config

Phase B (approach):
  MACHINE constants: same factors as Phase A
  FREEDOM config:    weighting.phaseBThresholdDays = 7
  Trigger: ScheduleDelayedAsync fires at (anchor − thresholdDays) → emits
           "xiigen.<flow>.approach-phase-activated.v1"

Phase C (imminent):
  MACHINE constants: urgency formula (a NEW formula, not just new weights)
  FREEDOM config:    weighting.phaseC.proximityWeight
  Trigger: ScheduleDelayedAsync fires at (anchor − 1 day)
```

**Key rule:** phase thresholds are FREEDOM config (tenant-tunable). The per-phase
formula structure is a MACHINE constant (changes only with a deployment).

---

## Section 3 — State Machine Design

```csharp
// One handler for ALL phases — parameterized by phase.
public enum TemporalPhase { Baseline, Approach, Imminent, Active, Decay, Archive }

public sealed record TemporalContext(
    DateTimeOffset AnchorTimestamp,   // from the payload, NOT DateTimeOffset.UtcNow
    DateTimeOffset EvaluatedAt,       // current evaluation time
    TemporalPhase Phase,              // derived from timeDelta + FREEDOM thresholds
    TemporalWeights Weights);         // from FREEDOM config for this phase
```

**Do NOT create a separate handler per phase.** One handler reads the anchor
timestamp, computes the delta, determines the phase from FREEDOM thresholds, and
applies the phase-appropriate formula. Separate handlers per phase duplicate formula
logic and turn phase transitions into a routing problem instead of a config problem.

---

## Section 4 — Three Iron Rules for Temporal Services

```
IR-TEMPORAL-1: temporal_threshold_from_config
  Phase boundary thresholds read from FREEDOM config. Hardcoding "7 days" /
  "24 hours" as a literal violates this rule.
  Violation symptom: integer literals adjacent to TimeSpan / timeDelta comparisons
  in non-test code.

IR-TEMPORAL-2: anchor_event_timestamp_present
  Time-delta uses the anchor event's timestamp from the payload, not
  DateTimeOffset.UtcNow alone. Using only current time makes the service
  non-reproducible — two calls at different wall-clock times produce different
  results for the same event.
  Violation symptom: Compute(string input) where input carries no anchor timestamp;
  the method uses only DateTimeOffset.UtcNow.

IR-TEMPORAL-3: phase_transition_uses_scheduler
  Phase transitions are scheduled via ISchedulerService.ScheduleDelayedAsync (or a
  hosted BackgroundService/Hangfire), not detected by polling DateTimeOffset.UtcNow
  per request.
  Violation symptom: if (DateTimeOffset.UtcNow - anchor > threshold) inside the main
  execution path instead of in a scheduler registration.
```

---

## Section 5 — C# Pattern (flows)

```csharp
public sealed class TemporalScoringHandler : MicroserviceBase<TemporalScoringHandler>
{
    private readonly IFreedomConfigService _freedom;

    public async Task<DataProcessResult<TemporalScore>> ExecuteAsync(
        TemporalScoreRequest request, CancellationToken cancellationToken = default)
    {
        // IR-TEMPORAL-2: anchor timestamp comes from the payload.
        var anchor = request.AnchorTimestamp;
        var timeDelta = DateTimeOffset.UtcNow - anchor;

        // IR-TEMPORAL-1: thresholds from FREEDOM config.
        var approachDays = Convert.ToInt32((await _freedom.GetAsync("weighting.phaseBThresholdDays", cancellationToken)).Data?.Value ?? 7);
        var imminentHours = Convert.ToInt32((await _freedom.GetAsync("weighting.phaseCThresholdHours", cancellationToken)).Data?.Value ?? 24);

        var phase = DeterminePhase(timeDelta, approachDays, imminentHours);   // MACHINE
        var weights = await GetWeightsForPhaseAsync(phase, cancellationToken); // FREEDOM
        var score = ApplyFormula(request.Factors, weights, phase);            // MACHINE

        return Success(new TemporalScore(phase, score, weights, DateTimeOffset.UtcNow));
    }

    private static TemporalPhase DeterminePhase(TimeSpan delta, int approachDays, int imminentHours)
    {
        // MACHINE constant: the phase-determination logic.
        if (delta.TotalDays > approachDays) return TemporalPhase.Baseline;
        if (delta.TotalHours > imminentHours) return TemporalPhase.Approach;
        if (delta.TotalHours > 0) return TemporalPhase.Imminent;
        if (delta.TotalDays > -1) return TemporalPhase.Active;
        return TemporalPhase.Decay;
    }
}

// IR-TEMPORAL-3: phase transitions scheduled via the real scheduler method.
public sealed class TemporalPhaseScheduler : MicroserviceBase<TemporalPhaseScheduler>
{
    private readonly ISchedulerService _scheduler;
    private readonly IFreedomConfigService _freedom;

    public async Task<DataProcessResult<bool>> ScheduleTransitionsAsync(
        DateTimeOffset anchor, string entityId, CancellationToken cancellationToken = default)
    {
        var approachDays = Convert.ToInt32((await _freedom.GetAsync("weighting.phaseBThresholdDays", cancellationToken)).Data?.Value ?? 7);
        var delay = (anchor.AddDays(-approachDays)) - DateTimeOffset.UtcNow;   // until the approach boundary
        await _scheduler.ScheduleDelayedAsync(
            "temporal-approach-phase-activated",                               // action
            delay > TimeSpan.Zero ? delay : TimeSpan.Zero,                     // delay
            new Dictionary<string, object?> { ["entityId"] = entityId },      // payload
            $"approach-{entityId}",                                           // idempotencyKey
            cancellationToken).ConfigureAwait(false);
        return Success(true);
    }
}
```

(There is no `ScheduleAt(...)` in this stack — the real method is the 5-parameter
`ScheduleDelayedAsync` shown above. See `api-shape-verification-SKILL.md`.)

---

## Section 6 — IR-TEMPORAL-2 Reproducibility Test (xUnit)

```csharp
[Fact]
public async Task TemporalScore_SameAnchorDifferentClock_IsReproducible()
{
    var fixture = FlowFixture.Create();
    var anchor = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    var request = new TemporalScoreRequest(anchor, factors);

    var first  = (await fixture.Handler.ExecuteAsync(request)).Data!;
    // ... advance the test clock / call again later ...
    var second = (await fixture.Handler.ExecuteAsync(request)).Data!;

    // Same anchor + same factors ⇒ same phase and score (no DateTimeOffset.UtcNow-only leak).
    Assert.Equal(first.Phase, second.Phase);
    Assert.Equal(first.Score, second.Score, precision: 6);
}
```

---

## Anti-Patterns

1. **"I'll create a Phase A handler, a Phase B handler, ..."** — one handler
   parameterized by phase is correct. Separate handlers duplicate formula logic and
   make transitions a routing problem instead of a config problem.
2. **"`DateTimeOffset.UtcNow` is the only time I need"** — without the anchor
   timestamp, the same event scored at different wall-clock times gives different
   results. Temporal services are reproducible only when both timestamps are present.
3. **"7 days is a business constant, not config"** — thresholds are FREEDOM config;
   different tenants need different windows.
4. **"Schedule it with `ScheduleAt`"** — that method does not exist here; use
   `ScheduleDelayedAsync` (verify the signature first).

---

## Integration

- `algorithm-as-service-SKILL.md` Class 2 (Temporal Decay) shares these iron rules.
- `freedom-machine-SKILL.md` verifies threshold = FREEDOM, formula = MACHINE.
- `event-contract-design-SKILL.md` governs the phase-transition events emitted.
- `api-shape-verification-SKILL.md` confirms `ScheduleDelayedAsync`'s real signature.
