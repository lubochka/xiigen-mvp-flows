# Design Artifact Completeness

> **Stack adaptation (xiigen flows).** Universal 5-check completeness contract ported from
> `llm_mvp_core/docs/skills/design-artifact-completeness-SKILL.md`. Detection commands are
> for C# on .NET 10 per-flow layers: C2 searches `NotImplementedException` / `throw new` /
> `TODO` and counts `[Fact]`/`[Theory]`; C5 (reachable/wired) = DI registration in
> `Program.cs` via the flow's `AddXxxFlow()` extension + project references between
> per-flow `Api`/`Application`/`Infrastructure` + `dotnet build` 0 Error(s). The domain
> wrapper is `DataProcessResult<T>` (NOT core `OperationResult<T>`).

## Core Problem

A `.cs` file that exists on disk is not automatically an implemented capability. An
interface that is defined is not automatically one that is wired. A test class that exists
is not automatically a test class that has assertions. This skill installs 5 orthogonal
checks that distinguish "file exists" from "file is a valid input to downstream processes."

---

## When to Invoke

- During reconnaissance, for every per-flow project the session's scope touches.
- Before any plan step that depends on "X is implemented" or "X is designed".
- When a claim about "X is done / complete / implemented" is captured.
- Before writing a phase plan that builds on existing artifacts.

One completeness check before planning = zero "built on empty stub" execution failures.

---

## Section 1 — The 5 Checks

Each artifact is evaluated on 5 independent dimensions — an artifact can pass Check 1 and
fail Check 2 (the classic stub pattern).

### Check 1 — Files Exist
**Verifies:** all expected files are present.
```bash
ls flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/<Service>.cs
ls flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Application/I<Name>.cs   # if an interface is declared
ls flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/<Service>Tests.cs
```
**PASS** = file exists at expected path. **FAIL** = missing.

### Check 2 — Fields/Methods Populated
**Verifies:** real content, not a skeleton.
```bash
# throw-only / stub method bodies
grep -n "NotImplementedException\|throw new\|TODO\|FIXME" flows/.../Application/<Service>.cs
# empty methods
grep -n "{ }" flows/.../Application/<Service>.cs
# test class has at least 1 [Fact]/[Theory]
grep -c "\[Fact\]\|\[Theory\]" flows/.../Tests/<Service>Tests.cs
```
**PASS** = 0 `NotImplementedException`/stub hits; ≥1 `[Fact]`/`[Theory]`.
**PARTIAL** = some methods implemented, some throw or return a stub `Failure`; few tests.
**FAIL** = any `NotImplementedException`; 0 test facts.

> Note: in this stack a "stub" is often not a throw but a method that always returns
> `Failure("NOT_READY", ...)` or an empty outcome. Treat a constant stub return the same
> as `NotImplementedException` for C2.

### Check 3 — Content Specific
**Verifies:** the implementation is specific to this capability, not copy-pasted.
```bash
grep -n "public.*Task<DataProcessResult<" flows/.../Application/<Service>.cs
# verify method names match the capability's stated contract (e.g. RegisterAsync for registration)
```
**PASS** = method names match the capability spec; body is capability-specific.
**FAIL** = methods identical to another flow's class with no distinction.

### Check 4 — Matches Specification
**Verifies:** the implementation aligns with its declared contract / route.
```bash
# every declared interface method has a concrete implementation
grep -n "interface I<Name>" flows/.../Application/I<Name>.cs
grep -n "<MethodName>" flows/.../Application/<Service>.cs
# the route in Program.cs calls the expected service method
grep -n "MapPost\|MapGet" flows/.../Api/Program.cs
```
**PASS** = contract ↔ concrete bidirectional match; route → method match.
**PARTIAL** = some methods match; route wiring missing.

### Check 5 — Reachable by Consumers
**Verifies:** the capability can actually be called.
```bash
# project references between per-flow layers
grep -n "Xiigen.Flow.<Name>.Application\|Xiigen.Flow.<Name>.Infrastructure" flows/.../Api/Xiigen.Flow.<Name>.Api.csproj
# DI registration through the flow extension
grep -n "Add<Name>Flow\|AddSingleton\|AddScoped\|AddTransient" flows/.../Infrastructure/*.cs flows/.../Api/Program.cs
# build succeeds
dotnet build flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Api → 0 Error(s)
# tests exercise the capability
grep -n "RegisterAsync\|FlowFixture" flows/.../Tests/<Service>Tests.cs
```
**PASS** = project references present; `AddXxxFlow()` registers the service; build succeeds; tests call it.
**PARTIAL** = references present, build green, no consumer tests yet.
**FAIL** = reference missing; build fails; service not resolvable (would `NullReferenceException`/DI error at runtime).

---

## Section 2 — Per-Artifact Output Format

```markdown
## Design Artifact Completeness (design-artifact-completeness-SKILL)

Capability: [CapabilityName]

| Artifact | C1 (exists) | C2 (populated) | C3 (specific) | C4 (matches spec) | C5 (reachable) | Status |
|---|---|---|---|---|---|---|
| <Service>.cs | PASS | PARTIAL | PASS | PARTIAL | FAIL | INCOMPLETE |
| <Service>Tests.cs | PASS | FAIL (0 facts) | — | — | — | INCOMPLETE |

Overall: INCOMPLETE
Blocking: C2 on <Service>.cs (stub Failure in N methods), C2 on <Service>Tests.cs (0 [Fact])
Non-blocking: C5 PARTIAL (service not registered in AddXxxFlow())
```

### Overall Status Categories

| Status | Meaning |
|---|---|
| COMPLETE | All 5 checks PASS across all artifacts |
| DESIGN_COMPLETE_NOT_WIRED | C1–C4 PASS, C5 FAIL (implemented, not registered in `AddXxxFlow()`) |
| PARTIAL | Some PASS, some FAIL, no structural blockers |
| STUB | C2 FAIL — file exists but throws / always returns a stub `Failure` |
| MISSING | C1 FAIL on any required artifact |

---

## Section 3 — Gate Enforcement

**Blocking checks:** Check 1 (missing file) or Check 2 (stub/empty) blocks treating the
artifact as a complete input to plans.

Plan options when a referenced artifact fails blocking checks:
1. **Implement first** — add a task to implement the stub before proceeding.
2. **Scope around the gap** — acknowledge: "this phase proceeds without X; consequence is Y".
3. **Mark DEFERRED** — Luba approves with explicit reason.

Silent assumption that a stub is usable is not an option.

**Non-blocking checks:** Check 3, 4, 5 are surfaced as CONCERNs but do not block the plan.

---

## Anti-Patterns

1. **"The file exists, so the design exists"** — Check 1 alone is not completeness; Check 2 is the difference.
2. **"The stub will be implemented later"** — "later" is a task; add it to the plan.
3. **"A constant `Failure(\"NOT_READY\")` is fine while prototyping"** — a STUB status blocks treatment as a complete input.
4. **"Tests exist"** — a test class with 0 `[Fact]` is not a test class.
5. **"Wiring is trivial, skip Check 5"** — a service not registered in `AddXxxFlow()` produces a DI resolution error at runtime. Check 5 is not optional.

## Integration

- `plan-self-validation-SKILL.md` Phase 0 uses Check 1–2 to classify prerequisites.
- `phase-preflight-SKILL.md` uses these checks to verify assumptions before code.
- Required by: `plan-review-SKILL.md` (Design Artifact Completeness gate before technical checks).
