# Session Scope Resolution

> **Stack adaptation (xiigen flows).** Universal session-type dispatch table ported from
> `llm_mvp_core/docs/skills/session-scope-resolution-SKILL.md`. Adapted to this repo:
> - **EXECUTOR prerequisite** = `dotnet build` 0 errors + a green `dotnet test` (xUnit) for the
>   affected flow `Xiigen.Flow.<Name>.Tests`; for client branches, the Angular 21 build /
>   Playwright baseline;
> - **minimum documents** are this repo's set: `AGENTS.md`/`CLAUDE.md` when present, the
>   greenfield `docs/DECISIONS-LOCKED.md`, the architecture/flow-map doc, the per-flow contracts
>   under `flows/FLOW-NN-<slug>/contracts`, and the `.xiigen/wiki-domain` manifests as the
>   consumed-domain source (these manifests are **evidence_only**, never a task authority);
> - the per-flow boundary is
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` plus
>   `client/angular/src`; the domain return type is `DataProcessResult<T>` (XIIGen wrapper), not
>   core `OperationResult<T>`;
> - this is a **dispatch table** — it routes to skills/documents; the routed content is
>   authoritative, this skill is the lookup.

## Authority Requirement Binding Override

Authority-chain requirement binding applies before every session dispatch.
Requirements from the current human operator / product owner, architect work
order, approved active plan, or accepted reviewer gate are binding unless that
authority explicitly marks them optional, exploratory, or preference-only. A
downstream session must not narrow an upstream requirement. Load
`authority-requirement-binding-SKILL.md` before planning, reviewing, issuing work
orders, or executing a bounded work order. The `.xiigen` manifests are
evidence_only and cannot create requirements.

## Core Problem

A session that loads the wrong documents, runs the wrong governance chain,
or misses a blocking condition before starting will either do the wrong work
or produce invalid output. The session type (ARCHITECT, EXECUTOR, INVESTIGATOR,
MAINTAINER, DEBUG) determines which documents to load, which skills to run first,
and what blocking conditions prevent starting. This skill is the dispatch table
that maps session type → minimum documents + governance chain + blocking conditions.

---

## When to Invoke

- At every session start, after the session mode is declared
- Before loading any project documents (determine what to load first)
- When planning-session-startup resolves mode + scope

---

## Section 1 — Dispatch Table

### EXECUTOR SESSION

Work target: implementing a slice in a specific flow

```
PREREQUISITE CHECKS:
  → dotnet build — must show 0 errors
  → dotnet test (affected Xiigen.Flow.<Name>.Tests) — record baseline (≥ prior baseline)
  → If any test fails at session start: STOP. Fix before any implementation.

MINIMUM DOCUMENTS:
  1. AGENTS.md + CLAUDE.md (when present) / current work order   (every session)
  2. docs/DECISIONS-LOCKED.md                                    (locked decisions D-01..D-NN)
  3. flows/FLOW-NN-<slug>/contracts §[relevant]                  (flow contract for this slice)
  4. The active plan / work order for the current slice          (what to implement)

GOVERNANCE CHAIN (mandatory, in order):
  1. session-output-contract (define done BEFORE reading any files)
  2. phase-preflight-SKILL.md                       ← assumption registry before code
  3. four-tier-decision-classification-SKILL.md     ← classify each new decision point
  4. freedom-machine-SKILL.md                       ← for each constant/value introduced
  5. verification-before-completion                 ← before claiming any slice done

BLOCKING CONDITIONS:
  - dotnet test failures > 0 at session start → STOP. Fix before flow work.
  - Locked decision (D-NN) contradicts the slice → STOP. Route to decision-reopening-SKILL.md.
  - Required flow contract missing → emit CONTEXT_INSUFFICIENT.
```

---

### ARCHITECT SESSION

Work target: designing flows, writing plans, reviewing architecture

```
PREREQUISITE CHECKS:
  → dotnet build — must show 0 errors (plans cannot assume a broken baseline)
  → Architect sessions are rarely blocked; check only if a prior session left open issues

