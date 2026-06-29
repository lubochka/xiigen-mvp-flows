# no-product-decisions-SKILL.md — When to stop vs self-resolve

> **Stack adaptation (xiigen flows).** Universal STOP-on-product-decision gate ported
> from `llm_mvp_core/docs/skills/no-product-decisions-SKILL.md`. Adapted to this repo:
> - **"public API"** = a per-flow ASP.NET Core (Web API) endpoint/DTO contract under
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api` PLUS the public inputs of an
>   Angular 21 standalone component under `flows/FLOW-NN-<slug>/client/angular/src`;
> - **locked decisions** live in `docs/DECISIONS-LOCKED.md` (greenfield — create the file
>   the first time a locked decision is captured; see `decision-capture-SKILL.md`);
> - the test that decides STOP vs proceed is **"would changing this require updating
>   `docs/DECISIONS-LOCKED.md`?"**;
> - **Gate-B** = `dotnet build` + `dotnet test` (xUnit) for the affected
>   `Xiigen.Flow.<Name>.Tests` project; the domain return type on `Application` public
>   methods is `DataProcessResult<T>` (XIIGen family wrapper), not core `OperationResult<T>`;
> - **R7 external-AI tool ban** applies verbatim: agents must not self-select an external
>   AI tool/service as an implementation shortcut without an explicit current Luba quote.

## When to load
Load for ARCHITECT sessions. Load when a plan contains two design alternatives.

## The rule
If a plan requires choosing between two approaches that affect:
- user-facing behaviour
- public API (Application Programming Interface) shape — per-flow Web API endpoint/DTO
  or an Angular standalone component's public inputs
- architectural direction
- a locked decision in `docs/DECISIONS-LOCKED.md`

→ STOP. Present both options. Wait for Luba's decision.
→ Do not self-select. Do not pick the "obviously better" one.

## What counts as a product decision

- Choosing between two architectural approaches (e.g., where a capability lives:
  `Application` vs `Infrastructure`, or which flow owns it)
- Deciding to add or remove a feature
- Choosing a default value for a user-configurable parameter (a FREEDOM value read
  through `IConfiguration` — its default is a product choice)
- Deciding which flow (`FLOW-NN`) to work on next
- Any choice that would require updating `docs/DECISIONS-LOCKED.md`

## Exception: purely implementation details
Minor implementation choices (variable names, internal helper structure,
xUnit test helper naming) may be self-resolved without stopping.
The test is: "would changing this require updating `docs/DECISIONS-LOCKED.md`?"
If yes → stop. If no → proceed.

## Tool approval boundary (R7)
Architect must not self-select external AI or unrelated tools as an
implementation shortcut. If the current work order or approved plan does not
explicitly require that exact tool family, explicit Luba approval is required
before use. Using an external AI tool without permission is a hard violation.

## What to do when a product decision is encountered
1. Stop at that point in the plan
2. Present the two (or more) options with pros/cons
3. Wait for Luba's decision
4. Proceed after explicit approval

## Three anti-patterns (from behavioral corrections)
1. Declaring a visual/API-contract regression "correct behaviour" without asking Luba
2. Marking a phase done when Gate B (`dotnet build` + `dotnet test`) is not met
3. Processing a mid-session file without asking whether to switch tasks
