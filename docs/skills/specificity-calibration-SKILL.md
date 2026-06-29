# specificity-calibration-SKILL.md — Concreteness as a mechanically scored property

> **Stack adaptation (xiigen flows).** Universal *specificity calibration* ported
> from `llm_mvp_core/docs/skills/specificity-calibration-SKILL.md` (G08).
> Greenfield here — no prior author version (core had a duplicate file pair; this
> is the single canonical copy). The universal core kept: concreteness is a
> mechanically counted score = count(file:line) + count(N-of-M) + count(verbatim);
> thresholds per session type; and the anti-gaming rules (a repeated ref counts
> once, verbatim = exact characters, paths must be real, counts must come from THIS
> session's reconnaissance). The stack details are adapted:
> - **file:line** examples are `flows/FLOW-NN-<slug>/server/**/*.cs:NN` and
>   `flows/FLOW-NN-<slug>/client/angular/src/**/*.ts:NN`.
> - **verbatim** excerpts come from `*.cs` / `*.spec.ts`, from `dotnet build`
>   errors (e.g. `CS0246: ...`), from xUnit output, and from Playwright logs.
> - **N-of-M** is "N of M flows / tests / contracts". The domain wrapper referenced
>   in excerpts is `DataProcessResult<T>` (XIIGen family) — **NOT** core
>   `OperationResult<T>`.

## Core Problem

An architect answer with no file:line references, no integer counts, and no
verbatim excerpts is an essay. It provides a frame for thinking but leaves the
reader to do the reconnaissance the architect already had access to. This skill
makes concreteness a mechanically scored property and rejects output below
threshold before any STOP gate.

---

## When to Invoke

- Before every STOP gate — self-score the output before presenting it
- Before emitting any architect-level synthesis or plan review
- Before handing any plan to `plan-review-SKILL.md`
- When `reconnaissance-gate-SKILL.md` wide-scope mode applies (doubles thresholds)

One self-score before STOP = zero "your answer is too abstract" correction cycles.

---

## Section 1 — What "Specific" Means

Three senses are often conflated:
1. **Word count** — more words. Not specificity; often the opposite.
2. **Narrow scope** — a smaller question. Orthogonal to specificity.
3. **Concreteness** — pointing at specific existing things. This is specificity.

This skill measures #3.

**Abstract:** "the flow's services appear to be partially wired"
**Concrete:** "3 of 5 Application services in
`flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/`
return `DataProcessResult<T>`; `OnboardingDeliveryService.cs:NN` returns a bare
`Task` (grep: 0 hits for `DataProcessResult` in that file)".

---

## Section 2 — The Scoring Formula

```
Score = count(file:line refs) + count(N-of-M claims) + count(verbatim excerpts)
```

Each reference counts once regardless of how many times restated.

### Dimension 1 — file:line References

Counts as ONE:
- Exact path + line: `flows/FLOW-01-user-registration/server/.../UserRegistrationService.cs:NN`
- Exact path + method: `UserRegistrationService.cs::RegisterAsync()`
- Exact path + line range: `Program.cs:NN-MM`

Does NOT count:
- Directory only: `.../Application/` — no file, no line
- Category: "the registration services" — no specific entity
- Hypothetical path: "something like `Services/Onboarding.cs`" — not verified

### Dimension 2 — N-of-M Integer Claims

Counts as ONE:
- Count with numerator and denominator: "10 of 14 services wrapped"
- Count with scope: "388 lines in UserRegistrationService.cs"
- Before/after: "was 4 of 14, is now 14 of 14"
- Zero-result grep: "0 hits for `IModerationDecision` in `contracts/`"

Does NOT count: "many" / "several" / "most"; "roughly half"; ratios without bases.

### Dimension 3 — Verbatim Quoted Excerpts

Counts as ONE:
- Direct quote from file content: `"return Failure<RegistrationOutcome>(\"DUP_EMAIL\", ...);"`
- Direct quote from error output: `"CS0246: The type or namespace 'ModerationOutcome' could not be found"`
- Exact symbol from grep result: `"AddUserRegistrationFlow"`

Does NOT count: paraphrase; summary; generic code not from a specific source file.

---

## Section 3 — Thresholds per Session Type

| Session type | file:line | N-of-M | verbatim | Total minimum |
|-------------|----------:|-------:|---------:|--------------:|
| EXECUTOR turn output | 5 | 2 | 3 | 10 |
| ARCHITECT answer | 5 | 3 | 3 | 11 |
| PLAN-REVIEW report | 10 | 5 | 5 | 20 |
| DESIGN-REVIEW output | 15 | 8 | 5 | 28 |

Total minimum is the primary gate. Each dimension also requires ≥1 reference (11
file:line and 0 N-of-M passes total but fails dimension diversity).

**Wide-scope mode doubles all thresholds** (when `reconnaissance-gate-SKILL.md` has
declared wide-scope mode for this session, e.g. scope spans ≥3 flows).

---

## Section 4 — Self-Check Protocol

Before any STOP gate, run the count on the output being presented:

```markdown
## Specificity self-check (specificity-calibration-SKILL.md)

Output scored: [session output from this response]

file:line references: [N]
  [list each: "path:line — what this reference shows"]

N-of-M integer claims: [M]
  [list each: "N of M — what is being counted"]

verbatim quoted excerpts: [K]
  [list each: `"quoted string"` — source file or command output]

Total: [N + M + K]
Threshold for [session type]: [threshold]
Dimension diversity: [≥1 in each? YES | NO — which is missing]

Verdict: PASS | FAIL — [reason if FAIL]
```

**If FAIL:** output rejected. Add specifics — run additional reconnaissance if
needed, re-count, retry. Do not present a FAIL output to the user.

**If PASS:** include the self-check block in the output (or log to session state).

---

## Section 5 — Anti-Gaming Rules

1. **Same file:line mentioned multiple times = 1 reference.** `UserRegistrationService.cs:NN` cited five times counts as 1.
2. **Same count, multiple phrasings = 1 claim.** "10 of 14 wrapped" and "10 of the 14 have the wrapper" count as 1.
3. **Verbatim requires exact characters.** Removing the quotes from `"DUP_EMAIL"` breaks verbatim status. Copy exactly.
4. **File paths must be real paths in the actual repo.** A hallucinated path is worse than no reference — verify by reading or globbing.
5. **Counts must be from reconnaissance, not memory.** "I recall there were about 14 services" is not a count. Count from a Grep/Glob result this session.
6. **Verbatim excerpts must come from files read this session.** Quotes from training-data memory are plausible strings, not verbatim excerpts.
7. **Placeholder refs score 0.** `[file]:123` signals the session never actually looked.

---

## Section 6 — Worked Example: FAIL vs PASS

**FAIL output:**
```
"The flow services appear to be partially wired. Some return types are missing the
wrapper. The session identified coverage gaps in the registration flow."
```
Score: 0 + 0 + 0 = 0 of 11 for ARCHITECT. FAIL — labels abstractions, points at no artifacts.

**PASS output:**
```
"3 of 5 services in flows/FLOW-01-user-registration/server/.../Application/ return
DataProcessResult<T> (grep: OnboardingDeliveryService.cs:NN — 0 hits for
DataProcessResult; UserRegistrationService.cs:NN — wrapper present).
Program.cs:NN calls `AddUserRegistrationFlow`. 0 hits for `IModerationDecision`
across contracts/. dotnet build emits `CS0246: ... 'ModerationOutcome' ...`."
```
Score: 3 file:line + 3 N-of-M + 3 verbatim = 9 of 11. One more grounded reference → 10+. PASS.

---

## Integration

- `reconnaissance-gate-SKILL.md` produces the evidence base; this skill scores the output. Both are required in ARCHITECT sessions.
- `session-mode-declaration-SKILL.md` mode determines the threshold (ARCHITECT = 11, PLAN-REVIEW = 20).
- `coverage-completeness-gate-SKILL.md` fires at STOP; this self-check fires first — a FAIL here means coverage claims are unsupported.
- `claim-verification-SKILL.md`: claim verdicts must themselves meet the threshold; abstract verdicts fail.
