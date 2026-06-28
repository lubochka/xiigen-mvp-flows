# reconnaissance-gate-SKILL.md — Evidence threshold before any synthesis

> **Stack adaptation (xiigen flows).** Universal *reconnaissance gate* ported from
> `llm_mvp_core/docs/skills/reconnaissance-gate-SKILL.md` (G08). Greenfield here —
> no prior author version. The universal core kept: no synthesis output (plan,
> review verdict, architect answer) before the session has met an evidence
> threshold (file reads / grep counts / verbatim excerpts) per session type AND
> written a RECON REPORT; evidence-layer tags (DESIGN_DOC / IMPLEMENTATION / TEST /
> RECONCILIATION); and the Subagent-Firewall sentinels. The stack details are
> adapted:
> - **IMPLEMENTATION** evidence = `*.cs` (server) + Angular `*.ts` (client) +
>   `*.csproj`; **TEST** evidence = `Xiigen.Flow.<Name>.Tests/*.cs` (xUnit) +
>   Playwright `*.spec.ts`; **DESIGN_DOC** = `README.md`,
>   `flow.manifest.json`, `contracts/`, `docs/`, skill files;
>   **RECONCILIATION** = state/matrix artifacts and `.xiigen/wiki-domain` manifests.
> - **Tier-0 reads** = `README.md`, `Directory.Build.props`,
>   `flows/FLOW-NN-<slug>/flow.manifest.json`, and
>   `.xiigen/wiki-domain/domain-manifest.json`.
> - The **High-Risk Recon Library Gate** applies to cross-flow / domain-model
>   changes. The core ML / ILearningMachine recon targets stay in `llm_mvp_core`
>   (R5 / G12); here the high-risk surface is cross-flow contracts and the
>   consumed wiki-domain model.

## Core Problem

A plan, review, or architect answer produced before the session read the files is
an essay, not an analysis. Without file reads, grep counts, and verbatim excerpts
from the actual code, synthesis claims are memory-retrieval dressed as evidence.
This skill installs the mandatory evidence threshold that must be met before any
synthesis output is produced.

---

## Subagent Firewall Sentinels

Preserve these exact proof strings from `AGENTS.md` as authority:
- `Parent Architect Subagent-Only Firewall`
- `parent local sidecar work ban`
- `read-only verification after a sub-agent packet returns`
- `sub-agent waiting is not a stop`
- `no local non-overlapping work escape`

---

## When to Invoke

- At session start, BEFORE any plan output or architectural synthesis
- Before any plan-review verdict
- BEFORE writing any phase plan or execution document
- When reviewing a completion claim against actual flow state

---

## Section 1 — Thresholds per Session Type

No synthesis output may be produced until the threshold is met AND the RECON
REPORT is written.

| Session type | File reads | Grep counts | Verbatim excerpts |
|---|---|---|---|
| EXECUTOR | 5 | 2 | 3 |
| PLANNING | 10 | 3 | 5 |
| REVIEW | 15 | 5 | 8 |
| ARCHITECT | 20 | 8 | 10 |

**Wide-scope mode** (user says "see the whole picture" / "load the real state" /
uploads ≥5 files, or scope spans ≥3 flows): double all thresholds.

**Counts are orthogonal.** Meeting the file-read count without the grep count
fails the threshold. Each dimension catches a different failure class:
- File reads catch "that file doesn't exist at that path".
- Grep counts catch "how many of X are there and how many are populated".
- Verbatim excerpts catch "the code actually says Y, not what I remembered".

---

## Section 2 — Evidence Layer Tags

Every verbatim excerpt carries one of four tags. A claim that something "works"
or "exists as implemented" must trace to at least one IMPLEMENTATION or TEST
excerpt.

```
DESIGN_DOC     Describes intended behavior — not evidence it exists in running code.
               Includes: README.md, Directory.Build.props, flow.manifest.json,
               contracts/*.json, docs/, skill files, AGENTS.md, CLAUDE.md.
               A "works" claim backed only by DESIGN_DOC is NOT verified.

IMPLEMENTATION Source code — what the system actually does.
               Includes: server *.cs, client *.ts, *.csproj, Program.cs.
               A "works" claim requires ≥1 IMPLEMENTATION excerpt.

TEST           A test — what has been verified.
               Includes: Xiigen.Flow.<Name>.Tests/*.cs (xUnit), Playwright *.spec.ts.
               A "passes" claim requires ≥1 TEST excerpt.

RECONCILIATION A state record that may be stale.
               Includes: state/matrix files, EXECUTION-LOG, .xiigen/wiki-domain manifests.
               RECONCILIATION excerpts are hypotheses until confirmed by IMPLEMENTATION.
```

---

## Section 3 — RECON REPORT Template

