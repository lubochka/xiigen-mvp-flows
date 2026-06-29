# chain-arithmetic-audit-SKILL.md — baseline + Σ(deltas) = final must converge

> **Stack adaptation (xiigen flows).** Universal chain-arithmetic audit ported from
> `llm_mvp_core/docs/skills/chain-arithmetic-audit-SKILL.md`. Adapted to this repo:
> - the authoritative "after" anchor is a fresh per-flow `dotnet test` (xUnit, `net10.0`) count
>   and/or `ng test`/`npx playwright test` count; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - count references are extracted from this repo's `STATE-*.json` / session / phase files under
>   `docs/`;
> - core ML metric chains (DPO/accuracy/loss) are core-only (G12) and out of scope here.

`claim-verification-SKILL.md` checks single-document consistency; this skill checks end-to-end
chain math: `baseline + Σ(phase deltas) = final` for every tracked metric across sessions/phases.

## When to load
- At session end when writing a summary — verify "Before" equals the previous session's "After".
- Any time a plan spans multiple sessions/phases with incremental counts.
- Whenever a test count has been reported across sessions.
- Before claiming "N tests passing" — verify the number chains back to baseline.

## Rules

1. Extract all count references from `STATE-*.json` and every session/phase file.
2. Build a directed chain: `baseline -> phase1 delta -> phase2 delta -> ... -> final`.
3. Verify every edge: `node[n].after == node[n+1].before`.
4. Verify the terminal: `baseline + Σ(all deltas) == final_expected`.
5. Any gap or mismatch = **CHAIN ARITHMETIC FAIL** with exact location and value.

Every declared gain/drop must have a numeric source (a `dotnet test`/`ng test`/Playwright run),
not an estimate. If the declared final differs from `baseline + Σ(deltas)`, the audit requires a
recount; it does not paper over the gap.

## Detection commands

```bash
# Authoritative "after" anchor for the touched flow
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests --no-build 2>&1 | tail -3

# Whole-repo build/test anchor when the chain spans flows
dotnet build Xiigen.Flows.slnx 2>&1 | tail -3

# Cross-check against what state files record
rg -n "testCount|passed|baseline|\"before\"|\"after\"|\"delta\"" docs/STATE-*.json
```

## Verification table format

| Transition | Before | Delta | After | Verified |
|-----------|--------|-------|-------|---------|
| Start of phase 2 (FLOW-01) | 12 | +8 | 20 | ✅ |
| Phase 2 -> phase 3 | 20 | +6 | 26 | ✅ |
| Phase 3 -> current | 26 | +N | 26+N | run dotnet test |

Every row's After must equal the next row's Before.

## STATE-Pn.json chain fields

```json
{
  "testChain": {
    "flow": "FLOW-01-user-registration",
    "sessionStart": 20,
    "sessionEnd": 26,
    "delta": 6,
    "previousSessionEnd": 20,
    "chainVerified": true
  }
}
```

If `sessionStart != previousSessionEnd` -> the chain is broken -> investigate before claiming
counts.

## Anti-pattern

Plan said `expected_test_count: 30` but phase deltas summed to 28. `claim-verification-SKILL.md`
passed (each document was internally consistent). Chain arithmetic failed because one phase's
"after" was recorded as 24 but the next phase started at 26. This skill catches what
single-document verification misses: the edges between documents.

## Integration

- Extends `plan-review-SKILL.md` count-drift check.
- Invoked at session start (verify the test baseline matches the previous session's end count).
- Results feed the session-end zero-regression delta and the commit/push report (the test delta
  must chain cleanly to the commit).
