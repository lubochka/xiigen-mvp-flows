# claim-as-hypothesis-SKILL.md — A user assertion about existing state is a hypothesis

> **Stack adaptation (xiigen flows).** Universal claim-as-hypothesis protocol ported from
> `llm_mvp_core/docs/skills/claim-as-hypothesis-SKILL.md`. Adapted to this repo:
> - verification actions target `net10.0` per-flow projects and Angular 21 clients; verified
>   per-flow shape `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - "service wired" is verified against DI registration in the flow's `Program.cs`/module;
>   "interface implemented" is verified by `rg` for unimplemented markers returning 0;
> - domain wrapper is `DataProcessResult<T>` via `MicroserviceBase<TSelf>` — NOT core
>   `OperationResult<T>`;
> - manifest / `.xiigen` / wiki-domain claims are verified by **load through the locator**, not
>   file presence;
> - core ML/`ILearningMachine`/DPO specifics are core-only (G12) and out of scope here.

## When to load
- At session start — capture every user assertion about existing state.
- When a new claim appears during the session (trigger phrases below).
- Before any plan step that depends on "X exists / works / is done".
- At every STOP — verify no BLOCKING claim is still PENDING.

Zero pending BLOCKING claims at STOP = zero "built on sand" execution failures.

## Section 1 — Trigger phrases

| Pattern | Example | Why it matters |
|---|---|---|
| Existence | "that service is already in the flow" | Service may be a stub returning `Failure<T>("NOT_IMPLEMENTED", ...)` |
| Completion | "the contract is done" | Contract file may exist but have empty endpoints |
| Operational | "that works" / "already works" | Test may be missing or skipped |
| Implementation | "the service is implemented" | Class may exist but methods throw / return no-op |
| Wiring | "X is wired to Y" | DI registration may be missing in `Program.cs` |
| Readiness | "it's ready" | Undefined — ready means different things |

Phrases that do NOT produce claim entries: goals ("I want X"), preferences ("I prefer Y"),
past decisions ("we decided to use X").

## Section 2 — Claim record schema

```json
{
  "claims": [
    {
      "id": "C1",
      "statement": "[verbatim user assertion — exact quote]",
      "capturedAt": "session start / turn N",
      "status": "PENDING",
      "importance": "BLOCKING | INFORMATIONAL",
      "verificationAction": "[specific rg/Test-Path/dotnet test command]",
      "verdict": null,
      "evidence": null
    }
  ]
}
```

Status: **PENDING** (queued, not run), **VERIFIED** (evidence matches), **DISCONFIRMED**
(evidence contradicts), **PARTIAL** (matches a subset only — most common), **DEFERRED** (only
Luba can defer; recorded with reason). Importance: **BLOCKING** (wrong claim changes the plan
fundamentally — cannot STOP without resolution) vs **INFORMATIONAL** (plan still holds).

## Section 3 — Verification protocol (a read action, not reasoning)

| Claim shape | Verification action (flows) |
|---|---|
| "X file exists" | `Test-Path` / Glob — record presence/absence |
| "X contains Y" | `rg -n [pattern] [path]` — record hit count + first matches |
| "interface is implemented" | `rg -n "NotImplementedException\|NOT_IMPLEMENTED\|TODO" [file]` — should be 0 hits |
| "service is wired" | `rg -n "[ServiceName]" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api/Program.cs` — should be ≥1 hit |
| "tests pass" | `dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --filter "[filter]"` — record actual count |
| "method exists" | `rg -n "[MethodName]" [file]` — record presence/absence |
| "client renders X" | `ng test --watch=false` / `npx playwright test` — record result |
| "manifest is available" | load it through the locator/runtime — record load result, not file presence |

What does NOT count as verification: "I know X likely exists", "a prior session mentioned X",
"the path suggests X", or the user confirming their own claim. The only valid verification: a
specific command, a specific observable result, recorded verbatim.

## Section 4 — Gate enforcement

**Before first synthesis:** every assertion captured PENDING; BLOCKING claims have a queued
verification action; any synthesis depending on a claim references it by id (C1, C2, ...).

**Before STOP:** no BLOCKING claim remains PENDING; every plan assumption that depends on a
claim has the verdict integrated; DEFERRED claims carry a carry-forward flag.

**Deferral rule:** only Luba can defer a claim; the session cannot self-defer.

```json
{ "status": "DEFERRED", "deferredBy": "LUBA", "deferralReason": "[what would be needed to verify]", "carryForwardFlag": true }
```

## Section 5 — Worked examples

**PARTIAL — "the contract is done"**
```
Test-Path flows/FLOW-NN-<slug>/contracts/endpoints/<x>.json  -> exists
rg -n "\"path\"" flows/FLOW-NN-<slug>/contracts/endpoints/<x>.json  -> endpoints listed
rg -n "<Name>Service" flows/FLOW-NN-<slug>/server/.../<Name>Service.cs  -> 0 hits (not implemented)
```
Verdict: PARTIAL — contract exists, no implementing service. Plan must include the
implementation task, not "use the existing contract".

**DISCONFIRMED — "the service is wired"**
```
rg -n "<Name>Service" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api/Program.cs  -> 0 hits
```
Verdict: DISCONFIRMED — DI registration not present. "Wiring is done" becomes a task.

**VERIFIED — "the interface exists"**
```
rg -rn "interface I<Name>" flows/FLOW-NN-<slug>/server  -> found at .../I<Name>.cs:5
```
Verdict: VERIFIED — interface defined at expected location.

## Anti-patterns

1. "The user confirmed it" — circular; the user confirming their own claim does not verify it.
2. "It's obviously true" — obviousness is memory, not evidence.
3. "Skip verification to save time" — a 30-second `rg` prevents a 3-round plan failure.
4. "PARTIAL is close enough to VERIFIED" — PARTIAL surfaces the gap that fails during execution.
5. "Verify once, assume forever" — codebase state changes; re-verify each session.
