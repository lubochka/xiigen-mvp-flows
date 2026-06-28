# Generated Code Review

> **Stack adaptation (xiigen flows).** The 5-layer generated-code review ported
> from `llm_mvp_core/docs/skills/generated-code-review-SKILL.md` (G04). Adapted
> here:
> - The generated/contract artifact in this repo is **per-flow server/contract
>   code** under `flows/FLOW-NN-*/server/Xiigen.Flow.<Name>.{Application,Infrastructure}`
>   and JSON contracts under `contracts/schemas`.
> - Layer-2 DNA greps run against `*.cs`; the Result type is `DataProcessResult<T>`
>   (XIIGen family wrapper, **not** core `OperationResult<T>`); services inherit
>   `MicroserviceBase<TSelf>`.
> - Layer-3 silent-failure tests are xUnit `[Fact]` with MACHINE constants:
>   `Assert.True(predicate.Evaluate(match))` AND `Assert.False(...nonMatch...)`.
> - Build half of every Gate-B is `dotnet build Xiigen.Flows.slnx` with **0 errors
>   AND 0 warnings** (`TreatWarningsAsErrors=true`).
> - **Layer 5 (trainable-vs-stub)** applies only when a flow actually contains a
>   trainable/learned unit. By the G12 / R5 boundary, ML / DPO / `ILearningMachine`
>   logic normally stays in `llm_mvp_core`; a flow that merely consumes a core model
>   through a `.xiigen` / wiki-domain manifest is a **scaffold / honest-untrained**
>   consumer and must be labelled as such — it must NOT borrow trained-feature
>   wording. If a flow genuinely ships its own trainable unit, Layer 5 numeric
>   held-out metrics become mandatory and UNKNOWN/validation-only fails.

## When to Apply

Apply this skill to any code produced by a flow generator/codegen step, and to
any per-flow contract or service file produced by an executor work order. Apply
**all** layers including Layer 5 below for any deliverable a plan calls
"trainable", "learned", "model", or "DPO" — otherwise Layer 5 is the
scaffold/honest-untrained label check.

Generated code cannot be verified by inspection alone. This skill defines the
5-layer review that replaces visual inspection.

For trainable-unit deliverables, contract/build/DNA evidence is NOT sufficient —
Layer 5 (numeric metrics) is required. This is the V20 anti-stub fix.

---

## Layer 1 — Semantic Review

Check that names and comments reflect intent, not implementation accidents.

```csharp
// Review questions for each generated class/method:
// 1. Does the class name describe what it represents (not what generated it)?
// 2. Does every method name describe the action, not the algorithm?
// 3. Are there any placeholder names? ("Item1", "Temp", "Result1")
// 4. Are comments present on every line?
// 5. Are all abbreviations expanded on first use?
```

**Gate B for Layer 1:**
```bash
grep -n "Item[0-9]\|Temp[0-9]\|Result[0-9]\|TODO\|FIXME\|PLACEHOLDER" [generated-file.cs]
→ 0 matches
```

---

## Layer 2 — DNA Compliance

Run the DNA grep checks against the generated file:

```bash
# Rule 3: No throw for business logic
grep -n "throw new" [generated-file.cs]
→ 0 matches (or only truly exceptional infrastructure paths with a comment)

# Rule 4: No direct fabric instantiation
grep -n "new .*Reader\|new .*Writer\|new .*Store\|new .*Broker\|new .*Pool\|new .*Service(" [generated-file.cs]
→ 0 matches in generated business logic (inject the interface instead)

# Rule 3: DataProcessResult<T> return types
grep -n "DataProcessResult" [generated-file.cs]
→ every public Application method should have at least one match

# Boundary: no core OperationResult leak, no ML framework import
grep -n "OperationResult<\|using TorchSharp\|using Microsoft\.ML\|using TensorFlow" [generated-file.cs]
→ 0 matches
```

**Gate B for Layer 2:** all grep checks return 0 (Rule 3-comment is manual), and
`MicroserviceBase<TSelf>` is present on each generated service.

---

## Layer 3 — Silent Failure (MACHINE Constant Test)

Generated code can produce output that looks correct but silently fails at
runtime. The MACHINE-constant test pattern catches this.

```csharp
[Fact]
public void GeneratedRule_WithKnownInput_ProducesExpectedOutput()
{
    // Arrange: a known input where the correct answer is pre-calculated
    var rule = GeneratedRuleFactory.Create("status = 'active'");

    // Act + Assert: MACHINE constants — expected outputs are fixed, not derived from the generator
    Assert.True(rule.Evaluate(new Record { Status = "active" }));    // must match
    Assert.False(rule.Evaluate(new Record { Status = "other" }));    // must NOT match
}

// "Security break" variant — verify it FAILS when it should fail
[Fact]
public void GeneratedRule_WithNonMatchingInput_ReturnsFalse()
{
    var rule = GeneratedRuleFactory.Create("status = 'active'");
    Assert.False(rule.Evaluate(new Record { Status = "completely_different" }));
}
```

