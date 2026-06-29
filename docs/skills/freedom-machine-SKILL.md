# Freedom vs Machine Classification

> **Stack adaptation (xiigen flows).** Universal MACHINE/FREEDOM test ported from
> `llm_mvp_core/docs/skills/freedom-machine-SKILL.md`. Adapted to this repo:
> - the canonical MACHINE/FREEDOM examples are stated against **real flow .NET 10 code**:
>   the domain return-type contract `DataProcessResult<T>` (XIIGen family wrapper, NOT core
>   `OperationResult<T>`), services that inherit `MicroserviceBase<TSelf>` and answer through
>   `Success<T>`/`Failure<T>`, and per-flow public contracts under
>   `flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.{Api,Application,Infrastructure}`;
> - **FREEDOM values are read through `IConfiguration`** with a safe default (no
>   `IConfiguration`-bypassing hardcode); the interface = MACHINE, the DI-chosen concrete
>   implementation = FREEDOM;
> - **Angular 21 client FREEDOM** is created as a provider / `InjectionToken` registered in
>   the real `app.config.ts` (`ApplicationConfig.providers`). These clients are greenfield and
>   have **no `environments/environment.ts` file** — point the create-path at `app.config.ts`,
>   not a non-existent env file;
> - **No-Secrets** reinforces this: a secret value is NOT hardcoded into the repo, but the rule
>   "secrets never live in the repo" is the MACHINE invariant; the value itself comes from
>   config/Vault at runtime;
> - the magic-number / model-name detector greps `.cs` under each flow's `server/` and `.ts`
>   under each flow's `client/angular/src`.

## When to Apply

Before writing any constant, threshold, string, or configuration value into code.
Before deciding whether a value should be in `IConfiguration`, an environment variable,
an Angular provider, or hardcoded.
When a Fabric-First check finds a hardcoded value that should be injected.

---

## The Classification Test

One question:

> **If this value changed, would the system's behavioral guarantees break?**

```
YES → MACHINE. This value enforces a system invariant.
      Hardcode it as a named constant. Test for its presence.
      Never put it in config — changing it changes what the system guarantees.

NO  → FREEDOM. This value is a configurable parameter.
      Read it from IConfiguration (server) or an Angular provider (client) with a
      safe default. Different deployments may reasonably use different values.
```

---

## Canonical MACHINE Values in This Repo

| Value | Why MACHINE |
|-------|-------------|
| `DataProcessResult<T>` return-type requirement | Violating it breaks error propagation for every caller of the flow's `Application` layer |
| `MicroserviceBase<TSelf>` `Success<T>`/`Failure<T>` contract | The success/failure envelope is the invariant all callers depend on |
| Fabric interface resolution (Fabric First) | Bypassing the interface breaks testability and swappability |
| xUnit test-must-pass gate | The zero-failures invariant — cannot be relaxed per flow |
| Tenant-scope isolation invariant | A flow must not read/write outside its tenant scope — changing it breaks isolation |
| No-Secrets-in-repo invariant | A secret in the repo is always wrong, regardless of value |
| Per-flow layer dependency direction (`Api → Application → Infrastructure`) | Reversing it corrupts the architecture boundary |

---

## Canonical FREEDOM Values in This Repo

| Value | Why FREEDOM |
|-------|-------------|
| Email verification TTL (e.g. FLOW-01 `email_verification_ttl_seconds`, default 86400) | Operational policy — different deployments choose different windows |
| Resend rate-limit window (e.g. `resend_rate_limit_minutes`, default 60) | Tuning — not a correctness invariant |
| Idempotency reservation TTL passed per call | A per-call FREEDOM parameter; the rule "reserve before acting" is MACHINE |
| Logging verbosity level | Operational policy — not a correctness invariant |
| xUnit/Playwright test timeout (milliseconds) | CI environment speed — not a correctness invariant |
| Angular client feature-flag default (provided in `app.config.ts`) | Deployment-specific UI toggle |

---

## The Borderline Cases

### Thresholds That Enforce Invariants vs Configure Behavior

