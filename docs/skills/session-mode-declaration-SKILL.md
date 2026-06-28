# Session Mode Declaration

> **Stack adaptation (xiigen flows).** The 5-mode declaration discipline ported
> from `llm_mvp_core/docs/skills/session-mode-declaration-SKILL.md` (G07). The
> universal part (exactly one of ARCHITECT / PLANNER / REVIEWER / EXECUTOR /
> INVESTIGATION declared before first synthesis; scope-in/scope-out is an
> enforceable gate; drift is detected and the session stops, it is not
> "stretched") is taken verbatim from core. Adapted here:
> - **EXECUTOR scope** = file edits under `flows/FLOW-NN-<slug>/server` (`net10.0`,
>   per-flow `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`) and
>   `flows/FLOW-NN-<slug>/client/angular` (Angular 21 standalone), plus xUnit /
>   `ng test` runs and `dotnet build Xiigen.Flows.slnx` checks.
> - **ARCHITECT scope** = decisions/interfaces without file paths or method
>   signatures; domain return type is the XIIGen `DataProcessResult<T>`
>   (`IsSuccess`/`ErrorCode`/`Data`, from `Xiigen.Kernel`) — **not** core
>   `OperationResult<T>`, which stays in `llm_mvp_core`.
> - The mode record is written into the per-flow session STATE-JSON kept next to
>   `flows/FLOW-NN-<slug>/flow.manifest.json` (greenfield governance: this is a
>   new write path, not an existing file).
> - Branch discipline: the current working branch of this worktree
>   (`UpdateUniversalSkills` / `DesignToCode` / `claude/<worktree>`), never `main`
>   or `master`.

## Core Problem

A session that drifts from one mode to another mid-stream produces contaminated
output. The contamination is invisible when it happens and costly to unwind later.
This skill makes mode an explicit, declared property with enforceable scope boundaries.

---

## When to Invoke

- At session start, before first synthesis
- When a session's output visibly leaves its declared mode's scope
- When Luba corrects "wrong level of output" — that is a mode drift signal

One explicit declaration at session start = zero slow-drift correction cycles.

---

## Section 1 — The 5 Session Modes

Every session declares exactly one of these modes.

| Mode | Produces | Scope-in | Scope-out |
|------|----------|----------|-----------|
| **ARCHITECT** | decisions, trade-off analyses, interface contracts | goals, interfaces, design invariants, acceptance criteria | file paths, method signatures, test code, implementation diffs |
| **PLANNER** | phase sequences, dependency graphs, EXECUTION files | phases, gates, Gate B conditions, dependencies, scope declarations | new design work, code implementation |
| **REVIEWER** | verdicts, gap reports, plan approval/rejection | plan evaluation, gate checks, DNA compliance, evidence verification | plan authoring, code changes, new design proposals |
| **EXECUTOR** | file changes, test runs, build outputs | file edits, line-precise changes, test runs, build checks | new design decisions, scope changes, architectural trade-offs |
| **INVESTIGATION** | discovery findings, artifact boundaries, capability maps | reading files, grep commands, test runs, verified facts | implementing changes, writing new code |

**The scope boundaries are enforceable gates, not preferences.**

If ARCHITECT mode output contains method signatures or `flows/FLOW-NN-*/.../*.cs:line`
references, the session has drifted to EXECUTOR. If EXECUTOR mode proposes a new
design pattern, that is ARCHITECT scope. Drift is detected, not tolerated.

---

## Section 2 — Declaration Protocol

At session start:

1. **Declare exactly one mode** — not two, not "mostly X but also Y"
2. **Write 1-2 sentence justification** — why this mode fits the question;
   why the adjacent mode is wrong
3. **Log immutably** — mode cannot silently change mid-session
4. **No output before declaration** — declaration is a prerequisite

Declaration template:
```markdown
## Session Mode (session-mode-declaration-SKILL)

Declared mode: [ARCHITECT | PLANNER | REVIEWER | EXECUTOR | INVESTIGATION]

Justification:
[1-2 sentences. What makes this mode correct for Luba's question?
Why is the adjacent mode wrong?]

Scope-out for this session:
[explicit list of what this session will NOT produce]
```

If Luba's question doesn't fit any single mode cleanly — stop and ask. Do not guess.
Multi-mode sessions are exactly how drift happens.

---

## Section 3 — Drift Detection Signals

Self-check before every STOP. Each mode has specific signals:

### ARCHITECT drifting to EXECUTOR
- Output contains `flows/FLOW-NN-*/.../*.cs:line` references or method signatures
- Output proposes a specific code change or diff
- Output prescribes an execution order ("first do X, then Y, then Z")

### ARCHITECT drifting to PLANNER
- Output produces a phase sequence or session file
- Output mentions specific Gate B conditions or test counts

### PLANNER drifting to EXECUTOR
- Output contains diffs or file contents
- Output runs or proposes running specific commands

### EXECUTOR drifting to ARCHITECT
- Output proposes a design pattern instead of implementing one
- Output debates the approach instead of executing it

### INVESTIGATION drifting to EXECUTOR
- Output starts modifying files before completing the discovery
- Output writes new code instead of reading existing code

---

## Section 4 — Resolution on Drift

**Detected drift is NOT corrected by continuing in the correct mode.**

Slow-drift correction after 3000 tokens of drifted output doesn't produce a
clean result — it produces a hybrid with the wrong frame baked in.

Resolution steps:
1. Session STOPS immediately when drift is detected, even mid-response
2. Log the drift: what scope-out signal was detected, what output triggered it
3. Luba decides: restart in correct mode (fastest), or change the declaration
   with new justification (if initial declaration was wrong)

A fresh session with explicit mode declaration is faster than correcting mid-course.

---

## Section 5 — Session Mode Record

Record in the per-flow STATE-JSON (next to `flows/FLOW-NN-<slug>/flow.manifest.json`)
or session notes:

```json
{
  "sessionMode": {
    "declared": "EXECUTOR",
    "justification": "Luba asked to implement the OnboardingDeliveryService completion gate and its xUnit tests in FLOW-01. The question is about code implementation, not design trade-offs.",
    "scopeOut": ["new design decisions", "architectural trade-offs", "scope changes"],
    "driftEvents": []
  }
}
```

---

## Anti-Patterns

1. **"I'll pick the mode as I go"** — mode declaration is before first synthesis,
   not inferred retrospectively.

2. **"It's multi-mode"** — no session is multi-mode. If the question requires
   two modes, handle one and defer the other to a separate session.

3. **"Implicit mode"** — implicit modes drift. Declare explicitly.

4. **"Mode changed because the question evolved"** — if the question genuinely
   evolved, stop the current session and restart with the new mode.

5. **"Let me correct the drift in the next response"** — corrections leave
   drifted context intact. Restart is faster than correction.

---

## Integration

- `session-file-authoring-SKILL.md` records the declared mode in the session-file header
- `materialization-session-type-SKILL.md` provides MATERIALIZATION as a session shape
  reachable from the EXECUTOR/PLANNER boundary
- `agent-output-format-SKILL.md` SESSION-0 gate assumes the mode was declared first
