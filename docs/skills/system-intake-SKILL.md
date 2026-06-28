# system-intake-SKILL.md — Extract structure / intent / constraints / quality before designing

> **Stack adaptation (xiigen flows).** Universal *system intake* discipline ported
> from `llm_mvp_core/docs/skills/system-intake-SKILL.md` (G08). Greenfield here —
> no prior author version. The universal core kept: before designing any new
> capability for an existing flow, extract the four properties of the existing
> system — **structure / intent / constraints / quality** — and assemble a domain
> context package. "Code is truth; the spec is intent." The stack details are
> adapted:
> - **Source types** are C# (`net10.0`) for `server/Application` + `Infrastructure`
>   and Angular 21 standalone for `client/angular` — not a single C# monolith.
> - **Intent** for a given `FLOW-NN-<slug>` is read from its `flow.manifest.json`
>   and `docs/`; it must be reconciled against `.xiigen/wiki-domain` manifests so
>   the design does not violate the already-generated domain model.
> - **Constraints** are `DataProcessResult<T>` boundaries (the XIIGen family
>   wrapper, `IsSuccess`/`ErrorCode`) + guard clauses — **NOT** core
>   `OperationResult<T>`, which stays in `llm_mvp_core`.
> - The **domain context package** is expressed as C# `record` types (this repo's
>   idiom), with an Angular note for client-bearing flows.
> - Core algorithm / trainable / ML / DPO logic is not redesigned here (R5 / G12):
>   this repo consumes the model through `.xiigen` / wiki-domain manifests.

## Core Problem

A design session that starts without extracting the existing flow's structure,
intent, constraints, and quality gaps will propose a capability that violates
existing invariants. This is the most common source of re-work: the design was
locally coherent but globally wrong because the designer did not know what the
flow already enforces. This skill defines the four properties to extract and the
extraction protocol per source type, producing a domain context package that
feeds capability design.

---

## When to Invoke

- Before designing any new capability for an existing `FLOW-NN-*`
- Before writing a new phase plan that extends an existing flow service
- Before any capability-design review of a new flow capability
- Before decomposing a capability that touches existing flow code

---

## Section 1 — Four Properties to Extract

Regardless of source type, every intake extracts the same four:

```
structure:   How the flow is organized.
             Api/Application/Infrastructure boundaries, the DI extension
             (Add<Name>Flow), contracts/ interfaces, minimal-API endpoints,
             Angular standalone components. The conceptual architecture — not file names.

intent:      What the flow does for users / tenants.
             Business rules, invariants, domain concepts. From flow.manifest.json +
             docs/ + the consumed .xiigen/wiki-domain model. Not what the code does —
             what the business requires of the code.

constraints: What must always be true.
             DataProcessResult<T> boundaries, tenant/correlation scoping
             (ITenantContextAccessor / ICorrelationContextAccessor), guard clauses.
             Explicit (docs/, contracts/schemas) + implicit (inferred from guards).

quality:     How well the flow currently serves its intent.
             Known gaps, divergence from intent, brittle spots, missing xUnit/Playwright coverage.
```

---

## Section 2 — Extraction Protocol by Source Type

### C# Source (server/Application + Infrastructure — primary source type)

```bash
# Step 1: Entry points — what the flow responds to
grep -rn "MapPost\|MapGet\|public.*DataProcessResult\|public.*Async" \
  flows/FLOW-NN-<slug>/server --include="*.cs" | grep -v Tests

# Step 2: Service boundaries
grep -rn "interface I\|: MicroserviceBase<\|: I[A-Z]" \
  flows/FLOW-NN-<slug>/server --include="*.cs"

# Step 3: Business rules (each Failure(...) / guard is a constraint)
grep -rn "Failure<\|return.*Failure\|if .* return\|throw new" \
  flows/FLOW-NN-<slug>/server --include="*.cs" | grep -v Tests

# Step 4: Tenant / correlation contracts
grep -rn "ITenantContextAccessor\|ICorrelationContextAccessor\|TenantContext" \
  flows/FLOW-NN-<slug>/server --include="*.cs"

# Step 5: Quality signals
grep -rn "TODO\|FIXME\|HACK\|NotImplementedException\|// stub" \
  flows/FLOW-NN-<slug>/server --include="*.cs"
```

### Contracts and Schema Documents

```bash
# Endpoint + schema contracts for this flow
cat flows/FLOW-NN-<slug>/contracts/endpoints/*.json
ls  flows/FLOW-NN-<slug>/contracts/schemas/*.json

# Intent + cross-flow dependencies
cat flows/FLOW-NN-<slug>/flow.manifest.json
```

### Angular Client (client-bearing flows only)