**Gate B for Layer 3:** both the match test AND the non-match test pass.

---

## Layer 4 — Generalization / Training Data Impact

Ask: does this generated pattern generalize, or does it only work for this case?

```
1. If this generator runs on 100 different inputs, will it produce 100 correct outputs?
2. Does the generated code rely on a specific value being hardcoded?
   grep -n "[\"'][a-zA-Z0-9_]{10,}[\"']" [generated-file.cs]  → hardcoded business strings are a red flag
3. Does it handle edge cases? (empty / single-element / max-length / special chars)
4. Would this generated code be valid as future training data? If not, fix the generator first.
```

**Gate B for Layer 4:** at least 3 different inputs tested, all producing correct
output.

---

## Layer 5 — Trainable vs Stub (MACHINE numeric-metric test)

A trainable unit can compile, pass its contract tests, and satisfy DNA while
being a pure stub that never learns or infers. Layer 5 catches that.

**Boundary note for flows:** most flows do not ship a trainable unit — they
consume a core model via a manifest. For such a flow, Layer 5 = **label it
scaffold / honest-untrained** and verify it does NOT claim a trained feature. Run
the numeric gate below only when the flow genuinely ships its own trainable unit
(which by R5/G12 should be rare and explicitly approved).

FAIL the review when the plan requires a trained algorithm but the delivered code
shows any of:
- a status string such as `not_ready_*`, `*_no_inference_implemented`,
  `not_trained_*`, or an equivalent placeholder presented as done;
- a method that returns a fixed JSON/descriptor object instead of computing a
  decision from learned state;
- an empty or no-op training loop (a `TrainBatch` that changes no learned state);
- `NotImplementedException`, `throw` for the main path, `TODO`, `FIXME`, or
  `PLACEHOLDER` on the inference/training path;
- inference that is exact-source/exact-prompt lookup presented as generalization.

Required numeric evidence (reproducible from the runner; UNKNOWN not allowed for a
done claim): accuracy / exact / loss / reward-margin on a held-out set;
`cpu_ms`, `ram_mb`, row counts; train vs test identity sets of the same type (no
replay leakage).

**Gate B for Layer 5:** numeric metrics present AND reproducible AND the unit
changes its decision after training on new pairs. If metrics are
UNKNOWN/validation_only/static/contract-only, Layer 5 = FAIL (or, for a pure
consumer flow, the deliverable must be labelled scaffold/honest-untrained).

### Layer 5A — Allowed Trainable Architecture Pattern

If a flow truly hosts learned/semantic/trainable/adaptive/DPO/selector/router/
classifier/scorer/retrieval/KNN/dictionary/counter/pattern-matching mechanism,
classify the actual runtime mechanism before judging quality. Allowed levels
only: `ILearningMachine<TDomain>`, `ILogicalOrchestrator`, `ILogicalFlow<TInput>`,
`ILogicalUnit<TInput,TOutput>`, `ILearningMachinePair<TPersonal,TGlobal>` (these
live in `llm_mvp_core`). Scorers, selectors, classifiers, routers, retrievers,
KNN/Jaccard/BM25 stores, dictionary/counter state, regex/keyword matching, and
private `TryDecide*`/`ChooseBestPattern`/`SelectBestPattern`/`CountMatchingSignals`
are not architecture patterns — they pass only as internal details inside an
allowed level, honestly labelled.

```text
Allowed hierarchy: <one of the five allowed levels, in llm_mvp_core>
Non-level classification: <internal detail only / none>
Actual mechanism: <short human explanation + code quote>
Runtime authority: yes/no
Semantic claim: full/baseline/scaffold/eval-only/too strong
Verdict: OK_ALLOWED_LEVEL / OK_INTERNAL_DETAIL / BASELINE_MUST_BE_LABELLED /
NEEDS_ILOGICALUNIT_WRAPPER / FORBIDDEN_RUNTIME_SHORTCUT / REPORT_CLAIM_TOO_STRONG
Concrete repair: current -> target / replacement allowed level / replacement
class/unit/flow / replacement algorithm / typed trained state / affected sets / affected tests
```

Layer 5A fails if `Concrete repair` is generic.

### Deterministic-Boundary Code Review Gate

Every code slice review must explicitly search for hidden deterministic
mechanisms posing as trainable behavior before it can pass:

