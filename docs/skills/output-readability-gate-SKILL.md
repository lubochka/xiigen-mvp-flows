# Output Readability Gate

> **Stack adaptation (xiigen flows).** The four prohibited prose patterns, the
> dashboard/API documentation readability checklist, the English-first
> Human-Readable Execution Board, and the No-Machine-Log-To-User rule ported from
> `llm_mvp_core/docs/skills/output-readability-gate-SKILL.md` (G07). The universal
> part (no shortcodes / ALL_CAPS_UNDERSCORE / letter-number tokens in prose
> outside backticks; dashboard/API docs must give a human name + example input +
> parameters + expected output + evidence boundary; the English board with Total/
> Done/In progress/Remaining/Result type/Next action; a machine status is
> never the whole user-facing report) is taken from core. Adapted here:
> - The "project equivalents" section uses this stack: `DataProcessResult<T>`
>   status / `ErrorCode` strings, xUnit, and Angular DI tokens — **not** core
>   `OperationResult<T>` and **not** the ML pipeline-signal example
>   (`OUTCOME` / `DPO_TRIPLE`), which is omitted (this repo trains no models — G12).
> - **Angular UX readability** is added: labels, accessible names/help text,
>   example values, error wording, and no internal jargon on the primary user path
>   in the `flows/FLOW-NN-<slug>/client/angular` surface.
> - The mojibake check stays relevant for multilingual board strings.
> - **The llm_mvp_core host-UI clusters are NOT ported.** Dual-Path Same-Scenario
>   UI Proof, Multi-Monitor Target Window Control, Cursor/Claude/Codex auth
>   preflight, and the 10-review-cycle Execution Plan Visibility Gate are
>   core-product-specific. This repo has no host-extension scenarios; the ported
>   gate is prose readability + dashboard readability + the execution board only.

## Core Problem

Generated output that contains shortcodes, status strings, or code-style
identifiers written as plain prose is unreadable to a human reviewer. The
same token that belongs in a code fence — `CF-12`, `CONTEXT_INSUFFICIENT`,
`Rule 1` — becomes noise when scattered through a paragraph. This skill
defines the four prohibited patterns, the exception for code fences, and
the gate check that must pass before any generated output is accepted.

---

## When to Invoke

- Before accepting any generated prose (plans, skill files, decision records)
- When reviewing context-package prose sections (not code sections)
- When writing flow documentation, dashboard/API docs, or user-facing examples
- When writing the Angular client's labels, help text, and error messages
- When a human reviewer reports that output is "hard to follow" or "reads like code"

---

## Section 1 — The Core Rule

```
No shortcodes, status strings, or code-style identifiers may appear
in prose outside of triple-backtick code fences.

Exception: inside ``` code fences, any identifier is allowed.
```

This rule applies to ALL prose — plan text, skill descriptions, decision
records, comments, and generated explanations. It does NOT apply to
code blocks, inline code spans with backticks, or table cells that display
code values.

---

## Section 2 — Four Prohibited Patterns

### Pattern 1 — LETTER(S)-DASH-NUMBER(S) identifiers in prose

```
Prohibited: CF-12, DNA-5, FC-7, BUG-ENGINE-001, D-01, FLOW-16 (as a bare prose adjective)
Reason: Shortcode syntax belongs in reference tables and code, not sentences.
Allowed form: "Capability Framework Rule 12 (CF-12)" on first use; "CF-12" after.
              Or: write out the rule in full and cite the identifier in parentheses.
              "FLOW-16 (marketplace payments)" on first use is fine — it is an expansion, not a bare code.
```

### Pattern 2 — Bare letter+number tokens in prose

```
Prohibited: R1, G3, Q2, P4 (when used as identifier shortcuts in prose)
Reason: Ambiguous without expansion — R1 could mean Rule 1, Requirement 1, or Round 1.
Allowed form: write the full term first: "Round 1 (R1)" or "Rule 1" without abbreviation.
```

### Pattern 3 — ALL_CAPS_UNDERSCORE status strings in prose sentences

```
Prohibited: "The service returned NOT_IMPLEMENTED so we escalated."
            "When the result is DEFERRED_SETTLEMENT the session may close."
