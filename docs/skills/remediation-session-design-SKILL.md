# remediation-session-design-SKILL.md — Root causes → ordered fix-session plan with a Gate B per group

> **Stack adaptation (xiigen flows).** The whole pipeline is the UNIVERSAL part,
> ported from
> `llm_mvp_core/docs/skills/remediation-session-design-SKILL.md` (G06 debugging /
> root-cause): take `root-cause-ladder-SKILL.md` output (K root causes ordered by
> dependency, NOT raw gaps) → classify each by fix class (CONTENT / EXTENSION /
> INFRASTRUCTURE / NEW_CAPABILITY) → blast radius by class (file/line thresholds →
> LOW/MEDIUM/HIGH; > 10 files always isolated) → assign a session type → order
> groups Level-3 → Level-2 → Level-1 and INFRASTRUCTURE → EXTENSION → CONTENT
> (registration before injection) → **a mandatory `Gate B:` line on EVERY group**
> ("works correctly" is not a Gate B) → the group format
> (Files / Lines / Blast / Depends-on / Blocks / Fixes / Pre-flight / Gate B).
> The worked example in core used ML subjects (IncrementalTrainer /
> WeightedScoringService); those are illustrative only — this skill is pure
> planning/governance discipline with no ML internals, so the example below uses
> flows subjects. Adapted:
> - **File/line estimation** = `grep` over `flows/**/*.cs` (+ `**/*.ts` for an
>   Angular client) and `wc -l`.
> - **INFRASTRUCTURE** = a new per-flow contract (`contracts/`) or a new
>   kernel-fabric interface → always its own session.
> - **Blast radius** is read off the real per-flow layout
>   `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`.
> - **Group dependencies** come from `flow.manifest.json` (cross-flow) + the
>   root-cause-ladder dependency order.
> - **Gate B per group** = `dotnet test --filter "FullyQualifiedName~<Flow>"`
>   returns "N passed, 0 failed"; for INFRASTRUCTURE groups also
>   `dotnet build Xiigen.Flows.slnx` → 0 Error / 0 Warning; for an Angular UI
>   group, `npx playwright test` in the flow's `client/angular`.
> - Domain wrapper is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`).

## Core Problem

Raw gaps do not map to sessions. A list of 12 gaps may collapse to 3 root causes
(via `root-cause-ladder-SKILL.md`), and those 3 may group into 2 sessions with a
clear dependency. Designing sessions from raw gaps produces 12 sessions instead of
2. This skill takes root-cause-ladder output as input and produces an ordered
session plan: groups of fixes, each with a session type, blast-radius
classification, dependency chain, pre-flight assumptions, and a Gate B.

---

## When to Invoke

- After `root-cause-ladder-SKILL.md` produces K root causes from N gaps
- Before scheduling remediation work (to avoid 12 sessions when 2 suffice)
- When a prior remediation session failed to close the gap (re-plan from root causes)
- As the planning output when a gate review produces multiple BLOCK verdicts

---

## Prerequisite

Run `root-cause-ladder-SKILL.md` first. This skill starts from its output: K root
causes, each with a dependency order.

```
Expected input from root-cause-ladder-SKILL.md:
  Root cause 1: [description] — covers gaps [N, M, P]
  Root cause 2: [description] — covers gaps [Q, R]
  Dependency: Root cause 1 must be fixed before root cause 2
