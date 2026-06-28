# diagnostic-escalation-SKILL.md — 4-step Gate-B failure diagnosis (TEST_DATA / WIRING / STUB / ALGORITHM)

> **Stack adaptation (xiigen flows).** The four root-cause layers of any Gate-B
> failure (wrong test data, broken wiring, missing implementation, wrong
> algorithm), the ordered 4-step protocol that stops at the first matching layer,
> the proposal format, and the escalate-to-Luba vs fix-autonomously split are the
> UNIVERSAL part, ported from
> `llm_mvp_core/docs/skills/diagnostic-escalation-SKILL.md` (G06 debugging /
> root-cause). Adapted here:
> - **Gate B** = `dotnet test` on the per-flow `Xiigen.Flow.<Name>.Tests` (xUnit)
>   producing a count below the declared passing threshold.
> - **Step 2 WIRING** = the kernel-fabric DI registration in the per-flow host
>   (`Program.cs` / `*ServiceCollectionExtensions.cs`) **and** the project
>   reference in the `.csproj` / `Xiigen.Flows.slnx`.
> - **Step 3 STUB** = `grep -rn "NotImplementedException\|TODO\|FIXME"` over
>   `flows/**/*.cs`.
> - **Iron-rule layer** = the named flow contract rules (DNA Rules 1–4 from
>   `dev-safety-SKILL.md`: `DataProcessResult<T>` everywhere, fabric-via-interface,
>   per-line comments, `MicroserviceBase<TSelf>`), not core DNA Rules 1–10.
> - Result type is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`).
>
> **Sibling-skill note.** This is the 4-step Gate-B *diagnosis* protocol. It is
> distinct from an arbitration/escalation-panel orchestration skill (the
> `llm_mvp_core` family's `escalation-orchestrator`, SK-446, is the arbitration
> panel): that decides *who* arbitrates a multi-reviewer conflict; this decides
> *which layer* a single Gate-B failure lives in. Do not merge the two.

## Core Problem

When a capability fails its Gate B, there are only four possible root causes:
wrong test data, missing implementation, broken wiring, or incorrect algorithm.
Patching the wrong layer wastes cycles — fixing a test when the wiring is broken,
or rewriting the algorithm when a test expectation was wrong. This skill defines
a 4-step diagnosis protocol that identifies the root-cause layer before any fix
is proposed, and distinguishes fixes that require Luba's approval from those that
can proceed autonomously.

---

## When to Invoke

- Any Gate B failure (`dotnet test` produces a count below the declared passing threshold)
- A capability's second remediation cycle still failing the same category as cycle 1
- A capability that regressed (test count dropped since the last green baseline)
- Before proposing any architectural change to a failing flow

One diagnosis before fixing = zero "patched the wrong layer" wasted cycles.

---

## Section 1 — When to Fire

**Situation 1 — Single failure:** one failing test after a change. Diagnosis runs;
one fix proposed.

**Situation 2 — Repeated failure pattern:** the same test category has failed in
3+ consecutive cycles. Diagnosis runs across all cycles; up to 3 fix proposals
ranked by expected impact.

**Situation 3 — Regression:** test count dropped since the previous passing
baseline. Diagnosis runs the regression check first — identify what changed since
the last green state.

**Does NOT fire when:**
- All Gate B conditions pass
- A test fails on a capability's very first cycle (no regression possible)

---

## Section 2 — The 4-Step Diagnosis Protocol

Run steps in order. Stop at the first step that identifies the failure layer. Do
not run all 4 and pick the most interesting — the first match is the root cause.

### Step 1 — Check test data

**Question:** Is the test failing because of wrong input data, not wrong behavior?

```
Check:
  Are hardcoded Arrange values appropriate for this flow scenario?
  Is the in-memory fabric seeded with the documents the Assert expects?
  Does the test exercise the declared scenario or a different one?

If test data is wrong:
  → Failure layer: TEST_DATA
  → Fix: correct the Arrange section / fabric seed in the test
  → STOP — do not patch the Application for a test-data error

If test data is correct:
  → Proceed to Step 2