Reason: Status strings written as prose identifiers are not English sentences.
Allowed form: "The service returned a not-implemented result, so we escalated."
              Or: write: the `NOT_IMPLEMENTED` error code (inline backtick is allowed).
```

### Pattern 4 — Version suffix codes in prose

```
Prohibited: "The fix shipped in v23.0+hotfix-2 of the flow."
Reason: Version identifiers belong in changelogs and metadata, not description prose.
Allowed form: "The fix shipped in version 23.0 (hotfix 2) of the flow."
```

---

## Section 3 — Project Equivalents (xiigen flows)

The same four patterns apply specifically to this project's identifiers:

```
Ban CF-N alone in prose sentences
  -> Write "Capability Framework Rule N" or use inline backtick `CF-N`

Ban "Rule N" alone without a subject
  -> Write "DNA Rule N (DataProcessResult<T> everywhere)" — name the rule

Ban DNA rule shortcodes as prose adjectives
  -> Not: "This is a Rule 1 violation in the return type."
  -> Yes: "This violates Rule 1 (DataProcessResult<T> everywhere) — the method returns void."

Ban DataProcessResult status / error-code strings as prose
  -> Not: "The method returned Failure with NOT_IMPLEMENTED."
  -> Yes: "The method returned a failure result with error code `NOT_IMPLEMENTED`."

Ban Angular DI token / contract names as prose descriptions
  -> Not: "The component injects CHECKOUT_SETTLEMENT_SERVICE and reads ErrorCode."
  -> Yes: "The component injects the checkout settlement service and reads the
          failure result's error code."
         (or use a code fence / inline backtick for token and type names)
```

(The core ML pipeline-signal example `OUTCOME` / `DPO_TRIPLE` is intentionally
omitted — this repo's flows learn no models of their own; they consume
`.xiigen/wiki-domain` manifests from `llm_mvp_core`.)

---

## Section 4 — Gate Check

Before accepting any generated prose output:

```
□ No LETTER(S)-DASH-NUMBER(S) identifiers outside backticks or code fences
□ No bare letter+number tokens (R1, G3, P4) used as identifier shortcuts in prose
□ No ALL_CAPS_UNDERSCORE status strings written as prose words
□ No version suffix codes in descriptive sentences
```

If any checkbox fails: return the output with the specific pattern identified.
Do not rewrite the prose manually — instruct the generator to rewrite the
failing sentence using the allowed form.

---

## Section 5 — Dashboard And API Documentation Readability

Dashboard/API documentation is accepted only when a non-implementer can answer:
"what API is this?", "what do I type?", "what will I see?", and "what does this
evidence prove?" without reading source code first.

Required for each exposed API method (minimal-API endpoint or Angular client call):

```
□ Human name, not only the method id or route
□ One realistic example input
□ Main parameters in plain language
□ Expected output in plain language (and the DataProcessResult<T> failure shape)
□ Evidence boundary: what the result proves and what it does not prove
```

Required for dashboard/flow docs:

```
□ UML or flow diagram is accompanied by examples/simulations for the edges
□ No-code algorithms name the actual class and method/function
□ The text explains parameters and outputs before showing code quotations
□ Code quotations are short and tied to a "why this matters" explanation
□ Test evidence is separated by scope: unit (xUnit), integration, full UI (ng test),
  end-to-end (Playwright)
□ Angular UX evidence is separated from click-path UI automation: labels,
  accessible names / help text, example values, error wording, history/copy/export
  clarity, layout/bounds, and no internal jargon on the primary user path
□ No mojibake or broken encoding markers such as "Ð", "Ñ", "Â", or "â€™"
  (relevant for the multilingual board strings)
