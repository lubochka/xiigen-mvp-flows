# ui-testing-SKILL.md — UI testing protocol: split responsibility, Dual-Path UI proof, UX-automation (xiigen flows)
## v1.0: ported from llm_mvp_core ui-testing → Angular 21 (Vitest + Playwright) / .NET 10 xUnit

## When to load
Load before any change to an Angular 21 standalone component, route, or template
in a per-flow `flows/FLOW-NN-<slug>/client/angular`.
Load when a change to a server `.Application`/`.Infrastructure` service could
affect what a client view displays.
Load when adding a new client view, or when reviewing any UI evidence claim.

---

## The problem this skill solves

The flows clients are Angular 21 standalone apps. The split-responsibility rule
keeps logic out of the view and lets the test suite cover all logic without a
fragile UI harness:

- All business logic lives in the per-flow server (`.Application` /
  `.Infrastructure`, returning `DataProcessResult<T>`) and is tested by xUnit.
  No business logic in the client.
- The client view only calls the API, formats the result, and renders state.
- Component-level behavior is tested with **Vitest** (`describe`/`it` +
  `TestBed`; the greenfield clients use Vitest, not Karma/Jasmine).
- The real interactive UI path is tested with **Playwright** (config to be
  created — `@playwright/test` is in `package.json` but `playwright.config.ts`
  does NOT exist yet).

Never rename weaker evidence as stronger evidence:
- "Component automation" = the component was rendered and queried with TestBed.
- "Full UI automation" = a real browser drove the running app and observed it.
- "Host runtime evidence" = a host (e.g. a packaged desktop wrapper) invoked its
  own entrypoint.
These are separate proof scopes.

"UX automation" is a fourth proof scope: it proves the UI is understandable and
operable for a human, not only that controls can be clicked.

---

## What belongs in the client view

Allowed in a standalone component:
- Template (`*.html`), styles (`*.css`), layout, and binding.
- A method that calls the flow API client and puts the result into a signal /
  bound field.
- Error display: render a human-readable message from `error?.code` /
  `error?.message`.
- Loading / empty / populated / error state rendering.

Not allowed in the component:
- Business logic (any algorithm; any `DataProcessResult<T>` construction).
- Any class that is not a component, directive, pipe, or thin client service.
- Any method longer than ~30 lines (extract to a service or to the server).

---

## Server-side evidence standard (.NET 10 / xUnit)

The logic the client renders is proven on the server:

```
□ The .Application/.Infrastructure service is covered by xUnit [Fact]/[Theory].
□ Assertions check the specific output (result.Data?.X, result.Error?.Code),
  not only result.IsSuccess.
□ Gate B: dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/... → 0 failed.
□ The report says "server logic proven", not "UI proven".
```

Rejected overclaim:
- "The service test passed, therefore the Angular UI is fully automated."
- "The client called the API, therefore the user journey is end-to-end proven."

---

## Dual-Path Same-Scenario UI Proof

When a plan or work order asks for visual/UI evidence for a flow screen, visual
evidence means the **same scenario twice**:

- **Path A**: automated boundary execution — the flow API/adapter/core request
  → response → trace, asserting `DataProcessResult<T>` shape, `Data`, and
  `Error?.Code` (xUnit and/or a thin API integration test).
- **Path B**: the real client UI path — Playwright drives the running Angular app
  for the same user task and observes the rendered result.

Both paths must use the same user prompt/task, the same expected behavior, and a
result comparison. A marker/correlation id may connect traces, but it is not
proof by itself.

Rejected weak visual evidence:
- A JSON/log/marker screenshot is not UI proof.
- Opening a JSON/log/result file in the app is not UI proof.
- Showing only a marker is not UI proof.
- Screenshotting a debug artifact is not UI proof.
- Component-only (TestBed) proof must not be reported as full UI proof.

Required UI-path (Path B) evidence:
- a screenshot/video of the actual rendered app interaction;
- a human-readable description of what the user did and what appeared;
- correlation between the UI result and the Path A result;
- mismatch handling that marks the scenario not proven and creates the next
  smallest action instead of a readiness claim.

