# Four-Tier Decision Classification

> **Stack adaptation (xiigen flows).** Universal four-tier decision test ported from
> `llm_mvp_core/docs/skills/four-tier-decision-classification-SKILL.md`. Adapted to this repo:
> - **Q1 BOUNDARY CODE** examples are native .NET 10: the `DataProcessResult<T>` contract
>   (XIIGen domain wrapper — NOT core `OperationResult<T>`), the `MicroserviceBase<TSelf>`
>   `Success<T>`/`Failure<T>` envelope, and a flow's public Web API DTO shape, each under
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}`;
> - **Q2 FREEDOM CONFIG** = an `IConfiguration` key with a safe default plus a FREEDOM
>   annotation (see `freedom-machine-SKILL.md`); on the Angular 21 client the FREEDOM leg is a
>   provider / `InjectionToken` in `app.config.ts`;
> - **Q3 LEARNABLE / Q4 AI PIPELINE** remain as *classifications*, but real ML/trainable units
>   are **NOT** built in this repo (R5 / G12). This repo consumes the shared model through
>   `.xiigen/wiki-domain/model-manifest.json` + locators; only an adaptive user-side leg is
>   local. When a decision classifies as Q3/Q4, record the consumption point (the manifest) and
>   stop — do not author a trainable unit here;
> - **Gate-B** for any component produced from this classification = `dotnet build` +
>   `dotnet test` (xUnit) on the affected `Xiigen.Flow.<Name>.Tests` project.

---

## When to Invoke

Before designing any class, service, or component that makes a decision:
any `if/else`, `switch`, lookup table, threshold comparison, enum-to-value
mapping, or conditional branch. Supersedes `freedom-machine-SKILL.md` for
NEW components by adding LEARNABLE and AI PIPELINE tiers.

---

## What This Skill Prevents

- Hardcoding a value that should be deployment-tunable (e.g., baking a TTL or a
  rate-limit as a constant when different deployments need different values)
- Putting a LEARNABLE decision in `IConfiguration` (it gets a value but never
  improves with more data)
- Building a learnable/AI pipeline locally for a decision that is genuinely
  static and well-understood — or worse, re-implementing a shared model here
  instead of consuming it via manifest

---

## The Four-Tier Test

For every decision point, run these four questions IN ORDER. Stop at first YES.

```
Q1: Does changing this value break a system guarantee AND require a code change?
    YES → BOUNDARY CODE (Layer 1)
    Hardcode as a const or invariant. Abstract interface + DI wiring.
    .NET 10 (flows) examples:
      - DataProcessResult<T> contract (always Success/Failure — never throw for business logic)
      - MicroserviceBase<TSelf> Success<T>/Failure<T> envelope shape
      - A flow's public Web API DTO contract (changing it breaks API consumers)
      - Per-flow layer dependency direction (Api → Application → Infrastructure)
    → STOP. Treat as MACHINE + Fabric-First.

Q2: Might different deployments or tenants reasonably want different values?
    YES → FREEDOM CONFIG (Layer 2)
    IConfiguration key with a safe default. Changeable without code change.
    .NET 10 (flows) examples:
      - Email verification TTL (FLOW-01 default 86400 s)
      - Resend rate-limit window (default 60 min)
      - Idempotency reservation TTL passed per call
      - Logging verbosity / retry budget
    Angular 21 client: provider / InjectionToken default in app.config.ts.
    → STOP. Use IConfiguration (or Angular provider) + FREEDOM annotation.

Q3: Could the correct answer improve if the system observed more outcomes?
    YES → LEARNABLE (Layer 3)
    Backed by training data + a continuous-learning path.
    IMPORTANT (R5/G12): the shared trainable model is NOT built here. Record the
    consumption point — .xiigen/wiki-domain/model-manifest.json + locator — and
    the local adaptive user leg only. Do not author a trainable unit in this repo.
    → Continue to the manifest-consumption design, not a local trainer.

Q4: Does answering this require contextual reasoning beyond pattern matching?
    YES → AI PIPELINE (Layer 4)
    Inference call through the shared model + optional arbiter challenge.
    Same R5/G12 rule: route through the consumed model via manifest/locator; the
    pipeline core lives in llm_mvp_core, not in a flow project.
```

Every Q3 or Q4 decision must also record where the learning signal is captured
(the manifest-consumed model path), not invent a local trainer.

---

## The Five Learnable Indicators (for Q3)

When Q1 and Q2 are both NO, apply these to determine if the decision is
LEARNABLE or simply an unclassified FREEDOM value:

1. The decision has been wrong before (corrections in git history)
2. The answer differs by corpus or domain (e.g., one flow's data vs another's)
3. The right answer needs evidence from multiple runs, not one observation
4. Domain experts disagree on the default (uncertainty = learning opportunity)
5. A human has had to override the default more than once

If 2+ indicators are YES → LEARNABLE → design for manifest consumption + adaptive leg.
If 0-1 indicators → likely FREEDOM CONFIG that hasn't been classified yet.

---

## Classification Table

Produce this table before writing any new service or component:

```
| Decision point                      | Q1  | Q2  | Q3  | Q4  | Tier            |
|-------------------------------------|-----|-----|-----|-----|-----------------|
| DataProcessResult return convention | YES | —   | —   | —   | BOUNDARY CODE   |
| Email verification TTL              | NO  | YES | —   | —   | FREEDOM CONFIG  |
| Resend rate-limit window            | NO  | YES | —   | —   | FREEDOM CONFIG  |
| Doc-wiki relevance ranking weight   | NO  | NO  | YES | —   | LEARNABLE*      |
| Ambiguous user-intent classification| NO  | NO  | NO  | YES | AI PIPELINE*    |
```
`*` LEARNABLE / AI PIPELINE = consume the shared model via
`.xiigen/wiki-domain/model-manifest.json`; do not build the trainable unit here.

---

## Anti-Patterns

1. **"Every tunable should be config"** — a value that improves with observed
   outcomes belongs in the LEARNABLE tier (consumed via manifest), not `IConfiguration`.

2. **"I'll make it learnable later"** — retrofitting a learning signal is far
   harder than designing the consumption point in. Classify before implementing.

3. **"Simple enough to hardcode"** — simplicity is not the test. A simple
   threshold that should improve with corpus size IS a learning target.

4. **"I'll just build a small trainer in this flow"** — R5/G12 forbids it. The
   shared model lives in `llm_mvp_core`; this repo consumes it via manifest and
   keeps only an adaptive user leg.

---

## Connection to Downstream Skills

```
Q1 → BOUNDARY CODE   → freedom-machine-SKILL.md (MACHINE) + Fabric First
Q2 → FREEDOM CONFIG  → freedom-machine-SKILL.md annotation + IConfiguration / app.config.ts provider
Q3 → LEARNABLE       → .xiigen/wiki-domain/model-manifest.json consumption + adaptive user leg (R5/G12)
Q4 → AI PIPELINE     → shared inference pipeline consumed via manifest/locator (core stays in llm_mvp_core)
All Q3/Q4            → record the manifest consumption point before implementing
```
