# code-execution-SKILL.md — Test gate, tier system, Gate-B format

> **Stack adaptation (xiigen flows).** Universal test-gate / tier / Gate-B rules
> ported from `llm_mvp_core/docs/skills/code-execution-SKILL.md` (G04). Adapted to
> this repo (greenfield — no prior author skill existed here):
> - **Gate-B** is an *executable condition*, not "works correctly". For server code
>   it is `dotnet build Xiigen.Flows.slnx` (0 errors) **and** `dotnet test` on the
>   per-flow `Xiigen.Flow.<Name>.Tests` (xUnit). `Directory.Build.props` sets
>   `TreatWarningsAsErrors=true`, so the gate is **0 errors AND 0 warnings**.
> - The per-flow shape is verified:
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   plus `flows/FLOW-NN-<slug>/client/angular` (Angular 21 standalone client).
>   (e.g. `FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.{Api,Application,Infrastructure,Tests}`).
> - The "subproject number" of core becomes the **per-flow project** here: the
>   subproject completion gate runs per `FLOW-NN-*`, not over a single monolith.
> - The domain return type on `Application` public methods is `DataProcessResult<T>`
>   (`IsSuccess` / `ErrorCode`); services inherit `MicroserviceBase<TSelf>` and return
>   `Success<T>(...)` / `Failure<T>("CODE", "message", details)`. This is the XIIGen
>   family wrapper — **NOT** core `OperationResult<T>` (which stays in `llm_mvp_core`
>   and must not leak here). The "OperationResult<T> rule" below is restated as the
>   `DataProcessResult<T>` rule.
> - Metrics in tests are concrete xUnit assertions: `Assert.Equal(...)`,
>   `Assert.True(actual > baseline, $"...")`. E2E for client branches uses Angular 21
>   + Playwright (`flows/FLOW-NN-*/client/angular`). UI-specific lines apply only to
>   flows that ship an Angular client.
> - Core algorithm / trainable / ML / DPO / `ILearningMachine` logic stays in
>   `llm_mvp_core` (R5 / G12 boundary). This repo consumes it through `.xiigen` /
>   wiki-domain manifests and locators only; the dual-path host / Final-Between-Slices
>   wording from core is referenced for honesty, not run as a flows gate.

## When to load

Load for any session that adds, modifies, or reviews flow code or tests.

## Human-readable plan/report precondition

Do not start implementation from an architecture, trainable, code-review,
documentation, or audit report that fails the Human-Readable Architecture
Artifact Gate. Code execution must reject table-only, shortcode-heavy,
text in a language other than the one requested by the user, schema-only, or prompt/spec inputs.

For suspicious Dictionary/counter/KNN/scorer/selector/private-method repairs,
the controlling artifact must pass the Concrete Repair Blueprint Gate: exact
classes/records/methods/tests/data rows, affected sets/checkpoints/manifests,
and lifecycle paths must be named. Generic "wrap in ILogicalUnit", "add KNN",
or "chosen/rejected/quarantine" text is not a solution.

Do not start implementation from an audit that claims "Architecture audit
sufficient/complete", "достаточно" ("sufficient"), "готово" ("ready"), or "закрыто" ("closed") unless it passed
the Audit Sufficiency Boundary Gate: exact bounded scope, green breadth
challenge, parent review question answered, and Delta-Severity Feedback Gate
recorded for later same-domain BLOCKING/HIGH misses.

## Sub-Agent-Only Execution Boundary

If the parent/current session must work through sub-agents, is architect-only,
governance-only, or forbidden to become executor, this skill does not grant the
parent permission to inspect, build, test, run, or repair locally. Convert
implementation, governance, skill, report, plan, data, and code work into bounded
sub-agent work orders.

No code executor may receive or keep an unbounded scope. "Do not stop" means
continue the parent-owned bounded phase chain, not one code executor running
until the whole plan is closed or all remaining work is done. Each
code-execution work order must name the exact deliverable, allowed
files/commands, max active time of 10 minutes or less, required
protocol/evidence report, and `must_stop_after_report=true`; remaining work goes
back to parent review for the next bounded work order.

If a code executor is found running under an unbounded scope and has an id, the
parent must interrupt/close it before accepting more output. If the id is not
available, quarantine its output as evidence-only and replace it with a bounded
worker. A code executor that runs longer than 10 minutes without a
protocol/evidence packet is over-scoped: request or interrupt for the packet,
close if needed, split the scope, and continue.

Every code-execution packet must open with plain English unless the user requests another
language: total phases/steps, done/in-progress/remaining, what the phase solves,
what the current step solves, what evidence exists, what is not proven, and the
next bounded action. Machine labels and paths may follow only after this
explanation.

While a sub-agent is running, the parent may not use this skill to read governed
files or code for independent loophole research, repair drafting, or
non-overlapping work. Parent read-only verification is allowed only after a
sub-agent packet returns and only to verify/integrate that packet. Sub-agent
waiting is not a stop; wait/review/orchestrate instead of becoming executor.

