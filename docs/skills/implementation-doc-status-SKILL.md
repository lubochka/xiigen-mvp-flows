# Skill: Implementation Documentation — Implementation Status Table (§4)

> **Stack adaptation (xiigen flows).** Universal Part-A §4 contract ported from
> `llm_mvp_core/docs/skills/implementation-doc-status-SKILL.md`. One row per per-flow
> `Api`/`Application`/`Infrastructure` class (and per Angular 21 standalone feature). The
> "what really works" column is phrased in this flow's domain methods returning
> `DataProcessResult<T>` (e.g. `RegisterAsync → рабочий`, `ExportFlowDataAsync → stub`) —
> NOT core ILearningMachine names like `ExportAsync`/`ImportAsync`. Paths are relative to
> repo root.

## Purpose

Every implementation document must end with a status table showing the current
implementation state of every component. This is the single source of truth for "what
works, what doesn't" before any plan starts.

## Required table format

| Компонент | Файл | Статус | Что реально работает сейчас |
|---|---|---|---|

**Columns:**
- **Компонент** — class/component name with backtick formatting.
- **Файл** — relative path from repo root, no absolute paths
  (e.g. `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs`).
- **Статус** — one of the status symbols below.
- **Что реально работает сейчас** — concrete method names / route names / return values,
  not "see above" and not "частично реализована".

## Status symbols

| Символ | Значение | Когда использовать |
|---|---|---|
| ✅ Полная | All public methods implemented, covered by `[Fact]`/`[Theory]`, `dotnet test` green | Use when nothing returns a stub `Failure`/empty outcome |
| ⚠️ Частичная | Core path works, some methods are stubs | Use when one domain method works and another always returns a stub `Failure` |
| 🔶 Скелет | Contract-only — record/DTO types exist, all calls return constant stub values | Use when a service always returns `Failure("NOT_READY", ...)` or an empty outcome |
| ✅ Hardcoded | Returns fixed values — not configurable | Use for registries/constants with hardcoded lists (e.g. `UserRegistrationBfaRuleRegistry`, `UserRegistrationConstants.RuleIds`) |

> **Note.** Do NOT port the core `✅ Bootstrap` symbol — it marks a deterministic
> Confidence=1.0 wrapper awaiting a trained ILearningMachine model and is core-specific
> (G12). For a static wrapper here, use `✅ Hardcoded`.

## What the "Что реально работает сейчас" column must contain

- For ✅ Полная: list the key working paths — e.g. "`RegisterAsync` (201/400/409),
  идемпотентность через `IIdempotencyStore`, событие в `IOutboxStore`, 12 `[Fact]` зелёные".
- For ⚠️ Частичная: explicitly name working methods AND stub methods:
  - ✅ "`RegisterAsync`, `VerifyAsync` рабочие; `ExportFlowDataAsync` → `Failure(\"NOT_READY\")`"
  - ❌ "Частично реализована" (no detail)
- For 🔶 Скелет: state what is SAVED (preserved contract fields / record shape) and what
  ALWAYS returns a stub:
  - ✅ "`OnboardingOutcome` запись определена; `DeliverWorkspaceAsync` всегда возвращает пустой outcome"
  - ❌ "Не реализована"

## Example row (this stack)

| Компонент | Файл | Статус | Что реально работает сейчас |
|---|---|---|---|
| `UserRegistrationService` | `flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/UserRegistrationService.cs` | ✅ Полная | `RegisterAsync` → `DataProcessResult<UserRegistrationOutcome>` (201/400/409), идемпотентность, outbox; покрыто `[Fact]` |
| `UserRegistrationBfaRuleRegistry` | `.../Application/UserRegistrationBfaRuleRegistry.cs` | ✅ Hardcoded | Фиксированный список BFA-правил `Rules`, проверяется `P11_Rules_...AreRegistered` |

## Validation gate

Before submitting §4:
- [ ] Every component mentioned in §1-§3 has a row (each per-flow `Api`/`Application`/`Infrastructure` class + Angular 21 feature)
- [ ] No row uses a status symbol outside the allowed list (no core `Bootstrap`)
- [ ] "Что реально работает сейчас" contains specific method/route names or return values, not prose
- [ ] Partial and skeleton components explicitly list which methods work and which return stubs

## Integration

Required by: `planning-session-startup-SKILL.md` (pre-execution doc gate),
`plan-review-SKILL.md` (FC-17).
