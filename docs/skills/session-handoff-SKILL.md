# Session Handoff

> **Stack adaptation (xiigen flows).** The self-contained cross-session brief
> ported from `llm_mvp_core/docs/skills/session-handoff-SKILL.md` (G07). The
> universal part (a `SESSION-BRIEF-<id>.md` is a self-contained bridge between
> actors — no references to files the reader must open; the sections PROJECT
> STATE / WHAT PHASE BUILT in prose / DISCOVERIES / REJECTED CLAIMS / OPEN ITEMS
> omitted when nothing needs deciding / WHAT NEXT WILL BUILD + Gate) is taken
> verbatim from core. Adapted here:
> - **Tests** = `dotnet test` on the affected per-flow `Xiigen.Flow.<Name>.Tests`
>   (xUnit) plus `ng test` for client-bearing flows; the Gate line reads
>   `dotnet test (per-flow .Tests) -> N passed, 0 failed` and, when a client
>   changed, `ng test -> N passed`.
> - The brief is **per-flow** and may reference `flows/FLOW-NN-<slug>/flow.manifest.json`
>   and the `.xiigen/wiki-domain` manifests consumed from core (those files exist,
>   so naming them does not break self-containment).
> - Created under `docs/session-files/` (greenfield write path).
> - Branch is the current working branch (`UpdateUniversalSkills` / `DesignToCode`
>   / `claude/<worktree>`), never `main`/`master`.
> - Domain return type cited is `DataProcessResult<T>` — **not** core
>   `OperationResult<T>`.

## Purpose

Development on this project has two distinct actors:
- **Executor session** — executes phases, writes code, runs tests
- **Planning / review session (new conversation)** — plans next phases, reviews
  results, makes decisions

These two actors share no context. Without a structured handoff, every new
conversation must re-explain the entire flow state from scratch.

A session handoff document is that structured bridge. Paste it at the start
of any new planning conversation and the session immediately knows: where things
are, what just happened, and what needs deciding next.

---

## When to Write

After every phase gate passes, as part of the `phase-completion-packager-SKILL.md`
procedure. Write AFTER the gate passes — never speculatively.

---

## File Location and Name

```
docs/session-files/SESSION-BRIEF-FLOW-NN-P[N].md

Examples:
  docs/session-files/SESSION-BRIEF-FLOW-01-P3.md
  docs/session-files/SESSION-BRIEF-FLOW-16-P4.md
```

---

## Required Sections

```markdown
# SESSION BRIEF — FLOW-NN Phase [N] -> Phase [N+1]
## For: new planning session | Produced by: executor session
## Phase completed: [phase_title] | Next: [next_phase_title]
## Date: [completed_at]

---

## PROJECT STATE

Branch:       [current working branch — e.g. UpdateUniversalSkills]
Flow:         FLOW-NN-<slug>
Phase done:   P[N] — [phase_title]
Phase next:   P[N+1] — [next_phase_title]
Tests:        [exit] passing (was [entry] at phase start, +[delta] this phase)
              [+ ng test: N passing — only if the client changed]
Build:        0 Error(s), 0 Warning(s)
Last commit:  [hash] — phase [N]: [description]

Flow status (from flow.manifest.json + per-flow STATE-JSON):
  [list active flows with current state]

---

## WHAT PHASE [N] BUILT

[3–5 sentences. Plain English. What was implemented and why it matters.
Focus on capability value, not file names or test counts.]

---

## DISCOVERIES — FACTS ESTABLISHED THIS SESSION

Each discovery: fact + verification command + implication + action taken/deferred.

  DISCOVERY-1:
    fact:        [precise factual statement]
    verified_by: grep -n "..." or dotnet test result
    implication: [what this means for future work]
    action:      RESOLVED: [what was done] | DEFERRED: [why and when]

---

## REJECTED CLAIMS

Claims that were wrong during this session. Prevents the same wrong claim
from being accepted in a future session.

  REJECTED-1:
    claim:       [exact wrong statement]
    source:      [who/what made it]
    truth:       [what is actually true]
    verified_by: [command or observation that disproved it]

---

## OPEN ITEMS — NEED YOUR DECISION

[Include only if items genuinely need Luba's input before next phase can start.
Omit this section entirely if nothing needs deciding.]

DECISION 1: [title]
  Context: [one sentence]
  Options:
    A) [option with consequence]
    B) [option with consequence]
  Recommendation: [A or B, and why]

---

## WHAT PHASE [N+1] WILL BUILD

[2–3 sentences: what the next phase covers, what it produces, what the gate looks like.]

Gate: dotnet test (per-flow .Tests) -> [expected test count] passed, 0 failed
      [+ ng test -> N passed — only if the next phase touches the Angular client]

---

## HOW TO USE THIS BRIEF

Paste this file at the start of your planning conversation.
The session will pick up from here without needing full repo context.

To plan Phase [N+1]: say "Plan FLOW-NN Phase [N+1] using this brief."
To approve: say "yes" or "proceed to Phase [N+1]."
```

---

## Writing Rules

1. PROJECT STATE section is always present — always current
2. "WHAT PHASE N BUILT" is prose — no file lists, no bullet points; explain the meaning
3. OPEN ITEMS omitted if nothing needs deciding — do not write "(none)"
4. The brief must be self-contained — no references to files the reader would need to open
   (naming `flow.manifest.json` / `.xiigen/wiki-domain` manifests is fine — those exist)
5. Test count must be exact — the number from `dotnet test` (and `ng test`) output
6. If no discoveries and no rejected claims in the session — omit those sections

---

## Negative Examples

```
WRONG: Brief that references a file the reader must open for meaning
  "See docs/session-files/EXECUTION-LOG-FLOW-16-P4.json for what happened."
  -> Brief must be self-contained. Summarize the meaning inline.

WRONG: "What Phase Built" as a file list
  "## What FLOW-16 Phase 4 Built
   - Created PaymentSettlementService.cs
   - Added 22 tests"
  -> This is a manifest, not a brief. Write what it MEANS for the flow.

WRONG: Including OPEN ITEMS when nothing needs deciding
  "## Open Items
   (none)"
  -> Omit the section entirely.
```

---

## Integration

```
Written by:      phase-completion-packager-SKILL.md (as the handoff artifact)
Read by:         Next planning conversation (paste at start)
Feeds from:      session-execution-log-SKILL.md (test counts and file lists)
Complements:     session-state-crystallization-SKILL.md (≤40-line snapshot for
                 within-session continuity vs. this skill's cross-session handoff)
Profile variant: investigation-handoff-SKILL.md when the session was INVESTIGATION
```
