# Skill: Implementation Documentation — UML Diagrams (§0 + §1)

> **Stack adaptation (xiigen flows).** Universal Part-A §0/§1 contract ported from
> `llm_mvp_core/docs/skills/implementation-doc-uml-SKILL.md`, adapted to this repo:
> .NET 10 (`net10.0`) per-flow solution `Xiigen.Flows.slnx`, where each flow lives in
> `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`
> plus an Angular 21 standalone client. Diagram classes come from those layers and
> from standalone Angular 21 components; the domain return type is
> `DataProcessResult<T>` (XIIGen family wrapper, NOT core `OperationResult<T>`).
> Gate-B for any plan that uses this section = `dotnet build` + `dotnet test`
> (xUnit) per-flow, plus Angular 21 Playwright for client branches.

## Purpose

Every implementation documentation section (Part A) must begin with two UML subsections:
- **§0 Class diagram** — technical reference for programmers (draw.io importable)
- **§1 Human-language flowcharts** — explain what happens in plain language, not method names

The default language for `note for` annotations, decision-node text, and arrow labels
is Russian unless Luba asks otherwise.

## §0 Class Diagram — required elements

1. **Mermaid `classDiagram` block** importable via draw.io Extras → Edit Diagram.
   - PlantUML is also acceptable when it is importable to draw.io and carries the
     same class responsibilities, relationship legend, and simulations.
2. Every class must have a `note for ClassName "..."` annotation in Russian explaining in 1-3 lines:
   - What this class does (not its method names) — e.g. a per-flow `Application`
     service, an `Infrastructure` adapter, a minimal-API endpoint module, or an
     Angular 21 standalone component;
   - Its status: ПОЛНОСТЬЮ РЕАЛИЗОВАНА / ЧАСТИЧНО / ЗАГЛУШКА / СКЕЛЕТ.
3. All relationship arrows must be present and used between the per-flow layers:
   - implementation (`<|..`) — service implements its flow interface;
   - inheritance (`--|>`) — service extends `MicroserviceBase<TSelf>`;
   - composition/association (`-->`) — `Api` endpoint module → `Application` service →
     `Infrastructure` adapter / `Xiigen.Kernel` fabric store;
   - dependency (`..>`) — transient call dependency.
4. **Legend table** immediately after the mermaid block with columns: Класс | Что делает | Статус.

**What the `note for` must say (example, this stack):**
```
note for UserRegistrationService "ПОЛНОСТЬЮ РЕАЛИЗОВАНА\nApplication-сервис flow регистрации →\nпринимает body+идемпотентность →\nвозвращает DataProcessResult<UserRegistrationOutcome>"
```

5. **Per-connection simulation table (REQUIRED).** For EVERY relationship arrow
   (connection) in the diagram, give at least one example simulation: a concrete
   sample input that flows across that connection AND the expected result/output on
   that path. The expected result is expressed in this stack's terms
   (`DataProcessResult<T>.IsSuccess`/`.Error`, `Results.Json` status code). Use this
   table immediately after the legend:

| Соединение (A → B) | Пример входа (симуляция) | Ожидаемый результат по этой ветке |
|---|---|---|
| Program (MapPost) → UserRegistrationService | body `{email, password}`, `X-Idempotency-Key: register-1` | `RegisterAsync` → `DataProcessResult.IsSuccess=true`, `Data.HttpStatus=201` |
| UserRegistrationService → IIdempotencyStore | `operationKey="...:register-1"` | `TryReserveAsync` → reservation `IsSuccess=true`; повтор → `IsFailure`, код `CONCURRENT_UPDATE` |
| Program (ToRegistrationHttpResult) → HTTP | `DataProcessResult.IsFailure`, `Error.Code="VALIDATION_FAILED"` | `Results.Json(..., statusCode: 400)` |

A connection without an example simulation and expected result is an incomplete §0.

**What NOT to do:**
- Class diagram without `note for` annotations (programmer sees method names, not meaning).
- `note for` that just repeats the class name or lists methods.
- Omitting the legend table.
- Connection/arrow without an example simulation and expected result (the reviewer
  cannot tell what data crosses it or what each branch produces).
- A diagram that contains only boxes and arrows but no concrete data-flow example or
  expected branch outcome.

## §1 Human-language flowcharts — required elements

1. Use `flowchart TD` (not `sequenceDiagram` for the primary human diagram).
2. Every decision node `{...}` must state the human question, not a method name:
   - ✅ `{Есть X-Idempotency-Key\nИ email и password заданы?}`
   - ❌ `{string.IsNullOrWhiteSpace(idempotencyKey)}`
3. Every action node must explain the business meaning:
   - ✅ `["✅ Регистрация принята (201)\nЗапись сохранена, событие в outbox"]`
   - ❌ `["RegisterAsync(request, ifMatch, idempotencyKey, ct)"]`
4. Label arrows with the condition in human language:
   - ✅ `-->|"НЕТ\nполя не заполнены → отказ"|`
   - ❌ `-->|false|`
5. Include emoji status indicators: ✅⚠️🔶 on skeleton/partial components.
6. **Per-branch simulation (REQUIRED).** For EVERY decision branch (each outcome of
   each `{...}` node), give a concrete simulation input + the expected result for that
   branch. Example: `Вход: body без idempotency-key → ветка НЕТ → DataProcessResult.IsFailure, Error.Code=BAD_REQUEST, HTTP 400`;
   `Вход: корректный body + ключ → ветка ДА → IsSuccess, HTTP 201`. Every branch of
   every decision node must show one simulated input and its expected output.

**Что НЕ делать (ветки):** ветка решения без примера-симуляции входа и ожидаемого
результата — нарушение Part-A §1 контракта (см. plan-review FC-14).

**One flowchart per major decision path.** The human gate (request validation /
intent classification / auth+tenant check) must have its own flowchart showing all
branches. For UI flows, the Angular 21 client decision path (e.g. form validation,
error display) gets its own `flowchart TD` and Playwright-backed per-branch sims.

## Validation gate

Before submitting a UML section, verify:
- [ ] Class diagram has `note for` on EVERY class (per-flow `Api`/`Application`/`Infrastructure` classes + Angular 21 components)
- [ ] Legend table present after class diagram
- [ ] Flowcharts use human-language labels (no raw method names as primary labels)
- [ ] Decision nodes ask human questions
- [ ] All diagrams open in draw.io without parse errors (test by reading the mermaid syntax)
- [ ] PlantUML diagrams, when used, are importable to draw.io and have the same simulation coverage
- [ ] Every connection (relationship arrow) in §0 has ≥1 example simulation + expected result (per-connection table present), expressed via `DataProcessResult<T>` / `Results.Json` outcomes
- [ ] Every flowchart branch in §1 has a simulation input + expected result

## Integration

Load alongside: `implementation-doc-algorithms-SKILL.md`, `implementation-doc-code-refs-SKILL.md`,
`uml-simulation-to-testcase-SKILL.md`.
Required by: `planning-session-startup-SKILL.md` (pre-execution doc gate),
`plan-review-SKILL.md` (FC-14 per-connection simulation; Human-Readable Architecture
Artifact Gate), `phase-planning-SKILL.md` (Part-A admission).

> **Note — §5 hierarchy is out of scope here.** The `implementation-doc-hierarchy`
> (§5 ILearningMachine/ILogicalOrchestrator/ILogicalFlow/ILogicalUnit levels) is
> core-specific and lives in the G12 group, NOT in xiigen flows. This skill maps §0
> and §1 only.
