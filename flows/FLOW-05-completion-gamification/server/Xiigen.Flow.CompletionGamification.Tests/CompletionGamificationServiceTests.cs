using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.CompletionGamification.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.CompletionGamification.Tests;

public sealed class CompletionGamificationServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = CompletionGamificationBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(CompletionGamificationConstants.RuleIds));
        Assert.Equal(15, CompletionGamificationBfaRuleRegistry.Rules.Count);
        Assert.All(CompletionGamificationBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(CompletionGamificationConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0501_PointsAreDerivedServerSideAndEarnedPointsInputIsRejected()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var accepted = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-points", "learner-a", scorePercent: 80),
            ifMatch: null,
            idempotencyKey: "points-ok");
        var rejectedPayload = CompletionRequest("lesson-points-2", "learner-a", scorePercent: 80);
        rejectedPayload["earnedPoints"] = 9999;
        var rejected = await fixture.CompletionGamification.SubmitCompletionAsync(rejectedPayload, null, "points-reject");

        Assert.True(accepted.IsSuccess, accepted.Error?.Message);
        Assert.Equal(170, accepted.Data?.Body["earnedPoints"]);
        Assert.True(rejected.IsFailure);
        Assert.Equal("VALIDATION_FAILED", rejected.Error?.Code);
    }

    [Fact]
    public async Task CF0502_StreakUsesRequiredTimezoneOffsetAndStoresLearnerLocalDate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var missing = CompletionRequest("lesson-timezone-missing", "learner-tz", scorePercent: 70);
        missing.Remove("userTimezoneOffset");
        var rejected = await fixture.CompletionGamification.SubmitCompletionAsync(missing, null, "tz-missing");
        var accepted = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-timezone", "learner-tz", scorePercent: 70, timezoneOffset: 180),
            null,
            "tz-ok");

        Assert.True(rejected.IsFailure);
        Assert.Equal("VALIDATION_FAILED", rejected.Error?.Code);
        Assert.True(accepted.IsSuccess, accepted.Error?.Message);
        Assert.Matches(@"\d{4}-\d{2}-\d{2}", Convert.ToString(accepted.Data?.Body["localDate"]));
        Assert.Equal(1, accepted.Data?.Body["currentStreak"]);
    }

    [Fact]
    public async Task CF0503_SocialShareRequiresAchievementAndExplicitConsent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var privateResult = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-private", "learner-social", scorePercent: 95, shareConsent: false),
            null,
            "social-private");
        var publicResult = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-public", "learner-social-public", scorePercent: 96, shareConsent: true),
            null,
            "social-public");

        Assert.True(privateResult.IsSuccess, privateResult.Error?.Message);
        Assert.False((bool)privateResult.Data!.Body["socialShareAllowed"]!);
        Assert.True(publicResult.IsSuccess, publicResult.Error?.Message);
        Assert.True((bool)publicResult.Data!.Body["socialShareAllowed"]!);
        var shares = await fixture.Documents.SearchAsync(
            CompletionGamificationConstants.SocialShares,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["learner_id"] = "learner-social-public" });
        Assert.Single(shares.Data ?? []);
    }

    [Fact]
    public async Task CF0504_CompletionData_IsTenantScoped()
    {
        var fixture = FlowFixture.Create();

        using (fixture.PushContext("tenant-a", "tenant_user"))
        {
            var created = await fixture.CompletionGamification.SubmitCompletionAsync(
                CompletionRequest("lesson-shared", "learner-same", scorePercent: 75),
                null,
                "tenant-a-completion");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        using (fixture.PushContext("tenant-b", "tenant_user"))
        {
            var status = await fixture.CompletionGamification.GetCompletionStatusAsync("lesson-shared", "learner-same");
            Assert.Equal("NOT_STARTED", status.Data?.Status);

            var created = await fixture.CompletionGamification.SubmitCompletionAsync(
                CompletionRequest("lesson-shared", "learner-same", scorePercent: 75),
                null,
                "tenant-b-completion");
            Assert.True(created.IsSuccess, created.Error?.Message);
        }

        var tenantA = await fixture.Documents.SearchAsync(CompletionGamificationConstants.Completions, new Dictionary<string, object?> { ["tenant_id"] = "tenant-a" });
        var tenantB = await fixture.Documents.SearchAsync(CompletionGamificationConstants.Completions, new Dictionary<string, object?> { ["tenant_id"] = "tenant-b" });
        Assert.Single(tenantA.Data ?? []);
        Assert.Single(tenantB.Data ?? []);
    }

    [Fact]
    public async Task CF867_ConcurrentCompletion_AllowsOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-race", "learner-race", scorePercent: 80, simulateSlowWrite: true),
            null,
            "race-a");
        var second = fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-race", "learner-race", scorePercent: 80, simulateSlowWrite: true),
            null,
            "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF868_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-replay", "learner-replay", scorePercent: 88), null, "same-key");
        var second = await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-replay", "learner-replay", scorePercent: 88), null, "same-key");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["completionId"], second.Data?.Body["completionId"]);

        var completions = await fixture.Documents.SearchAsync(
            CompletionGamificationConstants.Completions,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["lesson_id"] = "lesson-replay", ["learner_id"] = "learner-replay" });
        Assert.Single(completions.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == CompletionGamificationConstants.CompletionRecordedEvent);
    }

    [Fact]
    public async Task CF869_ValidationRejectsMissingPrimaryFieldAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.CompletionGamification.SubmitCompletionAsync(
            new Dictionary<string, object?> { ["lessonId"] = "lesson-invalid", ["scorePercent"] = 60, ["userTimezoneOffset"] = 0 },
            null,
            "invalid");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);

        var completions = await fixture.Documents.SearchAsync(CompletionGamificationConstants.Completions, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(completions.Data ?? []);
    }

    [Fact]
    public async Task CF870_TimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-timeout", "learner-timeout", scorePercent: 80, simulateFabricTimeout: true),
            null,
            "timeout-1");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);

        var completions = await fixture.Documents.SearchAsync(CompletionGamificationConstants.Completions, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(completions.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == CompletionGamificationConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF875_DuplicateCompletionWithDifferentKeyDoesNotDoubleAwardPoints()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-duplicate", "learner-dup", scorePercent: 82), null, "dup-a");
        var second = await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-duplicate", "learner-dup", scorePercent: 82), null, "dup-b");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsFailure);
        Assert.Equal("CONCURRENT_UPDATE", second.Error?.Code);
        var ledgers = await fixture.Documents.SearchAsync(
            CompletionGamificationConstants.PointLedger,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["learner_id"] = "learner-dup" });
        Assert.Single(ledgers.Data ?? []);
    }

    [Fact]
    public async Task ProductSpec_DashboardSummarizesPointsStreakAchievementsAndCompletions()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-dashboard-a", "learner-dashboard", scorePercent: 91, shareConsent: true), null, "dash-a");
        await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-dashboard-b", "learner-dashboard", scorePercent: 70), null, "dash-b");

        var dashboard = await fixture.CompletionGamification.GetDashboardAsync("learner-dashboard");

        Assert.True(dashboard.IsSuccess, dashboard.Error?.Message);
        Assert.Equal(2, dashboard.Data?.CompletionCount);
        Assert.True(dashboard.Data?.TotalPoints > 0);
        Assert.True(dashboard.Data?.AchievementCount >= 2);
        Assert.Equal(1, dashboard.Data?.CurrentStreak);
    }

    [Fact]
    public async Task ProductSpec_PeerGradeIsIdempotentAndEmitsCloudEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "referral_user");

        var first = await fixture.CompletionGamification.SubmitPeerGradeAsync(
            "lesson-peer",
            new Dictionary<string, object?> { ["learnerId"] = "learner-peer", ["graderId"] = "grader-1", ["grade"] = 5, ["comment"] = "Clear answer" },
            "grade-key");
        var second = await fixture.CompletionGamification.SubmitPeerGradeAsync(
            "lesson-peer",
            new Dictionary<string, object?> { ["learnerId"] = "learner-peer", ["graderId"] = "grader-1", ["grade"] = 5, ["comment"] = "Clear answer" },
            "grade-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == CompletionGamificationConstants.PeerGradeRecordedEvent);
    }

    [Fact]
    public async Task ProductSpec_StatusReportsNotStartedAndCompletedStates()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var empty = await fixture.CompletionGamification.GetCompletionStatusAsync("lesson-status", "learner-status");
        await fixture.CompletionGamification.SubmitCompletionAsync(CompletionRequest("lesson-status", "learner-status", scorePercent: 99), null, "status-complete");
        var completed = await fixture.CompletionGamification.GetCompletionStatusAsync("lesson-status", "learner-status");

        Assert.Equal("NOT_STARTED", empty.Data?.Status);
        Assert.Equal("COMPLETED", completed.Data?.Status);
        Assert.True(completed.Data?.EarnedPoints > 0);
    }

    [Fact]
    public async Task ProductSpec_LearningCompletionRunsTerminalOutboxEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");

        var result = await fixture.CompletionGamification.SubmitCompletionAsync(
            CompletionRequest("lesson-terminal", "learner-terminal", scorePercent: 100, shareConsent: true),
            null,
            "terminal");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotNull(result.Data?.Body["learningCompletionId"]);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == CompletionGamificationConstants.FlowCompletedEvent);
    }

    private static Dictionary<string, object?> CompletionRequest(
        string lessonId,
        string learnerId,
        int scorePercent,
        int timezoneOffset = 0,
        bool shareConsent = false,
        bool privateMode = false,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["lessonId"] = lessonId,
            ["learnerId"] = learnerId,
            ["questionnaireResult"] = new Dictionary<string, object?>
            {
                ["scorePercent"] = scorePercent,
                ["answersCorrect"] = Math.Max(0, scorePercent / 10),
                ["answersTotal"] = 10
            },
            ["userTimezoneOffset"] = timezoneOffset,
            ["shareConsent"] = shareConsent,
            ["privateMode"] = privateMode
        };
        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        if (simulateFabricTimeout)
        {
            request["simulateFabricTimeout"] = true;
        }

        return request;
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            CompletionGamificationService completionGamification,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            CompletionGamification = completionGamification;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public CompletionGamificationService CompletionGamification { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var freedom = new InMemoryFreedomConfigService();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var completionGamification = new CompletionGamificationService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<CompletionGamificationService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                freedom,
                completionGamification,
                tenantAccessor,
                correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = $"corr-{Guid.NewGuid():N}";
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" ? null : "user-1",
                new HashSet<string> { role },
                correlation,
                "test",
                DateTimeOffset.UtcNow));
            var correlationScope = CorrelationContextAccessor.Push(new CorrelationContext(correlation, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(tenant, correlationScope);
        }

        private sealed class CompositeScope : IDisposable
        {
            private readonly IDisposable _first;
            private readonly IDisposable _second;

            public CompositeScope(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                _second.Dispose();
                _first.Dispose();
            }
        }
    }
}
