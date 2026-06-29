# Materialization Session Type

> **Stack adaptation (xiigen flows).** The MATERIALIZATION session type ported
> from `llm_mvp_core/docs/skills/materialization-session-type-SKILL.md` (G07). The
> universal part (a distinct session type for "design exists + code exists + wiring
> gap to a user-visible surface": 3 mandatory inputs — design pointer, target
> surface pointer, round-trip step pointer; 4 steps — INVENTORY -> IDENTIFY GAP
> with file:line -> MINIMAL 1–5 task plan -> ROUND-TRIP verify; a hard cap of 5
> tasks and 0 new design decisions) is taken verbatim from core. Adapted here:
> - **All Windows Forms / `MainForm.cs` demo-stage examples are rewritten to
>   Angular / Playwright.** The target surface = an Angular 21 standalone component
>   under `flows/FLOW-NN-<slug>/client/angular` or a Playwright e2e spec.
> - The **round-trip** = a call that goes through the `net10.0` server endpoint in
>   `flows/FLOW-NN-<slug>/server` (`Xiigen.Flow.<Name>.Api`) and becomes visible in
>   the Angular UI; the acceptance check is `ng test` / Playwright on that surface.
> - INVENTORY runs against `flows/FLOW-NN-<slug>/contracts` and the consumed
>   `.xiigen/wiki-domain` manifests from core.
> - Gap statements use `grep` over `flows/FLOW-NN-<slug>/client/angular/src`; Gate B
>   uses `dotnet test --filter` (xUnit) or `ng test` / Playwright filters, not
>   `dotnet test --filter` on a monolith.
> - Domain return type cited is `DataProcessResult<T>` — **not** core
>   `OperationResult<T>`.
> - This skill pairs with `mvp-round-trip-verification-SKILL.md` (already ported)
>   for the per-step acceptance commands.

## Core Problem

Many sessions start with the design already complete and the code already written,
but nothing connects them to the user-visible surface. Treating these sessions as
PLANNING sessions produces 10-15 turns of re-design work for decisions already made.
Treating them as EXECUTOR sessions skips the inventory and round-trip verification
that makes the wiring safe. MATERIALIZATION is a distinct session type for this
specific work shape: existing design + existing code + wiring gap to user-visible surface.

---

## When to Invoke

- At session start, when reconnaissance finds existing design artifacts for the work in scope
- When the user's request includes: "make this visible," "wire up," "connect X to Y,"
  "surface this to the Angular UI," "make this work end-to-end"
- When `design-artifact-completeness-SKILL.md` reports PASS on checks 1-4 but the
  feature is not yet observable through the Angular client
- When `claim-as-hypothesis-SKILL.md` verifies "design exists" as VERIFIED

**Default rule:** If design exists (per `design-artifact-completeness-SKILL.md`) AND
the work touches user-visible surfaces (the Angular 21 client), default session type
is MATERIALIZATION. Overriding to PLANNING or GENERATION requires written justification.

---

## Section 1 — MATERIALIZATION vs Other Session Types

| Type | Purpose | Plan shape |
|---|---|---|
| PLANNING | New design decisions to make | 10-15 turns, arbiter rounds, gate reviews |
| GENERATION | New code to produce (no design yet) | Phase-by-phase with Gate B per phase |
| EXECUTOR | Execute a plan authored by another session | Follows existing plan, no plan-authoring |
| MATERIALIZATION | Wire existing design + code to user-visible surface | 1-5 tasks, specific wiring changes |

MATERIALIZATION is not EXECUTOR with extra steps. EXECUTOR executes someone else's plan.
MATERIALIZATION authors its own 1-5 task plan before executing.

---

## Section 2 — Three Mandatory Inputs

A MATERIALIZATION session cannot start without all three.

### Input 1 — Design source pointer

Path to the existing design artifact(s) the session is materializing:
- A contract declaration in `flows/FLOW-NN-<slug>/contracts`
- An architecture decision in the flow's locked-decisions doc
- A server service in `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application`

If the path doesn't exist or `design-artifact-completeness-SKILL.md` reports INCOMPLETE:
restart as GENERATION (populate the design first).

### Input 2 — Target user-visible surface pointer

Path to the UI component or observable output the session wires to:
- An Angular 21 standalone component under `flows/FLOW-NN-<slug>/client/angular/src`
- A Playwright e2e spec that exercises the end-to-end path
- A new capability visible through the Angular client UI

If the target surface does not exist: the session is MATERIALIZATION + GENERATION
combined — that is two sessions. Split them.

### Input 3 — Round-trip step pointer

Which step(s) of the `mvp-round-trip-verification-SKILL.md` round-trip does
this session advance? Example: "This session advances the step where the capability
is callable through the `Xiigen.Flow.MarketplacePayments.Api` checkout endpoint and
the step where the settlement result is observable in the Angular checkout component."

Without this pointer, the session has no acceptance test — success is undefined.

---

## Section 3 — Four-Step MATERIALIZATION Process

### Step 1 — INVENTORY

Run `claim-as-hypothesis-SKILL.md` on every design claim + `design-artifact-completeness-SKILL.md`
on every subject artifact. Produce a list:

