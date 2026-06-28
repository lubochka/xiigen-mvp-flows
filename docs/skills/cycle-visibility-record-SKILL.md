# cycle-visibility-record-SKILL.md — A cycle with no visibility record is a cycle that never ran

> **Stack adaptation (xiigen flows).** Only the **universal honesty core** of
> `llm_mvp_core/docs/skills/cycle-visibility-record-SKILL.md` is ported: count-honesty
> (`N ledger rows != N cycles`) and the rule that every review/fix cycle leaves an inspectable
> before/after record with a non-empty result-delta. The core 4-field
> SENT/RECEIVED(A/B/C models)/DECIDED(arbiter verdict)/CHANGED(DomainModelRegistry/training-index)
> form is multi-model-arbiter ML machinery and is **NOT ported** (G12, R5 — common models live
> in `llm_mvp_core`; flows consume them via `.xiigen`/wiki-domain manifests/locators). Adapted
> to this repo:
> - the review/fix record is a simple `record` whose "non-empty result" check returns
>   `DataProcessResult<Unit>` via `MicroserviceBase<TSelf>` (`Success`/`Failure`) — NOT core
>   `OperationResult<T>`;
> - records are persisted through a flow service/store on `net10.0`; the cycle's reasoning cites
>   a flow-contract / Gate-B criterion.

## When to load
- When the active plan or Luba requires several/N review or fix cycles.
- Before claiming any exact count of cycles/reviews/passes.
- When a status report needs auditable evidence that the cycles actually happened.

## Section 1 — The governing rule

```
A cycle with no visibility record is a cycle that never ran.
Its output cannot be trusted and its learning cannot be captured.
```

Every review/fix cycle must produce one inspectable record with a non-empty result-delta. An
empty result field is a failed record.

**Count honesty rule:** a row in a ledger, checklist, generated table, or summary is not a
cycle. A claim such as "10 cycles completed" is valid only when there are 10 independently
inspectable before/after review/fix records. If they do not exist, the honest claim is "10
ledger rows exist; 10 cycles are not proven."

## Section 2 — Required fields per review/fix record

```text
cycleId            unique id for this cycle
target             exact artifact/section reviewed (file:line or contract/test name)
reviewerOrAgentId  reviewer or sub-agent id (parent self-review is labelled as such)
loadedSkillsGates  skills/gates applied this cycle
findings           what review saw, OR an explicit no-fix justification
beforeRef          artifact state before (commit/path/diff ref)
afterRef           artifact state after (commit/path/diff ref) — non-empty
recheckEvidence    the command/output proving the fix (e.g. dotnet test ...Tests -> Failed: 0)
resultDelta        what changed; "nothing changed — <reason>" is valid ONLY when stated explicitly
```

`resultDelta` empty without a statement fails: silence cannot distinguish "nothing changed"
from "the field was forgotten". The cycle's `findings`/`resultDelta` reasoning must cite a
concrete flow criterion — a contract rule, a DataProcessResult outcome, or a Gate-B result.

## Section 3 — Record + non-empty validator (flows wrapper)

```csharp
public sealed record ReviewFixCycleRecord
{
    public required string CycleId { get; init; }
    public required string Target { get; init; }
    public required string ReviewerOrAgentId { get; init; }
    public required IReadOnlyList<string> LoadedSkillsGates { get; init; }
    public required string Findings { get; init; }
    public required string BeforeRef { get; init; }
    public required string AfterRef { get; init; }
    public required string RecheckEvidence { get; init; }
    public bool NothingChanged { get; init; }
    public string? NothingChangedReason { get; init; }
    public string? ResultDelta { get; init; }
}

// Validator lives in an Application service (MicroserviceBase<TSelf>) and returns the
// XIIGen family wrapper DataProcessResult<Unit> via Success/Failure — NOT core OperationResult<T>.
public DataProcessResult<Unit> ValidateCycle(ReviewFixCycleRecord r)
{
    if (string.IsNullOrWhiteSpace(r.Target))
        return Failure<Unit>("CYCLE_TARGET_MISSING", "Cycle target is required");
    if (string.IsNullOrWhiteSpace(r.AfterRef))
        return Failure<Unit>("CYCLE_AFTER_MISSING", "Cycle after-ref is required");
    if (string.IsNullOrWhiteSpace(r.RecheckEvidence))
        return Failure<Unit>("CYCLE_RECHECK_MISSING", "Cycle recheck evidence is required");
    if (r.NothingChanged && string.IsNullOrWhiteSpace(r.NothingChangedReason))
        return Failure<Unit>("CYCLE_NOTHING_CHANGED_NO_REASON", "NothingChanged requires an explicit reason");
    if (!r.NothingChanged && string.IsNullOrWhiteSpace(r.ResultDelta))
        return Failure<Unit>("CYCLE_RESULT_DELTA_MISSING", "Non-empty result delta is required");
    return Success(Unit.Value);
}
```

## Section 4 — Count claim gate

Before reporting "N cycles / N reviews / N passes completed":

```text
count_claimed:
independent_record_count:        // validated ReviewFixCycleRecord entries
ledger_rows_or_summary_only:
allowed_wording:
```

If `independent_record_count < count_claimed`, the honest wording is "N ledger rows exist;
fewer cycles are proven" — say the narrow truth first. Parent self-review and direct parent
repair are labelled as such; they are not independent sub-agent passes.

## Anti-patterns

1. "Result field empty when nothing changed" — empty differs from explicitly stating nothing
   changed with a reason; the validator rejects empty.
2. "10 ledger rows means 10 cycles" — rows can be generated without an independent before/after
   pass; this is the exact count-honesty loophole.
3. "Record references another document instead of the values" — `beforeRef`/`afterRef`/
   `recheckEvidence` must point to concrete states/output, not a vague pointer.
4. Importing the core arbiter form (A/B/C model outputs, DomainModelRegistry/training-index
   updates) — that is ML machinery owned by `llm_mvp_core` (G12); it is not required here.
