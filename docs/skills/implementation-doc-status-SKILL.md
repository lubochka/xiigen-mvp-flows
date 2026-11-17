# Skill: Implementation Documentation — Implementation Status Table (§4)

> **Stack adaptation (xiigen flows).** Universal Part-A §4 contract ported from
> `llm_mvp_core/docs/skills/implementation-doc-status-SKILL.md`. One row per per-flow
> `Api`/`Application`/`Infrastructure` class (and per Angular 21 standalone feature). The
> "what really works" column is phrased in this flow's domain methods returning
> `DataProcessResult<T>` (e.g. `RegisterAsync → working`, `ExportFlowDataAsync → stub`) —
> NOT core ILearningMachine names like `ExportAsync`/`ImportAsync`. Paths are relative to
> repo root.

## Purpose

Every implementation document must end with a status table showing the current
implementation state of every component. This is the single source of truth for "what
works, what doesn't" before any plan starts.

## Required table format

| Component | File | Status | What actually works now |
|---|---|---|---|

**Columns:**
- **Component** — class/component name with backtick formatting.
- **File** — relative path from repo root, no absolute paths
  (e.g. `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs`).
- **Status** — one of the status symbols below.
- **What actually works now** — concrete method names / route names / return values,
  not "see above" and not "partially implemented".

## Status symbols

| Symbol | Meaning | When to use |
|---|---|---|
| ✅ Complete | All public methods implemented, covered by `[Fact]`/`[Theory]`, `dotnet test` green | Use when nothing returns a stub `Failure`/empty outcome |
| ⚠️ Partial | Core path works, some methods are stubs | Use when one domain method works and another always returns a stub `Failure` |
| 🔶 Skeleton | Contract-only — record/DTO types exist, all calls return constant stub values | Use when a service always returns `Failure("NOT_READY", ...)` or an empty outcome |
| ✅ Hardcoded | Returns fixed values — not configurable | Use for registries/constants with hardcoded lists (e.g. `UserRegistrationBfaRuleRegistry`, `UserRegistrationConstants.RuleIds`) |

> **Note.** Do NOT port the core `✅ Bootstrap` symbol — it marks a deterministic
> Confidence=1.0 wrapper awaiting a trained ILearningMachine model and is core-specific
> (G12). For a static wrapper here, use `✅ Hardcoded`.

## What the "What actually works now" column must contain

- For ✅ Complete: list the key working paths — e.g. "`RegisterAsync` (201/400/409),
  idempotency through `IIdempotencyStore`, event in `IOutboxStore`, 12 `[Fact]` tests passing".
- For ⚠️ Partial: explicitly name working methods AND stub methods:
  - ✅ "`RegisterAsync`, `VerifyAsync` work; `ExportFlowDataAsync` → `Failure(\"NOT_READY\")`"
  - ❌ "Partially implemented" (no detail)
- For 🔶 Skeleton: state what is SAVED (preserved contract fields / record shape) and what
  ALWAYS returns a stub:
  - ✅ "`OnboardingOutcome` record is defined; `DeliverWorkspaceAsync` always returns an empty outcome"
  - ❌ "Not implemented"

## Example row (this stack)

| Component | File | Status | What actually works now |
|---|---|---|---|
| `UserRegistrationService` | `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs` | ✅ Complete | `RegisterAsync` → `DataProcessResult<UserRegistrationOutcome>` (201/400/409), idempotency, outbox; covered by `[Fact]` |
| `UserRegistrationBfaRuleRegistry` | `.../Application/UserRegistrationBfaRuleRegistry.cs` | ✅ Hardcoded | Fixed list of BFA rules `Rules`, checked by `P11_Rules_...AreRegistered` |

## Validation gate

Before submitting §4:
- [ ] Every component mentioned in §1-§3 has a row (each per-flow `Api`/`Application`/`Infrastructure` class + Angular 21 feature)
- [ ] No row uses a status symbol outside the allowed list (no core `Bootstrap`)
- [ ] "What actually works now" contains specific method/route names or return values, not prose
- [ ] Partial and skeleton components explicitly list which methods work and which return stubs

## Integration

Required by: `planning-session-startup-SKILL.md` (pre-execution doc gate),
`plan-review-SKILL.md` (FC-17).
