# Naming Conventions Enforcer

> **Stack adaptation (xiigen flows).** Naming standards ported from
> `llm_mvp_core/docs/skills/naming-conventions-enforcer-SKILL.md` (G04). Adapted to
> this repo:
> - The **flow number lives in the folder** `flows/FLOW-NN-<slug>/`, never in a class
>   or file name. The projects are already named
>   `Xiigen.Flow.<Name>.{Api,Application,Infrastructure,Tests}`; a file/class inside
>   them describes the **domain capability** (e.g. `OnboardingDeliveryService`), not a
>   number.
> - C# conventions: `PascalCase` types, `I`-prefix on injected service/port interfaces,
>   implementation name derived from the interface.
> - Tests are xUnit: test class `[ClassName]Tests`, methods `[Method]_[Scenario]_[Expected]`
>   on `[Fact]`/`[Theory]`.
> - Angular 21 standalone clients: components are `<capability>.component.ts` with a
>   meaningful name (not `helper`/`page1`), under `flows/FLOW-NN-*/client/angular`.
> - Commit messages: `phase N: capability — file list`.

## Why This Skill Exists

`Xiigen.Flow.UserRegistration.Application/Processor.cs` tells a reader nothing.
`Xiigen.Flow.UserRegistration.Application/OnboardingDeliveryService.cs` tells them
exactly what capability lives in that file.

`"testId": "T47"` in a log tells a reader nothing.
`"testName": "CompleteAsync_AllMaterialsPresent_ReturnsSuccess (T47)"` tells them
what the test does.

This skill enforces naming standards across:
1. C# file and class names (server: `Api`/`Application`/`Infrastructure`)
2. Interface and implementation names
3. Test class and method names (xUnit)
4. Angular component/file names (client)
5. STATE entries and commit messages

---

## Rule 1 — File Names Must Describe Capability, Not Sequence Number

**NEVER:**
```
flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/Processor1.cs
flows/FLOW-08-marketplace/server/Xiigen.Flow.Marketplace.Application/Helper.cs
flows/FLOW-16-marketplace-payments/server/.../Module.cs
```

**ALWAYS:**
```
flows/FLOW-01-user-registration/server/Xiigen.Flow.UserRegistration.Application/OnboardingDeliveryService.cs
flows/FLOW-08-marketplace/server/Xiigen.Flow.Marketplace.Application/ListingPublicationService.cs
flows/FLOW-16-marketplace-payments/server/.../PaymentSettlementService.cs
```

**Rule:** The flow number belongs in the **folder** `FLOW-NN-<slug>` and in the
project name `Xiigen.Flow.<Name>.*`. The file name must describe the DOMAIN
CAPABILITY, never echo the number.

---

## Rule 2 — Interface Names Follow I-Prefix Convention (injected ports)

**NEVER:**
```csharp
interface PaymentFabric        // missing I-prefix
interface Push                 // truncated
```

**ALWAYS:**
```csharp
interface IPaymentFabricService     // I + capitalized responsibility noun
interface IClientPushConnectionPool // I + clear responsibility
interface IOssCurriculumQueueBroker // I + clear responsibility
```

**Rule:** Injected service/port interfaces start with `I`, followed by a
capitalized noun phrase naming the RESPONSIBILITY, not the implementing technology.
First use of any abbreviation in comments must include its expansion (e.g.
"OSS (Open Source Software)").

---

## Rule 3 — Implementation Names Must Match Their Interface

For an interface `IThingDoer`, the default implementation is `ThingDoer` or
`[Qualifier]ThingDoer` — never a completely different name.

```csharp
// ✅
interface IOnboardingDelivery   → OnboardingDeliveryService
interface IPaymentFabricService → PaymentFabricService, StripePaymentFabricService

// ❌
interface IOnboardingDelivery   → DeliveryEngine   // no relation to the interface name
```

---

## Rule 4 — Test Class Names Include the Class Being Tested (xUnit)

**NEVER:**
```csharp
class TestSuite1
class VerificationTests
class FlowTests
```

**ALWAYS:**
```csharp
class OnboardingDeliveryServiceTests             // one class under test
class UserRegistrationServiceIntegrationTests    // integration tests for one service
```

**Rule:** Test class name = `[TestedClass]Tests` or `[TestedClass]IntegrationTests`.
Test method names: `[Method]_[Scenario]_[ExpectedResult]` on `[Fact]`/`[Theory]`,
e.g. `CompleteAsync_MissingUserId_ReturnsValidationFailed`.

---

## Rule 5 — Angular Component / File Names Must Describe Capability

For client-bearing flows (`flows/FLOW-NN-*/client/angular`):

```
// ❌ machine-only / generic
page1.component.ts
helper.ts

// ✅ capability-named, standalone component
registration-form.component.ts
onboarding-status.component.ts
```

Angular files use `kebab-case` with a domain name and the standard suffix
(`.component.ts`, `.service.ts`). Standalone Angular 21 components — no module
echo of the flow number.

---

## Rule 6 — STATE Files Must Include a Human-Readable Phase Name

```json
// ❌ machine-only
{ "phase": "P5", "status": "DONE" }

// ✅ machine + human readable
{ "phase": "P5", "phaseName": "OnboardingDeliveryService — completion gate + tests", "status": "DONE" }
```

Every STATE file must include `phaseName` alongside the phase number.

---

## Rule 7 — Commit Messages Must Name the Phase and Files

**NEVER:**
```
fix: update stuff
phase 6 done
tests added
```

**ALWAYS (per phase-git-report-SKILL.md):**
```
phase 6: capability — flows/FLOW-01-user-registration/server/.../OnboardingDeliveryService.cs, .../OnboardingDeliveryServiceTests.cs
```

Format: `phase N: short capability description — comma-separated file list`.

---

## Enforcement Gates

### At code-writing time:
```bash
# Verify injected interface names follow I-prefix (server only)
grep -rn "^public interface [^I]" flows/FLOW-NN-*/server --include="*.cs"
# Review each hit: data-shape/DTO/event interfaces may be intentional; injected ports must use I-prefix

# Verify test classes name the class under test
grep -rn "^public class [A-Z].*Tests$" flows/FLOW-NN-*/server --include="*.cs" | head -20
# Expected: each match follows [ClassName]Tests
```

### At commit time:
```bash
# Verify file names are descriptive (flag vague names for review)
grep -rln "class Helper\b\|class Processor\b\|class Module\b\|class Manager\b" flows/FLOW-NN-*/server --include="*.cs"
# Review: "ListingPublicationService" is fine; "Processor" is not
```

---

## Naming Lint Quick Reference

| Artifact type | Pattern | Example |
|---|---|---|
| Injected interface | `I[CapabilityNoun]` | `IPaymentFabricService`, `IClientPushConnectionPool` |
| Implementation | `[Qualifier][NounFromInterface]` | `OnboardingDeliveryService`, `StripePaymentFabricService` |
| Test class (xUnit) | `[ClassName]Tests` / `[ClassName]IntegrationTests` | `OnboardingDeliveryServiceTests` |
| Test method | `[Method]_[Scenario]_[Expected]` | `CompleteAsync_MissingUserId_ReturnsValidationFailed` |
| Server file | `[CapabilityNoun].cs` (in `Api`/`Application`/`Infrastructure`) | `OnboardingDeliveryService.cs` |
| Angular file | `[capability].component.ts` / `.service.ts` | `registration-form.component.ts` |
| Flow folder | `FLOW-NN-<slug>` (number lives here) | `FLOW-01-user-registration` |
| State phase | `phaseName` with full description | see Rule 6 |
| Commit | `phase N: description — files` | see Rule 7 |
