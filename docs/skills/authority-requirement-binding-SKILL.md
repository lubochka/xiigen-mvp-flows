# Authority Requirement Binding

> **Stack adaptation (xiigen flows).** Universal authority-binding ledgers + replacement rule +
> contradiction sweep ported from
> `llm_mvp_core/docs/skills/authority-requirement-binding-SKILL.md`. Adapted to this repo:
> - **authority sources** = the current human operator (Luba), an architect work order, an
>   approved active plan, and accepted reviewer gates. The `.xiigen/wiki-domain` manifests and
>   any pasted transcript are **evidence_only** — they cannot create or authorize a requirement;
> - each Authority Requirements Ledger row maps a requirement → the target flow project +
>   **Gate-B** (`dotnet build` + `dotnet test`, xUnit, on `Xiigen.Flow.<Name>.Tests`; Angular 21
>   Playwright for client branches) + the concrete artifact;
> - the domain return type is `DataProcessResult<T>` (XIIGen wrapper), not core
>   `OperationResult<T>`;
> - the **trainable-replacement** clause (Section 3/4) still applies, but per R5/G12 the
>   trainable model is consumed from `llm_mvp_core` via `.xiigen/wiki-domain/model-manifest.json`
>   + a locator; only an adaptive user leg is local. A "trainable replacement" requirement here
>   is satisfied by naming the consumed model asset + manifest + locator + fresh-load/continue
>   probes against that consumed model, not by building a trainer in a flow.

## Core Problem

Agents often preserve the shape of an older plan after the task authority adds
or strengthens a requirement. That turns explicit requirements into wishlist
items and lets downstream agents silently narrow the task. This skill makes
every upstream requirement a binding acceptance criterion before plan prose,
work orders, review verdicts, or execution.

---

## When to Invoke

- Before drafting, reviewing, approving, executing, or repairing any plan
- After any user/operator/product-owner correction
- Before an architect issues a bounded work order to a sub-agent
- After an accepted reviewer finding becomes a repair requirement
- When a plan references the consumed shared model, import/export, locators, or
  the `.xiigen` manifests

---

## Section 1 — Active Task Authority

Identify every active authority source:

```text
human_operator
product_owner
architect_work_order
approved_active_plan
accepted_reviewer_gate
```

Every explicit requirement from those sources is binding unless the same source
explicitly marks it optional, exploratory, or preference-only.

The `.xiigen/wiki-domain` manifests, pasted transcripts, old plans, and sub-agent
packets are **evidence_only**: they cannot create a requirement or authorize
execution unless Luba's current unquoted message reactivates the exact text.

Downstream agents must not downgrade, ignore, reinterpret, or replace an
upstream requirement with a more convenient narrower task.

---

## Section 2 — Required Ledgers

Before plan prose or review verdict, produce:

```text
Authority Requirements Ledger
Conversation Delta Ledger
Replacement Mapping Register
Contradiction Sweep
```

Each Authority Requirements Ledger row must contain:

```text
requirement_id
source_authority
source_quote_or_work_order_text
normalized_requirement
requirement_type = must_do | must_not_do | scope | sequence | artifact | gate | definition | replacement
binding_level = required | optional_by_source
old_assumption_invalidated
forbidden_or_superseded_mechanism
required_replacement_capability
plan_search_terms
plan_section
gate_or_test            # dotnet test <Xiigen.Flow.<Name>.Tests> | Playwright | build
required_artifact       # flow project / contract / Angular component / doc
contradiction_scan_result
status = mapped | unmapped | deletion_only | contradicted
next_action
```

The Conversation Delta Ledger records only requirements added, clarified, or
strengthened since the previous review. The delta must name the old assumption
that is no longer valid.

The Replacement Mapping Register is required whenever an authority forbids,
rejects, replaces, or supersedes a mechanism.

The Contradiction Sweep searches the whole plan, not only the edited section.

---

## Section 3 — Replacement Rule

Removing an old mechanism is not coverage for a required replacement.

If the authority requires a trainable replacement, the plan must name (R5/G12 —
the model is consumed from `llm_mvp_core`, not built here):

```text
consumed_model_unit_id            # from .xiigen/wiki-domain/model-manifest.json
manifest                          # .xiigen/wiki-domain/model-manifest.json
locator_or_marketplace_entry      # how the flow resolves the model
local_adaptive_leg                # the only locally-trained part, if any
fresh_load_probe                  # proves the consumed model loads
continue_training_probe           # proves the adaptive leg continues (if present)
ablation_or_leakage_gate          # proves it is not a deterministic lookup
```

A deterministic mechanism (dictionary, regex, direct lookup) cannot satisfy a
trainable replacement requirement unless the active authority explicitly allows
that deterministic mechanism in the current conversation or work order.

---

## Section 4 — Worked Replacement Example (flows)

If the authority requires the flow to answer documentation/wiki questions through
the **consumed trainable doc-wiki model**, then production answers must route
through that consumed model (resolved via `model-manifest.json` + locator).

These are NOT coverage for the consumed trainable model:

```text
hardcoded answer dictionary in a flow Application service
regex-only question classification in a production route
direct file grep returned as the answer
exact-question lookup table
```

Allowed deterministic helper scope:

```text
corpus labeling
build-time audit
fixture generation
evaluation setup
```

If any deterministic shortcut remains in a production routing path while the
ledger requires the consumed trainable model, the plan is contradicted.

---

## Section 5 — Review Verdict Rules

Approve only when:

```text
all binding requirements are mapped to flow project + Gate-B + artifact
all replacement requirements name the consumed model asset + manifest + locator + probes
contradiction sweep has no production deterministic shortcuts where the model is required
completion matrix has one row per ledger row
.xiigen manifests / transcripts were treated as evidence_only
```

Reject when:

```text
requirements are treated as suggestions
only the closest existing flow shape is covered
old mechanism is deleted without the required replacement
deterministic production shortcut replaces a required consumed-model route
sub-agent work order omits architect requirements
reviewer gate findings are not converted into downstream requirements
a .xiigen manifest or transcript was used as the authority for a requirement
```

---

## Output Format

```markdown
## Authority Requirement Binding Check

Active authorities:
- [authority]: [source]

Conversation Delta Ledger:
| ID | Source | Requirement | Old assumption invalidated | Required replacement | Status |
|---|---|---|---|---|---|

Replacement Mapping Register:
| Superseded mechanism | Required replacement | Plan section | Gate/artifact | Status |
|---|---|---|---|---|

Contradiction Sweep:
| Search term | Production occurrence | Allowed helper occurrence | Verdict |
|---|---|---|---|

Verdict:
APPROVE | REJECT

Next action:
[first unmapped/deletion-only/contradicted row repair]
```

---

## Anti-Patterns

1. Treating an explicit upstream requirement as a wishlist item.
2. Saying "removed X" when the requirement was "replace X with Y."
3. Checking only the edited plan section instead of searching the whole plan.
4. Letting a sub-agent execute a narrower version of the architect work order.
5. Counting a deterministic lookup, regex, or dictionary route as the consumed
   trainable model when the authority required the trainable route.
6. Treating a `.xiigen` manifest or pasted transcript as the requirement authority.
