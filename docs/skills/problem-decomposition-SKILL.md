# problem-decomposition-SKILL.md — Cause level, minimum info, claim class, scope, executable output

> **Stack adaptation (xiigen flows).** Universal *problem decomposition* ported
> from `llm_mvp_core/docs/skills/problem-decomposition-SKILL.md` (G08). Greenfield
> here — no prior author version. The universal core kept: five steps — cause
> level (three "why"s: immediate / structural / architectural), minimum
> information (3–5 files), incoming-claim classification (VERIFIED / INFERRED /
> ASSUMED), response scope (FUNCTION → CLASS → INTERFACE → FLOW → ARCHITECTURE),
> and executable output. The stack details are adapted:
> - **Examples** are flows cases (an Application service + xUnit `[Fact]` + a
>   `contracts/` DTO), not core retriever/scorer cases.
> - **Verify commands** are `dotnet build Xiigen.Flows.slnx` /
>   `dotnet test --filter` (`net10.0`, xUnit) and Playwright for UI.
> - The **scope ladder** top is a `FLOW-NN-*` ("flow change"), and the domain
>   wrapper is `DataProcessResult<T>` (XIIGen family) — **NOT** core
>   `OperationResult<T>`.
> - Architecture-level decisions are recorded in a decision artifact next to the
>   plan / `flow.manifest.json`, not in a core `DECISIONS-LOCKED.md`.

## When to Invoke

At the start of any session where the problem is complex or ambiguous. Before
reading any file, before proposing any fix, before writing any plan.

---

## The Five Steps

### Step 1 — Symptom or Root Cause?

Ask "why does this problem exist?" three times.

```
First answer  = immediate cause   → fix with a targeted code change
Second answer = structural cause  → fix with a pattern or design update
Third answer  = architectural cause → requires a design decision or new component
```

**Rule:** Identify the level before proposing the solution. A Level 1 solution
for a Level 3 problem fixes today and breaks tomorrow.

**Example in this repo:**

```
Problem: UserRegistrationService returns Failure("DUP_EMAIL") for valid new emails

Level 1: Email comparison is case-sensitive in RegisterAsync() → normalize before compare
Level 2: The duplicate check reads a non-tenant-scoped store
         Fix: route the lookup through the tenant-scoped contract (ITenantContextAccessor)
Level 3: No contract guarantees uniqueness scope (tenant vs global) for the flow
         Architectural: define the uniqueness boundary in contracts/ and record the decision
```

The right level depends on whether this is a one-time issue or a systematic pattern.

### Step 2 — Minimum Information Needed

Do NOT read everything. Which 3–5 files tell the whole story?

```
For test failures:        failing xUnit [Fact] + class under test + its contract/DTO
For service bugs:         the specific method + its callers + the test covering it
For contract questions:   contracts/schemas/*.json + the DTO + one Application consumer
For flow planning:        flow.manifest.json + plan section + per-flow test baseline
For "where does X live":  Xiigen.Flows.slnx + one grep — not reading every flow
For client bugs:          the standalone component + its HTTP service + the Playwright spec
```

**Rule:** Start narrow. Widen only when the narrow read is insufficient. Every
extra file read without a specific question is noise.

### Step 3 — Classify Incoming Claims

Before accepting any factual claim from a review, a prior summary, or another model:

```
VERIFIED   = a command that proves it is cited → spot-check the command yourself
INFERRED   = reasonable deduction, reasoning shown but command not run → run it yourself
ASSUMED    = stated as fact, no command shown → treat as hypothesis until confirmed
```

**Rule:** Test counts, contract signatures, file existence, line numbers — ALWAYS
verify before using in a plan. One grep costs 10 seconds; a wrong number in a
plan costs a blocked session.

### Step 4 — Scope the Response

Start at the lowest scope that genuinely solves the problem:

```
FUNCTION CHANGE → CLASS CHANGE → INTERFACE/CONTRACT CHANGE → FLOW CHANGE → ARCHITECTURE CHANGE
```

Document why each lower scope is insufficient before claiming a higher one.

```
Problem: Add a "preferred language" field to registration output

Function change: add field to the existing RegistrationOutcome record + map it in RegisterAsync()
  Sufficient? YES — no contract break if the field is additive and optional
  Why not contract change: existing consumers ignore unknown additive fields
  Decision: function/record change only; bump contracts/schemas response schema additively
```

### Step 5 — Produce Executable Output

Output is commands and facts, not prose. The next session must run the plan
without interpretation. Include:

- Exact file paths (not descriptions of where to look)
- Exact grep commands with expected output
- Exact `dotnet test --filter` / `npx playwright test` commands with expected pass count
- Discoveries and rejected claims (see `investigation-handoff-SKILL.md`)

---

## Anti-Patterns

1. **Starting to fix before identifying the level** — "obviously add a null check" may be right for Level 1 but wrong if the structural cause is upstream.
2. **Reading all files before identifying which 3 answer the question** — information overload buries the signal.
3. **Accepting summary numbers without verification** — test counts, line numbers, class names from summaries must be grep-verified before use in a plan.
4. **Proposing infrastructure for a one-line problem** — over-engineering.
5. **Treating every problem as architectural** — some problems are just wrong values in the right place.

---

## Level Decision Reference

| If the third "why" answer is... | Fix level |
|--------------------------------|-----------|
| A missing null check / normalization | Function-level fix |
| A wrong return type | Class-level fix (return `DataProcessResult<T>`) |
| A contract that doesn't guarantee a property (scope, shape) | Interface/contract-level fix in `contracts/` |
| A design decision never made explicit | Architecture-level decision + decision artifact next to the plan |
| A DNA rule that wasn't applied | Governance fix (update the relevant skill / DNA-DOTNET-TRANSLATION.md) |
