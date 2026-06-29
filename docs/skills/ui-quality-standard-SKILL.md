# ui-quality-standard-SKILL.md — Absolute AI-slop bans + design hierarchy + AI Slop Test (xiigen flows)
## v1.0: ported from llm_mvp_core ui-quality-standard → Angular 21 standalone / plain CSS / Vitest + Playwright

## When to load
Load before writing or reviewing any Angular 21 standalone component, template
(`*.html`), or stylesheet (`*.css` / `src/styles.css`) in a per-flow
`flows/FLOW-NN-<slug>/client/angular`.
Load when `ui-testing-SKILL.md` passes but the UI still "feels wrong".
Load before any UI change enters a per-flow phase-end Gate B / commit.

---

## The problem this skill solves

An Angular client generated without design-quality criteria looks like every
other auto-generated interface: identical card grids, generic layouts, no
intentional hierarchy. The same pattern of obvious choices appears regardless of
context. This skill defines the absolute bans that prevent AI-slop UI patterns,
the design principles that make an interface memorable rather than generic, and
the AI Slop Test that validates the result before claiming the UI is done.

The core skill is written in Windows Forms idioms (`SystemColors`, `BorderStyle`,
`Panel`). For flows the same bans are expressed in Angular 21 / plain-CSS idioms:
CSS custom properties in `src/styles.css` (the flows clients use plain CSS, not
SCSS — confirmed via `angular.json` → `"styles": ["src/styles.css"]`), utility
classes in component `*.css`, and hand-rolled dialogs (`role="dialog"` /
`aria-modal="true"`) as the BAN-5 marker (the greenfield clients have no
shadcn/Radix/Material overlay by default — dialogs are author-written).

This is the **design-quality** gate. `ui-testing-SKILL.md` is the **functional
correctness** gate. Both are required before a UI phase is done.

---

## Section 1 — Absolute Bans (Angular 21 / plain CSS)

These patterns are never acceptable. They are the most recognizable
AI-generated design tells. If any appear in a component template or stylesheet:
stop and redesign the element with a different approach.

```
BAN 1: Side-stripe borders on cards, list items, or status chips
  Pattern: border-left / border-right with a colored 3px+ stripe
           (e.g. `border-left: 4px solid var(--accent)`).
  CSS markers to grep: border-left, border-right, border-inline-start
  Why banned: The single most overused "design touch" in tool/admin UIs.
              Never looks intentional regardless of color.
  Rewrite: Use full borders, background tints, or no visual indicator at all.

BAN 2: Gradient text
  Pattern: `background: linear-gradient(...); -webkit-background-clip: text;
           color: transparent;` on headings for "impact".
  CSS markers to grep: background-clip: text, -webkit-text-fill-color: transparent
  Why banned: Decorative rather than meaningful; top AI design tell.
  Rewrite: Use a single solid foreground color. Use font-weight or size for emphasis.

BAN 3: Identical control grids
  Pattern: N cards of identical size, each with icon + label + description,
           repeated with no variation in size or visual weight
           (`grid-template-columns: repeat(N, 1fr)` with uniform cards).
  Why banned: Communicates no hierarchy. Every item looks equally important.
  Rewrite: Vary sizes based on importance. Use primary/secondary groupings
           (e.g. a featured card spanning two columns).

BAN 4: Generic drop shadows on every card
  Pattern: Every card has the same `box-shadow` offset/opacity regardless of
           depth intent (often a single shared `.card { box-shadow: ... }`).
  Why banned: Safe, forgettable, could be any generated output.
  Rewrite: Use shadow only where an actual depth relationship is intended.

BAN 5: Modal dialogs for low-stakes interactions
  Pattern: A hand-rolled `<div role="dialog" aria-modal="true">` or a
           `window.confirm()` for confirmations that don't require
           interrupt-level attention.
  Markers to grep: role="dialog", aria-modal, window.confirm, window.alert
  Why banned: Lazy interaction design; breaks flow for non-critical decisions.
  Rewrite: Use inline status indicators or confirmation within the existing view.
```

Suggested Gate-B grep (run against the per-flow client `src/app`):

```bash
# BAN markers — any hit must be justified or removed before the UI Gate B
grep -rnE "border-left|border-right|border-inline-start|background-clip:[[:space:]]*text|-webkit-text-fill-color:[[:space:]]*transparent" flows/FLOW-NN-<slug>/client/angular/src/app
grep -rnE "role=\"dialog\"|aria-modal|window\.(confirm|alert)" flows/FLOW-NN-<slug>/client/angular/src/app
```

---

## Section 2 — Design Hierarchy Principles

### Typography

```
- Use at most 2 font families per view (declare them as CSS variables, e.g.
  --font-body and --font-heading, in src/styles.css).
- Body text: a system sans stack (e.g. system-ui, "Segoe UI", sans-serif).
- Heading text: vary weight (600/700) and size, not font family.
- Do NOT set all text to the same size. Hierarchy requires at least a 1.3×
  ratio between heading and body sizes (a type scale in CSS variables).
- Do NOT use ALL-CAPS (`text-transform: uppercase`) for long labels.
  Reserve for 2-4 word status chips.
```

