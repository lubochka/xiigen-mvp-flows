# gap-to-proposal-SKILL.md — Climb the resolution ladder before proposing new components

> **Stack adaptation (xiigen flows).** Universal *gap to proposal* ported from
> `llm_mvp_core/docs/skills/gap-to-proposal-SKILL.md` (G08). Greenfield here — no
> prior author version (core's nearest was the engine-self `gap-to-proposal`,
> which was self-infrastructure-specific; this is the general product-gap ladder).
> The universal core kept: a 4-level resolution ladder — CONVENTION → ADAPTATION →
> EXTENSION → NEW COMPONENT — where you justify why the lower level cannot solve
> the problem before climbing; multi-gap dependency ordering; and a stop-gate
> requiring Luba approval before work begins. The stack details are adapted:
> - **LEVEL 2 (ADAPTATION)** = a new implementation under an existing `contracts/`
>   interface. **LEVEL 3 (EXTENSION)** = a new method on an existing
>   `server/Application` service. **LEVEL 4 (NEW COMPONENT)** = a new contract +
>   class returning `DataProcessResult<T>` (XIIGen family wrapper — **NOT** core
>   `OperationResult<T>`) + xUnit `[Fact]` + DI registration via the
>   `Add<Name>Flow()` extension in `Program.cs`.
> - A **UI gap** = a new Angular 21 standalone component + Playwright spec (for
>   client-bearing flows). Gate-B = `dotnet build` / `dotnet test` (+ `ng build` /
>   Playwright for UI).
> - Overrides are recorded in a decision artifact next to the plan /
>   `flow.manifest.json`, not in a core `DECISIONS-LOCKED.md`.

## The Resolution Ladder

Before proposing a new contract, new service, or new algorithm, climb the ladder.
Start at Level 1. Move up only if the lower level genuinely cannot solve the
problem.

```
LEVEL 1 — CONVENTION: Can a governance/skill/doc update fix this?
  Example: a flow's Application service skips a guard clause pattern
  Fix: update the relevant skill / DNA-DOTNET-TRANSLATION.md with a pre-commit grep check
  No new code. 10-minute governance update.

LEVEL 2 — ADAPTATION: Does an existing contract cover this with a new implementation?
  Example: need a different onboarding delivery channel
  Check: IOnboardingDelivery exists (it does)
  Fix: add EmailOnboardingDelivery : IOnboardingDelivery
  No new contract. One implementation session.

LEVEL 3 — EXTENSION: Does an existing service cover this with a new method?
  Example: UserRegistrationService needs a ResendVerificationAsync()
  Check: the service already exists with RegisterAsync()
  Fix: add ResendVerificationAsync() → DataProcessResult<T> + register nothing new
  A phase addition, not a new flow.

LEVEL 4 — NEW COMPONENT: None of the above apply.
  Example: a brand-new capability with no covering contract
  New contract (I<Name>) in contracts/ + new class returning DataProcessResult<T>
  + new xUnit tests + DI registration in the Add<Name>Flow() extension.
  UI gap → also a new Angular standalone component + Playwright spec.
```

Document why each lower level is insufficient before proposing a higher level.

---

## Step 1 — Classify the Gap

```
Gap: no contract covers "moderation decision" for the registration flow

Q1: Can CONVENTION fix it?
    No — a moderation decision is logic, not a configuration value.

Q2: Can ADAPTATION fix it? (existing contract + new implementation)
    No — no existing contract covers moderation; IOnboardingDelivery is a different concern.

Q3: Can EXTENSION fix it? (add a method to an existing service)
    Partially — RegisterAsync() could inline moderation, but that mixes concerns,
    breaks fabric-first, and makes moderation untestable in isolation.

Q4: New component needed?
    YES — IModerationDecision contract + ModerationDecisionService returning
    DataProcessResult<ModerationOutcome>, injected into the registration pipeline.
```

---

## Step 2 — Produce the Proposal

```markdown
## GAP RESOLUTION PROPOSAL — IModerationDecision

**Gap detected:** registration flow has no moderation decision component
**Resolution level:** LEVEL 4 — new contract + implementation

**What will be built:**
  - IModerationDecision contract: DecideAsync(RegistrationInput) → DataProcessResult<ModerationOutcome>
  - ModerationDecisionService : MicroserviceBase<ModerationDecisionService>, IModerationDecision
  - xUnit tests: unit tests for decision branches; integration test through the flow
  - DI: register in AddUserRegistrationFlow() (Program.cs path)
  - UI (if applicable): an Angular standalone review panel + Playwright spec

**Prerequisite gaps to resolve first:**
  - contracts/schemas moderation request/response schema must exist
  - tenant scoping (ITenantContextAccessor) must be resolvable in the service

**Estimated effort:** 1 phase (contract + implementation + tests)
**Builds on:** the existing IOnboardingDelivery + MicroserviceBase pattern

**Gate-B:** dotnet test --filter "FullyQualifiedName~ModerationDecision" → N passed, 0 failed
            dotnet build Xiigen.Flows.slnx → 0 Error(s), 0 warnings

**Stop required before:** beginning the flow integration test

**Needs Luba decision:**
  - Is the moderation threshold a FREEDOM/config value or fixed? (Recommendation: config with a default)
```

---

## Step 3 — Dependency Order for Multiple Gaps

```
Gap A (moderation schema in contracts/) → blocks Gap B
Gap B (IModerationDecision + service)   → blocks Gap C
Gap C (flow integration)                → blocks the user-facing capability

Order: A → B → C
Rationale: A is the prerequisite, B uses A, C uses both.
```

Cross-flow gaps follow the same rule; if Gap B needs another flow's contract,
that dependency goes in `flow.manifest.json` and is ordered first.

---

## Stop Gate

Every proposal requires Luba's approval before work begins. The proposal is the
INPUT to approval, not permission to start. No new-component session begins
without an explicit "yes".

---

## Anti-Patterns

1. **Jumping to LEVEL 4 without checking lower levels** — most gaps are Level 2 or 3.
2. **"We'll need this eventually so let's build it now"** — build when the gap blocks current work, not speculatively.
3. **Proposal without a prerequisite chain** — if Gap A depends on Gap B (incl. cross-flow), the proposal must say so.
4. **Starting work before Luba's approval** — the proposal gate is mandatory.
5. **A LEVEL 4 proposal that returns a bare type or `bool`** — every new Application method returns `DataProcessResult<T>`; a proposal that skips it is incomplete.