```text
deterministic_boundary_review = PASS | FAIL
searched_mechanisms = scorer/router/lookup/counter/exact-replay/static-fixture
hidden_deterministic_mechanism_found = true/false
actual_runtime_authority:
allowed_level = ILogicalUnit | ILogicalFlow | ILogicalOrchestrator | ILearningMachine | ILearningMachinePair | internal detail only
claim_matches_mechanism = true/false
project_coding_standards_checked = true/false
allowed_tests_or_gate_b_checked = true/false
core_logic_boundary_checked (llm_mvp_core owns trainable/ML) = true/false
flow_consumes_core_via_manifest_not_local_ml = true/false
deterministic_helpers_labelled_eval_only_helper_internal_or_baseline = true/false
docs_qa_impact_checked_when_applicable = true/false
required_fix:
```

The code review also fails if project coding standards, allowed tests
(`dotnet build`/`dotnet test`), the core-ownership boundary
(trainable/ML stays in `llm_mvp_core`), deterministic-helper labels, or docs/QA
impact were not explicitly checked. A passing test result or machine label alone
is not a complete code review.

### Human-readable code review report gate

Reject code review when the report is table-only, shortcode-heavy, English-only
when Russian is required, schema-only, or prompt/spec instead of an implementable
blueprint. For trainable/architecture findings, include: Russian human summary;
Mermaid/PlantUML importable to draw.io when data flow is explained; no-code
algorithm explanation; exact `file:line` quote plus `сейчас` / `почему плохо/ок`
/ `должно стать`; and Concrete Repair Blueprint fields for any suspicious
Dictionary/counter/KNN/scorer/selector/private-method quote. Generic "wrap in
ILogicalUnit"/"add KNN"/"add tests"/"chosen/rejected/quarantine" is not a решение.

---

## Review Output Record

```
GENERATED CODE REVIEW — [date]
File: [generated-file.cs]
Generator/source: [flow codegen step / executor work order]

Layer 1 (Semantic):           PASS / FAIL — [notes]
Layer 2 (DNA):                PASS / FAIL — [grep results]
Layer 3 (Silent failure):     PASS / FAIL — [test results]
Layer 4 (Generalization):     PASS / FAIL — [inputs tested]
Layer 5 (Trainable vs Stub):  PASS / FAIL / N-A-SCAFFOLD — [numeric metrics OR honest-untrained label]
Layer 5A (Allowed Architecture): PASS / FAIL / N/A — [allowed level, actual mechanism, verdict]
Code review completeness:     PASS / FAIL — [standards/tests/core boundary/deterministic labels/docs-QA]

Overall: PASS / FAIL
Action: [commit as-is / regenerate with fixed generator / escalate / downgrade claim]
```

## Multi-cycle code review floor

When the active plan requires at least four code-review/fix cycles before
commit/push, one Generated Code Review record is only one cycle:

```text
generated_code_review_cycle_1 = PASS after fixes/recheck
generated_code_review_cycle_2 = PASS after fixes/recheck
generated_code_review_cycle_3 = PASS after fixes/recheck
generated_code_review_cycle_4 = PASS after fixes/recheck
cycle_focuses_are_distinct = true
unfixed_findings_open = false
```

If any cycle finds a defect, apply the scoped fix and rerun the next cycle after
the fix.

## Bounded sub-agent evidence boundary

When generated-code evidence comes from a bounded sub-agent, review the
work-order boundary before judging code quality:

```text
work_order_scope:
exact_allowed_files:
actual_files_changed:
commit_push_authorized:
allowed_git_commands:
forbidden_commands:
commit_push_performed:
parent_will_perform_commit_push_after_review:
may_derive_next_phase:
must_stop_after_report:
parent_review_readback_done:
subagent_notification_checked_for_commit_push_claims:
```

Reject the packet when the sub-agent created later-phase artifacts, committed,
pushed, or derived the next phase without explicit authorization, or ran a git
write/ref command when `commit_push_authorized=false`. A sub-agent's self-review
or `parent-review` file is evidence only; it cannot replace parent code review.

---

## Anti-Patterns

1. **Visual inspection as verification** — "looks right to me" is not Layer 3
2. **One-input testing** — one passing case does not verify the pattern generalizes
3. **Skipping Layer 2** because "the generator would never do that" — verify, don't assume
4. **Committing generated files without a review record** — the record proves review happened
5. **Reviewing a trainable unit by contract/build test only** — a stub returning `not_ready` passes contract tests, builds clean, and satisfies DNA. Layer 5 numeric metrics are mandatory for a real trainable deliverable; a consumer flow must be labelled scaffold/honest-untrained, never a trained feature (the V20 failure).
