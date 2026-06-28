# score-zero-investigation-SKILL.md — A named check is never deleted to clear it

> **Stack adaptation (xiigen flows).** The universal protocol is: a *named check*
> (a named quality gate / contract assertion) is NEVER removed to make the gate
> green; investigate first, then apply one of three fix classes, with a
> silent-failure simulation before Class B/C. Ported from
> `llm_mvp_core/docs/skills/score-zero-investigation-SKILL.md` (G06 debugging /
> root-cause). **R5 boundary:** flows is greenfield and has no DPO / scoring /
> model-training — common models stay in `llm_mvp_core` and are consumed via
> manifests/locators. So this skill ports the *protocol*, not the ML mechanics.
> Here a "named check" is a **named flow quality gate / contract test**, e.g. a
> BFA rule in `*BfaRuleRegistry.cs`, a per-flow contract assertion, or a named
> xUnit test that encodes a DNA/iron rule (`dev-safety-SKILL.md` Rules 1–4).
> Adapted:
> - "Removing the check to clear the score" → "deleting/skipping a named gate to
>   make `dotnet test` green" — forbidden.
> - The three fix classes map to: A = the rule is not in the context/spec; B = the
>   check over-fires on a legitimately-correct variant → narrow the trigger; C =
>   the contract/iron rule itself is wrong → revise the contract (decision
>   reopening) before touching the check.
> - "Compiles + passes tests but teaches a wrong pattern" → "passes `dotnet test`
>   but encodes a wrong practice into the flow corpus / contract".
> - Result type is `DataProcessResult<T>` from `Xiigen.Kernel` (NOT core
>   `OperationResult<T>`).

## Core Problem

A named check that produces a zero score (or consistently fires when it should
not) must be investigated before any resolution is applied. The most dangerous
mistake is removing the named check: the check exists because a contract / iron
rule exists, and removing it to clear the gate creates a silent failure — code
that compiles, passes `dotnet test`, and looks correct, but encodes a wrong
practice. This skill defines the three fix classes, the six-step investigation
protocol, and the silent-failure detection that runs before any resolution.

---

## When to Invoke

- When a named flow gate / contract test fails to zero (or consistently fires) for 3+ sessions
- When a previously-green named gate suddenly drops to failing after a patch
- When a failure is classified PERSISTENT (N ≥ 3 cycles) by `diagnostic-escalation-SKILL.md`

---

## Rule 0 — Named Checks Are Never Removed

A named check (a named BFA rule, contract assertion, or iron-rule test) is never
removed until the investigation completes and a Class B or Class C resolution is
explicitly approved.

```
Investigation in progress → named check stays active
Class A resolved          → named check stays (problem was the spec, not the check)
Class B resolved          → named check stays (trigger narrowed, not removed)
Class C resolved          → named check may be modified (contract revision required)
```

The only Class C path that touches the check itself still requires a full contract
revision (the locked-decision / contract update + cross-flow validation) before
the check is modified. Deletion is never the resolution. **Skipping a named test
with `[Fact(Skip=...)]` to make the suite green is deletion in disguise — also
forbidden.**

---

## Section 1 — Three Fix Classes

### Class A — Spec/Context Addition

The implementer did not know the rule. The check is correct; the spec / context
is missing the instruction.

```
Symptom:   Gate fails on the first occurrence of a flow scenario class
           OR a new contract rule was added after the spec was written
Diagnosis: The flow cannot satisfy the check because the rule is not present in the spec/contract
Fix:       Add the named rule + a WRONG/CORRECT example to the per-flow contract/spec
Named check stays: YES — the gap was in the spec, not the check
```

### Class B — Check Over-Fires

The check fires on patterns that legitimately satisfy the underlying rule through
a different (correct) path.

```
Symptom:   Gate fails but the flow output appears correct
           OR fails only for a specific valid variant (e.g. an expression-bodied
           Application method returning DataProcessResult<T> directly)
Diagnosis: The trigger pattern is too narrow — it matches only one valid form
Fix:       Narrow the trigger: add an exception clause to the check
           DO NOT remove the check — add the exception
Named check stays: YES — with the exception clause added
```

