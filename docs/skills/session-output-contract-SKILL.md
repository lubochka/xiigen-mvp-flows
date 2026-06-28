# Session Output Contract

> **Stack adaptation (xiigen flows).** The output contract as the zeroth
> deliverable ported from `llm_mvp_core/docs/skills/session-output-contract-SKILL.md`
> (G07). The universal part (before any file is read: a named file list + exactly
> one binary verify command per file + a specification gate "does this match the
> original request?"; a "What this session does NOT produce" section; the bans on
> "and any other relevant files", on "N ledger rows = N cycles", and on
> "sub-agent did it" without a returned packet) is taken verbatim from core.
> Adapted here:
> - Verify commands use this stack: `ls` / `grep` for files; `dotnet test --filter`
>   (xUnit) and `dotnet build Xiigen.Flows.slnx`; `ng test --watch=false` for
>   client-bearing flows. "Test baseline >= pre-session" is the per-flow
>   `Xiigen.Flow.<Name>.Tests` count.
> - Deliverables are tied to a concrete `FLOW-NN` and its
>   `flows/FLOW-NN-<slug>/{server,client,contracts}` / `flow.manifest.json`.
> - **The llm_mvp_core host-extension subset is NOT ported** (Dual-Path host
>   verification, Claude/Cursor/Codex scenario counters, Multi-Monitor proof) — it
>   is core-product-specific, not universal. This repo has no host-extension
>   scenarios; do not add that machinery.
> - Domain return type cited is `DataProcessResult<T>` (`IsSuccess`/`ErrorCode`/
>   `Data`, from `Xiigen.Kernel`) — **not** core `OperationResult<T>`.

## Core Problem

A session that starts without defining what "done" means will either over-deliver
(produces things not requested, consuming time) or under-deliver (misses implied
deliverables, causing rework). Both failure modes recur across sessions. The output
contract is the zeroth deliverable — it must exist before any code is written or any
file is read.

---

## When to Invoke

- Before any session starts — produce the contract before reading any files
- Before writing any phase plan (to scope what the plan must contain)
- Before claiming a session complete (verify against the contract)
- When a session begins with "write me a plan for FLOW-NN..." or "implement phase N"

---

## Section 1 — What an Output Contract Is

A machine-verifiable specification of exactly what "done" means for this session.

Three parts:

### Part 1 — Exact File List

```markdown
## Output Contract — [Session Name]

### Deliverables
| # | File | Must contain |
|---|---|---|
| 1 | flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs | Idempotent SettleAsync returning DataProcessResult<SettlementOutcome> |
| 2 | flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementServiceTests.cs | xUnit coverage: idempotency, webhook reconcile, failure codes |
| 3 | flows/FLOW-16-marketplace-payments/flow.manifest.json | Updated contract entry for ICheckoutSettlementService |
```

No ambiguity. No "and any other relevant files." Every deliverable is named,
or the session is not scoped correctly.

### Part 2 — Verification Commands

For each deliverable, one verification command that produces a binary result:

```bash
# Service file exists and has the required method:
grep -n "SettleAsync" flows/FLOW-16-marketplace-payments/server/Xiigen.Flow.MarketplacePayments.Application/PaymentSettlementService.cs
# Expected: 1+ hit

# Manifest updated with the new contract:
grep -n "ICheckoutSettlementService" flows/FLOW-16-marketplace-payments/flow.manifest.json
# Expected: 1+ hit

# Test baseline preserved or grown (per-flow xUnit):
dotnet test Xiigen.Flow.MarketplacePayments.Tests --no-build 2>&1 | tail -3
# Expected: Failed: 0, Passed: >= baseline
```

### Part 3 — Specification-Level Question

After verification commands pass, answer this before claiming done:

> "Does this deliverable match what was originally requested?"

Not: "Are the file internals consistent?"
Not: "Does the content make sense?"
The question is about the original request. Re-read the request. Does the deliverable satisfy it exactly?

---

## Section 2 — Contract Format