```

### Step 2 — Check wiring

**Question:** Is the flow reachable by the test? Is the kernel-fabric DI
registration present?

```
Check:
  grep -rn "IDocumentStore\|IOutboxStore\|IIdempotencyStore\|<ServiceName>" \
      flows/FLOW-NN-<slug>/server/*.Infrastructure flows/FLOW-NN-<slug>/server/*.Api/Program.cs
  dotnet build flows/FLOW-NN-<slug>/server   → 0 Error(s) / 0 Warning(s)

If a fabric / service registration is missing:
  → Failure layer: WIRING
  → Fix: add the AddSingleton/AddScoped registration in the host / service-collection ext
  → STOP — an unreachable flow cannot produce correct output

If a project reference is missing:
  → Failure layer: WIRING
  → Fix: add the ProjectReference in the .csproj (and the project to Xiigen.Flows.slnx)
  → STOP

If wiring is correct:
  → Proceed to Step 3
```

### Step 3 — Check implementation completeness

**Question:** Does the implementation have real content, or is it a stub?

```
Check:
  grep -rn "NotImplementedException\|throw new\|TODO\|FIXME" \
      flows/FLOW-NN-<slug>/server/*.Application flows/FLOW-NN-<slug>/server/*.Infrastructure --include="*.cs"

If NotImplementedException found:
  → Failure layer: STUB
  → Fix: implement the method
  → STOP

If the method is empty / returns Success(default) with no logic:
  → Failure layer: STUB
  → STOP

If the implementation has real content:
  → Proceed to Step 4
```

### Step 4 — Check algorithm correctness

**Question:** Is the implementation's logic producing the wrong result?

```
Evidence:
  What does the test expect (Assert section)?
  What is the flow actually producing (the DataProcessResult<T>.Value / ErrorCode)?
  Which named contract rule does the test verify?

If the algorithm contradicts a named contract rule:
  → Two sub-cases:
    A) The rule was derived correctly → ALGORITHM_ERROR: fix the implementation
    B) The rule itself is wrong       → RULE_ERROR: re-derive the rule first

  Distinguish A from B — quality-test the rule:
    □ Can it be violated without an exception?
    □ Is it stated without implementation detail?
    □ Does it have a testable failure consequence?
    □ Does it trace to a specific failure mode?
    If ANY check fails → Sub-case B → re-derive the rule first
    If ALL checks pass → Sub-case A → fix the implementation

If a regression was detected (Situation 3):
  → Read the diff since the last green baseline (working branch only)
  → If a recent change introduced it: the regression fix has highest priority
  → Revert/correct that specific change, not the algorithm generally
```

---

## Section 3 — Proposal Format

```markdown
## Diagnostic Escalation Proposal — [FlowName]

Situation: [1 — single / 2 — pattern / 3 — regression]
Failure layer (from diagnosis): [TEST_DATA / WIRING / STUB / ALGORITHM_ERROR / RULE_ERROR]
Diagnosis step that identified it: [Step N]

Evidence:
  Test name: [exact failing test name]
  Expected: [what Assert expects]
  Actual: [what was produced — DataProcessResult value / ErrorCode]
  Diagnosis evidence: [specific grep result or diff that identified the layer]

Fix proposal:
  Type: [TEST_CORRECTION / WIRING_FIX / STUB_IMPLEMENTATION / ALGORITHM_FIX / RULE_REDERIVATION]
  Target: [exact flows/FLOW-NN-*/...:line]
  Change: [specific change — not "fix the bug"]

Expected result after fix: dotnet test --filter "FullyQualifiedName~[Flow]" → [N] passed, 0 failed
Regression risk: LOW / MEDIUM / HIGH
  HIGH = change affects a shared kernel-fabric contract or > 2 other flows
```

---

## Section 4 — Escalation Triggers

Some failures must be escalated to Luba before any fix is applied:

```
Escalate immediately when:
  □ Fix would change a flow DNA rule (dev-safety Rules 1–4) — no rule change without approval
  □ Fix would change a locked flows decision / frozen contract — cannot override
  □ Fix would change a shared kernel-fabric interface (IDocumentStore, IOutboxStore, ...) — breaks all consumers
  □ Same failure has persisted 3+ cycles with different fix attempts — architectural problem
  □ Regression check finds a change that was explicitly reviewed and approved — re-opening approved work

Do NOT escalate:
  □ TEST_DATA fixes (wrong Arrange values / unseeded fabric)
  □ WIRING fixes (missing DI registration or project reference)
  □ STUB implementations (filling in a NotImplementedException)
  □ ALGORITHM_FIX that affects only the failing test's scenario
```

---

## Section 5 — Escalation Briefing Format

When escalation is required, give Luba a complete briefing:

```markdown
## Diagnostic Escalation to Luba

WHY: [one sentence — specific reason for escalation]
TRAJECTORY: [cycle 1: failed category X / cycle 2: same / cycle 3: same]
BEST STATE: [last passing baseline and what changed since]

Gap analysis:
  Test: [name]
  Violation: [which named contract rule or interface is violated]
  Root cause: [the specific structural issue, not just the symptom]
  Likely fix: [what structural change would resolve it]
  Fix type: [INTERFACE_CHANGE / DNA_RULE_CHANGE / LOCKED_DECISION_CHANGE]

Options:
  [A] [concrete action — cost, likely outcome]
  [B] [concrete alternative — different tradeoff]
  [C] Accept current state with acknowledged gap: [what is the known gap]
```

---

## Anti-Patterns

1. **"Fixing the algorithm when the wiring is broken"** — an unreachable flow
   cannot produce correct output regardless of algorithm quality. Steps 1–3 must
   pass before Step 4 is relevant.

2. **"Patching the test to match the wrong output"** — a test adjusted to match
   wrong behavior is a hidden failure. Distinguish TEST_DATA (wrong input) from
   ALGORITHM_ERROR (wrong output for correct input).

3. **"Three cycles of the same failure, try a fourth"** — three cycles without
   improvement is an architectural problem. Escalate.

4. **"The contract rule must be wrong because the test fails"** — the test failing
   means either the implementation is wrong OR the rule is wrong. Apply the
   quality test before deciding.

5. **"Regression fix applied before checking what changed"** — read the diff
   first (Step 4 regression check).

---

## Integration

- `systematic-debugging-SKILL.md` Phase 4 invokes this skill when cycle ≥ 2 still fails
- `root-cause-ladder-SKILL.md` consumes the architectural (Step 4 / RULE_ERROR) signal as a Level 3 root
- `dev-safety-SKILL.md` defines the named DNA rules that Step 4 checks
- `data-flow-integrity-SKILL.md` Steps 2–3 cover wiring/stub evidence
