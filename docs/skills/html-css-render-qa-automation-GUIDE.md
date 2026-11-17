# html-css-render-qa-automation-GUIDE.md — Server-side render-QA contract for Angular candidates (xiigen flows)
## v1.0: ported from llm_mvp_core html-css-render-qa-automation-GUIDE → Angular 21 / ng build + Playwright

## Summary

This guide defines the **server-side render-QA (Quality Assurance) contract** for
Angular candidates in xiigen flows. Render-QA verifies that a built
(`ng build`) per-flow HTML (HyperText Markup Language) / CSS (Cascading Style Sheets)
screen can be reproduced deterministically in a controlled renderer
(Playwright) across a fixed viewport matrix and compared with the design source.
This is an **evidence layer**, not a trainable final authority. The trainable
quality model (G12) remains in `llm_mvp_core` and is consumed through a manifest.

Source boundary: in flows, the design source arrives as a `.xiigen/wiki-domain`
manifest, not as a Figma/Canva payload. he-RTL (`dir="rtl"`) is a separate
state row in the matrix.

## When to use

Use this guide when a plan or work order mentions Angular rendering,
screenshot capture, visual diff, responsive QA, artifact contracts, server-side
rendering, browser automation, or screenshot-dependent evidence for a per-flow
client (`flows/FLOW-NN-<slug>/client/angular`).

## Inputs

- Candidate: a built Angular bundle (`ng build` output) with a declared entry
  route and asset references.
- Normalized design source: the `.xiigen/wiki-domain` manifest for the flow,
  with original design/screenshot refs.
- Viewport matrix: width, height, device pixel ratio, orientation, theme/state,
  and direction (`ltr`/`rtl`). At minimum desktop / tablet / mobile.
- Asset bundle: images, fonts, icons, masks, and fallback markers.
- Render environment: engine name/version (Playwright + browser), OS, font
  policy, network policy, timeout policy, and a deterministic seed if used.
- Expected artifact destination (`flows/FLOW-NN/docs` or `artifacts/`) and
  retention policy.

## Outputs/artifacts

Render-QA must emit a multi-resolution screenshot artifact contract:

- `render_run_id`: stable run identifier.
- `candidate_ref`: built Angular candidate hash and artifact path.
- `source_refs`: `.xiigen/wiki-domain` source tree and original screenshot hashes.
- `viewport_results`: one row per viewport/state (including each direction).
- `rendered_screenshot_ref`: PNG (Portable Network Graphics) reference for the
  rendered candidate.
- `dom_snapshot_ref`: DOM (Document Object Model) snapshot if captured.
- `computed_style_ref`: computed style artifact when typography/color/spacing
  evaluation needs it.
- `asset_resolution_report`: missing, substituted, loaded, blocked, or external
  asset markers.
- `render_timing_ms`: wall-clock render duration per viewport.
- `stability_result`: pass/fail after a repeat render / pixel-stability check.
- `qa_status`: `passed_for_eval`, `failed_render`, `failed_assets`, `unstable`,
  `needs_review`, or `quarantine`.

## Required gates

- **Multi-resolution gate**: the plan must define at least desktop, tablet, and
  mobile viewports; production gates may add theme/state/direction variants
  (he-RTL is mandatory when the flow supports Hebrew).
- **Server-side gate**: screenshots used for evidence/eval must come from a
  controlled, repeatable render path (Playwright against served `ng build`
  output), not an ad-hoc local developer browser capture.
- **Artifact completeness gate**: no visual diff and no evidence row without
  candidate hash, source hash, viewport metadata, rendered screenshot, and asset
  report.
- **Stability gate**: a repeated render of the same candidate/source/viewport
  must be stable within the configured tolerance before the screenshot becomes
  evidence (mitigates flaky fonts/animation; disable animations and pin fonts).
- **Timeout/cost gate**: render time is measured and can stop the visual loop
  when the budget policy says stop.
- **Isolation gate**: external network calls during render are blocked or
  recorded explicitly; missing assets are evidence, not silently ignored.
- **Internal authority gate**: render-QA can block promotion or create
  correction evidence, but it **cannot prove trained model quality by itself**.

## Forbidden substitutes

- A developer's manual browser screenshot is not a server-side QA artifact.
- A single desktop screenshot is not responsive evidence.
- A successful render is not proof that visual quality is good.
- A visual AI (Artificial Intelligence) provider verdict is not a replacement for
  the local render artifact contract and requires explicit Luba approval.
- Ignoring missing fonts/assets is forbidden; missing assets must be reported.
- Render retry loops cannot hide unstable Angular output.

## Setup note (flows)

`@playwright/test` is already in each per-flow `client/angular/package.json`, but
`playwright.config.ts` does **not** exist yet — creating a deterministic config
(fixed viewport matrix, pinned fonts, disabled animations, served `ng build`
output, screenshot output dir) is the first work item when this contract is
applied to a flow. Drive role/state via the `?role=…&state=…` query params so
each row is deterministic.

## How it feeds the plan

A render-QA work order runs **before** visual metrics and before any
`visual-diff-taxonomy-and-metrics-GUIDE.md` verdict. The plan must define the
viewport matrix, screenshot artifact schema, asset contract, render-timing
metric, stability tolerance, quarantine statuses, and the budget link. This
guide keeps render automation as **evidence generation**, not as hidden
deterministic model authority. The trainable design-conversion model and its
held-out/ablation eval remain in `llm_mvp_core` (G12 / R5); flows produce only
the rendered evidence consumed by examination and visual-diff.

## Gate B / Gate B

```
□ ng build (per-flow client/angular) succeeds — there is a candidate to render.
□ Render run produces a screenshot per declared viewport AND per direction (ltr/rtl).
□ asset_resolution_report present; any missing asset is recorded, not ignored.
□ stability_result = pass (repeat render within tolerance).
□ qa_status recorded; artifact stored under flows/FLOW-NN/docs or artifacts/.
```

## Integration

- `ui-examination-round-SKILL.md`: consumes these PNGs as the per-cell capture.
- `visual-diff-taxonomy-and-metrics-GUIDE.md`: classifies source↔rendered
  differences from these artifacts.
- `ui-testing-SKILL.md`: Path B uses the same served build / render path.
