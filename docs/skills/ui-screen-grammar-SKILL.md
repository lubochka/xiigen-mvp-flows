# ui-screen-grammar-SKILL.md — Declare 1 of 7 screen grammars before any control (xiigen flows)
## v1.0: ported from llm_mvp_core ui-screen-grammar → Angular 21 standalone / route-per-grammar

## When to load
Load before writing any new Angular 21 standalone component, route view, or
multi-control layout in a per-flow `flows/FLOW-NN-<slug>/client/angular/src/app`.
Load when a view passes `ui-quality-standard-SKILL.md` but the interaction model
is unclear ("what is the user here to DO?").
Load as the closing step of intent capture: WHAT the user must accomplish
determines the grammar type.

---

## The problem this skill solves

An Angular route designed without declaring its grammar type defaults to
whatever pattern is easiest to implement: a data table, a tab panel, a flat
list. The result looks "like a screen" without being designed for the specific
interaction the user must perform. Grammar types are the seven fundamental
interaction patterns every screen belongs to — declaring one before writing any
control forces the design to commit to an interaction model, not just a layout.

Adaptation for flows: one grammar per route / standalone component. Reusable
grammars (G6 dashboard, G3 card-list) become reusable standalone components in
`src/app`; the route declares which grammar it is in the component header
comment and (where a flow keeps design notes) in the flow `docs`.

---

## The Seven Grammar Types (Angular 21 implementation)

### G1 — PROGRESS_STRIP
**User's question:** "Where is this in its lifecycle?"

Visualizes a sequence of phases/stages for a single subject. The primary visual
element is an ordered sequence with status per step — not a list of items.

```
Angular implementation:
  Primary: a horizontal/vertical strip component, one step element per phase
           (@for over phases) with name (strong) + status chip + elapsed +
           optional action button when the step is current.
  Progress indicator: a custom progress element showing N of M complete.
  Status colors (tokens): green=COMPLETE, blue=IN_PROGRESS, gray=NOT_STARTED, red=FAILED.

Applies to (flows examples):
  - Saga / durable-workflow step viewer (FLOW-19 durable-sagas-compliance)
  - Schema-registry DAG promotion lifecycle (FLOW-11)
  - Bundle activation lifecycle (FLOW-00)
  - ETL pipeline stage tracking (FLOW-14)
```

### G2 — VERDICT_GRID
**User's question:** "What did each evaluator decide, and why?"

A two-dimensional grid: items (rows) × evaluators (columns). Each cell is a
verdict. The dominant element is the verdict grid itself, not a summary.

```
Angular implementation:
  Primary: a table component with custom cell rendering per verdict type.
  Columns: one per evaluator/rule. Rows: one per candidate/cycle.
  Cell: APPROVED / BLOCK / CONCERN chip + brief reasoning (title/tooltip).
  Row summary: consensus verdict in the first or last column.

Applies to (flows examples):
  - Review/reputation verdict display (FLOW-10 reviews-reputation)
  - Compliance gate results across rules (FLOW-19)
  - Validation outcome grid across schemas (FLOW-11)
```

### G3 — CARD_LIST
**User's question:** "Which items need my attention, in what state?"

A list of items where each item has a state and may require action. The user
scans to find items in specific states.

```
Angular implementation:
  Primary: @for list rendering custom item cards.
  Each item: identity (strong) + state chip + key metric + optional action button.
  Sort: by state priority (BLOCKED first) then by date.
  Filter: a select / checkbox group for state filtering.
  Empty state: a human-readable message + guidance CTA (never a bare empty div).

Applies to (flows examples):
  - Marketplace listings (FLOW-08), friend requests / social feed (FLOW-07)
  - Event list (FLOW-03 event-management)
  - Outbox / pending-task inventory
```

### G4 — TOPOLOGY_CANVAS
**User's question:** "How do the parts connect?"

A graph/pipeline: nodes connected by edges. The user inspects structure and
relationships, not a list.

```
Angular implementation:
  Primary: an SVG (or canvas) graph component — nodes as labeled rects,
           edges as directional arrows labeled with the data type.
  Side panel: selected-node detail (inputs/outputs/wiring status).
  Selection: click a node to highlight and populate the side panel.

Applies to (flows examples):
  - Visual flow engine canvas (FLOW-18 visual-flow-engine)
  - Schema DAG topology (FLOW-11)
  - Saga step/dependency graph (FLOW-19)
```

### G5 — KIOSK
**User's question:** "I have one task, one decision."