### Class C — Contract / Iron Rule Wrong

The rule encoded in the check itself is incorrect or superseded by a locked
decision.

```
Symptom:   Investigation shows the check fires on patterns explicitly permitted
           by a locked flows decision / frozen contract
           OR the rule was written for an older flow architecture
Diagnosis: The contract needs revision, not the code that satisfies it
Fix:       Requires full contract revision:
             1. Load the locked decision / contract — verify the conflict is real
             2. Propose a decision-reopening challenge
             3. Update the rule with the correct constraint
             4. Cross-validate against every flow that uses this check
             5. Re-run the gate for all affected flows
Named check stays: YES — modified, never deleted
```

---

## Section 2 — Six-Step Investigation Protocol

```
Step 1: Identify which named check fired
  grep -rn "RuleId\|<CheckName>" flows/FLOW-NN-<slug>/server/*.Application --include="*.cs"
  Record: CheckId/RuleId, FlowScenario, how many sessions it has fired

Step 2: Trace to the rule it encodes
  Load the rule from the per-flow contract / BFA registry
  Confirm: is this a DNA rule (dev-safety 1–4), a locked decision, or a flow-specific rule?

Step 3: Locate the violation line
  Run the flow / test for this scenario; identify the exact line that triggers the check
  Record: the literal code line and the file:line

Step 4: Classify the fix
  □ Is the rule present in the spec/contract? NO → Class A
  □ Does the code satisfy the rule through a different valid path? YES → Class B
  □ Does a locked decision permit the code pattern? YES → Class C

Step 5: Silent-failure simulation (required for all Class B and Class C)
  Silent failure = code that compiles, passes dotnet test, and looks correct,
  but encodes a wrong practice into the flow corpus / contract.
  Simulation:
    Produce 5 outputs with the current check active
    Produce 5 outputs with the proposed exception/modification
    Compare: do any modified outputs teach a wrong pattern?
    If yes: the exception is too broad — narrow further before applying.

Step 6: Resolution record
  Record in the flow's STATE / investigation log:
  { "checkId": "...", "fixClass": "B", "flow": "FLOW-16",
    "resolutionApplied": "added expression-body exception",
    "silentFailureSimulation": "PASSED",
    "checkModified": false, "checkDeleted": false }
```

---

## Section 3 — Silent Failure Subtypes

```
Subtype 1: Compiles + passes tests but wrong API
  Example: an Application method returns void instead of DataProcessResult<T>, and
           every test uses the void path so they pass
  Detection: check that tests assert on .IsSuccess / .ErrorCode of the result

Subtype 2: Correct structure but wrong semantics
  Example: Success(...) returned with an empty body when the contract requires a populated payload
  Detection: check that tests assert on .Value, not just .IsSuccess

Subtype 3: Correct pattern but a configurable value hardcoded
  Example: a fee threshold hardcoded instead of read from IFreedomConfigService
  Detection: grep for the literal; it must come from freedom config, not a constant
```

---

## Anti-Patterns

1. **"Removing/skipping the named check to clear the gate"** — the most
   destructive action. The check encodes a contract. Removing it makes the flow
   corpus teach the wrong pattern.

2. **"Skipping the silent-failure simulation for Class B"** — a too-broad
   exception allows patterns that look correct but encode wrong practices.

3. **"Treating a Class C (contract wrong) as a Class A (spec missing)"** — adding
   a spec note to fix a contract violation makes the contract self-contradictory.

4. **"Applying a Class B fix without cross-flow impact check"** — an exception in
   a shared BFA rule affects every flow that uses it. Validate across flows first.

5. **"`[Fact(Skip)]` to make the suite green"** — a skipped contract test is a
   hidden failure, not a pass.

---

## Integration

- `diagnostic-escalation-SKILL.md` routes a PERSISTENT named-check failure here
- `root-cause-ladder-SKILL.md` — a Class C contract revision is a Level 3 architectural fix
- `dev-safety-SKILL.md` defines the DNA rules that named checks encode
- `test-integrity-SKILL.md` — the rule against weakening assertions to pass
