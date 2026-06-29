# UML Simulation To Test Case Skill

> **Stack adaptation (xiigen flows).** Universal per-connection / per-branch → test-case
> contract ported from `llm_mvp_core/docs/skills/uml-simulation-to-testcase-SKILL.md`.
> `test_layer` maps to: **xUnit per-flow `Tests` project** (`unit`, `integration`),
> **Angular 21 Playwright** (`e2e_host`, `qa_automation`), plus `negative`.
> `responsible_class` / `responsible_method` come from the per-flow
> `Api`/`Application`/`Infrastructure` layers; expected outputs are expressed as
> `DataProcessResult<T>` outcomes / `Results.Json` status codes. Part A↔Part B parity is
> required.

## Purpose

Use this skill for Luba-readable implementation plans that include UML, data flow,
algorithm branches, or extension/core interaction diagrams. The goal is to turn every
diagram connection and algorithm branch into a testable scenario in this stack's test
layers.

## Required Per-Connection Row

For every UML arrow:

```text
uml_connection_case:
  connection_id:
  from_component:                 # e.g. Program (MapPost) | UserRegistrationService | IIdempotencyStore
  to_component:
  data_sent:                      # body / headers / fabric call args
  example_input:                  # concrete values
  expected_output:                # DataProcessResult.IsSuccess + Data.HttpStatus, or Error.Code
  failure_case:                   # e.g. IsFailure, Error.Code="CONCURRENT_UPDATE"
  responsible_class:              # per-flow Api/Application/Infrastructure class
  responsible_method:             # e.g. RegisterAsync
  test_case_id:                   # e.g. CF01_Registration_...
  test_layer: unit | integration | e2e_host | qa_automation | negative
  evidence_path:                  # path to xUnit test or Playwright spec
```

`test_layer` guidance for this repo:
- `unit` — xUnit `[Fact]`/`[Theory]` constructing the service via the flow test fixture
  (`FlowFixture.Create()`), `Xiigen.Fabrics.InMemory` fabrics, no HTTP.
- `integration` — xUnit through the minimal-API host (`WebApplicationFactory<Program>`;
  note each flow exposes `public partial class Program;`), real DI from `AddXxxFlow()`.
- `e2e_host` / `qa_automation` — Angular 21 Playwright against the running client + API.
- `negative` — invalid body / missing idempotency key / cross-tenant token / wrong role.

## Required Per-Branch Row

For every algorithm branch:

```text
algorithm_branch_case:
  branch_id:
  human_condition:                # plain Russian question from the §1 flowchart
  input_example:
  expected_result:                # e.g. IsSuccess, HTTP 201
  not_expected_result:            # e.g. NOT a 500, NOT a second stored record
  responsible_class:
  responsible_method:
  test_case_id:
  test_layer: unit | integration | e2e_host | qa_automation | negative
```

## Worked example (this stack)

```text
uml_connection_case:
  connection_id: C1
  from_component: Program (MapPost DynamicEndpoint)
  to_component: UserRegistrationService
  data_sent: JSON body + X-Idempotency-Key
  example_input: { "email": "Luba@Example.COM", "password": "correct horse battery staple" }, key="register-1"
  expected_output: DataProcessResult.IsSuccess=true, Data.HttpStatus=201, accountStatus=PENDING_VERIFICATION
  failure_case: missing key → IsFailure, Error.Code="BAD_REQUEST" → HTTP 400
  responsible_class: UserRegistrationService
  responsible_method: RegisterAsync
  test_case_id: CF01_Registration_HashesCredentialsStoresPrivateRecordAndEmitsEvent
  test_layer: unit
  evidence_path: flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Tests/UserRegistrationServiceTests.cs
```

## Gate

- A diagram with boxes and arrows but no simulation table is incomplete.
- A branch with no test row is incomplete.
- Part A and Part B must match: every Part A row maps to a Part B work order or test
  (xUnit `test_case_id` / Playwright spec), and every Part B test maps back to a Part A
  branch. A whole missing branch in either direction is a FAIL.
- Gate-B for the owning phase = `dotnet test` (xUnit, exact pass count) and/or Playwright
  green for `e2e_host`/`qa_automation` rows.

## Integration

Load alongside: `implementation-doc-uml-SKILL.md` (the diagrams whose connections/branches
this skill converts).
Required by: `plan-review-SKILL.md` (FC-14 / Part A↔Part B parity),
`phase-planning-SKILL.md` (test matrix).
