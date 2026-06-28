# Level Correction Response

> **Stack adaptation (xiigen flows).** Universal "abandon-the-frame, rebuild one level up"
> rule + 3 proactive tests ported from
> `llm_mvp_core/docs/skills/level-correction-response-SKILL.md`. Adapted to this repo:
> - **provider-vs-capability naming** examples are .NET 10 / Angular 21: name a flow service
>   for the capability it provides, not the vendor/tech it uses (`IRedisService` ❌ →
>   `ISessionStore` ✅; `ISqlOutboxService` ❌ → `IOutboxStore` ✅; Angular `OpenAiClient` ❌ →
>   `IntentResolver` ✅);
> - **"going up"** examples use the per-flow layer model:
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}` and the
>   Angular 21 standalone client / DI tokens in `app.config.ts`;
> - the **Old-Model-Survival** scan runs over the plan text for surviving provider names,
>   concrete-type names where interfaces belong, and per-flow solutions where a general
>   mechanism belongs; the domain return type stays `DataProcessResult<T>` (XIIGen wrapper),
>   not core `OperationResult<T>`.

## Core Problem

When a proposal is built at the wrong abstraction level — "new flow service" when
the answer is "extend an existing interface" — adding more detail to the wrong
proposal makes it worse, not better. The correct response to a level challenge is
to abandon the current frame entirely and rebuild from one level higher. This skill
defines the three proactive tests that catch wrong abstraction levels before they
are embedded in a plan, and the single rule for responding when a challenge fires.

---

## When to Invoke

- BEFORE drafting any plan, phase description, or component design (proactive scan)
- When Luba says "you're thinking as X, not as Y" (reactive correction)
- When "add another layer" or "try again" is the response to a proposal
- When the same problem is solved twice instead of once

Load order: before all other planning skills. A session where a level challenge is
met with elaboration rather than frame abandonment has failed before any checklist fires.

---

## Section 1 — Three Proactive Tests (Run Before Planning Starts)

Run all three tests before writing any plan. They catch wrong levels before
a proposal is embedded in a document where it costs a revision cycle to fix.

### Test 1 — Next Instance Test

**Trigger words in the plan:** "for each flow," "per-flow," "maintain a list of,"
"pre-authored," "one per capability," "per provider."

**Ask:** if one more instance (one more flow) of this case arrives tomorrow, what happens?
- Manual work required again → **WRONG ABSTRACTION LEVEL** — find the general capability
- Solution runs automatically → proceed

```
FAILS: hand-write a bespoke registration service for each flow
       → new flow added → manual work again → unbounded

PASSES: each flow registers its capability through a shared interface
       → new flow added → registration runs automatically → bounded
```

---

### Test 2 — Provider vs Capability Test

Apply to every interface name, class name, configuration key, DI token, and prerequisite name.

**The test:** does this name identify a CAPABILITY (what it does) or a PROVIDER (what it uses)?

**Naming examples:**

```
FAILS (provider names):       PASSES (capability names):
IRedisService                 ISessionStore (scoped, ephemeral)
IElasticsearchService         IDocumentSearch (search capability)
ISqlOutboxService             IOutboxStore (durable append capability)
OpenAiClient (Angular)        IntentResolver (capability)
```

A class named for its provider cannot be swapped without renaming every caller.
A class named for its capability can swap providers without touching callers.

---

### Test 3 — Old Model Survival Test

**Trigger:** any time a reframe was established in this session ("the correct model
is X instead of Y").

After any architectural reframe, scan the entire plan for surviving remnants of
the previous thinking:
- Provider names where capability names should be
- Concrete class names where interface names should be
- Per-flow solutions where a general mechanism should be
- Direct dependency names where DI/fabric injection should be

Evidence: even after a reframe is established, the old model has at least one
hiding place in the remaining plan text. Find it before writing session files.

**Rule:** do not begin writing any file until all three tests return clean.

---

## Section 2 — The One Rule for Responding to a Challenge

**When Luba challenges the LEVEL of a proposal — not the content — abandon the
current frame entirely and rebuild from one level higher.**

Do NOT:
- Revise the current proposal with additions
- Add a peer proposal alongside the current one
- Explain why the current level was reasonable
- Run the current proposal through more checks
- Defend by citing the skill that produced it

DO:
- Stop. Go silent on the current frame.
- Ask: what is one level above what I just proposed?
- Build from there.

---

## Section 3 — Distinguishing Level Correction from Detail Correction

**Detail correction:** "That's wrong — the method returns `DataProcessResult<UserOutcome>` not `Task<bool>`."
Response: revise the specific fact. Stay at the same level.

**Level correction:** "You're still thinking as a single-flow utility writer, not as a shared-interface designer."
Response: abandon the entire frame. The problem is the perspective, not a fact.

**The signal:** Luba says what you ARE, not what you got wrong.
"You're thinking as X" = level correction. Go up.

Other level correction signals:
- "Another layer" — you proposed a new class; the answer is a convention
- "You don't see the abstraction" — the solution exists at a higher level of generalization
- "Try again" after you have already added detail — detail is not the answer
- "That's still the same thing" after you added an alternative

---

## Section 4 — "Going Up" in Practice

Going up means asking: what is the more abstract version of this problem?

**Example from a flow:**

```
Proposed: new IFlow01AuditService interface + concrete Flow01AuditService in FLOW-01
Challenge: "you're designing a per-flow dependency, but auditing is a cross-flow concern"

Wrong response: improve the FLOW-01 interface design
Wrong response: add an audit extension method alongside the FLOW-01 interface

Correct response:
  What is one level above "how do I add auditing to FLOW-01?"
  → "How do cross-cutting concerns get added to flow services in this repo?"
  → The answer is a shared convention/middleware applied at the Api host layer,
    not a per-flow interface.
  → The answer is a convention, not a new per-flow interface.
```

---

## Section 5 — Adding a Layer Is Defending the Frame

When told the level is wrong, the instinct is to add something beside or below
the current proposal that addresses the challenge while keeping the current
proposal intact. This feels like being responsive. It is defending the frame.

```
❌ "You're right that IConfiguration works — and we could also have a
   Flow01ConfigService wrapper for cases where we need typed access..."

   This keeps Flow01ConfigService. It adds a condition that makes it seem
   reasonable. The frame has not changed. A layer has been added.

✅ "IConfiguration is the right abstraction. No wrapper needed. The four-tier
   decision classification governs whether each value is MACHINE or FREEDOM,
   and IConfiguration already provides that access pattern."

   The frame is gone. The answer is at the correct level.
```

---

## Anti-Patterns

1. **"I added the provider name in the implementation, not the interface"** — if the
   provider name lives anywhere in a plan, the old model has survived. Test 3 catches it.

2. **"The reframe was about Y, not about Z"** — every reframe propagates. A reframe
   of "use interfaces not concrete types" touches every class name in the plan.

3. **"I'll apply the abstraction in the next phase"** — a plan that starts at the wrong
   level and intends to correct it later embeds the wrong level in Phase 1 artifacts.
   Correct before drafting.

4. **"Explaining why the previous frame was reasonable"** — when the level is wrong,
   the explanation is wasted. Abandon and rebuild.

---

## Integration

- `solution-scope-gate-SKILL.md` defines the 5-level scope ladder; this skill detects when
  a proposal is at the wrong level on that ladder
- `four-tier-decision-classification-SKILL.md` Q1–Q4 provides the questions that resolve
  Test 1 (next instance test) for new capability proposals
- naming follows Test 2 (provider vs capability) at the file/class/DI-token level
