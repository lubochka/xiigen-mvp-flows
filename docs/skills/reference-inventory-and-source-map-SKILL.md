# reference-inventory-and-source-map-SKILL.md — Every claim traces to an exact read source

> **Stack adaptation (xiigen flows).** Universal *reference inventory and source
> map* ported from `llm_mvp_core/docs/skills/reference-inventory-and-source-map-SKILL.md`
> (G08). Greenfield here — no prior author version (the only nearby thing was the
> flow-specific `A-TO-B-SOURCE-MAP`; this is the general gate over the whole repo).
> The universal core kept: every planning/review claim traces to
> `source_id` + `path` + `line_range` + `source_layer`; benchmark / other-model
> plan / transcript / sub-agent packet = evidence-only; a license/source gate; a
> source-disjoint-evidence rule; and population-proof for named artifacts /
> folders. The stack details are adapted:
> - **Source rows** cover `flows/FLOW-NN-*/{server,client,contracts}` and
>   `.xiigen/wiki-domain` (as third-party-derived / manifest evidence); evidence
>   line ranges are in `*.cs` / Angular `*.ts`.
> - **Population proof** is mandatory whenever a claim leans on
>   `.xiigen/wiki-domain/documentation/*` (hundreds of auto-generated `.md`).
> - **License/source boundary**: Anthropic/OpenAI/Google SDK material and any
>   third-party corpus must be marked; private/tenant data and the trained model
>   body are not copyable into a universal flows skill — the model stays in
>   `llm_mvp_core` (R5).

## When to load

Load before deep planning, plan review, benchmark-depth comparison,
shallow-planning/shallow-review repair, architecture review, or any artifact that
makes claims about existing files, contracts, code, tests, or governance rules in
this repo.

## Purpose

Force every planning or review claim to trace back to exact read sources. A broad
statement such as "the repo/docs say" is not evidence until it is mapped to a
source id, exact path, line range, and the plan/review claim it supports.

## Subagent Firewall Sentinels

Preserve these exact proof strings from `AGENTS.md` as authority:
- `Parent Architect Subagent-Only Firewall`
- `parent local sidecar work ban`
- `read-only verification after a sub-agent packet returns`
- `sub-agent waiting is not a stop`
- `no local non-overlapping work escape`

Человеческое правило (RU): карта источников нужна не для красивой таблицы, а
чтобы Luba видела, откуда взят каждый вывод. Чужой пример, старый план,
benchmark, sub-agent packet или broad folder search могут быть только
доказательным контекстом. Они не становятся текущей задачей, текущим артефактом,
лицензией на копирование или product-ready доказательством без прямой текущей
команды Luba и прямого чтения источника.

## Required Source Inventory

Create a source inventory before plan prose or before approving a review:

```text
Reference Inventory:
  source_id:
  path:                  # flows/FLOW-NN-*/server|client|contracts/..., .xiigen/wiki-domain/...
  line_range:
  source_layer: current_luba_quote | work_order | plan | guide | skill | code | test | contract | doc | manifest | benchmark_evidence | sub_agent_packet
  claim_supported:
  plan_section:
  decision_supported:
  active_artifact_scope: exact_current_target | evidence_only | out_of_scope
  source_license_status: project_owned | third_party | unknown | not_applicable
  reuse_boundary: universal_pattern | flow_specific | benchmark_only | evidence_only
  source_disjoint_evidence: independent_roots | duplicate_derived | single_source
  confidence_effect:
  held_out_roots_or_sources:
  status: used | rejected | unavailable | evidence_only
  why_used_or_rejected:
```