```markdown
## Output Contract — [Session Name]
Requested by: [reference to user instruction]
Session type: IMPLEMENTATION | PLANNING | SKILL_PORT | REVIEW
Flow: FLOW-NN-<slug>

### Deliverables
| # | File | Verification command | Expected output |
|---|---|---|---|
| 1 | [path] | [bash command] | [expected result] |
| 2 | [path] | [bash command] | [expected result] |

### Specification gate (run last, before claiming done)
- [ ] Re-read the original request
- [ ] Every named deliverable in the request exists
- [ ] No deliverable was silently merged into another file
- [ ] Files updated from memory of prior content were verified against the original source
- [ ] Every count claim is backed by matching independent evidence records
- [ ] Every sub-agent claim names the sub-agent id/nickname and returned packet
- [ ] Ledger/checklist rows are not described as cycles
- [ ] dotnet build Xiigen.Flows.slnx -> 0 Error(s) AND 0 Warning(s)
- [ ] dotnet test (per-flow .Tests) baseline >= pre-session baseline (zero regression)
- [ ] ng test -> N passed (only when an Angular client changed)
- [ ] Status reporting is human-readable first (see output-readability-gate-SKILL.md);
      no bare machine-only status label as the user-facing report.

### What this session does NOT produce
[Explicit list of in-scope-but-not-this-session items]
```

The "does NOT produce" section is as important as the deliverables list. It prevents
scope creep ("while I'm here I'll also fix...") and prevents the next session from
assuming the current session already did something it didn't.

---

## Section 3 — Read-Before-Write Rule

If a deliverable updates an existing file, the source file must be read completely
before any character is changed. Updating from memory produces the prior version,
not the current state.

```
□ For each file in the deliverables list that is an UPDATE (not a new file):
  -> Read the complete file before writing
  -> Record one specific fact from the end of the file as proof (Rule 5)
  -> Only then: apply the update
```

---

## Section 4 — Two-Deliverable Minimum Verification

A session that produces one deliverable can verify it by reading it.
A session that produces multiple deliverables must verify each independently.

If any deliverable cannot be verified independently — either it is incomplete,
or it is not a real deliverable (it should be part of another file).

For a service + its tests specifically: the service file and the test file are two
independent deliverables. Verify them separately:
```bash
# Service file:
ls flows/FLOW-16-marketplace-payments/server/Xiigen.Flow.MarketplacePayments.Application/PaymentSettlementService.cs
# Expected: file exists

# Test file present and green:
dotnet test --filter "PaymentSettlement" --no-build 2>&1 | tail -3
# Expected: Failed: 0
```

---

## Anti-Patterns

1. **"Starting to write code before the output contract exists"** — the contract is the
   zeroth step. It exists before reading any files. Code is at least the second step.

2. **"Output contract written as prose: 'This session will produce updated services...'"** —
   prose cannot be verified. Name the files. Write the verification commands.

3. **"'And any other relevant files' in the deliverables list"** — this is not a contract.
   It is permission to produce anything. If it's relevant, name it. If unknown, that's a
   scoping problem to resolve before starting.

4. **"Claiming done before running verification commands"** — run them. If they don't
   pass, the session is not done.

5. **"Two deliverables merged into one file without noting it"** — the contract must
   list each requested deliverable separately. Silent merging is a scope violation.

6. **"N ledger rows means N cycles"** — false. The contract must require N
   independent evidence records for N cycles/reviews/passes.

7. **"Sub-agent did it" without a sub-agent packet** — false. Name the
   sub-agent id/nickname and returned packet, or downgrade to parent/self work.

8. **Opaque execution status as the user report** — false. A bare row such as a
   phase code, `passed: true`, or a JSON/STATE label is not the output contract
   unless it is preceded by the human-readable phase/step/count/review/fix/
   next-action contract (see `output-readability-gate-SKILL.md`).

---

## Integration

- `agent-output-format-SKILL.md` governs what session files look like internally; this skill governs whether the right files are produced
- `verification-before-completion-SKILL.md` enforces the specification gate at session end
- `plan-self-validation-SKILL.md` produces the output contract for a multi-phase plan
- `documentation-sync-SKILL.md` uses the contract's deliverables list to verify sync completeness
- Rule 5 (read before touching) is enforced by Part 3 of this skill's Read-Before-Write rule
