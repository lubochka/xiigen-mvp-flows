# goal-context-persistence-SKILL.md — Keep the goal a first-class persistent artifact

> **Stack adaptation (xiigen flows).** Universal goal-persistence protocol ported from
> `llm_mvp_core/docs/skills/goal-context-persistence-SKILL.md`. Adapted to this repo:
> - progress deltas are stated in flows product terms (per-flow capability or client behaviour),
>   e.g. "FLOW-04 attendance moves from 0 to 1 tested path", "FLOW-29 client moves from
>   unrendered to e2e-covered list"; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`;
> - the goal context record is stored as JSON in this repo (e.g. a `STATE-*.json` / session
>   note under `docs/`);
> - core ML/`ILearningMachine`/DPO framing is core-only (G12) and not used for flows deltas.

## When to load
- At session start, before first synthesis.
- At every STOP (Goal Reminder block required at top of output).
- At designated checkpoints for sessions exceeding 10 turns.
- Whenever output direction feels unclear — re-read the goal.

## Section 1 — Goal loading protocol

1. **Capture the goal verbatim** — exact characters Luba typed, not a paraphrase. Word choice
   encodes constraints ("expose X" ≠ "surface X").
2. **If no explicit goal** — ask Luba; do not infer. A session without a declared goal has no
   anchor against which drift can be detected.
3. **If multiple goals** — capture as a list and track each separately. Untracked multi-goal
   sessions are exactly how 3-of-4 coverage failures happen.
4. **The goal is immutable for the session.** If Luba restates/refines it, that is a new
   session, not an edit.

## Section 2 — Goal Reminder block (required at every STOP)

```markdown
## Goal Reminder (goal-context-persistence-SKILL)

Luba's stated goal for this session:
"[verbatim goal statement]"

This session advances the goal by:
[ONE SENTENCE — concrete observable progress]
"By [verb] [specific artifact], [specific goal element] moves from [prior state] to [new state]."

What remains to complete the goal: [specific outstanding items or "COMPLETE"]
```

The "This session advances the goal by" field is mandatory. If the session cannot state
concrete observable progress, it produced no goal-relevant output — surface that explicitly.

FAILS the concrete-progress test: "working on goal X" (no verb of completion), "making progress
on Y" (no observable delta), "explored implications of Z" (no state change).

PASSES (flows examples):
- "By adding `UserRegistrationService` + its tests, FLOW-01 moves from contract-only to a
  tested idempotent registration path."
- "By writing 8 xUnit tests for the conflict path, FLOW-01 moves from 0 to 1 tested Fail path."
- "By adding the Playwright e2e, the FLOW-29 client moves from unrendered to verified-list."

## Section 3 — Goal checkpoint for long sessions

Sessions over 10 turns are subject to drift regardless of per-turn quality.

| Turn | Action |
|------|--------|
| 5 | Re-read goal. Note alignment verdict. |
| 10 | Re-read. Note alignment verdict. |
| 15, 20, 25, ... | Re-read. Checkpoint each 5 turns. |

Alignment verdicts:
- **ON_GOAL** — recent turns produced output traceable to the goal. Continue.
- **DRIFTING** — recent turns respond to prior output rather than the goal. Next turn must
  re-anchor.
- **OFF_GOAL** — recent turns answer a different question. Stop and ask Luba for redirection;
  do not silently continue.

## Section 4 — Goal context record format

```json
{
  "goalContext": {
    "statement": "[verbatim goal statement]",
    "capturedAt": "2026-06-28",
    "goalElements": ["element 1", "element 2"],
    "checkpoints": [
      { "turn": 5, "progressStatement": "[what objectively changed toward the goal]", "alignmentVerdict": "ON_GOAL" }
    ],
    "goalComplete": false
  }
}
```

## Section 5 — Goal drift signals (self-check before every STOP)

| Signal | Threshold | Action |
|--------|-----------|--------|
| Rounds since last goal reference in output | ≥ 5 | Re-anchor |
| Consecutive DRIFTING checkpoints | ≥ 2 | STOP, re-anchor |
| Session length > 10 turns without checkpoint | any | Force checkpoint |
| "This advances the goal by" is empty/abstract | any | Rewrite turn |
| Luba's recent messages contain a correction ("no — X", "stop doing Y") | any | STOP, re-anchor immediately |

The last signal is the most reliable drift detector: if Luba corrects direction, the session
has drifted, regardless of its internal sense of alignment.

## Anti-patterns

1. "The goal is obvious, I don't need to quote it" — obvious goals drift fastest because no one
   watches them.
2. "I'll paraphrase the goal for cleanliness" — capture verbatim; word choice matters.
3. "Luba hasn't objected, so we're on-track" — silence is not alignment; run checkpoints.
4. "Progress is abstract" — if progress is abstract, the turn produced little goal-relevant
   output; surface that rather than hiding it in vague language.