```
Artifacts present and complete (usable as inputs):
  □ [contract path] — reachable ✅
  ...

Artifacts present but incomplete (blocking):
  □ [service path] — specific ❌ (still a stub returning Failure<T>("NOT_IMPLEMENTED",...))
  ...

Artifacts referenced but missing (blocking):
  □ [expected path] — file not found
  ...
```

Session cannot proceed to Step 2 until every blocking gap is either resolved
(create the missing artifact) or explicitly scoped out with a written justification.

### Step 2 — IDENTIFY WIRING GAP

What specifically sits between the complete design and the user-visible surface?
Use concrete gaps with file:line references, not abstract descriptions:

```
✅ GOOD gap statement:
  "PaymentSettlementService.SettleAsync() is implemented at
   flows/FLOW-16-marketplace-payments/server/Xiigen.Flow.MarketplacePayments.Application/PaymentSettlementService.cs:55
   but the Angular checkout component has no call that hits the /settle endpoint —
   grep shows 0 hits for 'settle' in flows/FLOW-16-marketplace-payments/client/angular/src"

❌ BAD gap statement:
  "The settlement service is not connected to the UI"
```

### Step 3 — MINIMAL WIRING PLAN (1-5 tasks, hard upper bound)

Each task:
- Names 1-3 specific files
- Specifies the concrete change ("add a `settle()` call in the checkout component's
  submit handler that calls the `CheckoutService` Angular client, which hits the
  `Xiigen.Flow.MarketplacePayments.Api` /settle endpoint")
- Has a verification step (usually a round-trip check or specific test)

```
Example 3-task plan:
  Task 1: Wire SettleAsync into the Angular checkout flow
    Files: client/angular/src/app/checkout/checkout.component.ts (add submit call),
           client/angular/src/app/checkout/checkout.service.ts (HTTP call to /settle)
    Change: call checkoutService.settle(checkoutId) from the submit handler
    Verify: ng test --include="**/checkout.component.spec.ts" -> Failed: 0

  Task 2: Add a Playwright e2e for the full settlement path
    Files: client/angular/e2e/checkout-settle.spec.ts (new)
    Change: open checkout -> submit -> assert "settled" state visible
    Verify: npx playwright test checkout-settle -> 1 passed

  Task 3: Add the /settle minimal-API endpoint binding (if missing)
    Files: server/Xiigen.Flow.MarketplacePayments.Api/Program.cs (map POST /settle)
    Change: bind the endpoint to PaymentSettlementService.SettleAsync, return DataProcessResult<T>
    Verify: dotnet test --filter "SettleEndpoint" -> Failed: 0
```

If the work genuinely needs > 5 tasks: either split into multiple MATERIALIZATION sessions
(one per round-trip step) or identify hidden GENERATION work and pull it into its own session.

### Step 4 — ROUND-TRIP VERIFICATION

Execute the `mvp-round-trip-verification-SKILL.md` per-step verification commands for the
round-trip steps declared in Input 3.

The session's acceptance test is not "tests pass" — it is "the round-trip step declared
at Input 3 now shows PASS on its verification command" (the call goes through the
`net10.0` server endpoint and the result is observable in the Angular UI).

---

## Section 4 — Output Shape Constraints

| Constraint | Value |
|---|---|
| Task count | 1–5 (hard upper bound) |
| Files changed per task | 1–3 typical; 5 maximum |
| New design decisions | 0 (if > 0, session is misclassified) |
| New capability archetypes | 0 |
| Arbiter panel rounds for plan review | 0 (plan review is not needed for wiring) |
| Round-trip step advances | ≥ 1 required |

---

## Anti-Patterns

1. **"Let's treat this as PLANNING for safety — more review is better"** — more review
   of the wrong session type adds design noise to wiring work. 10 turns of re-designing
   what's already decided is not safety; it is waste.

2. **"Design exists but I want to revise it while I'm here"** — design revision is PLANNING
   or GENERATION. MATERIALIZATION sessions do not re-design. If revision is needed,
   that is a separate session with a separate session type.

3. **"The task list is 7 tasks but each is small"** — the constraint is 5 tasks,
   not 5 large tasks. 7 tasks means hidden GENERATION work is in the task list,
   or the session needs to be split into two.

4. **"Round-trip pointer is optional — we'll know when it's done"** — without a
   round-trip pointer, success is undefined. The session produces output that "looks
   done" but cannot be verified as delivered.

5. **"Skipped Step 1 INVENTORY — design obviously exists"** — design artifacts that
   "obviously exist" sometimes contain stubs (a service returning
   `Failure<T>("NOT_IMPLEMENTED",...)`) or a contract not reachable from the Angular
   client. Run the inventory; the check takes 5 minutes and prevents a session wasted
   on wiring to a stub.

---

## Integration

- `session-mode-declaration-SKILL.md` includes MATERIALIZATION as one of the 5 mode values
- `design-artifact-completeness-SKILL.md` runs against every artifact in Step 1 INVENTORY
- `claim-as-hypothesis-SKILL.md` verifies user design claims during Step 1 INVENTORY
- `mvp-round-trip-verification-SKILL.md` provides the acceptance test at Step 4
- `goal-delivery-completeness-SKILL.md` (G03) verifies task-to-goal mapping; for
  MATERIALIZATION sessions, "delivers goal" means "round-trip step declared in Input 3
  passes verification"