MINIMUM DOCUMENTS:
  1. AGENTS.md + CLAUDE.md (when present) / current work order   (every session)
  2. docs/DECISIONS-LOCKED.md                                    (load first — do not propose what's locked)
  3. architecture/flow-map doc                                   (current flow statuses)
  4. .xiigen/wiki-domain manifests                               (consumed-domain source, evidence_only)
  5. flows/FLOW-NN-<slug>/contracts                              (for contract design)

GOVERNANCE CHAIN (mandatory, in order):
  1. planning-session-startup-SKILL.md              ← pre-plan questions before first output
  2. authority-requirement-binding-SKILL.md         ← bind every upstream requirement
  3. solution-scope-gate-SKILL.md                   ← assign the correct scope level
  4. plan-review-SKILL.md                           ← FC gates before any plan is final
  5. multi-reviewer-design-SKILL.md                 ← multi-perspective review of any interface/architecture decision

BLOCKING CONDITIONS:
  - Any proposal contradicts docs/DECISIONS-LOCKED.md → STOP. Run decision-reopening-SKILL.md.
  - Plan has no Gate B for any phase → BLOCK.
```

---

### INVESTIGATOR SESSION

Work target: diagnosing gaps, tracing failures, simulation

```
PREREQUISITE CHECKS:
  → None. Investigator sessions can start from any state.
  → Load the specific artifact being investigated first, not reference documents wholesale.

MINIMUM DOCUMENTS:
  1. The specific file being investigated            (targeted, not wholesale)
  2. docs/DECISIONS-LOCKED.md                        (to check if the gap is a known issue)
  DO NOT load the whole architecture/flow-map or all contracts upfront —
  load specific sections when needed.

GOVERNANCE CHAIN (mandatory):
  1. claim-as-hypothesis                            ← verify claims before investigation
  2. problem-decomposition                          ← ask "why" 3× before proposing fixes
  3. root-cause tracing                             ← after gap catalog produced
  4. investigation handoff                          ← at session end

BLOCKING CONDITIONS:
  - None. Investigation sessions are always safe to start.
  - Any issue found: ISSUE INVENTORY entry required before session ends
    (FIXED / DEFERRED with authorization / EXCEPTION — not "noted").
```

---

### MAINTAINER SESSION

Work target: fixing files, updating skills, syncing documentation

```
PREREQUISITE CHECKS:
  → Assess blast radius (which flows/clients) before any file changes.
  → If the change touches tests: dotnet test must pass after the change.

MINIMUM DOCUMENTS:
  1. docs/DECISIONS-LOCKED.md                        (to know what's locked)
  2. The specific file being updated                 (read before touching)
  DO NOT load reference documents unless the change requires contract verification.

GOVERNANCE CHAIN (mandatory):
  1. blast-radius assessment                        ← FIRST, before any change
  2. change-propagation                             ← derived views update in the same commit
  3. verification-before-completion                 ← before declaring any change done
  4. documentation-sync                             ← at session end

BLOCKING CONDITIONS:
  - Any issue found: FIXED / DEFERRED (authorization + DECISIONS-LOCKED note) / EXCEPTION.
    "Noted" is not a valid status. "Out of scope" is not a valid status.
  - If tests touched: dotnet test must show 0 failures before session ends.
  - One commit per logical change (not one commit per file).
```

---

### DEBUG SESSION

Work target: a specific failing test, broken method, or broken endpoint

```
PREREQUISITE CHECKS:
  → None. Load the failing test output or error message directly.
  → Do NOT load any document that doesn't contain the failing code.

MINIMUM DOCUMENTS:
  1. The failing test file or error output          (direct and targeted)
  2. The implementation file being debugged         (direct)
  Nothing else unless the root cause trace leads there.

GOVERNANCE CHAIN (mandatory):
  1. bug-to-tests                                   ← failing tests before any fix
  2. root-cause tracing                             ← trace backward through the call chain
  3. three-level verification                       ← after fix: unit → integration → suite
  4. verification-before-completion                 ← before declaring the bug fixed

BLOCKING CONDITIONS:
  - Every issue found: ISSUE INVENTORY entry required.
    FIXED / DEFERRED (authorization) / EXCEPTION — not "out of scope."
  - The fix must pass dotnet test with 0 failures before session ends.
```

---

## Section 2 — What This Skill Does NOT Contain

```
✗ Prerequisite verification commands — those are in the prerequisite chain
✗ Bug fix procedures — those are in bug-to-tests
✗ Flow-specific decisions — those are in docs/DECISIONS-LOCKED.md and the flow contracts
✗ Contract shapes — those are in flows/FLOW-NN-<slug>/contracts
```

This skill is a dispatch table. It routes to the right skills and documents
for the session type. The content of those skills and documents is authoritative;
this skill is the lookup, not the source.

---

## Anti-Patterns

1. **"Loading all reference documents at session start regardless of type"** —
   an INVESTIGATOR session that loads the whole flow-map upfront is loading what
   it intends to investigate. Load targeted documents first; expand as the trace requires.

2. **"Starting EXECUTOR work without an output contract"** — the output contract
   is the zeroth step for every EXECUTOR session. Define done before reading files.

3. **"MAINTAINER session ends without ISSUE INVENTORY"** — every issue found
   during maintenance must be classified FIXED/DEFERRED/EXCEPTION. "Noted" means it recurs.

4. **"ARCHITECT session proposes something without loading docs/DECISIONS-LOCKED.md"** —
   it is the first document in the ARCHITECT minimum list for this reason.

5. **"Treating a `.xiigen` manifest as a task authority"** — manifests are
   evidence_only; the authority is Luba's current message or the bounded work order.

---

## Integration

- the session-mode declaration feeds this skill's session-type input
- planning-session-startup is where this skill's output is produced
- `authority-requirement-binding-SKILL.md` binds upstream requirements before dispatch
- `document-hierarchy-SKILL.md` supplies the routing table for the minimum-document sets
- verification-before-completion is the final step for all session types
