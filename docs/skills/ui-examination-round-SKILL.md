# ui-examination-round-SKILL.md — Cell-by-cell UI examination with systemic-first fixes (xiigen flows)
## v1.0: ported from llm_mvp_core ui-examination-round → Angular 21 / Playwright-PNG cell matrix

## When to load
Load before any UI change that affects more than one component or screen feature
in a per-flow `flows/FLOW-NN-<slug>/client/angular`.
Load when `ui-quality-standard-SKILL.md` checker passes but the UI "still doesn't
feel right".
Load when a UI change has been made and the result must be scored and verified,
or when starting a new examination session after a significant screen change.

---

## The problem this skill solves

A UI review that examines "the screen" as a single unit misses most of the
quality surface. A screen has multiple features, multiple user roles, multiple
languages (the flows clients support `en` / `he`-RTL and may add `fr`), and
multiple states — and a fix that improves one cell can silently regress another.
Systemic problems affect many cells and must be fixed before per-cell fixes,
because per-cell patches applied before systemic fixes get overwritten.

This skill defines the cell model, the five-phase examination structure, the
convergence conditions, and the rules that govern improvement claims. It is the
**design-quality round** for the Angular client; functional correctness is
covered by `ui-testing-SKILL.md` and the per-flow xUnit suite.

---

## Section 1 — The Cell Model (enriched for flows)

A cell is NOT "a screen". For flows the cell carries language and role, because
the Angular clients are multi-language (he-RTL) and role-aware (the standalone
clients select role/state via query params, e.g. `?role=tenant_user&state=rsvp`).

```
Cell = Flow × Screen × Language × UserRole × State

  Flow:     FLOW-NN-<slug>
  Screen:   the route / standalone component
  Language: en | he-RTL | fr (per flow support; dir=rtl is its own cell)
  UserRole: the ?role= the screen serves (e.g. tenant_user, tenant_admin, guest)
  State:    Idle | Loading | Empty | Populated | Error | <business state>
```

Example cells for an event-attendance screen (FLOW-04):

```
Cell 1: FLOW-04 × RsvpView × en      × tenant_user × Idle
Cell 2: FLOW-04 × RsvpView × en      × tenant_user × Submitting
Cell 3: FLOW-04 × RsvpView × he-RTL  × tenant_user × Idle
Cell 4: FLOW-04 × RsvpView × en      × guest       × Empty
Cell 5: FLOW-04 × AttendeeList × en  × tenant_admin × Populated
Cell 6: FLOW-04 × AttendeeList × en  × tenant_admin × Error
```

A screen with 2 features × 2 languages × 1 role × 3 states = 12 cells minimum.
All primary cells must be examined before a screen-level improvement claim.

PNG capture: each cell is captured with Playwright (the flows clients ship
`@playwright/test` in `package.json`, but `playwright.config.ts` does NOT exist
yet — creating it is part of `ui-testing-SKILL.md` / `html-css-render-qa`
setup). Drive the standalone client with the role/state query params so each
cell is a deterministic screenshot. Persist a coverage-matrix JSON next to the
flow (`flows/FLOW-NN/docs` or `docs/screen-examination`).

---

## Section 2 — Five-Phase Examination Structure

### Phase 1 — Baseline

```
Purpose: Establish a score for every primary cell before touching anything.
Output:  A baseline table: Cell ID / Score / Issues found.
Rule:    NO improvement claims before a baseline exists. If you have not scored
         the before-state, you cannot claim improvement.
```

Baseline table format:

```
| Cell ID | Flow | Screen | Lang | Role | State | Baseline Score | Issues |
|---------|------|--------|------|------|-------|----------------|--------|
```

Score each cell using `ui-quality-standard-SKILL.md` Phase 1-4 checker. Record
the score as a decimal (0.00–1.00) and list every issue. For he-RTL cells, also
record direction-specific issues (mirrored padding, icon direction, alignment).

### Phase 2 — Systemic Fixes

```
Purpose: Identify and fix problems affecting multiple cells.
Rule:    Systemic fixes BEFORE per-cell fixes. A per-cell fix applied before a
         systemic fix is overwritten when the systemic fix changes the shared
         component / shared token.
```

Systemic issues (affecting ≥3 cells) in an Angular client:

```
- Spacing scale inconsistency (a shared --space-* token or a shared component’s padding)
- Accent token not applied (affects all status chips)
- Font hierarchy wrong (shared heading style / shared component)
- BAN violation (BAN 1-5 from ui-quality-standard-SKILL.md) in a shared component
- RTL not handled in a shared layout (logical properties missing → every he-RTL cell breaks)
```

Apply all systemic fixes first. Re-capture and re-read all affected cells after
systemic fixes to confirm no new issue was introduced.

### Phase 3 — Per-Cell Fixes

```
Purpose: Fix issues specific to one cell, not shared with others.
Input:   Baseline table with systemic issues already resolved.
Rule:    Only address issues not already resolved by Phase 2.
```

Per-cell fix record format:

