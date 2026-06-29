# debug-session-SKILL.md — Persist a multi-session investigation (active-session.md)

> **Stack adaptation (xiigen flows).** The persistent investigation file, the
> ELIMINATED/CONFIRMED/INVESTIGATING status vocabulary, the "Confirmed Facts (do
> NOT re-investigate)" protocol, the Current Best Theory / Next Action recovery
> fields, and the "read the debug file BEFORE the codebase" load order are the
> UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/debug-session-SKILL.md` (G06 debugging / root-cause).
> Adapted here:
> - **File location:** `.claude/debug/active-session.md` in this repo (or
>   `docs/debug/active-session.md` if the worktree has no `.claude/`).
> - **Confirmed Facts** record .NET 10 shapes: the exact `DataProcessResult<T>`
>   generic argument, sync vs `async Task<…>`, the per-flow `sealed record` field
>   path, and which concrete fabric is resolved for a kernel interface — **NOT**
>   core `OperationResult<T>`.
> - **Elimination commands** are `dotnet test --filter "FullyQualifiedName~<Test>"`
>   and `grep` over `flows/**/*.cs`; for UI investigations,
>   `npx playwright test -g "<title>"` in the flow's `client/angular`.
> - **Archive** only after a green `dotnet test` on the affected per-flow
>   `Xiigen.Flow.<Name>.Tests` (and `dotnet build Xiigen.Flows.slnx` = 0 Error /
>   0 Warning).

## Core Problem

When a fix attempt fails during execution and the investigation must span
multiple sessions, starting the next session from scratch re-investigates paths
already eliminated. The debug session file persists the investigation state:
every hypothesis, its ELIMINATED/CONFIRMED/INVESTIGATING status, the exact
command that proved or disproved it, and the confirmed API shapes that must not
be re-verified. A session that starts by reading the debug file instead of the
codebase resumes at the exact point debugging stopped.

---

## When to Invoke

- When a fix attempt fails during execution (test fails after applying the fix)
- When a public method signature or `DataProcessResult<T>` return type differs
  from the plan
- When a C# compile error (or `TreatWarningsAsErrors` warning) cannot be resolved
  in 2 attempts
- When investigation must pause mid-stream (context limit, end of session)

---

## Section 1 — File Location and Lifecycle

**Location:** `.claude/debug/active-session.md` (fallback `docs/debug/active-session.md`)

**Lifecycle:**
```
1. CREATE the file when a fix attempt fails or an API shape is wrong
2. APPEND to the file after every hypothesis test
3. ARCHIVE the file when the fix is confirmed working:
   rename to .claude/debug/YYYY-MM-DD-[issue-description].md
```

**Load order:** At the start of any session where debugging was in progress, read
`active-session.md` BEFORE reading any other file.

```bash
# Check if a debug session is active
ls .claude/debug/active-session.md 2>/dev/null || ls docs/debug/active-session.md 2>/dev/null
# If it exists: read it first — skip the codebase read until the hypothesis is current
```

---

## Section 2 — File Format

```markdown
# Debug Session: [issue title]
**Date:** YYYY-MM-DD
**Flow:** FLOW-NN-<slug>
**Issue:** [one-line description of what is failing]

## Hypotheses

### H1: [hypothesis title]
**Status:** ELIMINATED / CONFIRMED / INVESTIGATING
**Evidence for:** [what supports this]
**Evidence against:** [what disproves this]
**Elimination command:**
```bash
[exact command — exact output]
```
**Result:** [exact output — not summarized]

### H2: [next hypothesis]
...

## Confirmed Facts (do NOT re-investigate)
- [symbol]: [type/signature] — returns DataProcessResult<[T]> (line N of [file])
- [method]: is [sync / async Task<…>], signature: [exact]
- [kernel interface]: resolved as [concrete fabric in DI / test wiring]
- [per-flow record]: field path [Outcome.Body["..."]] holds [value]

## Current Best Theory
[What the investigation currently believes is happening — one paragraph]

## Next Action
[Exact next step when this session resumes — specific enough to execute without re-reading the history]
```

---

## Section 3 — Status Values

```
INVESTIGATING: Active hypothesis — not yet tested
CONFIRMED:     Evidence confirms this IS the cause
ELIMINATED:    Evidence proves this is NOT the cause — do not re-investigate
```

An ELIMINATED hypothesis with no supporting evidence record is incomplete. If you
cannot cite the command and output that eliminated it: it has not been eliminated.

---

## Section 4 — Confirmed Facts Protocol

The "Confirmed Facts" section is the most important part of the file. Every
confirmed fact prevents a future session from repeating work:

```
WRONG:  Hypothesis H3 was ELIMINATED (vague — why? what proved it?)
RIGHT:  H3 ELIMINATED: MarketplacePaymentsService.CheckoutAsync returns
        Task<DataProcessResult<MarketplacePaymentsOutcome>>, not void.
        Confirmed by reading MarketplacePaymentsService.cs line 49.
        Grep: grep -n "Task<DataProcessResult" \
          flows/FLOW-16-marketplace-payments/server/*.Application/MarketplacePaymentsService.cs
```

Add to Confirmed Facts immediately when any of these are verified:
- A method's exact return type (including the `DataProcessResult<T>` generic argument)
- Which concrete fabric a kernel interface resolves to in DI / test wiring
- Whether a method is sync or `async Task<…>`
- The exact field path for accessing a nested value in a per-flow record

---

## Section 5 — Integration with the Session Protocol

**At preflight time:** check for an active debug session before writing any code:
```bash
ls .claude/debug/active-session.md 2>/dev/null && echo "ACTIVE DEBUG SESSION — read first"
```

**Before a forced stop:** write "Current Best Theory" and "Next Action" to the
active debug session file. These survive a context reset.

**When investigation completes:** archive the file. Archived debug sessions are
reference material for `systematic-debugging-SKILL.md`'s pattern library.

---

## Anti-Patterns

1. **"Starting investigation from scratch each session"** — the most expensive
   failure mode. ELIMINATED hypotheses are re-investigated; confirmed API shapes
   re-verified. Create the file when debugging starts, not after the third
   failed session.

2. **"ELIMINATED hypothesis with no elimination evidence"** — marking H3
   ELIMINATED without recording what proved it is useless. The next session
   cannot trust the status without the proof.

3. **"Current Best Theory left empty"** — the theory at session end is the most
   valuable recovery point. A resuming session with no theory rebuilds the
   entire chain.

4. **"Archiving before fix is confirmed working"** — archive only after
   `dotnet test` on the per-flow `*.Tests` passes with the fix applied and all
   pre-existing tests still pass. A premature archive closes the investigation
   before it is done.

5. **"Not reading the debug file before reading the codebase"** — the debug file
   changes the read order. If H7 is CONFIRMED, you read the fix site first, not
   the whole flow. Load order: debug file first.

---

## Integration

- `phase-preflight-SKILL.md` checks for an active debug session before any code write
- `code-examination-SKILL.md` provides the reading protocol; this skill tracks investigation state
- `root-cause-tracing-SKILL.md` produces hypotheses; this skill tracks their status
- `systematic-debugging-SKILL.md` archives provide the historical pattern library