### Color

```
- Use one primary accent color, declared once as --accent in src/styles.css.
- The 60-30-10 rule: 60% neutral surface, 30% secondary text/borders, 10% accent.
- Functional colors must follow conventions: red = error, green = success,
  amber = warning. Do not use these colors for decorative purposes.
- Never use pure #000 or #fff for text/background. Use near-neutrals
  (e.g. --surface, --on-surface tokens).
- he-RTL note: color usage is direction-independent, but verify accent placement
  still reads correctly when `dir="rtl"` mirrors the layout.
```

### Layout and Spacing

```
- Use a 4px spacing unit. Allowed values: 4, 8, 12, 16, 24, 32, 48, 64
  (expose as --space-1..--space-N CSS variables; never raw magic px).
- Do NOT apply identical padding to every card. Vary for hierarchy.
- Establish one dominant visual anchor per view (the primary action or primary
  result display). Everything else is secondary.
- Left-aligned (or for RTL, start-aligned) text with asymmetric composition
  feels more designed than everything centered.
- Constrain long text with `max-width` (~70-80ch) rather than letting it run
  edge to edge.
```

---

## Section 3 — AI Slop Test

Before claiming any UI change is complete, apply this test:

```
If you showed this interface to someone and said "AI made this," would they
believe you immediately?

If yes: that is the problem. A distinctive interface should make someone ask
"how was this made?" — not "which AI template was used?"
```

Review this list. If any apply: redesign before committing:

```
□ Every card has the same padding
□ Every heading is the same font size
□ Controls are arranged in an identical grid pattern (repeat(N, 1fr), uniform)
□ The primary action is not visually distinct from secondary actions
□ All colors are framework defaults with no project-specific accent token
□ There is no visual anchor — no single element clearly dominates
□ Every status chip uses the same visual style regardless of severity
```

---

## Section 4 — Checker Protocol

When reviewing an existing Angular standalone component:

```
Phase 0: Context
  □ What is this view for? Who uses it (which ?role=)? What is the primary action?
  □ What aesthetic direction was intended (clinical/tool/explorer/dashboard)?

Phase 1: Absolute Bans
  □ No BAN 1 side-stripe borders
  □ No BAN 2 gradient text
  □ No BAN 3 identical control grids
  □ No BAN 4 generic drop shadows on every card
  □ No BAN 5 modal/confirm dialogs for low-stakes interactions

Phase 2: Hierarchy
  □ One dominant visual element (the anchor)
  □ At least 2 size levels for text (heading vs body)
  □ Varied spacing communicates grouping and importance

Phase 3: Color
  □ Accent color used selectively (10% rule), driven by one --accent token
  □ Functional colors (red/green/amber) reserved for semantic use only
  □ Neutral surfaces use tokens, not pure black/white

Phase 4: AI Slop Test
  □ Run the test above — would an observer immediately identify this as AI output?
```

A reviewable score: record each cell as a decimal 0.00–1.00 so
`ui-examination-round-SKILL.md` can use it as the per-cell baseline.

---

## Anti-patterns

1. **"It passes ui-testing-SKILL.md, so it's done"** — passing tests verify
   correctness. This skill verifies quality. Both are required.

2. **"Framework defaults are fine"** — default tokens provide a correct baseline,
   not a design direction. Apply the 10% accent rule on top of the baseline.

3. **"The client is tool-facing, not user-facing — design doesn't matter"** —
   tool-facing views with poor hierarchy create cognitive overhead. The AI Slop
   Test applies to every view.

4. **"I used different colors, so it's distinct"** — color variation without
   hierarchy and a visual anchor produces noise, not design. Anchor first, then
   vary color.

---

## Integration

- `ui-screen-grammar-SKILL.md`: declare the grammar before bans are applied; bans
  are not grammar-specific.
- `ui-testing-SKILL.md`: functional correctness + UX-automation; this skill is
  visual quality.
- `ui-examination-round-SKILL.md`: the Phase 1-4 score here is the per-cell
  baseline for the examination round.
- `verification-before-completion-SKILL.md`: fires before any completion claim —
  apply this skill as part of that verification for UI phases.

---

## Stack adaptation notes (xiigen flows)

- Tokens/themes live in `flows/FLOW-NN-<slug>/client/angular/src/styles.css`
  (`:root` CSS custom properties); component-local styles in each component `*.css`.
- BAN markers are CSS/template strings (`border-left`, `box-shadow`,
  `background-clip: text`, `role="dialog"`), grep-able in `src/app`.
- Gate B for the visual layer: `ng build` succeeds (no template/style errors)
  **and** the BAN grep returns no unjustified hit. Server logic remains
  `DataProcessResult<T>` and is covered by xUnit, not by this skill.
- This is the **evidence/quality** layer only; no trained model lives here. The
  common design-quality model (G12) stays in `llm_mvp_core` and is consumed via
  manifest — this skill never trains or stores a model.
