# Session File Authoring

> **Stack adaptation (xiigen flows).** The crystallization protocol, 8
> self-containment checks, and Gate D (goal advancement) ported from
> `llm_mvp_core/docs/skills/session-file-authoring-SKILL.md` (G07). The universal
> part (a session file is self-contained for a cold reader — no references to
> prior session files, no implicit state, abbreviations expanded; the 8 checks
> and Gate D are mandatory) is taken verbatim from core. Adapted here:
> - Session artifacts are created from scratch under `docs/session-files/`
>   (greenfield: this is a new write path).
> - **Check 7 "test baseline"** = `dotnet test Xiigen.Flows.slnx` (or the affected
>   per-flow `Xiigen.Flow.<Name>.Tests`, xUnit) plus Angular `ng test` for
>   client-bearing flows — not a Jest or memorized count.
> - **Check 4 (file references exist)** is matched against `flows/FLOW-NN-<slug>/...`
>   paths and `.xiigen/wiki-domain` manifests consumed from core.
> - Abbreviation expansion (Rule 9) covers `FLOW-NN` names on first use.
> - Domain return type referenced in session files is `DataProcessResult<T>`
>   (`IsSuccess`/`ErrorCode`/`Data`, from `Xiigen.Kernel`) — **not** core
>   `OperationResult<T>`.
> - Branch field records the current working branch (`UpdateUniversalSkills` /
>   `DesignToCode` / `claude/<worktree>`), never `main`/`master`.

## Core Problem

A session file that references another session file, assumes prior context,
or contains implicit state is not a session file — it is a fragment. A fragment
cannot be handed off to a cold session. This skill defines the crystallization
protocol (what makes a session file self-contained), the 8 self-containment
checks (with verification commands), and Gate D (the goal advancement block
that every session file must contain).

---

## When to Invoke

- Before writing any session-execution, session-brief, or STATE file
- When `agent-output-format-SKILL.md` SESSION-0 gate requires a new execution file
- When handing off to another session (`session-handoff-SKILL.md`)
- When a cold-session reader says "I can't understand this without the prior context"

---

## Section 1 — Crystallization Protocol

A session file is "crystallized" when a reader who has never seen this project
before can understand: what the session was trying to accomplish, what state
the project was in when it started, what the session produced, and what the
next session must do.

**Four crystallization requirements:**

1. **No cross-references to prior session files**
   "See SESSION-04 for context" is not crystallized. The context must
   be present in this file, not pointed to.

2. **No implicit state**
   "Continuing from last time" is not crystallized. State the relevant prior
   state explicitly in the file header.

3. **No assumed knowledge**
   Abbreviations must be expanded on first use (Rule 9). Flow names, capability
   class names, and interface names must be defined. Example:
   "FLOW-19 (durable sagas / compliance)" on first use.

4. **No dangling references**
   Every file mentioned by name must either be readable from the project
   directory or explained inline. "See `flows/FLOW-NN-<slug>/flow.manifest.json`"
   is acceptable because that file exists. "See my earlier analysis" is not.

---

## Section 2 — Eight Self-Containment Checks

Run these checks on every session file before committing or handing off.

```bash
# Check 1: No cross-references to prior session files
grep -n "SESSION-[0-9]" docs/session-files/SESSION-NN.md
# Expected: 0 hits (if any hits: replace with inline context)

# Check 2: No "continuing from" or "as before" phrases
grep -in "continuing from\|as before\|as discussed\|from last time" \
    docs/session-files/SESSION-NN.md
# Expected: 0 hits

# Check 3: All abbreviations expanded on first use
# Manual: scan for any uppercase abbreviation or FLOW-NN name not followed by "(full form)"
# Example: "FLOW-16 (marketplace payments)" on first use ✅
# Example: "FLOW-16" with no prior expansion ❌

# Check 4: File references exist
grep -oE '[A-Za-z0-9_./-]+\.(md|cs|ts|json)' docs/session-files/SESSION-NN.md \
    | while read f; do [ -f "$f" ] || echo "MISSING: $f"; done
# Expected: 0 MISSING lines

# Check 5: Goal advancement block present
grep -n "Goal advancement\|Goal Advancement" docs/session-files/SESSION-NN.md
# Expected: 1 hit (Gate D — see Section 3)

# Check 6: Verbatim goal quote present
grep -n "verbatim\|\".*goal.*\"" docs/session-files/SESSION-NN.md
# Expected: 1+ hits (at least one quoted goal string)

# Check 7: Test baseline recorded
grep -n "testBaseline\|passed.*0 failed\|Passed:.*Failed: 0" docs/session-files/SESSION-NN.md
# Expected: 1+ hits (pre-session and post-session baselines)
#   .NET: dotnet test Xiigen.Flows.slnx  (or per-flow Xiigen.Flow.<Name>.Tests)
#   Angular: ng test  (for client-bearing flows)

# Check 8: Next session requirements stated
grep -n "Next session\|prerequisite\|blocked by" docs/session-files/SESSION-NN.md
# Expected: 1+ hits
```