```
Step 1: standalone components (entry surfaces) under client/angular/src
Step 2: provideRouter / provideHttpClient registrations (composition root)
Step 3: typed HTTP service calls that map to the server endpoints (the contract boundary)
Step 4: Playwright e2e specs (what user-facing behavior is already verified)
```

### Consumed Domain Model (`.xiigen/wiki-domain`)

```
Read domain-manifest.json + model-manifest.json. Treat them as third-party-derived
intent evidence: this flow consumes the generated model, it does not own it.
The model body lives in llm_mvp_core (R5). A capability that contradicts the
manifest is a SPEC_CONFLICT — record it, do not silently override.
```

---

## Section 3 — Domain Context Package Format

```csharp
public record DomainContextPackage
{
    public string SourceType { get; init; }              // "csharp" | "contracts" | "angular" | "mixed"
    public List<string> SourceArtifacts { get; init; }   // file paths read

    public StructureRecord Structure { get; init; }
    public IntentRecord Intent { get; init; }
    public ConstraintsRecord Constraints { get; init; }
    public QualityRecord Quality { get; init; }
}

public record StructureRecord
{
    public List<string> ServiceBoundaries { get; init; } // e.g. "UserRegistrationService owns registration only"
    public List<string> Dependencies { get; init; }      // "Service depends on IEmailVerification (contract)"
    public List<string> EntryPoints { get; init; }       // minimal-API routes + public method signatures
    public List<string> FabricContracts { get; init; }   // which contracts/ interfaces are injected
}

public record IntentRecord
{
    public string Summary { get; init; }                 // one sentence: what does this flow do?
    public List<string> BusinessRules { get; init; }     // invariants as plain-language sentences
    public List<string> DomainConcepts { get; init; }    // abstract concepts implemented
    public List<string> Invariants { get; init; }        // what must always be true about output
}

public record ConstraintsRecord
{
    public List<string> Explicit { get; init; }  // documented in docs/ / contracts/schemas
    public List<string> Implicit { get; init; }  // inferred from guard clauses + tenant scoping
    public List<string> Unknowns { get; init; }  // questions requiring human judgment
}

public record QualityRecord
{
    public List<string> Gaps { get; init; }
    public List<string> KnownFailures { get; init; }
    public List<string> BrittleAreas { get; init; }
    public List<string> MissingCoverage { get; init; }   // untested xUnit/Playwright paths
}
```

---

## Section 4 — Minimum Viable Intake

When time is constrained, produce at minimum:

```
□ Intent.Summary (one sentence — what does this capability do for the flow?)
□ Intent.Invariants (3-5 most important things that must always be true)
□ Constraints.Implicit (what the code enforces that docs don't state — incl. tenant scoping)
□ Quality.KnownFailures (what breaks silently today)
```

A minimal intake is better than no intake. It prevents the most common mistake:
proposing an architecture that violates an existing invariant.

---

## Section 5 — CONTEXT_INSUFFICIENT During Intake

If extraction cannot answer a question required for capability design:

```
Missing contract from another flow             → DOWNSTREAM_CONTRACT (read flow.manifest.json deps)
Missing external API contract                  → EXTERNAL_CONTRACT
Ambiguous business rule needing a decision     → HUMAN_JUDGMENT (route to Luba)
Conflicting docs / .xiigen model vs code       → SPEC_CONFLICT (document both, flag)
```

Do not guess. Emit the CONTEXT_INSUFFICIENT signal from
`pipeline-position-check-SKILL.md` Section 2 and wait.

---

## Anti-Patterns

1. **Designing before completing the intake** — "I'll figure out the constraints as I go" produces re-work when a violation surfaces at Gate B. Intake first.
2. **Intake reads only the happy path** — the most important constraints are in guard clauses. Every `Failure<T>(...)` is a constraint. Read the failure paths first.
3. **Intent.Summary describes the implementation** — "uses IEmailVerification and MicroserviceBase" is structure. "Registers a new user and triggers onboarding" is intent. Use domain language.
4. **Skipping the Quality section** — known failures inform invariants; brittle areas inform where to add tests.
5. **Treating contracts/schemas as ground truth without reading the code** — the schema describes intended shape; the code describes actual behavior. When they conflict, the code is truth — note it in Constraints.Unknowns.

---

## Integration

- `intent-to-plan-SKILL.md` / `phase-planning-SKILL.md` consume the domain context package as design input.
- `claim-as-hypothesis-SKILL.md` verifies claims extracted during intake before acting on them.
- `pipeline-position-check-SKILL.md` CONTEXT_INSUFFICIENT format applies to intake gaps.
- `reconnaissance-gate-SKILL.md` thresholds govern how much must be read before intake synthesis.
