# test-failure-triage-SKILL.md — Classify every test failure before fixing (xiigen flows)
## v1.0: ported from llm_mvp_core test-failure-triage → .NET 10 per-flow / xUnit

## What This Skill Prevents

Treating all test failures identically. Modifying production code to silence a
fragile test. Patching the wrong layer and leaving the real bug untouched.

Gate B requires `Failed: 0`. Without classification, ALL failures get the same
response (fix production code). That is wrong when the test itself is wrong, or
when DI/infrastructure changed beneath the code.

---

## Step 1 — Capture the Failure Report

```bash
dotnet test flows/FLOW-NN-<slug>/server/Xiigen.Flow.<Name>.Tests/Xiigen.Flow.<Name>.Tests.csproj --no-build 2>&1 | tee /tmp/test-output.txt
# Look for: test names, error messages, stack traces, and the line number of the failure
```

---

## Step 2 — Classify Each Failure (4 classes)

For each failing test, apply this decision tree. **Classify before fixing.**

```
Was this test passing at the start of this session?

  YES (regression) → Was production (.Application/.Infrastructure) code changed this session?
    YES → CLASS 1: Implementation regression
    NO  → Was DI / Program.cs / a constructor / a package version changed?
      YES → CLASS 2: Infrastructure regression
      NO  → CLASS 4: Test environment failure (investigate setup)

  NO (new test) → Is the test code correct?
    NO  → CLASS 3: Test fragility (test was written wrong)
    YES → Was the production code supposed to satisfy this test?
      YES → CLASS 1: Implementation regression (new code broke something)
      NO  → Review: is this test for the right class?
```

---

## Class 1 — Implementation Regression

**Definition:** `.Application`/`.Infrastructure` code changed in a way that
breaks a previously-passing test. A behavioral invariant is no longer satisfied.

**Detection:**
```bash
git diff --name-only HEAD          # which flow service files changed
# Read each changed file completely; find which change broke the invariant
```

**Response:**
1. DO NOT modify the test — the test is correct.
2. Find what changed in the per-flow service and why the invariant broke.
3. Fix the service to satisfy the invariant again.
4. Re-run the specific test after the fix.

---

## Class 2 — Infrastructure Regression

**Definition:** DI registration, a constructor signature, a package version
(`Xiigen.Kernel`/`Xiigen.Fabrics.*`), or `Program.cs` changed. Production code
and test are both correct; the test fails because the wiring changed beneath them.

**Detection:**
```bash
git diff --name-only HEAD | grep -iE "Program|Startup|Registration|csproj"
# If a constructor changed in .Application: every caller/fixture may need updating
```

Typical flows marker: a new constructor parameter on `<Flow>Service` (e.g. a new
accessor) breaks every fixture that calls `new <Flow>Service(...)`.

**Response:**
1. DO NOT modify the production algorithm.
2. Fix the wiring: update DI registration in `.Api`, update the fixture's `new ...()` call, fix package version.
3. Re-run after the infrastructure fix only.

---

## Class 3 — Test Fragility

**Definition:** The test was always fragile — it passed by accident or tests
implementation details instead of behaviour.

**Detection markers (xUnit/flows):**
- Asserts on exact mock call counts / sequences (`Times`, `Received(3)`)
- Asserts on internal state that should not be observable
- Depends on a specific execution order or static state
- Exact-string matching on messages that may change
- Mocks the per-flow service under test instead of its dependencies

**Response:**
1. Fix the test to assert behaviour (`result.IsSuccess` / `result.Error?.Code` /
   `result.Data?.X`), not implementation details.
2. Apply the Iron Rule (bug-to-tests-SKILL.md): an assertion change requires
   Luba's approval.
3. After rewriting, run the test to confirm it passes for the right reason.

---

## Class 4 — Test Environment Failure

**Definition:** The test setup itself is broken — the test cannot reach the code
under test because DI setup failed, a fabric/service is not constructed, or
seed data is missing.

**Detection (common xUnit/flows patterns):**
```
// - InvalidOperationException: No service for type '...' has been registered
// - NullReferenceException at the FlowFixture.CreateAsync() / new ...() line (before Act)
// - Missing seeded document → GetAsync returns null and the Arrange crashes
```

**Key distinction:** Class 4 fails **before the Act line**. Class 1 fails **at
the Assert line**. Check the line number of the exception.

**Response:**
1. DO NOT modify the production class.
2. DO NOT patch the test body to "fix" it.
3. Fix the setup: construct the missing fabric/service, seed the missing
   document, push the required tenant/correlation context.

---

## Step 3 — Record Classification (before fixing)

```
Failure triage: [date]

Test: BundleActivationServiceTests.Activate_StaleVersion_ReturnsConcurrentUpdate
  Class: CLASS_2_INFRASTRUCTURE_REGRESSION
  Evidence: new constructor param on BundleActivationService not supplied in FlowFixture
  Response: update FlowFixture.CreateAsync() to pass the new accessor

Test: BundleStatusTrackerServiceTests.Fanout_UpdatesAllBundles
  Class: CLASS_4_TEST_ENVIRONMENT_FAILURE
  Evidence: NullReferenceException at the seed line (BDL-secondary not upserted) — before Act
  Response: seed BDL-secondary in Arrange
```

---

## Anti-Patterns

1. **Patching Class 3 by changing production code** — makes tests pass but the
   code is now wrong.
2. **Fixing Class 1 by changing the assertion** — violates the Iron Rule.
3. **Not classifying before acting** — fixing the wrong layer every time.
4. **Treating "test failed" as "code is wrong"** — Class 2/3/4 are not code bugs.
5. **Committing with unclassified failures** — classification is the first step,
   never skip it.
