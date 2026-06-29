# Artifact Numbering

> **Stack adaptation (xiigen flows).** The live-document numbering discipline
> ported from `llm_mvp_core/docs/skills/artifact-numbering-SKILL.md` (G07). The
> universal part (NEVER take artifact numbers from a plan file, a session summary,
> or memory; before assigning, read the live canonical registry, cross-check for a
> collision, reserve the number in STATE immediately, never reuse a deleted
> number) is taken verbatim from core. Adapted here:
> - The **canonical registry** is the live `flows/FLOW-NN-<slug>/` directory listing
>   plus each `flows/FLOW-NN-<slug>/flow.manifest.json` and the per-flow STATE-JSON
>   — not `DECISIONS-LOCKED.md` / `SUBPROJECT-MAP.md` (those are llm_mvp_core docs).
> - **Decision and bug numbers are per-flow**: `FLOW-NN-D-MM` / `FLOW-NN-BUG-MM`,
>   recorded in that flow's locked-decisions doc and per-flow STATE-JSON.
> - A **new flow number** is the next free `FLOW-NN` in the live registry.
> - Reservations are written to the per-flow STATE-JSON next to
>   `flow.manifest.json`; commits land on the current working branch
>   (`UpdateUniversalSkills` / `DesignToCode` / `claude/<worktree>`), never
>   `main`/`master`.

## The Core Rule

**NEVER use artifact numbers from a plan file, a session summary, or memory.**
Numbers in plans decay. The live canonical registry is the only authoritative source.

At the start of every session that assigns new numbers:
```bash
# Read live registry — do not use any cached or plan-file numbers
ls -d flows/FLOW-*/ | sort                                  # highest FLOW-NN in use
grep -rn "FLOW-[0-9][0-9]-D-[0-9]" flows/*/docs 2>/dev/null  # per-flow decision numbers
grep -rn "FLOW-[0-9][0-9]-BUG-[0-9]" flows/*/                # per-flow bug numbers
```

---

## When to Invoke

- When assigning a new flow number (`FLOW-NN`)
- When assigning a new per-flow decision number (`FLOW-NN-D-MM`)
- When assigning a new per-flow bug number (`FLOW-NN-BUG-MM`)
- When assigning a new phase number (`P-N` in the per-flow STATE-JSON)
- Before any per-flow STATE-JSON is saved with artifact numbers

---

## Numbering Protocol

### Step 1: Read live numbers (MANDATORY before assigning)

```bash
# Highest flow number in use
ls -d flows/FLOW-*/ | sed -E 's#flows/FLOW-([0-9]+).*#\1#' | sort -n | tail -1

# Highest decision number for the flow in scope (FLOW-16 example)
grep -rn "FLOW-16-D-[0-9]" flows/FLOW-16-marketplace-payments/docs 2>/dev/null | tail -5

# Highest bug number for the flow in scope
grep -rn "FLOW-16-BUG-[0-9]" flows/FLOW-16-marketplace-payments/ 2>/dev/null | tail -5
```

Record in the per-flow STATE-JSON before assigning new numbers:
```json
{
  "artifactNumbersRead": {
    "readAt": "session-start",
    "highestFlowNumber": "FLOW-47 (from flows/ listing)",
    "highestDecisionNumber": "FLOW-16-D-04 (from flows/FLOW-16-marketplace-payments/docs)",
    "highestBugNumber": "FLOW-16-BUG-02 (from the per-flow STATE-JSON)"
  }
}
```

### Step 2: Cross-check before claiming a number

Before assigning `FLOW-16-D-05`, verify it is not already in use:
```bash
grep -rn "FLOW-16-D-05" flows/FLOW-16-marketplace-payments/
# Expected: 0 matches — safe to assign
```

### Step 3: Assign sequentially

- Flow numbers: next sequential integer after the highest `FLOW-NN` in use
- Decision numbers: next sequential integer after the highest `FLOW-NN-D-MM` in use
- Bug numbers: next sequential integer after the highest `FLOW-NN-BUG-MM` in use
- Never skip numbers — gaps create audit confusion
- Never reuse a number from a deleted or abandoned artifact

### Step 4: Record the assignment in the per-flow STATE-JSON immediately

```json
{
  "numbersAssignedThisSession": {
    "flows": [],
    "decisions": ["FLOW-16-D-05"],
    "bugs": []
  }
}
```

This prevents a second session (or another worktree) from claiming the same
number before the first session's commit is pushed.

---

## Collision Prevention

| Collision type | Prevention |
|---|---|
| Two sessions assign same FLOW-16-D-MM | Both check the per-flow STATE-JSON at session start; higher session uses next available |
| Plan file says FLOW-16-D-05 but the live flow docs already have FLOW-16-D-05 | Live registry wins; plan file number is stale |
| FLOW-16-BUG-03 assigned but not yet committed | Reserve in the per-flow STATE-JSON; do not reuse until commit confirmed |
| New flow number collides across worktrees | Read the live `flows/` listing in this worktree; reserve in STATE before creating the directory |

---

## Numbers at Plan Creation (REFERENCE ONLY — DO NOT USE)

Numbers recorded in a REFERENCE or EXECUTION file at plan creation time may be
stale by the time the session runs. Always read the live registry before assigning.

```
FLOW-16-D-04 (plan creation) -> READ REGISTRY before assigning next FLOW-16-D-MM
FLOW-16-BUG-02 (plan creation) -> READ REGISTRY before assigning next FLOW-16-BUG-MM
```

---

## Anti-Patterns

1. **"The plan says the next decision is FLOW-16-D-05."** Plan numbers decay. Read the live registry.
2. **"I'll assign the number and update the flow docs later."** Assign only after
   reading live. Record in the per-flow STATE-JSON immediately.
3. **"This decision number is unused — I'll reclaim it."** Never reuse a number.
   Use next sequential.
4. **"Only one session runs at a time."** Multiple sessions may run in different
   worktrees. The per-flow STATE-JSON reservation prevents collision.
