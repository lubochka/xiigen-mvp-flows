# output-skepticism-SKILL.md — Ask whether the evidence actually supports the claim

> **Stack adaptation (xiigen flows).** Universal skeptic gate ported from
> `llm_mvp_core/docs/skills/output-skepticism-SKILL.md`. Adapted to this repo:
> - proxy examples use the flows stack (`dotnet build` vs `dotnet test`, `rg` vs correct logic,
>   `ng build` vs `ng test`/Playwright, `passed:true` JSON vs end-to-end); verified per-flow
>   shape `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
>   on `net10.0`;
> - core ML runtime/plugin/host proxies are core-only (G12) and out of scope here; core logic
>   lives in `llm_mvp_core`.

## When to load
Any response that contains a result claim. Trigger phrases: improvement / reduced / fixed /
resolved / addressed; percentage changes; completion claims (done / complete / finished /
passes); progress claims; count claims (N cycles / reviews / passes / files / checks); actor
claims (sub-agent did it / reviewer did it / tests ran / e2e checked). If the response contains
none of these: "No result claims — skepticism N/A."

## The three skeptic questions

Answer all three before sending. Each answer is a specific statement with evidence, not a
reassurance. "I believe so" is not an answer.

### Q1 — Refutation evidence
"What observable evidence would prove this result wrong? Have I looked for it?"

```
For test pass claims:
  Evidence that would refute: dotnet test shows failed count > 0
  Did I look: YES — [paste last lines of dotnet test output incl. Failed: 0]
  OR: NO — I did not run dotnet test in this message -> cannot claim tests pass

For bug fix claims:
  Evidence that would refute: the original symptom still reproduces
  Did I look: YES — ran the test that exercises the symptom: [test name] -> [result]
  OR: NO — did not re-run -> downgrade to "fix implemented, not verified"

For wrapper/rules claims:
  Evidence that would refute: rg finds throw new / OperationResult< in changed files
  Did I look: YES — rg -n "throw new|OperationResult<" [changed files] -> 0 matches
  OR: NO — did not rg -> cannot claim rules compliance
```

### Q2 — Scope validity
"Does the evidence I have cover the scope I am claiming?"

```
Scope claimed: [one project / one flow / all flows / full repo]
Evidence scope: [what was actually checked — filter used, files read]
Coverage sufficient: YES / NO
Count claimed: [N or N/A]
Independent evidence records: [N or lower actual count]
Actor claimed: [parent / sub-agent / tool / unknown]
Actor evidence: [sub-agent id, command output, file diff, or NONE]
If NO: downgrade claim to match actual evidence scope
```

Examples:
```
"Tests pass" without --filter on a flow's Tests project -> scope: that flow, evidence: that flow -> PASS
"FLOW-01 tests pass" with --filter "Registration" -> scope: registration, evidence: that subset -> PASS if claim is "registration tests pass"
"All flows pass" with one flow's Tests run -> scope mismatch -> FAIL -> revise to "FLOW-01 tests pass"
```

### Q3 — Proxy check
"Am I measuring a proxy that could improve without the underlying thing improving?"

| Proxy metric | Underlying thing | Can proxy improve without underlying? |
|-------------|-----------------|--------------------------------------|
| `dotnet build`: 0 errors | Tests pass | YES — build checks compile, not behaviour |
| `rg`: 0 violations | Code is correct | YES — rg checks patterns, not logic |
| Single test pass | All related tests pass | YES — isolation may hide regressions |
| Test count increased | Test quality improved | YES — tests may be shallow assertions |
| `ng build` succeeds | Client behaviour works | YES — build does not render/interact |
| Playwright passed headless | Real UX is good | YES — headless pass ≠ usable UX |
| `passed: true` in a JSON report | Behaviour works end-to-end | YES — report may be static/contract/mixed/validation-only |
| `.xiigen`/manifest file present | The unit loads | YES — presence ≠ load through the locator |
| Ledger row count | Review/fix cycles completed | YES — rows can be generated without independent passes |
| Parent self-review | Sub-agent review completed | YES — actor differs; independence not proven |
| Summary says command ran | Command actually ran | YES — only output/exit code proves execution |

```
Proxy check:
  Metric measured: [what command was run and what it showed]
  Underlying goal: [what was actually supposed to change]
  Are they the same: YES / NO — [one sentence connecting them]
  If NO: state explicitly what was and was not verified
```

> **G12 note.** Core's runtime/plugin/host proxies (compatibility/rollback refs, plugin host
> integration, MOCK readiness, Claude/Cursor/Codex Extension-Host invocation) are not used here
> — those belong to `llm_mvp_core`. Do not borrow that wording to imply capability this repo
> does not own.

## Output skepticism format (before reporting)

```
OUTPUT SKEPTICISM CHECK:
Q1 — Refutation evidence: [what would refute + whether I looked + exact evidence]
Q2 — Scope validity: [scope claimed] vs [scope of evidence] -> PASS / FAIL
Q3 — Proxy check: [metric] -> [goal] — same / different; if different, state what was not verified
Overall: ALL PASS -> report with evidence; ANY FAIL -> revise claim before reporting
```

For count or sub-agent claims, add:

```text
COUNT / ACTOR HONESTY CHECK:
  count_claimed:
  independent_evidence_count:
  ledger_rows_or_summary_only:
  subagent_id_or_NONE:
  allowed_wording:
```

If `independent_evidence_count < count_claimed`, or `subagent_id_or_NONE=NONE` for a sub-agent
claim, say the narrow truth first.

## Worked examples

**A — PASS.** Claim: "FLOW-01 registration tests pass after the idempotency fix."
```
Q1: ran dotnet test ...Tests --filter "Registration" -> Failed: 0, Passed: 12 -> PASS
Q2: scope claimed = registration tests (12); evidence = --filter "Registration" -> 12 -> MATCH -> PASS
Q3: metric = pass count; goal = idempotent registration correct; the idempotency test is named in the 12 -> PASS
Overall: report "FLOW-01 registration tests pass (12/12). Idempotency fix verified."
```

**B — FAIL → revised.** Claim: "The flows repo is now bug-free."
```
Q1: ran one flow's tests, not the whole repo -> FAIL (insufficient scope)
Q2: scope claimed = repo (all flows); evidence = one flow's Tests -> FAIL
Overall: revised: "FLOW-01 tests pass (12/12). Full repo not run in this message. Cannot claim repo-wide status without dotnet test on Xiigen.Flows.slnx."
```

## Anti-patterns

1. "I answered the questions in my head" — answers must appear in the response.
2. Listing the questions without answering them — every question needs a specific, falsifiable
   answer.
3. "Q1 answer: I believe the fix is correct" — belief is not evidence; name the observable that
   would refute it.
4. "Q3 answer: the metric and goal are related" — relatedness is not sufficiency.
5. "Skipping because I just ran the command" — if the output is not in the message, it cannot
   be verified.