All 8 checks must pass before the file is handed off. A check that cannot
pass means the file is not crystallized. Fix the file, not the check.

---

## Section 3 — Gate D: Goal Advancement Block

Every session file must contain a Gate D block. Gate D is a binary test:
did this session advance the stated goal?

```markdown
## Goal Advancement

**Stated goal (verbatim):** "[exact quote from session start — never paraphrased]"

**Advancement:** [one sentence with a state-change verb]
Examples of valid advancement sentences:
  - "FLOW-01 phase 3 complete: OnboardingDeliveryService completion gate now passes 12 xUnit tests."
  - "Gate B passed: 12 new tests added, all green, baseline preserved."
  - "BLOCKED: missing locked decision for idempotency-key semantics in FLOW-16; session halted."

**Next session must:** [one sentence stating the concrete starting condition]
```

**State-change verb requirement:** The advancement sentence must contain a verb
that names a state change: "complete," "added," "fixed," "blocked," "produced,"
"removed," "passed." Verbs like "worked on," "investigated," "reviewed" are
not state-change verbs — they describe activity, not outcomes.

**Gate D FAIL conditions:**
- Advancement sentence has no state-change verb → rewrite
- Goal quote is paraphrased, not verbatim → go back to source, quote exactly
- "Next session must" is vague ("continue the work") → name the specific file,
  method, or decision the next session must start from

---

## Section 4 — Session File Header Template

Every session execution file begins with this header:

```markdown
# SESSION-NN — [Flow + Phase Name]

## Session State (pre-session)
- **Branch:** [current working branch — e.g. UpdateUniversalSkills / DesignToCode]
- **Test baseline:** [N passed, 0 failed — from dotnet test (per-flow .Tests) at session start;
                      + ng test for client-bearing flows]
- **Last commit:** [hash + message]
- **Flow in scope:** [FLOW-NN-<slug>]

## Goal Advancement
**Stated goal (verbatim):** "[from session start or planning document]"
**Advancement:** [state-change sentence — written at session END]
**Next session must:** [specific starting condition — written at session END]

## Session Scope
This session implements: [specific phases or capabilities in FLOW-NN]
This session does NOT cover: [explicit list of out-of-scope items]
```

The "does NOT cover" section prevents scope creep and prevents the next session
from assuming this session completed something it didn't.

---

## Section 5 — Anti-Patterns

1. **"Session file says 'see SESSION-04 for background'"** — the background
   must be in this file. A cold reader does not have SESSION-04. Inline it.

2. **"Goal advancement sentence: 'Made progress on the service'"** — "made
   progress" has no state change. What specifically changed? What passes now
   that didn't before?

3. **"Test baseline not recorded"** — without the pre-session baseline, the
   post-session baseline is meaningless. Record both (xUnit and, when present,
   `ng test`).

4. **"Abbreviations used without expansion"** — Rule 9 applies to session files
   as much as to code comments. The next session reader may not know what
   "FLOW-19" covers. Write "FLOW-19 (durable sagas / compliance)" on first use.

5. **"'Next session must: continue implementation'"** — this is not actionable.
   "Next session must: implement PaymentSettlementService.SettleAsync() in
   `flows/FLOW-16-marketplace-payments/server/...Application`, Gate B =
   `dotnet test --filter PaymentSettlement` Failed: 0" is actionable.

6. **"Gate D written before the session ends"** — the advancement sentence must
   describe what actually happened, not what was planned. Write it last.

---

## Integration

- `agent-output-format-SKILL.md` defines the three-file rule (REFERENCE/STATE/EXECUTION)
- `session-handoff-SKILL.md` uses this skill's Gate D block as the handoff summary
- `session-output-contract-SKILL.md` Section 3 (read-before-write rule) applies to all UPDATE deliverables
- `goal-context-persistence-SKILL.md` (G03) governs the verbatim goal capture that feeds Gate D
- `verification-before-completion-SKILL.md` runs before any session is declared done; Gate D is its output
