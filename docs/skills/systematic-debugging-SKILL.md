# systematic-debugging-SKILL.md — Root cause before any fix (4 phases + Iron Law)

> **Stack adaptation (xiigen flows).** The Iron Law, the four phases, the
> 3-failed-fixes architectural threshold, and the "one hypothesis, minimal test"
> rule are the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/systematic-debugging-SKILL.md` (G06 debugging /
> root-cause). The laws are language-independent; only the reproduce/evidence
> commands are stack-bound. Adapted here:
> - **Reproduce / Gate B** = `dotnet test` on the affected per-flow test project
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests` (xUnit 2.9.x), and
>   `dotnet build Xiigen.Flows.slnx`. A single failing test:
>   `dotnet test --filter "FullyQualifiedName~<TestName>"`.
> - **Recent changes** are inspected via the diff of the working branch
>   (`UpdateUniversalSkills` / `DesignToCode`), never a switch to `main`/`master`.
> - **Multi-component layering** for this repo is interface → `Application` →
>   `Infrastructure` → DI in the per-flow host (`Program.cs` /
>   `*ServiceCollectionExtensions.cs`) → `Tests`; grep over `flows/**/*.cs`.
> - For UI symptoms a flow's Angular 21 standalone client
>   (`flows/FLOW-NN-*/client/angular`) reproduces via
>   `npx playwright test -g "<title>"`.
> - Domain return type is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`); services inherit `MicroserviceBase<TSelf>`.

## Core Principle

Random fixes waste time and create new bugs. Quick patches mask underlying issues.

**ALWAYS find root cause before attempting fixes. Symptom fixes are failure.**

## The Iron Law

```
NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
```

If you have not completed Phase 1, you cannot propose fixes.

---

## When to Use

Use for ANY technical issue: test failures, `dotnet build` errors, unexpected
flow behavior, performance problems, cross-flow integration issues, Angular
client defects.

Use this **especially** when:
- Under time pressure (emergencies make guessing tempting)
- "Just one quick fix" seems obvious
- You've already tried multiple fixes that didn't work
- You don't fully understand the root cause

**Do not skip when:**
- Issue seems simple (simple bugs have root causes too)
- You're in a hurry (systematic is faster than thrashing)

---

## The Four Phases

Complete each phase before proceeding to the next.

### Phase 1: Root Cause Investigation

**BEFORE attempting any fix:**

1. **Read error messages carefully**
   - Read `dotnet build` and `dotnet test` output completely
   - Note file paths, line numbers, error codes (CS-codes; with
     `TreatWarningsAsErrors=true` a warning is a build error here)
   - Stack traces show the exact call chain

2. **Reproduce consistently**
   ```bash
   dotnet test --filter "FullyQualifiedName~FailingTestName" --no-build
   # Can you trigger it reliably? Does it fail every time?
   ```
   For a UI symptom in a flow client:
   ```bash
   cd flows/FLOW-NN-<slug>/client/angular && npx playwright test -g "<title>"
   ```

3. **Check recent changes**
   ```bash
   git diff HEAD~1 -- flows/FLOW-NN-<slug>/server
   git log --oneline -5
   # What changed that could cause this? (working branch only — never checkout main)
   ```

4. **Gather evidence in multi-component systems**

   When the system spans interface → Application → Infrastructure → DI → test,
   find WHERE the chain breaks before proposing fixes:
   ```bash
   # Layer 1: Does the Application method exist with the right return type?
   grep -rn "public.*MethodName" flows/FLOW-NN-<slug>/server/*.Application --include="*.cs"

   # Layer 2: Does the Infrastructure / concrete service match the contract?
   grep -rn "public.*MethodName" flows/FLOW-NN-<slug>/server/*.Infrastructure --include="*.cs"

   # Layer 3: Is the DI registration present in the host / service-collection ext?
   grep -rn "AddSingleton\|AddScoped\|AddTransient\|services\." \
       flows/FLOW-NN-<slug>/server/*.Infrastructure --include="*.cs"
   grep -rn "AddSingleton\|AddScoped" flows/FLOW-NN-<slug>/server/*.Api/Program.cs

   # Layer 4: Does the test inject the right concrete (often Xiigen.Fabrics.InMemory)?
   grep -rn "new \|GetRequiredService" flows/FLOW-NN-<slug>/server/*.Tests --include="*.cs" | head
   ```

   Run once to find WHERE it breaks → THEN analyze → THEN investigate that layer.

5. **Trace data flow** — see `root-cause-tracing-SKILL.md` for backward tracing.

### Phase 2: Pattern Analysis

**Find the pattern before fixing:**

1. **Find working examples** — locate a sibling flow (same `Api/Application/
   Infrastructure/Tests` shape) that does the same thing correctly
2. **Compare against references** — read the working flow's service COMPLETELY;
   do not skim
3. **Identify differences** — list every difference between working and broken,
   however small (a different fabric registration, a missing `ErrorCode`, a
   `DataProcessResult<T>` with the wrong `T`)

### Phase 3: Hypothesis and Testing

**Scientific method:**

1. **Form single hypothesis** — "I think X is the root cause because Y" — write
   it down (see `debug-session-SKILL.md` for persisting it across sessions)
2. **Test minimally** — the SMALLEST possible change to test the hypothesis;
   one variable at a time
3. **Verify before continuing** — did it work? Yes → Phase 4. No → form NEW
   hypothesis. Do NOT add more fixes on top.
4. **When you don't know** — say "I don't understand X" — do not pretend to know.

### Phase 4: Implementation

**Fix the root cause, not the symptom:**

1. **Create failing test case FIRST** (`bug-to-tests-SKILL.md` — 3 failing xUnit
   tests before fix)
2. **Implement single fix** — address the root cause identified; ONE change at a
   time; no "while I'm here" improvements; no bundled refactoring
3. **Verify fix** — `dotnet test` on the per-flow `*.Tests` passes? `dotnet build
   Xiigen.Flows.slnx` is 0 Error / 0 Warning? No other tests broken?
4. **If fix doesn't work** — STOP. Count: how many fixes have you tried?
   - If < 3: return to Phase 1 with new information
   - **If ≥ 3: STOP and question the architecture** — do not attempt Fix #4
     without discussing the architectural problem

5. **If 3+ fixes failed: question architecture**

   Pattern indicating architectural problem:
   - Each fix reveals new coupling or shared kernel-fabric state problem in a
     different flow
   - Fixes require restructuring (a new contract, a new fabric interface) to
     implement
   - Each fix creates new symptoms in a sibling flow

   STOP and ask: Is this pattern fundamentally sound?
   Escalate via `diagnostic-escalation-SKILL.md` (interface / locked-decision /
   3+ cycle triggers) before attempting more fixes.

---

## Red Flags — STOP and Follow Process

If you catch yourself thinking any of these, return to Phase 1:
- "Quick fix for now, investigate later"
- "Just try changing X and see if it works"
- "I don't fully understand but this might work"
- "One more fix attempt" (when already tried 2+)
- Each fix reveals a new problem in a different flow

---

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Issue is simple, don't need process" | Simple issues have root causes too. Process is fast for simple bugs. |
| "Emergency, no time for process" | Systematic debugging is FASTER than guess-and-check thrashing. |
| "Just try this first, then investigate" | First fix sets the pattern. Do it right from the start. |
| "I'll write the test after confirming the fix works" | Untested fixes don't stick. Test first proves it. |
| "Multiple fixes at once saves time" | Can't isolate what worked. Causes new bugs. |
| "I see the problem, let me fix it" | Seeing symptoms ≠ understanding root cause. |
| "One more fix attempt" (after 2+ failures) | 3+ failures = architectural problem. Question design, don't fix again. |

---

## Integration With Other Skills

- `root-cause-tracing-SKILL.md` — trace backward through call chain (Phase 1 step 5)
- `defense-in-depth-SKILL.md` — add validation at multiple layers after finding root cause
- `bug-to-tests-SKILL.md` — write 3 failing tests before fix (Phase 4 step 1)
- `diagnostic-escalation-SKILL.md` — 4-step Gate-B diagnosis when cycle ≥ 2 still fails
- `verification-before-completion-SKILL.md` — verify fix worked before claiming success