Rules:
- Every read source must map to a claim, plan section, review finding, or explicit rejection row.
- Broad repo, folder, or doc-family references cannot satisfy a plan/review claim without exact `source_id`, `path`, and `line_range`.
- Benchmark examples, quoted transcripts, old plans, state files, and sub-agent packets may support `benchmark_evidence` / `evidence_only` rows only; they cannot create active requirements unless Luba reactivates them in the current unquoted instruction.
- Every source row distinguishes a universal reusable pattern from flow-specific detail. Flow-specific paths, tenant data, private prompts, raw design files, and licensed third-party material cannot be copied into a universal skill unless the row records the license/source boundary and restates the reusable pattern generically.
- A source with `source_license_status=unknown` cannot support implementation, publication, dataset, or readiness claims. It may only support a recon task, an evidence-only note, or a rejected/deferred row.
- For cross-flow, multi-root, or broad architecture planning, every source row states whether evidence is from independent roots, duplicate-derived material, or a single source; how that changes confidence; and which roots remain held out.
- The exact current artifact stays explicit. A map for one plan/contract/packet cannot silently repair, approve, stage, commit, or replace a different artifact.
- A source read but not affecting the plan/review is `status=rejected` with a reason. Silent unused reads fail the map.
- A source needed but unavailable is `status=unavailable`; name the blocked claim and either remove it or queue a recon task.

## Source-To-Plan / Source-To-Review Map

```text
Source Map:
  source_id:
  claim_supported:
  plan_section_or_review_finding:
  decision_supported:
  exact_evidence_line_range:
  active_artifact_scope:
  source_license_status:
  reuse_boundary:
  source_disjoint_evidence:
  confidence_effect:
  held_out_roots_or_sources:
  status: mapped | rejected | missing | contradicted
  why_used_or_rejected:
```

Review must check:
- every binding requirement has ≥1 mapped current-authority source;
- every code/contract/governance claim has an exact source id and line range;
- every review finding has PASS/FAIL, exact line or absence evidence, impacted requirement, required fix, and re-review trigger;
- every rejected or evidence-only source stays out of active target scope;
- every license/source row and universal-vs-flow-specific boundary is explicit before a detail is reused outside its original flow;
- cross-flow / multi-root claims record source disjointness, confidence impact, and held-out roots before acceptance;
- no completion/readiness claim depends on a broad folder search, memory, or a sub-agent summary without direct source read-back;
- no plan/governance review claims product evidence or implementation readiness unless the exact mapped artifact contains the build/test evidence its Gate B requires (`dotnet build` / `dotnet test` / Playwright).

If any row is `missing` or `contradicted`, the plan or review is not ready.

## Named Artifact / Folder Population Proof

When a claim depends on a named artifact, evidence library, or folder (especially
`.xiigen/wiki-domain/documentation/*`), add a population proof row before
completion:

```text
Population Proof:
  exact_path:
  file_list_or_index:
  non_empty_proof:
  cutoff_snapshot:
  late_file_queue:
  evidence_depth_score: targeted | recon-library | full-pass | legal-verified
```

Reject completion when the proof is a broad folder grep, remembered path,
summary-only packet, or empty directory. Accepted late files must be merged into
the current target artifact or rejected/deferred with reason and line/section
read-back.

## Cross-Flow / Domain-Model Recon Library Hook

When `reconnaissance-gate-SKILL.md` requires the High-Risk Recon Library Gate
(cross-flow or `.xiigen/wiki-domain` changes), this source map links to the
Knowledge Library artifact instead of replacing it. A generic source inventory is
not a substitute for the quantified library passes. The core ML / ILearningMachine
recon library stays in `llm_mvp_core` (R5 / G12).

Review must fail when:
- cross-flow / domain-model plan prose appears before the required pass floors;
- Knowledge Library rows lack `coverage_critic` or `decision_supported`;
- multi-root rows omit `source_disjoint_evidence`, `confidence_effect`, or `held_out_roots_or_sources`;
- a broad source map, folder grep, or old packet is used as the library;
- a named artifact/folder lacks exact path, file list/index, non-empty proof, cutoff snapshot, or evidence depth score;
- source rows do not separate document locations (`contracts/`, `docs/`) from implementation locations (`*.cs`, `*.ts`) for decisions that depend on both.
