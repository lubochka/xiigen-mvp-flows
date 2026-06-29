# subproject-examination-SKILL.md — Pre-implementation 3-document gate per flow

> **Stack adaptation (xiigen flows).** Universal *subproject examination* ported
> from `llm_mvp_core/docs/skills/subproject-examination-SKILL.md` (G08). Greenfield
> here — no prior author version (core's nearest were flow-completeness / ui /
> sub-episode checkers; this is the general pre-implementation examination). The
> universal core kept: a pre-implementation gate that produces three documents —
> MASTER (infrastructure decision tree + capability contract + phase plan),
> MISSING-INFRASTRUCTURE (BUILD specs), LEARNING-SPEC — before the first line of
> code; a decision tree (not a checklist); BLOCKER = stop; approval before code.
> The stack details are adapted:
> - The **natural unit** is a new or growing `FLOW-NN-*`.
> - **CHECK commands** = `dotnet build Xiigen.Flows.slnx` (0 errors / 0 warnings),
>   `dotnet test`, `grep "interface I..."` in `contracts/`, and the
>   `ProjectReference` in each `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}.csproj`
>   (`net10.0`).
> - **Capability contract** returns `DataProcessResult<T>` (XIIGen family wrapper —
>   **NOT** core `OperationResult<T>`) and services inherit `MicroserviceBase<TSelf>`.
> - **Document 3 (LEARNING-SPEC)** is tied to `.xiigen/wiki-domain`: what this flow
>   adds to the consumed domain model. The model body itself stays in
>   `llm_mvp_core` (R5 / G12) — this repo records what it contributes/consumes.
>   UI documents apply only to client-bearing flows (Angular 21 + Playwright).

## Core Problem

A flow that starts implementation before its infrastructure gaps are known will
stall mid-phase when a missing contract or dependency is discovered. This skill
installs a pre-implementation examination gate that produces three documents
before any code is written. Nothing starts until those documents exist and are
approved.

---

## When to Invoke

- When a new `FLOW-NN-*` is added
- When an existing flow advances from skeleton to active
- When a new capability (contract + implementation + tests) is planned
- Before any phase that creates more than one new `.cs` file

One examination before implementation = no mid-phase infrastructure discoveries.

---

## Section 1 — The 3-Document Output

Produces exactly 3 documents before any `.cs` file is written. Do not produce
implementation until all 3 are reviewed and approved by Luba.

### Document 1: `flows/FLOW-NN-<slug>/docs/SESSION-<FLOW>-MASTER.md`

**Header:**
```
# SESSION-<FLOW>-MASTER.md
## Flow: FLOW-NN — <name>
## Date: <date>
## Status: DRAFT — review before executing
```

**Section 1: Infrastructure Decision Tree** (a tree, NOT a checklist — every FAIL
has a BUILD instruction):

```markdown
Run each check in order. If it fails, follow the BUILD instruction.
Only begin Phase 1 after ALL checks pass.

CHECK 1: Build baseline
  Command: dotnet build Xiigen.Flows.slnx → must output "0 Error(s)" (0 warnings)
  PASS → CHECK 2.  FAIL → fix build errors first; record failing files; do not proceed.

CHECK 2: Test baseline
  Command: dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests → record (N passed, 0 failed)
  Record as the flow test_baseline. Measurement only → CHECK 3.

CHECK 3: Required contracts
  For each contract this flow depends on:
  Command: grep -rn "interface <IContract>" flows/FLOW-NN-<slug>/contracts flows/FLOW-NN-<slug>/server
  PASS (found) → next contract.  FAIL (not found) → record as BLOCKED in MISSING-INFRASTRUCTURE.

CHECK 4: Required project references
  Command: grep -n "<ReferencedProject>" flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api/*.csproj
  PASS → CHECK 5.  FAIL → add ProjectReference; dotnet build to verify; then proceed.

CHECK 5: DI registration + naming
  The flow exposes an Add<Name>Flow() extension called in Program.cs.
  New files follow <ClassName>.cs / <ClassName>Tests.cs; no existing-file conflicts.
  Proceed to implementation.

All checks pass → produce Document 2 and Document 3 → await Luba approval → begin Phase 1.
```

**Section 2: Capability Contract** — for each class/contract to create:

```markdown
### <ClassName>

Contract: I<ClassName> (in contracts/ or the Application boundary)
Concrete: <ClassName> : MicroserviceBase<<ClassName>>, I<ClassName>
Test class: <ClassName>Tests (xUnit)

Public methods:
  <MethodName>(<params>) → DataProcessResult<<T>>
  [what this method does]

DNA requirements (DataProcessResult variant):
  every public Application method returns DataProcessResult<T> — never throws for business logic, never returns null
  fabric-first: depends on I<Contract>, not the concrete class
  every production line has a comment

Constructor dependencies (fabric-first):
  I<Contract> _<fieldName>

Gate B:
  dotnet test --filter "FullyQualifiedName~<ClassName>Tests" → <N> passed, 0 failed
```