Status/reporting rule: every phase/step update says which scenario is repeated
through UI and how many paths are complete: zero of two, automated only, UI
only, or both joined. Use `Dual-Path Same-Scenario Coverage`. If Path B cannot
complete, record the exact local blocker, the next scenario/product fix, and the
next bounded action — do not convert the blocker into a stop.

For flows, Path B runs in a normal browser against the served app
(`ng serve` / built `ng build` output). There is no signed-in third-party host
window; the host-window/auth-preflight machinery from the core skill does not
apply here. If a future flow adds a hosted/desktop wrapper, add a host
auth-preflight step then (`SIGNED_IN_READY` / `LOGIN_RECOVERY_REQUIRED` /
`HUMAN_LOGIN_REQUIRED`) and never capture secrets — `no secrets captured`.

---

## No-Blind-Spot UI Scenario Script

Before any UI scenario is executed, the work order must contain a No-Blind-Spot
UI Scenario Script. "Run the screen scenario" is not a valid scenario, and agent
judgment is not a scenario step.

Each UI scenario script must include:
- scenario id and purpose in human language;
- required skills/guides to load (including qa/testing skills);
- exact app launch and preflight steps (`ng serve` / serve built output, base URL);
- exact route + query params (`?role=…&state=…`);
- exact clicks, typing, and expected visible UI text/state;
- exact input prompt/task and exact expected UI result;
- exact screenshot/video moments and exact evidence filenames;
- exact comparison against automated Path A;
- exact pass/fail/blocked statuses and the next smallest action per blocked branch;
- forbidden substitutions: JSON/log/marker screenshot, component-only proof,
  debug-file display.

If exact selectors/labels/commands are unknown, discovery is written as an
explicit scenario step with expected evidence — never left as free-form judgment.

Scenario prep NEEDS_FIX blocks the executor work order: a UI executor may not
receive a run command while the same scenario prep review is `NEEDS_FIX`. Plan
not closed/readable/exact is a repair loop, not execution and not a stop.

UI scenario work orders must not use huge inline shell scripts. If a
PowerShell/Node command would exceed 20 nonblank lines, 4000 characters, or more
than one function/class, prep must create a scoped `script-file runner` in the
evidence/run folder and execute it by path (capture stdout/stderr + exit code).
Windows error 206 / oversized inline command is a tooling failure, not a product
blocker; the next step is preflight-only script-file prep, not another huge
inline retry.

---

## UX automation standard

When the task asks for UI/UX, dashboard, or human-testable UI quality, final
completion requires automated UX evidence in addition to click-path evidence.

Required UX assertions (Playwright against the real Angular UI):

```
□ Primary controls have human-readable labels, not only ids or component selectors.
□ Interactive controls expose accessible names (aria-label / aria-labelledby /
  associated <label for>) and, where meaning is non-obvious, help text / tooltip.
□ Examples explain what the user should type and what output to expect.
□ Empty / error states show a human-readable message in the UI (not a raw code).
□ Raw JSON / debug controls explain safety and evidence boundaries.
□ History/copy/export controls explain what is copied/exported and what is not.
□ Layout has stable bounds for primary panels (no overflow/clipping at the
  declared viewport).
□ Primary path does not rely on internal jargon such as raw status keys.
□ he-RTL: with dir="rtl", labels/help still read correctly and layout mirrors.
```

Accepted UX automation evidence:
- a Playwright test that drives the real UI and asserts accessible names/help
  text, visible copy, examples, error wording, safety wording, and layout/bounds;
- a QA script that fails if the UX test or UX metadata disappears.

Rejected UX substitution:
- a click-path test that never checks what a user reads;
- a metadata test with no running UI assertion;
- screenshots/docs with no repeatable automated check;
- a QA script that only checks file presence.

---

## What replaces @testing-library queries (Angular 21 / Vitest)

