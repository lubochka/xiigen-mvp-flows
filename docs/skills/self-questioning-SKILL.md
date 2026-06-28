# self-questioning-SKILL.md — QUESTION YOURSELF before finalizing an artifact

> **Stack adaptation (xiigen flows).** Universal QUESTION YOURSELF protocol ported from
> `llm_mvp_core/docs/skills/self-questioning-SKILL.md`: minimum 3 / default 5 questions, the
> mandatory non-empty `ModificationsFromQuestioning` field, and the downstream
> SELF-Q-001/002/003 checks. The core ML domain sets (topology iron rules, RAG/DomainModelRegistry,
> IncrementalTrainer/DPO) are **replaced** with flows domain sets (.NET service/contract,
> Angular 21 component, configuration), because trainable/ML logic lives in `llm_mvp_core` (G12).
> Adapted to this repo:
> - the artifact record returns / validates through the family wrapper `DataProcessResult<Unit>`
>   via `MicroserviceBase<TSelf>` (`Success`/`Failure`) — NOT core `OperationResult<T>`;
> - target framework `net10.0`; verified per-flow shape
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`.

## When to load
- Before finalizing any flow service/contract design.
- Before submitting any contract/endpoint/schema record.
- Before finalizing any Angular 21 component for a user-facing flow.
- When a downstream SELF-Q-001 check fires (missing questions).

## Section 1 — QUESTION YOURSELF protocol

**Minimum: 3 questions. Default: apply all 5 standard questions.**

```
Q1 — Capability classification:
  "Have I classified this capability correctly (Api / Application / Infrastructure / client),
   or could it belong to a different layer that changes which rules apply?"

Q2 — Failure-path coverage:
  "Does this artifact have an explicit Fail path? Every Application method returns
   DataProcessResult<T> via Failure<T>('CODE', ...) on the error branch — is each error branch
   present and tested, not only the happy path?"

Q3 — Boundary correctness:
  "Where does this artifact cross a project/wrapper boundary? Does it return DataProcessResult<T>
   (family wrapper) and not leak OperationResult<T> (core)? Are dependencies injected through
   interfaces, not newed up?"

Q4 — Threshold/configuration:
  "Are any thresholds, timeouts, or limits hardcoded? Should they come from IConfiguration /
   IOptions instead?"

Q5 — Completeness:
  "Is there a behaviour/validation in this artifact I cannot verify will actually fire on a real
   violation? If so, I need a test for it before this artifact is final."
```

## Section 2 — Output format

```csharp
public sealed record SelfQuestionedArtifact<T>
{
    public required T Artifact { get; init; }
    public required IReadOnlyList<SelfQuestion> SelfQuestions { get; init; }   // minimum 3
    public required IReadOnlyList<string> Answers { get; init; }               // one per question, in order
    public required IReadOnlyList<string> ModificationsFromQuestioning { get; init; }
    // What changed because of the questioning. If nothing changed, include at least one entry:
    // "No modifications required — [specific reason]". An empty list is NOT valid.
}

public sealed record SelfQuestion
{
    public required string QuestionId { get; init; }  // "SELF-Q-001" style or custom
    public required string QuestionText { get; init; }
    public required string Domain { get; init; }      // SERVICE / CONTRACT / CLIENT / GENERAL
}
```

`ModificationsFromQuestioning` is the most important field. If empty, the downstream validator
rejects the artifact. It must contain a concrete change made OR an explicit statement of why no
change was needed.

## Section 3 — Downstream validation checks

The validator (an Application service returning `DataProcessResult<Unit>` via `MicroserviceBase`)
verifies three checks:

```
SELF-Q-001: selfQuestions.Count >= 3
  FAIL -> Failure<Unit>("SELF_QUESTIONING_INCOMPLETE", "fewer than 3 questions")

SELF-Q-002: answers.Count == selfQuestions.Count
  FAIL -> Failure<Unit>("UNANSWERED_QUESTIONS", "a question listed but unanswered")
  (a listed-but-unanswered question is worse than no question — it signals an abandoned protocol)

SELF-Q-003: modificationsFromQuestioning is not empty
  FAIL -> Failure<Unit>("SELF_Q_003_EMPTY_MODIFICATIONS", "empty modifications list")
  Note: an explicit "No modifications required — [reason]" entry PASSES; an empty list does not.
```

## Section 4 — Domain-specific question sets

Replace or extend the standard five with these when the artifact type recurs.

### .NET service / Application

```
S1: "Does every public method return DataProcessResult<T> via MicroserviceBase Success/Failure
     (not OperationResult<T>, not a raw exception on the business path)?"
S2: "Are all external dependencies injected through interfaces (Fabric-First), not newed up?"
S3: "Is there a test asserting the Fail path (a specific Failure<T> code), not only the Ok path?"
```

### Contract / endpoint / schema

```
C1: "Does this contract list every endpoint/field the implementing service exposes?"
C2: "Does the error model map each Failure<T> code to a documented HTTP/response shape?"
C3: "Is there a contract/integration test verifying the request/response shape, including the
     conflict/validation error case?"
```

### Angular 21 client component

```
A1: "Does this component cover loading / empty / error states, not only the happy state?"
A2: "Does it expose accessible names / aria / help text on primary controls?"
A3: "Is there an ng test (component) and a Playwright step (user journey) for the new behaviour?"
```

## Anti-patterns

1. "Filling in selfQuestions[] after writing the artifact" — the point is to change the artifact
   before it is final, not to document questions after the fact.
2. "ModificationsFromQuestioning: []" — an empty list signals a mechanical protocol; SELF-Q-003
   fails it. Write "No modifications required — [specific reason]" if nothing changed.
3. "Using Q1-Q5 verbatim when a domain set applies" — a service artifact that uses generic Q2
   instead of S1/S3 misses the wrapper/Fail-path checks.
4. "Answering questions without changing anything for 3+ sessions" — consistent zero-modification
   records signal the protocol is not finding genuine gaps; rotate the question sets.
5. "Skipping self-questioning for 'simple' artifacts" — simple-looking artifacts most often hide
   silent errors (hardcoded thresholds, missing Fail paths, untested validations).