**Section 3: Phase Plan:**

```markdown
Phase 1: <ClassName> implementation
  Files: <ClassName>.cs, <ClassName>Tests.cs
  Gate B: dotnet test --filter "<ClassName>Tests" → <N> passed, 0 failed

Phase 2: Integration / DI wiring (if applicable)
  Files: Program.cs (Add<Name>Flow), <integration test>
  Gate B: dotnet test --filter "<filter>" → <N> passed, 0 failed

Phase [LAST]: client / docs update (if a UI or skill file was added)
  Gate B: ng build clean + Playwright spec present (client-bearing) / doc exists
```

---

### Document 2: `flows/FLOW-NN-<slug>/docs/<FLOW>-MISSING-INFRASTRUCTURE.md`

One entry per gap found in CHECK 3/4 (missing contracts, missing project
references, missing base wiring):

```markdown
### GAP-<N>: <Feature Name>

**What it is:** one paragraph — what is missing and what it provides

**What fails without it:**
- [which phase Gate B fails]
- [which tests cannot be written]

**Build spec:**
  File to create: [exact path]
  Contract/class: [full C# signature with namespace]
  Verification: dotnet build → 0 Error(s) / 0 warnings
               dotnet test --filter "[filter]" → [N] passed, 0 failed

**Priority:** BLOCKER | CRITICAL | IMPORTANT
  BLOCKER   = Phase 1 cannot start
  CRITICAL  = phase completes with wrong behavior
  IMPORTANT = quality or test coverage degraded
```

End with: (1) summary table Gap | Name | Priority | Blocks Phase | Also needed by;
(2) implementation order (dependency chain, incl. cross-flow from
`flow.manifest.json`); (3) "What is NOT missing" — explicitly verified-present list.

---

### Document 3: `flows/FLOW-NN-<slug>/docs/<FLOW>-LEARNING-SPEC.md`

```markdown
## Domain input
[Plain-English description of what this flow does — no C# class names]

## New capabilities introduced
| Capability | What it adds | First consumer |
|---|---|---|
| [capability] | [description] | [who calls it] |

## Behavioral tests required
For each capability:
  Input: [specific input value or state]
  Expected output: [specific DataProcessResult with value/ErrorCode]
  Verified by: dotnet test --filter "[TestName]"

## Domain-model contribution (.xiigen/wiki-domain)
  What this flow adds to / consumes from the generated domain model: [list]
  Note: the model body lives in llm_mvp_core (R5); this flow consumes it via manifests.

## Completion criteria
  All tests pass: dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests → [N] passed, 0 failed
  Build: dotnet build Xiigen.Flows.slnx → 0 Error(s), 0 warnings
  DNA: DataProcessResult<T> everywhere, fabric-first, comments
  Client (if applicable): ng build clean + Playwright green
```

---

## Section 2 — Examination Rules

1. **No placeholder values** — every field in Document 1 Section 2 must have a real value. Unknown → state it as a gap in Document 2.
2. **Gaps have BUILD specs** — a gap that says "add this contract" without the full C# signature is incomplete.
3. **Decision tree, not checklist** — every CHECK failure has a BUILD instruction, not "STOP and investigate."
4. **BLOCKER means stop** — a GAP-N marked BLOCKER means Phase 1 cannot start. Resolve it first.
5. **Approval required** — do not write `.cs` files until Luba has reviewed all 3 documents.
6. **Gate B per phase** — every phase in Document 1 Section 3 has one `dotnet test` command with an exact expected count.

---

## Anti-Patterns

1. **"I know what's needed, skip the examination"** — the examination takes one session; discovering a missing contract mid-Phase 2 takes two.
2. **"Infrastructure gap can be fixed later"** — BLOCKER gaps block Phase 1; Phase 2 depends on Phase 1 completing.
3. **"Document 3 is optional"** — it is required for flows that contribute to the `.xiigen/wiki-domain` model; omitting it means the contribution can't be designed.
4. **"Gap has no build spec"** — a gap without a build spec is not actionable for an executor session.

---

## Integration

- `prerequisite-chain-SKILL.md` verifies the BLOCKER items in Document 2 before execution.
- `plan-self-validation-SKILL.md` requires Document 1 Section 3 to have Gate B per phase.
- `solution-scope-gate-SKILL.md` / `gap-to-proposal-SKILL.md` determine the resolution level for each gap in Document 2.
- `phase-completion-packager-SKILL.md` uses Document 3 completion criteria as the packager's evidence baseline.