```markdown
# RECON REPORT — Session [date/ID]

Session type: [EXECUTOR | PLANNING | REVIEW | ARCHITECT]
Wide-scope: [YES | NO]
Threshold: [N] file reads / [M] grep counts / [K] excerpts
Actual: [n] file reads / [m] grep counts / [k] excerpts
Threshold met: [PASS | FAIL — short in dimension X]

## Domain Universe (ARCHITECT sessions only)
Entity type examined: [flows | contracts | Application services | tests | other]
Total universe (N): [integer] — source: [command, e.g. `ls flows | wc -l`]
Examined this session: [n]
Coverage fraction: [n/N = X%]

Unread and material:
- [entity] — [why it could change a synthesis claim if read]
(If all material entities examined: "none — all material entities examined.")

## File Inventory
- [filepath]: [line count] lines — [one-line summary]

## Grep Findings
- Pattern "[pattern]" in [scope]: [N] hits
  - [file:line] [excerpt]

## Verbatim Excerpts
- [filepath:line]: "[exact text]"
  Evidence layer: [DESIGN_DOC | IMPLEMENTATION | TEST | RECONCILIATION]

## Hypotheses (N≥5 entity examination only)
H1: [claim] — Confirms if: [observable] — Refutes if: [observable]
```

---

## Section 4 — Gate Enforcement

### Before first synthesis output

```
□ RECON REPORT exists
□ RECON REPORT meets threshold (all three dimensions)
□ Synthesis output cites RECON REPORT entries
```

### Before any "works" or "exists" claim

```
□ ≥1 IMPLEMENTATION or TEST excerpt backs the claim
□ If only DESIGN_DOC evidence exists: reword to assert intent, not existence
  ("was designed to do X" not "does X")
```

### For ARCHITECT sessions: before flow-system-level claims

```
□ Domain universe block present (coverage fraction declared)
□ Universal quantifiers ("no flow", "every contract", "all tests") require either
  ≥50% coverage in the domain universe, or an explicit scope qualifier:
  "among the [n] flows examined..."
```

### For multi-entity sessions (N≥5 instances): before examining instance 1

```
□ Hypotheses declared in RECON REPORT with Confirms-if / Refutes-if
□ per-entity-examination-SKILL.md activated
```

---

## Section 5 — Flows Tier 0 Reads

At the start of every ARCHITECT session in this repo, read these before any
other file. Tier 0 reads count toward the Section 1 threshold.

```
1. README.md                                  — repo orientation, run scripts
2. Directory.Build.props                      — net10.0, Nullable, TreatWarningsAsErrors
3. flows/FLOW-NN-<slug>/flow.manifest.json    — target flow identity + cross-flow deps
4. .xiigen/wiki-domain/domain-manifest.json   — generated domain model this flow consumes
```

Total Tier 0 contribution: 4 file reads + 2 grep counts (count of flow projects
in `Xiigen.Flows.slnx`; count of `contracts/` interfaces in the target flow).
Tier 0 covers the governance/anchor layer; flow-specific reads follow.

---

## Section 5A — High-Risk Recon Library Gate (cross-flow / domain-model)

For cross-flow designs, changes that touch the consumed `.xiigen/wiki-domain`
model, or any plan where a wrong foundation would cause broad downstream work,
the normal RECON REPORT is only the startup layer. Before architecture prose,
build a Knowledge Library from quantified focused passes.

A focused pass = one bounded question, named roots, exact files/line ranges, and
a decision the pass supports or rejects. Generic folder summaries do not satisfy
this gate.

Knowledge Library row schema:

```text
path:
type:
summary:
why_to_use:
when_to_use:
relevance_to_target_plan:
document_locations:        # contracts/, docs/, flow.manifest.json
implementation_locations:  # *.cs, *.ts
evidence_line_range:
decision_supported:
source_disjoint_evidence: independent_roots | duplicate_derived | single_source
confidence_effect:
held_out_roots_or_sources:
status: accepted | rejected | deferred | evidence_only
coverage_critic:
```

Population proof is required when a claim names a folder/corpus/artifact as
evidence: exact path, file list or index, non-empty proof, cutoff snapshot,
late-file queue. A broad folder grep or row presence alone is not completion
evidence.

If the floor is not met, the output is a recon plan or blocker — not architecture
prose. The core ML / ILearningMachine recon library stays in `llm_mvp_core`
(R5 / G12); here the high-risk surface is cross-flow contracts and the wiki-domain
model only.

---

## Anti-Patterns

1. **"I already know this repo"** — flow state changes between sessions. The RECON REPORT is a current-session snapshot.
2. **"Project knowledge gives me the counts"** — the RECON REPORT requires actual file reads and grep output, not recalled summaries.
3. **"I'll count later"** — counts go in the RECON REPORT before synthesis.
4. **"DESIGN_DOC proves implementation"** — `flow.manifest.json` describes intent; a `*.cs` file shows what exists. An "implemented" claim requires IMPLEMENTATION evidence.
5. **"Threshold met means the picture is complete"** — threshold confirms minimum evidence; coverage fraction is declared separately.

---

## Integration

- `per-entity-examination-SKILL.md` activates when the domain universe shows N≥5 unread material entities (e.g. many flows).
- `planning-session-startup-SKILL.md` baseline question populates the Tier 0 reads.
- `claim-as-hypothesis-SKILL.md` / `claim-verification-SKILL.md`: user claims about existing state require IMPLEMENTATION verification; those actions count toward the threshold.
- `code-examination-SKILL.md`: single-file protocol; reconnaissance applies at session level.
