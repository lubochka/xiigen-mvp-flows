# root-cause-ladder-SKILL.md — Classify every gap at 3 levels before fixing

> **Stack adaptation (xiigen flows).** The three-level structure (Level 1
> immediate / Level 2 structural-practice / Level 3 architectural-decision), the
> Root Cause Record format, the Cross-Gap Convergence protocol (shared Level-3
> root fixed before individual Level-1 fixes), the Rejection Response protocol
> ("never abandon a valid Level-2/3 fix because it feels over-engineered"), and
> the DESIGN_FLAW escalation when a Level-3 root contradicts a locked decision are
> the UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/root-cause-ladder-SKILL.md` (G06 debugging /
> root-cause). The level *targets* are mapped to this repo:
> - **Level 1 — Immediate Fix:** a code/config change in one concrete
>   `flows/FLOW-NN-<slug>` (server or `client/angular`).
> - **Level 2 — Structural Fix:** a new or updated skill file under
>   `docs/skills/`, or a named pre-phase check / iron rule for flows.
> - **Level 3 — Architectural Decision:** a flows architecture artifact — a
>   per-flow contract (`contracts/`), the flow manifest (`flow.manifest.json` /
>   `.xiigen` wiki-domain), a kernel-fabric interface contract, or an ADR /
>   decisions register for this repo (the flows analogue of
>   `llm_mvp_core` `DECISIONS-LOCKED.md`).
> - **Gate B** for any Level-1 fix is `dotnet test --filter
>   "FullyQualifiedName~<Flow>"` (xUnit) + `dotnet build Xiigen.Flows.slnx`.
> - `DataProcessResult<T>` from `Xiigen.Kernel` is the result type (NOT core
>   `OperationResult<T>`).

## Core Problem

Every gap has three possible explanations that sit at different levels: the
immediate fix, the structural practice that let it recur, and the architectural
decision that made it possible in the first place. A session that only applies
the immediate fix will see the same gap re-emerge. A session that addresses the
wrong level will over-engineer a small symptom or under-solve a deep pattern.
This skill defines the three-level structure, the convergence protocol, and the
rejection response protocol.

---

## When to Invoke

- After Gate B fails — before deciding what to fix
- When the same category of error appears in more than one flow / phase
- When a remediation was applied in a prior session but the problem recurred
- When `diagnostic-escalation-SKILL.md` produces an architectural escalation signal

---

## Section 1 — The Three-Level Structure

Every gap must be classified at all three levels before any fix is written.

### Level 1 — Immediate Fix

The specific code or configuration change that makes this instance stop failing.

```
Format:
  What failed:   [exact method or assertion that failed]
  Why it failed: [proximate cause — the concrete missing or wrong thing]
  Fix:           [specific change: file + line + what to change, in flows/FLOW-NN-*]
  Test:          [the per-flow xUnit test that will be green after this fix and not before]