```
Cell ID: [cell identifier]
Issue:   [specific issue in this cell]
Fix:     [what changed — file:line in client/angular/src/app/...]
Risk:    [which other cells could be affected — or NONE]
```

### Phase 4 — Rescore

```
Purpose: Measure the delta from baseline for every cell touched in Phase 2-3.
Output:  Updated table with before / after / delta per cell.
```

Rescore table format:

```
| Cell ID | Baseline | After Fix | Delta | Verdict |
|---------|----------|-----------|-------|---------|
```

Verdict: IMPROVED (delta > 0.05) / STABLE (delta ≤ 0.05) / REGRESSED (delta < 0).

### Phase 5 — Convergence Check

Three convergence conditions — ALL must be true to close the round:

```
Condition 1: Score delta < 1% across all cells since the last fix pass
             (fixing has plateaued).
Condition 2: All primary cells examined (Phase 1 baseline exists for each),
             including each supported language and each served role.
Condition 3: Special-case cells marked NEEDS_PURPOSE_BUILT_UI are verified as
             acknowledged (not merely noted as unexamined).
```

If any condition is false: continue examination. State which condition failed
and the step needed to satisfy it.

---

## Section 3 — Improvement Claim Rules

An improvement claim states "the UI is better". Claims have scopes and evidence:

```
Screen-level claim: "This screen is improved"
  Requires: all primary cells rescored with positive/stable delta;
            no REGRESSED verdict anywhere on the screen (all languages/roles).

Feature-group-level claim: "The RSVP feature is improved"
  Requires: all cells in that feature rescored; no REGRESSED in the group.

Flow-level claim: "All screens in FLOW-NN are improved"
  Requires: screen-level claims for every screen; systemic fix verified not to
            regress any screen.

Convergence claim: "This examination round is complete"
  Requires: all three convergence conditions satisfied (Section 2 Phase 5).
```

**A claim at any scope without the required evidence is invalid.** "Looks
better" without a rescore table is not an improvement claim.

Anti-grep-trap: a high automated grep/lint "score" is not a cell score. The cell
score must come from reading the captured PNG (or the rendered screen), not only
from counting BAN-marker absences. A clean grep with an unread screenshot is not
an examination.

---

## Section 4 — NEEDS_PURPOSE_BUILT_UI Cells

Some cells cannot be improved by systematic fixes — they need a purpose-built
design out of scope for the current change:

```
Mark as: NEEDS_PURPOSE_BUILT_UI
Provide: one sentence explaining why the standard approach does not apply
Action:  acknowledge as a known limitation; do not block convergence on it
```

Example:

```
Cell 6: FLOW-04 × AttendeeList × en × tenant_admin × Error
NEEDS_PURPOSE_BUILT_UI: A rich diagnostic error surface (per-attendee failure
                         drill-down) is a separate component out of scope for
                         this examination round.
```

A cell marked NEEDS_PURPOSE_BUILT_UI satisfies Convergence Condition 3. It does
NOT satisfy Condition 2 if primary cells remain unexamined.

---

## Anti-patterns

1. **"I reviewed the screen and it looks better"** — a screen-level claim
   requires a rescore table with deltas.

2. **"I fixed the per-cell issue before the systemic fix"** — it will be
   overwritten when the systemic fix changes the shared component. Systemic
   first.

3. **"I only scored the cells I changed"** — baseline must cover all primary
   cells (all languages, all served roles). Unchanged cells can be regressed by
   a systemic fix.

4. **"NEEDS_PURPOSE_BUILT_UI means this cell is done"** — it means acknowledged,
   not fixed. Note it as an open limitation in the convergence claim.

5. **"Delta 0.03 counts as improvement"** — delta ≤ 0.05 is STABLE, not
   IMPROVED. The cell is not better; that's scoring noise.

6. **"The lint score went up, so the UI improved"** — automation score trap.
   Cell scores come from reading the screen/PNG, not only grep counts.

---

## Integration

- `ui-quality-standard-SKILL.md`: Phase 1-4 checker provides the per-cell scoring.
- `ui-screen-grammar-SKILL.md`: the GRAMMAR field per cell comes from the
  grammar declaration.
- `html-css-render-qa-automation-GUIDE.md`: supplies the deterministic
  server-side PNG capture contract the cell matrix consumes.
- `ui-testing-SKILL.md`: functional + UX-automation; this skill is design-quality.
- `improvement-measurement-SKILL.md`: a UI improvement claim needs the
  before/after cell deltas from this round as its observable delta.

---

## Stack adaptation notes (xiigen flows)

- PNG capture via Playwright driving the standalone client with `?role=…&state=…`
  query params; create `playwright.config.ts` per-flow (not present yet).
- Coverage-matrix JSON persisted in `flows/FLOW-NN/docs` or `docs/screen-examination`.
- he-RTL is a first-class language cell (`dir="rtl"`), not an afterthought.
- This is the **evidence/quality** layer; no model trained here. The common
  examination/scoring model (G12) stays in `llm_mvp_core` and is consumed via
  manifest. Server logic stays `DataProcessResult<T>` + xUnit, unaffected by
  this skill.
