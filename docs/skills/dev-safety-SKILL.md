# dev-safety-SKILL.md — Build gate, git discipline, DNA quick check

> **Stack adaptation (xiigen flows).** SESSION GATE / compile-error-blocking /
> test-floor / sensitive-files rules ported from
> `llm_mvp_core/docs/skills/dev-safety-SKILL.md` (G04). The original was itself
> translated from XIIGen NestJS; the seed translation lives in
> `docs/skills/DNA-DOTNET-TRANSLATION.md` (§Dev Safety Gate). Adapted here:
> - **SESSION GATE** = `dotnet build Xiigen.Flows.slnx` + `dotnet test` on the
>   affected per-flow `Xiigen.Flow.<Name>.Tests` (xUnit), plus `git diff --name-only`.
> - Compile-error trigger includes **warnings**: `Directory.Build.props` sets
>   `TreatWarningsAsErrors=true`, so a CS-warning is a build failure.
> - Domain return type is `DataProcessResult<T>` (`IsSuccess`/`ErrorCode`); services
>   inherit `MicroserviceBase<TSelf>`; helpers `Success<T>(...)` / `Failure<T>("CODE","message",details)`.
>   This is the XIIGen family wrapper — **NOT** core `OperationResult<T>` (which stays
>   in `llm_mvp_core`).
> - Target framework `net10.0` (SDK 10.0.300). xUnit test runner; `npm`/`ng` for the
>   Angular 21 client where a flow ships one (`flows/FLOW-NN-*/client/angular`).
> - Branch discipline: work on the current working branch of this worktree
>   (`UpdateUniversalSkills` / `DesignToCode` / `claude/<worktree>`), never `main`
>   or `master` directly.
> - Sensitive files for this repo: `appsettings.*.json` with real connection strings,
>   `*.pfx`/`*.pem`/`*.key`/`*.cert`, `.env`/`.env.*`, and any file with tokens or
>   credentials.

## When to load

Load at the start of every EXECUTOR session.
Load before any commit.
Load when a new `.cs` service file is created (Rule 1 + Rule 2 quick check).

---

## SESSION GATE — runs at start AND end of every phase

```bash
dotnet build Xiigen.Flows.slnx                                          # 0 errors AND 0 warnings
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests        # 0 failed, count >= baseline
git diff --name-only                                                    # only expected files changed
```

All three must pass. If any fails: fix before proceeding.

---

## Build errors = BLOCKED

A C# compile error is not a warning you can defer. Because
`TreatWarningsAsErrors=true`, a warning is also a build error here.

Compile errors in this project cause real runtime failures because:
- `DataProcessResult<T>` generics break if the type argument is wrong — the wrong
  `T` silently changes which payload is returned;
- interface mismatches cause the DI (Dependency Injection) container to resolve
  the wrong concrete type;
- a `MicroserviceBase<TSelf>` self-type mismatch breaks the base helpers
  (`Success<T>`/`Failure<T>`) at compile time.

**Protocol when you encounter a compile error (or warning):**
1. STOP all other work
2. Fix the error completely
3. Re-run `dotnet build` — must show 0 Error(s) and 0 Warning(s)
4. Then continue

**Anti-patterns to reject immediately:**
- "I'll fix the type error later, the logic is right" → REJECT
- "It's just a warning, harmless" → REJECT — with `TreatWarningsAsErrors` it fails the build
- "The compiler is being overly strict" → REJECT — fix the code, not the compiler

---

## Test suite discipline

### Count must never decrease

Session-start count is the floor. If tests drop:
1. Do NOT proceed
2. Identify which tests disappeared and why
3. If a test was accidentally deleted: restore it
4. If a test helper was renamed: update all references
5. Re-run until count is ≥ baseline

Always read the actual number from `dotnet test` output — never copy from memory.

### Never change an assertion to make a test pass

```csharp
// WRONG — changing the assertion to accept wrong output
Assert.Equal("DELIVERY_INCOMPLETE", result.Error!.Code);  // ← changed from "VALIDATION_FAILED" because code returned the wrong code

// CORRECT — fix the service so it returns the right ErrorCode
```

Changing an assertion to make a test pass is fixing the test to accept wrong
output. Fix the service. The only exception: an explicit written instruction that
the old behavior was wrong and the test must be updated.

---

## Git discipline

### Branch

In a worktree, the working branch is the worktree's branch (e.g.
`UpdateUniversalSkills`, `DesignToCode`, or `claude/<worktree>`). Before any code
change, verify:

```bash
git rev-parse --abbrev-ref HEAD
# Must be the intended working branch — never main or master directly
```

### Commit hygiene

- One commit per phase — never bundle multiple phases
- Commit message format: `"phase N: what changed — files changed"`
- Never `git commit --no-verify`
- Never commit with failing tests or build warnings
- Push only when the phase explicitly authorizes it, to the working branch

### Sensitive files — never commit

- `.env` or `.env.*` files
- Any file containing API keys, tokens, or credentials
- `*.pfx`, `*.pem`, `*.key`, `*.cert` files
- `appsettings.Development.json` / `appsettings.*.json` with real connection strings

---

## DNA quick check — run before every commit

For every new or modified `.cs` file under `flows/FLOW-NN-*/server/...` (not in a
`.Tests` project):

```
☐ Rule 1: every public Application method returns DataProcessResult<T> — no void, no raw bool, no throw for business failure
☐ Rule 2: every external dependency injected through an interface — no "new ConcreteService()" in a ctor
☐ Rule 3: every production code line has a plain-English comment
☐ Rule 4: every service inherits MicroserviceBase<TSelf> and returns Success<T>/Failure<T>
☐ Boundary: no core OperationResult<T> import; no ML.NET/TorchSharp/trainable logic in this repo (G12 → llm_mvp_core)
```

If any check fails: fix before committing.

---

## Zero failures rule

"Zero failures" means zero. Not "zero new failures." Not "pre-existing failures
excluded." Pre-existing failures must be fixed in the current phase, not deferred.
There is no narrow reading where only new code must pass. With
`TreatWarningsAsErrors=true`, "zero" also covers build warnings.