## Runtime/Tool Command Construction Gate

UI/runtime/executor work orders must not use huge inline shell scripts. If a
command would exceed 20 nonblank lines, 4000 characters, or include more than
one function/class, the executor must write a scoped `script-file runner` under
the evidence/run folder and execute that file by path. The work order must name
the runner path, expected output, exit-code handling, stdout/stderr files, and
exact evidence filenames.

Windows error 206, oversized inline shell command, or any tool-command
construction failure is a work-order/tooling failure, not a product blocker.
After such a timeout/failure, the next step is preflight-only script-file
prep/execution, not another huge inline command retry.

## Evidence Text Formatting Gate

Runtime and evidence-writing sub-agents must create every text evidence
artifact in commit-ready format on the first write: UTF-8 without BOM, LF line
endings, and no trailing whitespace. JSON and JSONL evidence must parse before
the packet returns.

Before returning, run `git diff --check -- <changed evidence files>` for
in-repository changes. For generated, untracked, or no-index evidence files,
run an equivalent explicit scan that checks UTF-8 BOM, CRLF, trailing
whitespace, and JSON/JSONL parsing when applicable. The packet must list the
files checked, command or script path, exit code, and result.

## Zero failures is a hard gate

`dotnet test` must report 0 failed before every commit.
Not "only new tests fail." Not "pre-existing failures excluded." Zero.
Because `TreatWarningsAsErrors=true`, the build half of the gate is also a hard
gate: a single warning fails the build and therefore fails the phase.

## One test = one metric

```csharp
// ✅ Assert.Equal(0.895, result.BoundaryAccuracy, precision: 2);
// ✅ Assert.True(enhanced > baseline, $"expected enhanced > {baseline}, got {enhanced}");
// ❌ Assert.True(result.IsSuccess);  // too vague — what succeeded?
```

A test that only asserts `result.IsSuccess` proves the call ran, not that the
flow produced the right outcome. Assert the domain value (the `DataProcessResult`
payload field), an `ErrorCode`, a count, or a shape.

## Test-before-after rule

Every placement in a plan has a metric measured before and after.
The test captures both numbers and asserts `enhanced >= baseline`.
For a flow, the "metric" may be a schema assertion, a registry/handler count, a
`dotnet build` result, or a targeted `[Fact]`/`[Theory]` — depending on the phase.
No metric = not done.

## Tier system for experiments

```
Tier 1: 1-10 cases     (unit validation)
Tier 2: 11-100 cases   (integration signal)
Tier 3: 100+ cases     (production signal)
```

FAILED at Tier 2 → escalate to Tier 3 with more data.
SCALE_UP at any tier → needs more data, algorithm is correct.

## Per-flow completion gate

Before any flow phase is marked done, verify (run per affected `FLOW-NN-*`):

```bash
dotnet build Xiigen.Flows.slnx                                          # 0 errors AND 0 warnings
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests        # 0 failed, count >= baseline
git diff --name-only                                                    # only expected files changed
```

For client-bearing flows, also:

```bash
cd flows/FLOW-NN-<slug>/client/angular && npx ng build                  # client builds clean
npx playwright test                                                     # e2e green (if e2e exists)
```

## DataProcessResult<T> rule — every Application public method

```csharp
// ✅ public DataProcessResult<string> GetName() => Success("...");
// ✅ public Task<DataProcessResult<OnboardingOutcome>> DeliverAsync(...) => ...;
// ❌ public string GetName() => ...        // no DataProcessResult wrapper
// ❌ public bool Save() => ...             // bool without context
// ❌ throw new InvalidOperationException(...) for a business failure → use Failure<T>("CODE","message")
```

This is the XIIGen family wrapper. Do not import or substitute core
`OperationResult<T>` — that type belongs to `llm_mvp_core` and must not leak into
this repo.

## MicroserviceBase rule — every Application service class

```csharp
// ✅ self-typed base + an interface contract
public sealed class OnboardingDeliveryService : MicroserviceBase<OnboardingDeliveryService>, IOnboardingDelivery
{
    // returns Success<T>(...) / Failure<T>("CODE","message")
}
// ❌ public sealed class OnboardingDeliveryService                      // no base, no interface
// ❌ public sealed class OnboardingDeliveryService : IOnboardingDelivery  // missing MicroserviceBase<TSelf>
```

## Final Between Bounded Slices Code-Execution Gate (referenced, not a flows runtime gate)

Deprecated term: use **dual-path scenario coverage** instead of "product
readiness" as a working gate, primary status, stop reason, or counter. The full
live-host Claude/Cursor/Codex dual-path apparatus from core is **not run in this
repo** (G12 boundary); it is referenced only so a flows report never borrows its
wording to overclaim.

Within this repo the rule that still binds: a bounded executor/code slice may
return its packet, but the parent must not convert that return — or a later
commit/push — into a final stop while a known safe next bounded action exists.
"I will continue" is not continuation; the valid packet either starts the next
bounded sub-agent or names a real blocker. After commit/push, continue to the
next known safe bounded slice.
