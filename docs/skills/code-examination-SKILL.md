# Code Examination

> **Stack adaptation (xiigen flows).** The "Understand before touching" 5-step
> protocol ported from `llm_mvp_core/docs/skills/code-examination-SKILL.md` (G04).
> Adapted here:
> - greps run against `flows/FLOW-NN-*/server/**/*.cs`;
> - the caller/consumer chain is examined within the per-flow `Api → Application →
>   Infrastructure` layering;
> - tests are `Xiigen.Flow.<Name>.Tests` (xUnit `[Fact]`/`[Theory]`), not `*.spec.ts`;
> - the structured-file recheck (Step 6) covers JSON contracts under
>   `contracts/schemas` and `.csproj`/`Directory.Build.props`;
> - the domain return type is `DataProcessResult<T>` (XIIGen family wrapper, not core
>   `OperationResult<T>`); services inherit `MicroserviceBase<TSelf>`;
> - the trainable add-on applies through the G12 boundary: trainable/ML code lives in
>   `llm_mvp_core`, so a deterministic-shortcut found in a flow is a finding to
>   downgrade or to route back to a core unit, not to "make trainable here".

## Core Problem

A file modified without being fully read produces two failure modes: the fix
corrects the symptom at the call site while the root cause is in the caller; or
the fix is correct in isolation but breaks a consumer that depended on the old
behavior. "Read before touching" requires reading. This skill defines the 5-step
examination protocol that makes reading effective: read completely, map callers,
map consumers, form a hypothesis before reading tests, then read tests.

---

## When to Invoke

- Before modifying any service class, interface, or test in a flow
- Before any phase implementation (Gate 0 in phase planning)
- When a bug report references a file not read this session
- When a deep call-chain source is identified during root-cause tracing

---

## The Rule

> **Understand before touching.**
> Never modify a file you have not read and understood in the context of its
> callers and consumers.

## Sub-Agent-Only Parent Boundary

When the parent/current session is sub-agent-only, architect-only, or forbidden
to become executor, this examination protocol belongs inside the bounded
sub-agent work order. The parent must not use code examination as local sidecar
work while the sub-agent runs. Parent may read governed files or code only after
the sub-agent packet returns and only for verification/integration of that
packet. Sub-agent waiting is not a stop.

---

## Section 1 — Five-Step Protocol

### Step 1 — Read the File Completely

Read the whole file. Not the method the bug is in. The whole file.

```bash
# Read the target service and its interface contract
cat flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<ClassName>.cs
cat flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/I<ClassName>.cs
```

Do not skip lines. A hidden assumption three methods above the bug site is often
the actual cause.

### Step 2 — Map Upstream Callers

```bash
# Find every caller across the flow's server projects
grep -rn "<ClassName>\|<MethodName>" flows/FLOW-NN-<slug>/server --include="*.cs" | grep -v "//"

# Find interface injectors (DI registration, controller, other services)
grep -rn "I<ClassName>" flows/FLOW-NN-<slug>/server --include="*.cs"
```

A change to an output shape (`DataProcessResult<T>` generic type, payload fields)
affects every downstream consumer. A change to a method signature affects every
caller. The `Api` layer (controllers/endpoints) and other `Application` services
are the usual callers — know both before touching anything.

### Step 3 — Map Downstream Consumers

For Application methods: identify what reads this method's return value (the
controller in `Api`, or another `Application`/`Infrastructure` collaborator).

```bash
# Find consumers of the return value / event
grep -rn "<MethodName>(" flows/FLOW-NN-<slug>/server --include="*.cs"
```

Build a chain, e.g.:
```
Controller (Api) → UserRegistrationService (Application) → OnboardingDeliveryService (Application) → store (Infrastructure)
```

A change to the middle of the chain affects both ends.

### Step 4 — Form a Hypothesis BEFORE Reading Tests

State in plain language: "I believe this code does X because Y. If I change Z,
then W will break." Write it in your working notes before opening the test file.
Tests confirm or refute the hypothesis. If the hypothesis is wrong after reading
the tests, re-read the implementation — the mental model needs correction before
the code does.

### Step 5 — Read the Test File

```bash
cat flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/<ClassName>Tests.cs
```

Verify: does the test match your mental model? If the test asserts behavior your
hypothesis says should not happen, your model is wrong. Fix the model first, then
modify the code.

### Step 6 — Final Diff And Structured-File Recheck

After edits, examine the final diff for the files you touched. If you edited JSON
contracts, `.csproj`, `Directory.Build.props`, YAML, or generated reports, run the
matching parse or `dotnet build` check after the final state update, not before it.

For project-boundary work, the final diff must show that production code lives in
the requested per-flow project and tests reference that project normally. A
test-only `Compile Include`, source-link, temporary project, or script adapter is
evidence only; it is not the implementation boundary unless the work order
explicitly asked for that artifact type.

### Step 6A — Code Review Completeness Scope

