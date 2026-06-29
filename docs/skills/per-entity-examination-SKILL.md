# per-entity-examination-SKILL.md — Examine all N instances rigorously before synthesizing

> **Stack adaptation (xiigen flows).** The 7-step protocol (declare entity type
> and counted N → declare hypotheses BEFORE instance 1, max 8 → fixed per-instance
> step sequence, max 5 → fixed result-record format → examine all N in declared
> order, NOT_TESTABLE is a finding → running tally every 10 → synthesize only
> after the Freeze Gate), the Freeze Gate (Completeness / Convergence /
> Implementation-Evidence), and the Examiner-vs-Synthesis session split are the
> UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/per-entity-examination-SKILL.md` (G06 debugging /
> root-cause). The entity types are mapped to this repo:
> - The 48 `flows/FLOW-NN-*` projects — each with the identical
>   `server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}` shape, which
>   makes them an ideal uniform N-instance audit.
> - The Angular 21 standalone clients (`flows/FLOW-NN-*/client/angular`).
> - The shared kernel-fabric interfaces (`IDocumentStore`, `IOutboxStore`,
>   `IIdempotencyStore`, `ISchedulerService`, `IFreedomConfigService`).
> - Other repo entities: per-flow contract files, xUnit test methods within a class.
> - **N is counted, not estimated:** `ls flows | grep -c "FLOW-"` (currently 48).
> - Evidence excerpts are cited as `file:line` over `flows/**/*.cs` and `**/*.ts`.

## Core Problem

A session that examines 3 of 48 flows and synthesizes architectural conclusions
has a ~6% evidence base producing a 100% claim scope. The correction arrives
downstream and forces re-examination — but with the sunk cost of the wrong
synthesis already in the plan. This skill installs the 7-step protocol that makes
multi-entity examination rigorous: declare hypotheses before examining instance 1,
examine all N in a fixed sequence, synthesize only after all N are complete.

---

## When to Invoke

- Any ARCHITECT / PLANNING / INVESTIGATION session examining N≥5 instances of the
  same entity type to answer a system-level question (e.g. "do all flows return
  `DataProcessResult<T>` and register fabrics by interface?")
- When the hypotheses list is empty at the time per-instance records begin appearing

Entity types this protocol governs in this repo: flow projects, Angular clients,
kernel-fabric interfaces, per-flow contracts, xUnit test methods.

---

## Section 1 — The 7-Step Protocol

### Step 1 — Declare entity type and universe size (N)

Before reading any instance file, state:

```
Entity type: [flow projects | angular clients | kernel fabrics | contracts | other]
Total universe (N): [integer] — source: [command that established N]
```

N is counted, not estimated. Record the command, e.g.
`ls flows | grep -c "FLOW-"` → 48.

### Step 2 — Declare hypotheses (minimum 1, maximum 8) before instance 1

Before opening any instance file:

```
H-[N] — [short name]
Claim:        [one sentence asserting what H-N says about the entity type as a whole]
Confirms if:  [specific observable if H-N holds]
Refutes if:   [specific observable that would be found if H-N fails]
Scope:        [which architectural question this tests, or "cross-cutting"]
```

Example (flows): "H-1 — every flow Application method returns `DataProcessResult<T>`;
confirms if every public method in `*.Application` has that return type; refutes
if any returns `void`/raw `bool`/throws for business failure."

**Why before instance 1:** hypotheses formed after reading the first instances
carry confirmation bias. Pre-declaration removes the feedback loop.

**Maximum 8:** more than 8 hypotheses make per-instance records too long to fill
consistently. Consolidate before beginning.

### Step 3 — Define the per-instance step sequence (maximum 5 steps)

Before examining instance 1, define the fixed procedure every instance goes
through:

```
Step 0 — Inventory: list the four server projects + client without reading — what exists
Step 1 — [name]: read *.Application, test H-1 / H-2
Step 2 — [name]: read *.Infrastructure DI, test H-3 (fabric-by-interface)
...
Step N — Verdict: assemble H verdicts from steps 1-N
```

Maximum 5 steps keeps the procedure executable without a full context window per
instance. The same steps apply to every instance in the same order.

### Step 4 — Define the result record format before instance 1

No field may be added mid-examination. Every instance uses the same format:

```
ENTITY_ID:   [FLOW-NN-<slug>]
TITLE:       [human-readable flow name]

STEP_0_INVENTORY:
[Api / Application / Infrastructure / Tests / client present — YES / NO]

H_VERDICTS:
H-1 ([name]): [CONSISTENT | INCONSISTENT | PARTIAL | NOT_TESTABLE] — [one sentence evidence, file:line]
H-2 ([name]): [...]
[... one row per hypothesis]

OPEN_QUESTIONS:
Q[N]: [question] — raised by: [specific finding]
```

Verdict vocabulary — four values, no others:

```
CONSISTENT    Instance evidence supports the hypothesis. Cite file:line.
INCONSISTENT  Instance evidence contradicts the hypothesis. Cite the finding.
PARTIAL       Holds for part of this instance. Name which part holds and which does not.
NOT_TESTABLE  No implemented code / relevant docs to test this hypothesis.
              NOT_TESTABLE is a finding, not a shortcut.
```

### Step 5 — Examine all N instances in declared sequence

No instance is skipped. No instance receives a reduced procedure. The sequence is
stated before examination begins and does not change.

`NOT_APPLICABLE` (step has no material) and `NOT_TESTABLE` (hypothesis cannot be
tested for this instance) are valid findings. An instance with these verdicts is a
complete instance.

### Step 6 — Running tally after every 10 instances

```
RUNNING TALLY — after instance [N]
H-1 ([name]):  CONSISTENT: [n]  INCONSISTENT: [n]  PARTIAL: [n]  NOT_TESTABLE: [n]
H-2 ([name]):  CONSISTENT: [n]  INCONSISTENT: [n]  PARTIAL: [n]  NOT_TESTABLE: [n]
No conclusions drawn. Tally only.
```

The tally is counts with no analysis language. The synthesis session draws
conclusions from the tally. The examining session does not.

### Step 7 — Synthesis only after all N instances are complete

The examining session produces: result records, running tally, and open-question
list. No architectural conclusions. Synthesis is produced in a separate session
after the Freeze Gate (Section 2) passes.

---

## Section 2 — Freeze Gate

The examination is done when all three conditions hold simultaneously:

```
Condition 1 — Completeness
  Every instance in the declared universe has a complete result record.
  Complete means every mandatory field populated (NOT_APPLICABLE and NOT_TESTABLE
  are populated values, not omissions).
  Verification: count result records; count instances with all H_VERDICT fields
  populated. Both counts must equal N.

Condition 2 — Convergence
  The second half of the examination (instances N/2+1 .. N) produced no finding
  classes absent from the first half (1 .. N/2).
  A "finding class" is a structural pattern type, not an instance of a pattern.
  "Some flow Application methods return void" is a finding class.
  "FLOW-16.CheckoutAsync returns void" is an instance of that class.
  If Condition 2 fails: add H-N+1 for the new class; re-examine from the first
  instance where it appeared; check whether earlier instances need re-evaluation.

Condition 3 — Implementation Evidence
  At least one finding per non-NOT_TESTABLE hypothesis cites an IMPLEMENTATION or
  TEST excerpt (file:line in flows/**/*.cs or **/*.ts).
  If Condition 3 fails: produce a HANDOFF — the specific grep command that would
  confirm or deny each unverified hypothesis. The examination is FROZEN until the
  HANDOFF returns. FROZEN is a named state, not "going in circles".
```

**Examination complete** = all three conditions met.
**Synthesis session authorized** when examination is complete.

---

## Section 3 — Examiner vs Synthesis Session

| Role | Produces | Does NOT produce |
|------|----------|-----------------|
| Examiner | Result records, running tally, open-question list | Architectural conclusions, design proposals |
| Synthesis session | Verdict matrix, recommendations from the tally | Per-instance re-examination, new instance reads |

---

## Anti-Patterns

1. **"Pattern is obvious after 5 flows — synthesize now"** — five flows are not a
   system-level sample of 48. The pattern may be a cluster, not the system.

2. **"NOT_TESTABLE is a shortcut"** — it is a finding about that flow's current
   implementation state (e.g. a flow with no event activation).

3. **"I'll define the result record format as I go"** — mid-examination format
   changes produce records that cannot be aggregated.

4. **"The synthesis session can re-examine instances"** — no. If a record is
   incomplete, the examining session finishes it.

5. **"I'll declare hypotheses after I see what the first few show"** — post-hoc
   hypotheses carry the bias of the first instances.

---

## Integration

- `root-cause-ladder-SKILL.md` — open-question fix classes feed the 3-level ladder
- `investigation-handoff-SKILL.md` — the Freeze-Gate HANDOFF format
- `data-flow-integrity-SKILL.md` — per-instance schema checks during a contract audit
- This protocol is the rigorous base for any "do all flows obey rule X?" audit