```

If you have raw gaps but have not run root-cause-ladder: run it first. Do not group
raw gaps into sessions — collapse to root causes first.

---

## Section 1 — Fix Class Taxonomy

Apply this taxonomy to each root cause before assigning a session type.

| Fix class | Definition (flows) |
|---|---|
| CONTENT | Change to existing logic in a flow — no new interfaces, no new registrations |
| EXTENSION | Add to existing structure — new method on an existing flow service, new manifest edge |
| INFRASTRUCTURE | New per-flow contract OR new kernel-fabric interface / registration required |
| NEW_CAPABILITY | New flow or new service + interface + tests (full capability) |

**Blast radius by fix class:**

```
CONTENT:        ≤ 3 files, ≤ 50 lines  → LOW    → may batch with other CONTENT fixes
CONTENT:        ≤ 3 files, 50–150 lines → MEDIUM → own session if functionally unrelated
EXTENSION:      ≤ 5 files, ≤ 150 lines, no new interfaces → MEDIUM → may batch if contained
EXTENSION:      > 5 files OR blast radius HIGH → own session
INFRASTRUCTURE: always own session (new interface/contract = blast radius always HIGH)
NEW_CAPABILITY: always own session + plan-review Gate before execution
```

**Hard rule:** > 10 files changed in one session → always its own isolated
session. A 10-file change in a maintenance session is a test-failure risk.

---

## Section 2 — Session Type Assignment

| Fix class | Session type | Rule |
|---|---|---|
| CONTENT (LOW) | MAINTENANCE (batched) | ≤ 3 files, ≤ 50 lines → combine CONTENT fixes |
| CONTENT (MEDIUM) | MAINTENANCE (isolated) | functionally unrelated → own session |
| EXTENSION (MEDIUM) | MAINTENANCE (isolated) | contained blast radius → may batch |
| EXTENSION (HIGH) | MAINTENANCE (isolated) | > 5 files → own session |
| INFRASTRUCTURE | MAINTENANCE + gate review | plan-review FC check required |
| NEW_CAPABILITY | EXECUTOR session | own session + Gate B declaration |

---

## Section 3 — Session Group Format

After classifying each root cause, produce an ordered session plan. **Every group
carries an explicit `Gate B:` line** — a group without a Gate B is a failed plan.

```markdown
## Remediation Session Plan

### Group A — [session type]: [one-line description]
Fix class:    [CONTENT / EXTENSION / INFRASTRUCTURE / NEW_CAPABILITY]
Files:        [list of files — flows/FLOW-NN-*/...]
Lines:        ~[N] estimated
Blast radius: LOW / MEDIUM / HIGH
Depends on:   [nothing / Group X]
Blocks:       [Group Y / nothing]

Fixes:
  A1: [root cause 1 description] — [file:what to change]
  A2: [root cause 2 description] — [file:what to change]

Pre-flight assumptions:
  INFRA-1: [assumption] | verify: [bash command] | expected: [result]
  BEHAV-1: [assumption] | verify: [bash command] | expected: [result]

Gate B:
  [exact passing condition — a command + count, e.g.
   dotnet test --filter "FullyQualifiedName~FLOW16" → N passed, 0 failed]

### Group B — [session type]: [description]
Depends on:   Group A complete
Blocks:       Group C
...
```

---

## Section 4 — Dependency Ordering Rules

Order groups by:

1. **Dependency chain from `root-cause-ladder-SKILL.md`** — Level 3 (architectural:
   contract / manifest / fabric interface) before Level 2 (structural: skill /
   pre-phase check) before Level 1 (immediate code change in a flow)
2. **Within the same level:** INFRASTRUCTURE before EXTENSION before CONTENT
3. **Infrastructure before capability** — a flow injects what infrastructure
   registers; the registration must exist before the injection

```
Example ordering:
  Group A: INFRASTRUCTURE — adds a new kernel-fabric registration (Level 3, first)
  Group B: EXTENSION — wires that fabric into an existing flow service (Level 2, depends on A)
  Group C: CONTENT — fixes the flow logic that uses it (Level 1, depends on B)
```

---

## Section 5 — Worked Example

**Input from `root-cause-ladder-SKILL.md` (3 root causes):**

```
Root cause 1: CheckoutAsync omits platformFeeBps from the document body when freedom returns a default
  → covers: gaps 1, 3 (dashboard throws on legacy docs; payout fee under-charged)
  Level: CONTENT fix (one flow)