```

Level 1 is necessary but not sufficient. A Level-1-only response means the same
gap will recur in the next flow/phase.

### Level 2 — Structural Fix

The skill, rule, or practice that, if present, would have caught Level 1 before
it reached Gate B.

```
Format:
  Missing practice: [what should have been checked but wasn't]
  Skill or rule:    [which docs/skills/*.md file addresses this — or "NEW SKILL NEEDED"]
  Action:           [update skill / add iron rule / add pre-phase check]
```

Level 2 examples (flows):
- "`data-flow-integrity-SKILL.md` was not run before wiring flow A → flow B"
- "No iron rule for 'every Application method returns `DataProcessResult<T>`'"
- "`dev-safety-SKILL.md` SESSION GATE (build + per-flow test) was not applied"

### Level 3 — Architectural Decision

The design decision or document that, if correct, would have prevented the
structural gap at Level 2.

```
Format:
  Root cause: [the missing or incorrect design artifact or decision]
  Document:   [where it belongs — per-flow contract, flow.manifest.json, .xiigen domain, ADR / decisions register]
  Action:     [write the contract / update the manifest / add an ADR entry]
```

Level 3 examples (flows):
- "No locked decision for which kernel fabric (`IDocumentStore` vs `IOutboxStore`)
  owns escrow state"
- "The per-flow contract does not specify the `DataProcessResult<T>` return shape
  of the dashboard endpoint"
- "The flow manifest has no entry defining the cross-flow event contract between
  FLOW-16 and FLOW-09"

---

## Section 2 — Root Cause Record Format

After classifying a gap at all three levels, produce one record:

```markdown
### Root Cause Record — [Gap Name]

**Level 1 — Immediate Fix**
- Failed:   [method/assertion]
- Why:      [proximate cause]
- Fix:      [flows/FLOW-NN-*/...:line change]
- Test:     [per-flow xUnit test name]

**Level 2 — Structural Fix**
- Gap:      [missing practice or skill]
- Action:   [docs/skills update / new iron rule / pre-phase check]

**Level 3 — Architectural Decision**
- Root:     [missing or incorrect design artifact]
- Document: [contract / manifest / .xiigen domain / ADR]
- Action:   [new contract clause or manifest/ADR entry]
```

Do not proceed to Level 1 fix work until the record is complete. A record missing
Level 3 means the architectural root was not identified.

---

## Section 3 — Cross-Gap Convergence Protocol

When multiple gaps appear in the same session, check for shared Level 3 roots
before fixing each individually.

```
Step 1 — List all gaps (Level 3 only):
  Gap 1: [Level 3 root cause]
  Gap 2: [Level 3 root cause]
  Gap 3: [Level 3 root cause]

Step 2 — Group by shared root:
  If Gap 1 and Gap 3 share the same Level 3 root:
    → One Level 3 fix closes both
    → Priority: Level 3 fix first, then Level 1 fixes for both

Step 3 — Report shared root count:
  "3 gaps found. 2 share a Level 3 root: the flow manifest has no cross-flow
   event contract. Adding that contract closes gaps 1 and 3. Gap 2 has an
   independent Level 3 root."
```

A shared Level 3 root is always fixed before the individual Level 1 fixes. Fixing
Level 1 without the shared Level 3 fix means the next flow will reproduce the same
pattern.

---

## Section 4 — Rejection Response Protocol

When a proposed fix is challenged by another reviewer or by Luba:

```
The challenged fix is at Level [1/2/3].
The challenge claims the fix is at the wrong level.

Response protocol:
  Step 1: State which level the challenger is operating at.
  Step 2: Agree or disagree at each level explicitly.
  Step 3: If the challenge is correct — restate the fix at the right level.
  Step 4: If the challenge is incorrect — show the Level 3 root cause record
          and explain why it confirms the current level.

NEVER: abandon a valid Level 2 or Level 3 fix because it "feels over-engineered"
       for the immediate symptom. The symptom is Level 1. The structural and
       architectural fixes operate at different scopes.
```

Example rejection response (flows):

```
Challenge: "This is a one-line fix in FLOW-16 — adding a manifest contract is overkill."

Response:
  Level 1 fix (one line in FLOW-16): correct — I will apply it.
  Level 3 fix (cross-flow event contract in the manifest): also required.

  Reason: this is the second flow where the FLOW-16 → FLOW-09 event shape was
  ambiguous. The missing manifest contract is the Level 3 root. Without it, a
  third flow will ask the same question. The contract entry costs a few lines.
  Not adding it costs a future session.
```

---

## Section 5 — Escalation to DESIGN_FLAW

When Level 3 analysis produces a root that contradicts an existing locked
decision (an ADR / decisions-register entry, or a frozen contract), escalate via
the decision-reopening path rather than writing a competing Level 3 fix.

```
Trigger: Level 3 root contradicts a locked flows decision / frozen contract

Action:
  1. Apply Level 1 fix only (immediate — stops the bleed)
  2. Do NOT apply the Level 3 fix
  3. Emit: "DESIGN_FLAW: Level 3 root [X] contradicts locked decision [Y].
           Routing to decision reopening."
  4. The decision-reopening review runs its test before any change
```

---

## Anti-Patterns

1. **"Level 1 fix applied — issue resolved"** — Level 1 stops the bleeding. It
   does not prevent recurrence. An unfixed Level 2/3 root produces the same
   symptom in the next flow.

2. **"Level 3 analysis skipped — too slow"** — Level 3 is 3–5 lines. The cost of
   a missing contract entry that causes a future re-investigation is one full
   session.

3. **"Rejected the Level 3 fix to avoid scope creep"** — scope creep is adding
   features. Writing a contract / manifest / ADR entry is documenting a decision
   that has already been made.

4. **"Each gap fixed independently without a convergence check"** — 3 gaps with a
   shared Level 3 root require 3 Level 1 fixes + 1 Level 3 fix. Without the
   convergence check the Level 3 root stays unclosed and the gaps recur.

---

## Integration

- `diagnostic-escalation-SKILL.md` produces architectural escalation signals → Level 3
- `remediation-session-design-SKILL.md` consumes this skill's K root causes to order fix sessions
- `systematic-debugging-SKILL.md` Phase 4 routes here after 3+ failed fixes
- `data-flow-integrity-SKILL.md` and `event-driven-debugging-SKILL.md` surface Level 3 contract gaps
