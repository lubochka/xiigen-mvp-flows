# retroactive-development-SKILL.md — Fix the engine, not the output (xiigen flows)
## v1.0: ported from llm_mvp_core retroactive-development → .NET 10 per-flow / xUnit

## Core Principle

**Fix the engine that produces wrong output, not the output itself.**

If a per-flow `.Application` service generates a wrong document/outcome, fix the
service. If an `.Infrastructure` mapper returns a wrong shape, fix the mapper.
Patching the consumer or a generated artifact and leaving the generator broken
ships the same bug next time.

---

## Fix Propagation Protocol

### Step 1 — Identify the Source

Trace backward from the symptom to the generator:

```
Symptom: activation document has wrong status field
  ↓ trace
Immediate cause: BundleStatusTrackerService writes "DEGRADED" on the wrong condition
  ↓ trace
Root cause: fan-out query matches bundles by the wrong field
  ↓ source found
Fix location: flows/FLOW-00-bundle-activation/server/Xiigen.Flow.BundleActivation.Application/BundleStatusTrackerService.cs
```

### Step 2 — Fix the Engine

Fix the source service. Do not patch downstream consumers or generated artifacts.

```csharp
// ✅ Fix in the per-flow service — the generator
// correct the fan-out predicate so all bundles containing the regenerated flow are updated

// ❌ Patch in the consumer — treating the symptom
// adding a post-process "fix the status" step in the Api endpoint or a downstream flow
```

### Step 3 — Re-validate All Flows Using the Same Component

After fixing the engine, run all tests that exercise the fixed class:

```bash
grep -rn "BundleStatusTrackerService\|OnFlowRegeneratedAsync" \
    flows/FLOW-00-bundle-activation --include="*.cs" -l | grep -v obj

dotnet test --filter "FullyQualifiedName~BundleStatusTracker"
→ Must show: Failed: 0
```

### Step 4 — Verify Fix Is Generative, Not Cosmetic

The fix must cause the engine to produce the right output for **ALL** inputs of
the same type, not just the one reported input. Test with at least 3 inputs of
the same type using `[Theory]` + `[InlineData]`:

```csharp
[Theory]
[InlineData("FLOW-01")]
[InlineData("FLOW-02")]
[InlineData("FLOW-03")]
public async Task OnFlowRegenerated_UpdatesEveryBundleContainingFlow(string flowId)
{
    var fixture = await FlowFixture.CreateAsync();
    using var _ = fixture.PushContext();
    // Arrange: seed multiple bundles that require flowId
    var result = await fixture.StatusTracker.OnFlowRegeneratedAsync(flowId, "v1");
    Assert.True(result.IsSuccess, result.Error?.Message);
    // Assert: every seeded bundle that contains flowId is updated (generative, not one-case)
}
```

```
Cosmetic fix: handles "FLOW-02" specifically → other flows still broken
Generative fix: fixes the fan-out rule → all flows now handled correctly
```

---

## Blast Radius After Engine Fix

An engine fix can silently break callers that relied on the wrong output.

```bash
# 1. Find all callers of the fixed method across flows
grep -rn "OnFlowRegeneratedAsync\|BundleStatusTrackerService" flows --include="*.cs" | grep -v obj

# 2. For each caller, verify its tests still pass
dotnet test --filter "FullyQualifiedName~<CallerClass>"

# 3. Run the full per-flow suite
dotnet test flows/FLOW-00-.../Xiigen.Flow.BundleActivation.Tests.csproj --no-build 2>&1 | tail -10
→ Failed: 0
```

---

## When Generated Artifacts Must Be Updated

Sometimes fixing the engine means previously-generated artifacts (scaffolded
files, snapshots under `docs/e2e-snapshots`, exported schemas) are now wrong.
This is correct — regenerate them.

```
Steps:
1. Fix the engine (per-flow service / mapper)
2. Re-run generation to produce new output
3. Verify the new output is correct (not just different)
4. Commit BOTH the engine fix AND the regenerated output
5. Document in the commit message: "fix <Service>; regenerate <artifact>"
```

Do not leave stale generated output after an engine fix.

---

## Anti-Patterns

1. **Patch the output** — editing a generated artifact instead of fixing the generator.
2. **One-case fix** — handling only the reported input, not the class of inputs (`[Theory]`, ≥3 inputs).
3. **Skip blast radius** — fixing the engine without verifying callers still pass.
4. **Silent output staleness** — fixing the engine and not regenerating dependent output.
5. **Post-hoc test** — writing the regression test after the fix passes (see bug-to-tests-SKILL.md).