| @testing-library (React, core history) | xiigen flows equivalent (Angular 21) |
|---------------------------------------|--------------------------------------|
| `render(<Component/>)` | `TestBed.createComponent(Component)` + `fixture.detectChanges()` |
| `getByRole('button')` | query the rendered DOM for the role/selector and assert enabled/visible |
| `getByText('Done')` | assert `nativeElement.textContent` contains the expected copy |
| `getByLabel('Email')` | assert the input has an accessible name (aria/`<label for>`) |
| `fireEvent.click(btn)` | dispatch a click in the Vitest component test (or `page.click` in Playwright) |
| `waitFor(...)` | `await fixture.whenStable()` (Vitest) / `await expect(locator)...` (Playwright) |

---

## Vitest component test pattern (flows)

```ts
// Component renders the right state for a given role/state (no business logic here)
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('RSVP view', () => {
  beforeEach(async () => {
    window.history.pushState(null, '', '?role=tenant_user&state=rsvp'); // role/state via query params
    document.documentElement.lang = 'en';                                // language cell
    document.documentElement.dir = 'ltr';                                // direction cell
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders a human-readable RSVP prompt for members', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('RSVP');     // visible copy, not an internal key
  });
});
```

## Playwright UI path pattern (flows)

```ts
// Path B — real UI drives the same scenario as Path A and observes the result
import { test, expect } from '@playwright/test';

test('member can RSVP and sees a confirmation', async ({ page }) => {
  await page.goto('/?role=tenant_user&state=rsvp');   // exact route + query params
  await page.getByRole('button', { name: /rsvp/i }).click();
  await expect(page.getByText(/you.?re going/i)).toBeVisible(); // human-readable result
});
```

---

## Manual / CI verification checklist

```
Before declaring a UI change done:
☐ dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/...  → 0 failed
☐ ng build (per-flow client/angular)                                    → 0 errors
☐ ng test (Vitest component specs)                                      → 0 failed
☐ Playwright UI path (Path B) for each named scenario                   → pass or recorded blocker
☐ Dual-Path Same-Scenario Coverage stated: total / both / api_only / ui_only / blocked
☐ UX assertions present for user-facing views (accessible names, copy, error wording)
```

---

## Anti-patterns

1. **Business logic in the component** — any algorithm or `DataProcessResult<T>`
   construction belongs on the server; the view only formats and renders.

2. **Reporting component-only proof as full UI proof** — TestBed rendering is not
   a real browser driving the running app.

3. **Marker/JSON screenshot as UI evidence** — a debug artifact is never Path B
   proof.

4. **Skipping Path A or Path B** — both paths of the same scenario are required;
   one alone is `api_only` or `ui_only`, not coverage.

5. **Click-path without UX assertions** — clicking proves operability, not
   readability. UX automation checks what the user reads.

---

## Integration

- `ui-quality-standard-SKILL.md`: visual quality (bans, hierarchy, AI Slop Test).
- `ui-screen-grammar-SKILL.md`: the grammar the view implements.
- `ui-examination-round-SKILL.md`: cell-by-cell design-quality rounds.
- `html-css-render-qa-automation-GUIDE.md`: deterministic server-side render/PNG
  capture used by Path B / examination.
- `e2e-test-matrix-SKILL.md` / `user-journey-testing-SKILL.md`: scenario
  categories feeding the No-Blind-Spot script and Gate B.
- `verification-before-completion-SKILL.md`: a UI phase requires Dual-Path
  coverage + UX evidence before any completion claim.

---

## Stack adaptation notes (xiigen flows)

- Unit/component: **Vitest** (`describe`/`it` + `TestBed`); UI path:
  **Playwright** (`playwright.config.ts` to be created per-flow).
- Server Gate B: `dotnet test` (xUnit) in `flows/FLOW-NN/.../Tests`; client Gate
  B: `ng build` + `ng test` + Playwright.
- Role/state matrix is driven by `?role=…&state=…` query params; he-RTL is a
  first-class direction cell.
- This is the **evidence/testing** layer; no model lives here. The common UI/UX
  quality model (G12) stays in `llm_mvp_core` and is consumed via manifest.