```
MACHINE: An idempotency key MUST be reserved before a side-effecting operation runs.
  → Removing the reservation breaks at-most-once guarantees.
  → The RULE is MACHINE.

FREEDOM: The TTL of that idempotency reservation (TimeSpan passed to TryReserveAsync).
  → A longer/shorter TTL changes retention, not the guarantee.
  → The PARAMETER is FREEDOM: read it through IConfiguration.
```

The rule is MACHINE. The parameters of the rule may be FREEDOM.

### Fabric Interface vs Concrete Type

```
MACHINE: The public interface (IDocumentStore, IOutboxStore, IIdempotencyStore,
         ISchedulerService, ITenantContextAccessor)
  → Cannot be changed without breaking all callers.
  → Define it as an interface; never bake a concrete type into a caller.

FREEDOM: The implementation chosen at runtime
  → InMemory vs persisted store — both implement the interface.
  → Select via DI registration in the flow Api host, not hardcoded instantiation.
```

NOTE on existing flow surfaces: interfaces such as `ISchedulerService` /
`IOutboxStore` do **not** expose poll-interval / lease-duration / retry-count /
batch-size on the interface itself (Scheduler = `ScheduleDelayedAsync(delay)` /
`ListAsync`; Outbox = `AppendAsync` / `ListAsync`; `OutboxRecord.Attempts` is
record data, not a setting). Configurability lives in the flow's `IConfiguration`
FREEDOM keys — reference those, do not invent config parameters on the interface.

---

## The Annotation Format

In code, annotate the classification explicitly.

Server (.NET 10):
```csharp
// MACHINE: idempotency rule — every side-effecting op reserves a key first
private const string IdempotencyScope = "flow01.registration";

// FREEDOM: email verification TTL — read from config, deployments differ
private readonly int _verificationTtlSeconds;
public UserRegistrationService(IConfiguration config)
{
    // Read freedom value from config with a safe default
    _verificationTtlSeconds = config.GetValue<int>("Flow01:EmailVerificationTtlSeconds", 86400);
}
```

Angular 21 client (`app.config.ts`):
```typescript
// FREEDOM: client feature toggle provided via InjectionToken in ApplicationConfig
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    { provide: REGISTRATION_FEATURE_FLAGS, useValue: { allowResend: true } }, // FREEDOM
  ],
};
```

---

## Hardcoded Value Detection

Run periodically to find FREEDOM values incorrectly hardcoded:

```bash
# Server: magic numbers in production flow code (potential FREEDOM values that should be config)
grep -rn "= 0\.[0-9]\|= [0-9][0-9][0-9]" flows/*/server --include="*.cs" | grep -v "const\|//" | grep -v "Tests"

# Server: model names / specific endpoints that should be configurable
grep -rn "\"claude-\|\"gpt-\|\"https://" flows/*/server --include="*.cs" | grep -v "//"

# Client: hardcoded URLs/flags that should be provided via app.config.ts
grep -rn "http://\|https://" flows/*/client/angular/src --include="*.ts" | grep -v "//"
```

Every hit is a potential MACHINE/FREEDOM classification error.

---

## Anti-Patterns

1. **"It's just a default, we can change it later"** — defaults become dependencies; if it's FREEDOM, add the `IConfiguration` key (or Angular provider) now.
2. **"The threshold is internal, no caller will change it"** — that's the definition of MACHINE territory; test it explicitly.
3. **Using the same literal value in 5 places without annotation** — is it the same invariant or three different ones?
4. **Putting fabric interface methods on a concrete type** — interface = MACHINE (stable); implementation = FREEDOM (swappable).
5. **Reading MACHINE constants from config/env** — a constant that config can change is not a constant.
6. **Inventing config parameters on `ISchedulerService`/`IOutboxStore`** — their tuning lives in `IConfiguration` FREEDOM keys, not on the interface surface.

---

## Connection to Project Rules

| Rule | MACHINE/FREEDOM Application |
|------|-----------------------------|
| `DataProcessResult<T>` envelope | The return-type pattern is MACHINE — never bypass with nullable or raw bool |
| Fabric First | The interface is MACHINE; the concrete class is FREEDOM |
| No-Secrets-in-repo | The prohibition is MACHINE — no configuration can override it; the value comes from Vault/config |
| Flow parameters (TTLs, rate limits) | FREEDOM — read from `IConfiguration`, provide safe defaults |