Root cause 2: MarketplacePaymentsService does not validate the idempotency key on the refund path
  → covers: gap 2 (duplicate refunds possible)
  Level: EXTENSION fix (adds a guard + uses IIdempotencyStore on the refund path)
Root cause 3: the flow manifest has no cross-flow event contract for FLOW-16 → FLOW-09
  → covers: gap 4 (the same event-shape question recurs in every payments flow)
  Level: ARCHITECTURAL (write the contract into flow.manifest.json / the per-flow contract)
Dependency: Root cause 3 must be recorded before 1 and 2 are fixed
  (the fixes encode the contract; the contract must be locked first)
```

**Output:**

```markdown
### Group A — MAINTENANCE + gate review: Record the FLOW-16 → FLOW-09 event contract
Fix class:    INFRASTRUCTURE (contract)
Files:        flows/FLOW-16-marketplace-payments/contracts/endpoints/...  + flow.manifest.json
Lines:        ~15
Blast radius: HIGH (contract — affects every consumer of the event)
Depends on:   nothing
Blocks:       Group B, Group C

Fixes:
  A1: Add the "PaymentCaptured" event contract (payload schema + consumer = FLOW-09) to the manifest/contract

Pre-flight:
  INFRA-1: flow.manifest.json exists for FLOW-16 | verify: ls flows/FLOW-16-marketplace-payments/flow.manifest.json | expected: 1 hit

Gate B: dotnet build Xiigen.Flows.slnx → 0 Error, 0 Warning
        AND grep -n "PaymentCaptured" flows/FLOW-16-marketplace-payments/contracts -r → ≥ 1 hit

### Group B — MAINTENANCE: Fix CheckoutAsync to write a resolved default fee
Fix class:    CONTENT
Files:        flows/FLOW-16-marketplace-payments/server/*.Application/MarketplacePaymentsService.cs
Lines:        ~15
Blast radius: LOW (one flow)
Depends on:   Group A (contract locked)
Blocks:       nothing

Fixes:
  B1: CheckoutAsync resolves the platformFeeBps default and always writes it to the document body

Gate B: dotnet test --filter "FullyQualifiedName~MarketplacePayments" → N passed, 0 failed

### Group C — MAINTENANCE: Add idempotency guard on the refund path
Fix class:    EXTENSION
Files:        flows/FLOW-16-marketplace-payments/server/*.Application/MarketplacePaymentsService.cs
Lines:        ~20
Blast radius: LOW (one flow service)
Depends on:   nothing (independent of B)

Fixes:
  C1: RefundAsync calls IIdempotencyStore before mutating state, returning Failure<T>("DUPLICATE_REFUND", ...) on replay

Gate B: dotnet test --filter "FullyQualifiedName~MarketplacePayments&Refund" → ≥ N passed, 0 failed
```

---

## Anti-Patterns

1. **"Grouping raw gaps into sessions without running `root-cause-ladder-SKILL.md` first"**
   — 12 gaps may collapse to 3 root causes. Grouping 12 gaps = 12 sessions.

2. **"Assigning a session type before estimating file count"** — file count
   determines blast radius; blast radius determines session type.

3. **"Batching high-blast-radius fixes 'for efficiency'"** — a 15-file change in
   one session is a test-failure risk. The extra session is cheaper.

4. **"Starting Group B before Group A completes"** — dependency chains are not
   optional. Group A locked the contract that Group B encodes in code.

5. **"Gate B omitted from a group"** — every group must have a Gate B. "Works
   correctly" is not a Gate B.

---

## Integration

- `root-cause-ladder-SKILL.md` provides the K root causes this skill starts from
- `solution-scope-gate-SKILL.md` classifies the fix class per gap
- `phase-preflight-SKILL.md` verifies the pre-flight assumptions before each group
- `dev-safety-SKILL.md` SESSION GATE is the per-group Gate B baseline (build + per-flow test)
