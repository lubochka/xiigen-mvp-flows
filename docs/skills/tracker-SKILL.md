# Tracker — IProjectTrackerService

> **Stack adaptation (xiigen flows).** The swappable tracker adapter ported from
> `llm_mvp_core/docs/skills/tracker-SKILL.md` (G07). The universal part
> (fabric-first: the interface is fixed, the provider is swappable; local-file is
> the zero-config default and is human-readable; a card is marked Complete only
> after the gate passes; the tracker is observability, never the source of truth —
> the per-flow STATE-JSON is) is taken from core. Adapted here:
> - The interface stays C# (flows is .NET, not the TypeScript mvp) but the wrapper
>   is the XIIGen family `DataProcessResult<T>` (`IsSuccess`/`ErrorCode`/`Data`,
>   from `Xiigen.Kernel`) — **not** core `OperationResult<T>`. Services follow the
>   `MicroserviceBase<TSelf>` family pattern with `Success<T>(...)` /
>   `Failure<T>("CODE","message",details)` helpers.
> - DI is ASP.NET Core `services.AddSingleton(...)` (`net10.0`), the same
>   fabric-first registration used by the flows (alongside `IDocumentStore`,
>   `IOutboxStore`, `ISchedulerService`).
> - Cards are **per-flow**: `phase`/`session` map to `FLOW-NN`; the local file lives
>   under the flow's tooling folder (a new write path, greenfield).
> - Gate evidence in a card comment = `dotnet test Xiigen.Flows.slnx` (or the
>   per-flow `Xiigen.Flow.<Name>.Tests`) showing `Failed: 0`, plus `ng test` for
>   client-bearing flows.

## What This Skill Does

Provides session observability through a swappable tracker adapter.
Follows Rule 2 (Fabric First): the interface is fixed (MACHINE), the provider is swappable (FREEDOM).

Default is always local-file — no configuration needed.

---

## Interface (MACHINE — never changes)

```csharp
/// <summary>Project tracker adapter — fabric-first, provider is swappable.</summary>
public interface IProjectTrackerService
{
    /// <summary>Create a new tracker card for a flow session.</summary>
    Task<DataProcessResult<CardCreated>> CreateCardAsync(
        string flow,        // "FLOW-16-marketplace-payments"
        string phase,       // "P1", "P2", etc.
        string session,     // "session-1", "session-2", etc.
        string title,
        string? description = null);

    /// <summary>Update the status of an existing card.</summary>
    Task<DataProcessResult<Unit>> UpdateStatusAsync(
        string cardId,
        CardStatus status,  // InProgress, Complete, Blocked
        string? note = null);

    /// <summary>Add a comment to an existing card.</summary>
    Task<DataProcessResult<Unit>> AddCommentAsync(
        string cardId,
        string comment);
}

public record CardCreated(string CardId);
public enum CardStatus { InProgress, Complete, Blocked }
```

All methods return `DataProcessResult<T>` — never throw for business logic. (Rule 1)

---

## Provider Configuration (FREEDOM — swappable)

| Provider | Config | When to Use |
|----------|--------|-------------|
| `local-file` (default) | No config needed | Always — zero-config start |
| `linear` | `TRACKER_PROVIDER=linear` + `LINEAR_API_KEY` | When Linear is configured |
| `jira` | `TRACKER_PROVIDER=jira` + `JIRA_URL` + `JIRA_TOKEN` | When Jira is configured |
| `github` | `TRACKER_PROVIDER=github` + `GH_TOKEN` + `GH_REPO` | When GitHub Issues is configured |

Default is `local-file`. No environment variable = no external service = sessions still track locally.

---

## Local File Provider

Writes to the flow's tooling folder, for example
`flows/FLOW-16-marketplace-payments/tools/tracker/cards.json`.

```json
{
  "cards": [
    {
      "cardId": "FLOW-16-P4-session-1-001",
      "flow": "FLOW-16-marketplace-payments",
      "phase": "P4",
      "session": "session-1",
      "title": "FLOW-16 Phase 4: PaymentSettlementService",
      "status": "Complete",
      "createdAt": "2026-06-28T10:00:00Z",
      "updatedAt": "2026-06-28T14:00:00Z",
      "comments": [
        "Starting PaymentSettlementService implementation",
        "Gate: 233 tests passing, 0 failed (per-flow .Tests)"
      ]
    }
  ]
}
```

Human-readable and durable. If the tracker code fails, Luba can read the JSON directly.

---

## Session Usage Protocol

**At session start:**
```csharp
// Create a card for this session's work
var result = await tracker.CreateCardAsync(
    "FLOW-16-marketplace-payments", "P4", "session-3", "FLOW-16 P4: settlement");
var cardId = result.Data!.CardId; // "FLOW-16-P4-session-3-001"
```

**During session (at skill start):**
```csharp
// Record which skill / step is active
await tracker.AddCommentAsync(cardId, "Starting PaymentSettlementService — STEP 3");
```

**At session end (gate passed):**
```csharp
// Mark complete with test gate evidence
await tracker.UpdateStatusAsync(cardId, CardStatus.Complete, "Gate: 233 passed, 0 failed");
```

**At session end (blocked):**
```csharp
// Record the blocker
await tracker.UpdateStatusAsync(cardId, CardStatus.Blocked,
    "BLOCKED: webhook signature reconcile not converging on partial captures");
```

---

## DI Registration

```csharp
// In Program.cs or the flow's module registration (ASP.NET Core, net10.0)
services.AddSingleton<IProjectTrackerService>(provider =>
{
    // Read TRACKER_PROVIDER from environment — defaults to local-file
    var trackerProvider = Environment.GetEnvironmentVariable("TRACKER_PROVIDER") ?? "local-file";

    return trackerProvider switch
    {
        "linear"  => new LinearTrackerService(Environment.GetEnvironmentVariable("LINEAR_API_KEY")),
        "jira"    => new JiraTrackerService(/* ... */),
        "github"  => new GitHubTrackerService(/* ... */),
        _         => new LocalFileTrackerService("tools/tracker/cards.json")
    };
});
```

This is the same fabric-first registration the flows use for their other adapters
(`IDocumentStore`, `IOutboxStore`, `ISchedulerService`): the interface is fixed,
the provider is chosen at composition time.

---

## Anti-Patterns

1. **Update status before gate passes** — mark Complete only after `dotnet test`
   (the per-flow `.Tests`) shows `Failed: 0` (and `ng test` passes for a client-bearing flow)
2. **Use tracker as source of truth** — the per-flow STATE-JSON is the source of truth; the tracker is observability only
3. **Hardcode the provider** — always read from the environment variable so the provider is swappable
4. **Skip tracker when blocked** — a Blocked status with a reason is more useful than silence