□ No code-style status noise replaces a human explanation
```

For an API/flow dashboard specifically, this gate rejects any document that:
- lists only technical method ids/routes without human names;
- shows examples without explaining the expected output;
- calls a unit/view-model test "full UI" evidence;
- calls click-path UI automation "UX automation" evidence;
- hides the `DataProcessResult<T>` failure shape behind a happy-path-only example.

---

## Section 6 — Human-Readable Plan/Execution Log Fields

Every phase packet, plan review packet, execution review packet, and completion
packet must be readable as a primary human status, not only as machine state.

Required human-readable fields:

```text
stage:
total_major_plan_blocks_or_current_phase_tasks:
completed_count_and_names:
current_plan_step:
current_active_task_and_owner:
remaining_count_and_names:
current_result_type: code | artifact | review | policy | evidence-only | commit boundary
what_changed:
what_remains:
claims_opened:
claims_closed:
claims_still_closed_and_why:
why_this_status_is_true:
next_required_action:
```

Machine-only JSON, ledger rows, hidden logs, or status codes may be attached as
evidence, but they cannot be the primary explanation for Luba. If these fields
are absent, the packet fails readability even when the machine evidence is valid.

### Human-Readable Execution Board

Every long plan-execution status must include a progress board in plain English unless the user requests another language:

```text
Total: <total major plan blocks and/or current-phase tasks>
Done: <completed count>/<total>; <completed names>
In progress: <current task and owner>
Remaining: <remaining count>; <remaining names>
Current result type: <code | artifact | review | policy | evidence-only | commit boundary>
Accepted evidence: <accepted evidence in human words>
Claims that remain closed and why: <claim names + human reason they remain closed>
Open gaps: <open gaps>
Next action: <next bounded action>
```

If the board is missing, the status is not readable enough even when STATE, JSON,
or build/test output exists. The board must appear in the user-visible message
before or alongside machine evidence.

### No Machine Log To User Rule

User-facing progress cannot be only a bare gate id, `goal_reached=true`, a JSON
object, or an internal status label. Internal labels may appear only with a human
translation:

```text
Check: what was checked
Result: what passed or failed
What was fixed: repair made or NONE
What remains: remaining gap or NONE
```

If a status cannot be understood without opening raw logs, return it to
readability repair.

---

## Section 7 — Architecture Audit Readability

Architecture, code-review, documentation, and audit artifacts must also pass the
Human-Readable Architecture Artifact Gate (see
`human-readable-architecture-audit-SKILL.md`):

```
□ Primary explanation is English unless the user requests another language
□ Machine labels are secondary, not the explanation
□ UML is PlantUML or Mermaid classDiagram importable to draw.io
□ Data-flow simulations and expected branch outcomes are present
□ Algorithms are explained in no-code human language
□ Code quotes have exact file:line refs and short quotes
□ Every problem quote has an adjacent concrete solution
□ No table-only, shortcode-heavy, text in a language other than the one requested, schema-first, or prompt/spec artifact
```

"Create schema", "wrap in a service", or "add tests" without concrete classes,
records, methods, tests, and data rows is not a solution. Return the artifact to
governance/authoring repair before accepting it.

---

## Anti-Patterns

1. **"Shortcodes are faster to write"** — shortcodes are faster for the
   generator; they are slower for the reader. The gate protects review
   efficiency, not generation efficiency.

2. **"It's obvious what CF-12 means in context"** — shortcodes are only
   obvious after reading the reference table. Prose must be self-explanatory
   to a reader who has not read the table.

3. **"I'll expand it later"** — the gate runs before output is accepted.
   "Later" never comes in a generation pipeline. Expand at generation time.

4. **"The code fence exception covers this"** — inline code fences (single
   backticks) are allowed for identifiers referenced within a sentence. The
   triple-backtick exception only applies to code blocks. Both forms are
   allowed; ALL_CAPS_UNDERSCORE as a bare word in a prose sentence is not.

---

## Integration

- `context-package-design-SKILL.md` prose sections must pass this gate
- `agent-output-format-SKILL.md` references this gate for the English-first board and machine-status bans
- `human-readable-architecture-audit-SKILL.md` governs the architecture-audit readability detail
- Table cells and code fences that display literal identifiers are exempt — the gate applies to prose only
