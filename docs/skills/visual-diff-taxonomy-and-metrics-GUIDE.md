# visual-diff-taxonomy-and-metrics-GUIDE.md — 8-category visual-diff taxonomy + severity + metrics (xiigen flows)
## v1.0: ported from llm_mvp_core visual-diff-taxonomy-and-metrics-GUIDE → Angular 21 evidence layer (no model training)

## Summary

This guide defines the **visual taxonomy and metrics** for comparing a design
source with a built Angular candidate in xiigen flows: how to describe
source ↔ rendered differences, which measurements to collect, and where the boundary
lies between QA/evaluation evidence and trained-model quality.

**Boundary for flows.** Held-out evaluation, ablation/no-hidden-determinism, and
negative-DPO (Direct Preference Optimization) are **core-ML
evaluation/training mechanisms (G12 / R5 / R6)** and **remain in `llm_mvp_core`**.
In flows, this guide governs **only the evidence layer** (source ↔ rendered
visual differences): it does not train a model, create held-out/ablation gates,
or bind findings to DPO. `dominant_error_category` records the cause of a defect,
**without binding it to a DPO row** (the core does that).

## When to use

Use this guide for a plan, review, or work order involving visual differences,
visual quality, screenshot comparison, or design-to-code accuracy for a per-flow
Angular client. **Do not** use it to build held-out/ablation/DPO mechanisms in flows;
those belong to the core.

## Inputs

- Original design refs and the normalized `.xiigen/wiki-domain` design tree.
- Rendered candidate screenshot refs from server-side render-QA
  (`html-css-render-qa-automation-GUIDE.md`).
- DOM (Document Object Model) snapshot and computed-style refs when available.
- Viewport/state metadata (including direction `ltr`/`rtl`).
- Asset resolution report.
- Human visual verdict and correction when available.
- Optional external visual AI (Artificial Intelligence) provider evidence **only
  after explicit Luba approval**.

## Outputs/artifacts

Visual evaluation produces a taxonomy-based verdict (written to
`flows/FLOW-NN/docs` or `artifacts/`):

- `overall_visual_quality_score`: configured numeric score with threshold.
- `taxonomy_findings`: findings using the 8 categories below.
- `dominant_error_category`: the single primary defect (evidence note;
  **not bound to a DPO row in flows**).
- `severity`: `minor`, `moderate`, `major`, `blocking`.
- `viewport_coverage`: which viewport/state/direction rows were checked.
- `metric_breakdown`: local numeric metrics + human-readable notes, each linked
  to source/rendered/viewport/hash.
- `external_evidence_ref`: optional, approval-bound external provider output.
- `verdict`: `accept_candidate`, `reject_candidate`, `quarantine`,
  `needs_human_review`, or `insufficient_evidence`.

## Required gates (flows scope)

- **Taxonomy coverage gate**: every visual verdict classifies findings using:
  layout, typography, color, spacing, responsiveness, image/crop, asset-missing,
  and interaction/state when applicable.
- **Metric traceability gate**: every score links to source screenshot, rendered
  screenshot, viewport, candidate hash, and taxonomy row.
- **Dominant-error gate**: a rejected candidate names one dominant
  visual/layout error rather than a bundle of unrelated failures
  (recorded as evidence; **DPO authoring stays in `llm_mvp_core`**).
- **External-provider approval gate**: a visual AI provider is an
  external-evidence step requiring explicit Luba approval. It is not itself proof
  of trained model quality.
- Exact proof phrase for review: **external visual AI evidence is not itself proof of trained model quality**.

### Gates that DO NOT run in flows (stay in `llm_mvp_core` / G12)

- **Held-out gate** (source-disjoint held-out visual metrics before a trained
  quality claim) — core only.
- **Ablation / no-hidden-determinism gate** (prove the trained model is not
  succeeding via deterministic source ids / file paths / template ids / fixed
  diff shortcuts) — core only.
- **Negative-DPO authoring** (chosen/rejected pairs, reward margin) — core only.

Flows produces the visual-diff **evidence**; the model quality claim, its
held-out proof, and its DPO training live in `llm_mvp_core` and are consumed via
manifest.

## Taxonomy

- **Layout**: hierarchy, grid, alignment, order, nesting, constraints, overflow,
  z-index, component placement.
- **Typography**: font family, size, weight, line height, letter spacing,
  wrapping, text overflow, text hierarchy.
- **Color**: fill, stroke, background, opacity, gradients, shadows, contrast,
  theme/state colors.
- **Spacing**: margins, padding, gaps, rhythm, baseline spacing, density.
- **Responsiveness**: breakpoint behavior, resize flow, orientation, scroll area,
  mobile/tablet/desktop consistency.
- **Image/crop**: object fit, focal point, mask, aspect ratio, crop, resolution,
  asset scaling.
- **Asset missing**: missing fonts, icons, images, masks, videos, blocked network
  assets, fallback substitutions.
- **Interaction/state**: hover, focus, active, disabled, selected, open/closed,
  loading, error, and other state variants when applicable.

RTL note: under `dir="rtl"`, layout/spacing/image findings include mirroring
correctness (logical vs physical properties) as a first-class check.

## Metrics

- Layout match rate per viewport.
- Typography match rate per text group.
- Color delta and contrast compliance per semantic token.
- Spacing deviation by component and container.
- Responsive breakpoint pass rate.
- Image/crop match rate.
- Missing-asset count and severity.
- State coverage pass rate.
- Human correction acceptance rate.

(Metrics that belong to the trained model — DPO chosen/rejected reward margin,
held-out visual quality before/after training — are **not** computed in flows;
they are `llm_mvp_core` metrics.)

## Forbidden substitutes

- Pixel similarity alone is not visual quality.
- A visual AI provider response is not a trained-model quality proof.
- A taxonomy label without screenshot refs and hashes is not evidence.
- A broad "looks good" note cannot become a verdict.
- A high render success rate cannot replace structured taxonomy findings.
- A deterministic diff threshold cannot be described as semantic learning.

## How it feeds the plan

Define the visual taxonomy and metrics for the flow's design-conversion
**evidence** before reporting visual quality. The plan must require local metric
artifacts, human-readable findings, and explicit approval for any external
visual AI. The categories and proof boundary here measure design-conversion
evidence; the trained-model quality decision, its held-out eval, ablation, and
DPO remain in `llm_mvp_core`.

## Gate B / Gate B

```
□ Every finding cites source ref + rendered ref + viewport + candidate hash.
□ taxonomy_findings cover all applicable categories (8) including RTL where relevant.
□ dominant_error_category set for any reject_candidate (evidence only, no DPO row in flows).
□ Any external-provider evidence carries an explicit Luba-approval reference.
□ verdict + metric_breakdown stored under flows/FLOW-NN/docs or artifacts/.
```

## Integration

- `html-css-render-qa-automation-GUIDE.md`: supplies the rendered screenshots and
  asset report this guide classifies.
- `ui-examination-round-SKILL.md`: taxonomy categories overlay the per-cell
  examination axes.
- `ui-quality-standard-SKILL.md`: BAN/hierarchy findings map into the layout /
  spacing / color taxonomy rows.