Before a code slice can be accepted, the final examination/review must name the
exact files, sections, and claims inspected. It must explicitly check project
coding standards, allowed tests / Gate-B (`dotnet build` 0/0 + `dotnet test`),
whether trainable/ML logic stays in `llm_mvp_core` (the flow only consumes it via
manifest), whether hidden deterministic shortcuts exist, whether deterministic
helpers are labelled eval-only/helper/internal-baseline, and whether docs/QA must
change. Passing tests or a machine label alone is not a complete code review.

### Step 7 — Independent Review/Fix Cycle Count

When the plan asks for several/multiple/repeated code-review/fix cycles, the
minimum is two independent review passes after the implementation or final fix:

```text
review_cycle_1: { reviewer_or_pass_id, scope, findings, fixes_or_zero_findings_evidence }
review_cycle_2: { reviewer_or_pass_id, scope, findings, fixes_or_zero_findings_evidence }
```

A single final summary review is not "several cycles". If cycle 1 finds issues
and a fix is applied, cycle 2 must run after that fix.

---

## Section 2 — Diagnostic Routing Table

| Symptom | First action | Then |
|---------|-------------|------|
| `DataProcessResult.Failure(...)` returned unexpectedly | Read guard clauses in the Application method | Trace backward to the controller/caller |
| Store/repository returns wrong value | Check write site and key naming | Verify tenant/correlation scope from the ambient accessor |
| Idempotency/outbox event not processed | Binary search the dispatch chain | Verify store-before-dispatch order |
| Service returns wrong `ErrorCode` | Check `Failure<T>("CODE",...)` literals | Compare to the test's expected code |
| `dotnet test` passes but behavior is wrong | Check for a stub returning a hardcoded value | Add a MACHINE-constant `[Fact]` (generated-code-review Layer 3) |

---

## Section 3 — Reading Checklist

```
□ Read the complete file — not just the method under investigation
□ Identified all callers of the method being changed (Api + Application + Infrastructure)
□ Identified all consumers of the method's DataProcessResult<T> return type
□ Written a hypothesis in plain language before opening tests
□ Read the test file — does it match the hypothesis?
□ If hypothesis was wrong: re-read the implementation
□ Final diff reviewed after edits
□ Code-review completeness scope recorded: standards, tests/Gate-B, core boundary,
  deterministic-shortcut sweep, deterministic labels, docs/QA impact
□ Structured files (JSON contracts / .csproj / props) parsed or built after the final edit
□ New project boundaries use real per-flow project references, not test-only source links
□ When several review/fix cycles were required, ≥ 2 independent post-fix passes recorded
```

---

## Anti-Patterns

1. **Grepping for the bug location then editing only the matching line** — the
   caller may be setting the wrong field. Read the full `Api → Application →
   Infrastructure` chain first.
2. **Reading tests before reading implementation** — tests describe behavior, not
   mechanism. Read implementation first, tests second.
3. **"I know this class — I don't need to re-read it"** — code changes; session
   context does not persist. Re-read every file intended for modification.
4. **Modifying without a written hypothesis** — if you cannot state in one
   sentence what the code does and what your change affects, you are not ready.
5. **Mapping only the direct caller, not the full chain** — a `DataProcessResult<T>`
   generic change at the bottom can produce a compiler error several layers up.

---

## Trainable Architecture Examination Add-On

When examining learned/semantic/trainable/adaptive/DPO/selector/router/
classifier/scorer/retrieval/KNN/dictionary/counter/pattern-matching code, also
search for: `TryDecide`, `ChooseBestPattern`, `SelectBestPattern`,
`CountMatchingSignals`, `NormalizeSignal`, `FindNearest`, `Jaccard`, `BM25`,
`Dictionary<string, int>`, `Regex`, `Contains`.

For each hit, trace whether the method is inside an allowed level
(`ILearningMachine` / `ILogicalOrchestrator` / `ILogicalFlow` / `ILogicalUnit` /
`ILearningMachinePair`, all in `llm_mvp_core`) or outside the allowed hierarchy.

**Flows boundary:** by R5/G12, a flow should not host its own trainable authority.
If a deterministic shortcut is found acting as semantic/trainable authority inside
a flow, the repair is to downgrade the claim and route the decision to a core
unit consumed via manifest — not to "make it trainable here".

Report every finding in this form:

```text
Human explanation in English (unless the user requests another language):
Code quote with exact file:line:
current behavior:
why this is wrong/acceptable:
required behavior:
Allowed location (in llm_mvp_core):
Current location:
Dataset/claim risk:
Test/doc impact:
Verdict:
Concrete repair:
  current -> target:
  replacement allowed level:
  replacement class/unit:
  replacement algorithm:
  input/output contracts:
  affected tests:
  documentation/report wording:
```

Do not accept "make it trainable", "wrap it", "add tests", or "mark as
provenance-only" as a complete repair. The repair must name the actual unit/flow
shape and the algorithm it runs.