A focused single-action surface. One primary CTA dominates. Everything else is
context supporting that one decision.

```
Angular implementation:
  Primary: one large CTA button, prominently placed.
  Supporting context: facts that inform the decision.
  Secondary actions: smaller, visually subordinate buttons.
  No in-view navigation — this is the whole task. Cancel/Back always present,
  visually subordinate.

Applies to (flows examples):
  - RSVP / attendance confirmation (FLOW-04 event-attendance)
  - Checkout / pay confirmation (FLOW-16 marketplace-payments)
  - Subscription upgrade confirm (FLOW-12 subscription-billing)
```

### G6 — DASHBOARD
**User's question:** "What are my key metrics right now?"

Shows multiple metrics simultaneously. The user reads system state, not performs
an action. Action buttons are secondary to information display.

```
Angular implementation:
  Primary: a grid of metric cards (CSS grid, fixed layout).
  Each card: metric name (secondary) + value (large, strong) + trend indicator.
  Color coding: green=healthy, amber=watch, red=action needed.
  No primary CTA — the purpose is observation.

Applies to (flows examples):
  - Data-warehouse analytics overview (FLOW-13 data-warehouse-analytics)
  - Tenant health overview (FLOW-15 saas-multi-tenancy)
  - Completion / gamification summary (FLOW-05)
```

### G7 — SETTINGS_TABS
**User's question:** "Which setting do I need to change?"

Organizes configuration by domain, each domain in a tab. The user knows what to
change and navigates to the right tab.

```
Angular implementation:
  Primary: a tab component with descriptive tab names (never Tab1/Tab2).
  Each tab: related settings grouped in labeled sections.
  Inputs: text/select/number with validation.
  Save: explicit per-tab save OR an unsaved indicator. Inline error per field.

Applies to (flows examples):
  - Tenant configuration (FLOW-15)
  - Billing / plan settings (FLOW-12)
  - Profile enrichment settings (FLOW-02 profile-enrichment)
```

---

## Section 2 — Pre-Code Declaration

Before writing any control, declare (in the component header comment, and in the
flow `docs` design note when one exists):

```
Grammar type: G[N] — [TYPE_NAME]
User question answered: "[exact question from the grammar definition]"
Primary control structure: [Angular structure from the implementation guide above]
Why this grammar, not the alternatives: [one sentence — what makes the next-closest grammar wrong here]
```

A view that cannot answer "why NOT G2?" when it serves one evaluator verdict has
not committed to a grammar. Generic justifications ("G6 seems right") are not
declarations.

---

## Section 3 — Grammar Violation Signals

| Signal | Likely cause |
|--------|-------------|
| View has both a data table AND a progress strip as primary elements | G1/G2 conflict — pick one grammar |
| Primary CTA is hidden below a large table | G5 mixed with G3 — restructure |
| Dashboard cards all show the same type of metric | Not G6 — the cards are a G3 list of items |
| Topology canvas has a submit button as the primary action | G4 mixed with G5 — separate concerns |
| Settings tabs contain a progress indicator | G7 mixed with G1 — settings and monitoring are different grammars |

---

## Anti-patterns

1. **"The view has multiple sections so it doesn't fit one grammar"** — a view
   that genuinely cannot commit should be split into two routes/components. "The
   topology AND the settings" is two views sharing a container.

2. **"G6 Dashboard is the default for complex views"** — G6 means the user is
   observing metrics. If the user is here to act, G6 is wrong. Most operational
   views are G5 or G3.

3. **"Grammar type is cosmetic"** — it determines which control is primary, how
   information is organized, and what the user can DO. Declaring the wrong
   grammar then "making it work visually" produces views that confuse users
   about their task.

---

## Integration

- `ui-quality-standard-SKILL.md`: bans apply after grammar is declared.
- `ui-examination-round-SKILL.md`: the GRAMMAR field in the examination record is
  populated from the declaration made here before implementation.
- `interface-design-craft-SKILL.md` (if present): intent capture (WHO/WHAT/FEEL)
  — the WHAT answer determines the grammar type; declare it at the end of intent.

---

## Stack adaptation notes (xiigen flows)

- One grammar per Angular route / standalone component in `client/angular/src/app`.
- Reusable G3/G6 components live in `src/app` and are imported per route.
- Declaration recorded in the component header comment + flow `docs` note.
- This is a design-discipline skill — no model, no `DataProcessResult<T>` logic.
  Server logic stays in the per-flow `.Application`/`.Infrastructure` and is
  covered by xUnit.
