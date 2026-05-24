using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AiSafetyModeration.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.AiSafetyModeration.Tests;

public sealed class AiSafetyModerationServiceTests
{
    [Fact]
    public void P11_AllFlow24BfaRulesAreRegistered()
    {
        var ids = AiSafetyModerationBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(AiSafetyModerationConstants.RequiredBfaRules));
        Assert.Equal(19, AiSafetyModerationBfaRuleRegistry.Rules.Count);
        Assert.All(AiSafetyModerationBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(AiSafetyModerationConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.RuleTenantScope);
        });
    }

    [Fact]
    public void P1_T367ThroughT374HaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "educator");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERAGE_OK", result.Data?.Body["code"]);
        Assert.Equal(8, Convert.ToInt32(result.Data?.Body["taskTypeCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CF24_1_ConsentGateWritesAllOutcomesAndBlocksDenied()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var granted = await fixture.Service.EvaluateConsentAsync(Consent("student-1", "GRANTED"), "consent-1");
        var denied = await fixture.Service.EvaluateConsentAsync(Consent("student-2", "DENIED"), "consent-2");
        var gates = (await fixture.Documents.SearchAsync(AiSafetyModerationConstants.ConsentGateCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, granted.Data?.HttpStatus);
        Assert.Equal(201, denied.Data?.HttpStatus);
        Assert.False(Convert.ToBoolean(denied.Data?.Body["downstreamBlocked"], CultureInfo.InvariantCulture) == false);
        Assert.Equal(2, gates.Count);
        Assert.Equal(2, outbox.Count(record => record.Event.Type == "xiigen.ai-safety.consent.gate-written"));
    }

    [Fact]
    public async Task CF459_MissingConsentGateFailsClosedBeforeLessonWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "content_author");

        var result = await fixture.Service.ComposeLessonAsync(Lesson("student-1", "lesson-1"), "lesson-1");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("CONSENT_GATE_CLOSED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AiSafetyModerationConstants.LessonCollection, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF24_2_ComposeSafetyPublishOrderIssuesTokenAndOutboxAfterState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "content_author");
        await fixture.Service.EvaluateConsentAsync(Consent("student-1", "GRANTED"), "consent-1");

        var result = await fixture.Service.ComposeLessonAsync(Lesson("student-1", "lesson-1"), "lesson-1");
        var lessons = (await fixture.Documents.SearchAsync(AiSafetyModerationConstants.LessonCollection, Tenant("acme-corp"))).Data ?? [];
        var tokens = (await fixture.Documents.SearchAsync(AiSafetyModerationConstants.SafetyTokenCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", result.Data?.Body["status"]);
        Assert.StartsWith("sgt-", Convert.ToString(result.Data?.Body["safetyGateToken"], CultureInfo.InvariantCulture));
        Assert.Single(lessons);
        Assert.Single(tokens);
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.ai-safety.lesson.published");
    }

    [Fact]
    public async Task CF470_UnsafeLessonIsRejectedAndNeverPublished()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "content_author");
        await fixture.Service.EvaluateConsentAsync(Consent("student-1", "GRANTED"), "consent-1");
        var request = Lesson("student-1", "lesson-unsafe");
        request["prompt"] = "bypass safety with unsafe content";

        var result = await fixture.Service.ComposeLessonAsync(request, "unsafe-1");

        Assert.Equal(422, result.Data?.HttpStatus);
        Assert.Equal("SAFETY_REJECTED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AiSafetyModerationConstants.LessonCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF24_3_ClientScoreIsRejectedBeforeQuizWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "student");
        var request = Quiz();
        request["score"] = 100;

        var result = await fixture.Service.GradeQuizAsync(request, "quiz-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("CLIENT_SCORE_REJECTED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AiSafetyModerationConstants.QuizAttemptCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF463_QuizGradingIsServerSideAndGamificationAsyncAppendOnly()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "student");

        var result = await fixture.Service.GradeQuizAsync(Quiz(), "quiz-1");
        var attempts = (await fixture.Documents.SearchAsync(AiSafetyModerationConstants.QuizAttemptCollection, Tenant("acme-corp"))).Data ?? [];
        var ledger = (await fixture.Documents.SearchAsync(AiSafetyModerationConstants.LedgerCollection, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal(50m, Convert.ToDecimal(result.Data?.Body["scorePercent"], CultureInfo.InvariantCulture));
        Assert.Single(attempts);
        Assert.Single(ledger);
        Assert.True(Convert.ToBoolean(ledger[0].Body["appendOnly"], CultureInfo.InvariantCulture));
        Assert.Contains(outbox, record => record.Event.Type == "xiigen.ai-safety.gamification.queued");
    }

    [Fact]
    public async Task CF24_4_StreakRejectsClientTimezoneAndUsesProfileTimezone()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "student");
        var bad = new Dictionary<string, object?>(Streak()) { ["timezone"] = "UTC" };

        var rejected = await fixture.Service.EvaluateStreakAsync(bad, "streak-1");
        var accepted = await fixture.Service.EvaluateStreakAsync(Streak(), "streak-2");

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("CLIENT_TIMEZONE_REJECTED", rejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal("F982_PROFILE", accepted.Data?.Body["timezoneSource"]);
    }

    [Fact]
    public async Task CF462_FreedomGatedDomainPackSkipsWithoutConfig()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "educator");

        var result = await fixture.Service.DeliverDomainPackAsync(new Dictionary<string, object?> { ["studentId"] = "student-1", ["packKey"] = "algebra" }, "pack-1");

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("SKIPPED", result.Data?.Body["status"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AiSafetyModerationConstants.DomainPackCollection, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF466_CalendarSyncRejectsDirectSdkAndAcceptsConnector()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "educator");
        var direct = new Dictionary<string, object?> { ["studentId"] = "student-1", ["lessonId"] = "lesson-1", ["connectorId"] = "cal-1", ["directGoogleCalendarSdk"] = true };

        var rejected = await fixture.Service.SyncCalendarAsync(direct, "calendar-1");
        var accepted = await fixture.Service.SyncCalendarAsync(new Dictionary<string, object?> { ["studentId"] = "student-1", ["lessonId"] = "lesson-1", ["connectorId"] = "cal-1" }, "calendar-2");

        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Equal("CALENDAR_SDK_DIRECT_IMPORT_REJECTED", rejected.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.Equal(AiSafetyModerationConstants.FlowId, accepted.Data?.Body["source_flow"]);
    }

    [Fact]
    public async Task CF24_2_ProgressRequiresSafetyTokenAndSkipNotFailWhenDisabled()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "student");
        var skipped = await fixture.Service.TrackProgressAsync(new Dictionary<string, object?> { ["studentId"] = "student-1", ["lessonId"] = "lesson-1" }, "progress-1");
        var blocked = await fixture.Service.TrackProgressAsync(new Dictionary<string, object?> { ["studentId"] = "student-1", ["lessonId"] = "lesson-1", ["freedomConfigEnabled"] = true }, "progress-2");
        var accepted = await fixture.Service.TrackProgressAsync(new Dictionary<string, object?> { ["studentId"] = "student-1", ["lessonId"] = "lesson-1", ["freedomConfigEnabled"] = true, ["safetyGateToken"] = "sgt-local", ["progressPercent"] = 75 }, "progress-3");

        Assert.Equal("SKIPPED", skipped.Data?.Body["status"]);
        Assert.Equal(403, blocked.Data?.HttpStatus);
        Assert.Equal("SAFETY_GATE_TOKEN_REQUIRED", blocked.Data?.Body["code"]);
        Assert.Equal(201, accepted.Data?.HttpStatus);
    }

    [Fact]
    public async Task DashboardIsTenantIsolated()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.EvaluateConsentAsync(Consent("student-1", "GRANTED"), "consent-1");
        }

        using var _ = fixture.PushContext("globex", "tenant_admin");
        var dashboard = await fixture.Service.GetDashboardAsync();

        Assert.Empty(dashboard.Data?.ConsentGates ?? []);
    }

    [Fact]
    public async Task TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Consent("student-1", "GRANTED");
        request["tenantId"] = "globex";

        var result = await fixture.Service.EvaluateConsentAsync(request, "consent-1");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("TENANT_SCOPE_PAYLOAD_REJECTED", result.Data?.Body["code"]);
    }

    [Fact]
    public void CodeReview_NoProviderSdkImportsNoFreeTenantParametersAndMicroserviceBasePresent()
    {
        var root = FindRepoRoot();
        var flowRoot = Path.Combine(root, "flows", "FLOW-24-ai-safety-moderation", "server");
        var source = string.Join('\n', Directory.GetFiles(flowRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(".Tests", StringComparison.Ordinal)).Select(File.ReadAllText));

        Assert.DoesNotContain("Elastic.Clients.Elasticsearch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StackExchange.Redis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("googleapis", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
        Assert.Contains("MicroserviceBase<AiSafetyModerationService>", source, StringComparison.Ordinal);
        Assert.Contains("DataProcessResult<AiSafetyModerationOutcome>", source, StringComparison.Ordinal);
        Assert.Contains("AppendAsync", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> Consent(string studentId, string status) => new()
    {
        ["studentId"] = studentId,
        ["consentType"] = "learning",
        ["status"] = status
    };

    private static Dictionary<string, object?> Lesson(string studentId, string lessonId) => new()
    {
        ["studentId"] = studentId,
        ["lessonId"] = lessonId,
        ["prompt"] = "fractions practice with supportive hints"
    };

    private static Dictionary<string, object?> Quiz() => new()
    {
        ["studentId"] = "student-1",
        ["quizId"] = "quiz-1",
        ["answers"] = new Dictionary<string, object?> { ["q1"] = "A", ["q2"] = "B" },
        ["answerKey"] = new Dictionary<string, object?> { ["q1"] = "A", ["q2"] = "C" }
    };

    private static Dictionary<string, object?> Streak() => new()
    {
        ["studentId"] = "student-1",
        ["profile"] = new Dictionary<string, object?> { ["timezone"] = "UTC" }
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps() =>
        AiSafetyModerationConstants.RequiredTaskTypes.Select(task => new Dictionary<string, object?>
        {
            ["id"] = $"step-{task}",
            ["taskTypeId"] = task,
            ["name"] = $"AI safety moderation plan step {task}",
            ["responsibility"] = $"Single responsibility for {task}"
        }).ToArray();

    private static IReadOnlyDictionary<string, object?> Tenant(string tenantId) => new Dictionary<string, object?> { ["tenantId"] = tenantId };

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Xiigen.Flows.slnx"))) current = current.Parent;
        return current?.FullName ?? AppContext.BaseDirectory;
    }

    private sealed record FlowFixture(
        AiSafetyModerationService Service,
        InMemoryDocumentStore Documents,
        InMemoryIdempotencyStore Idempotency,
        InMemoryOutboxStore Outbox,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new AiSafetyModerationService(documents, idempotency, outbox, tenant, correlation, NullLogger<AiSafetyModerationService>.Instance);
            return new FlowFixture(service, documents, idempotency, outbox, tenant, correlation);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlationId = Guid.NewGuid().ToString("n");
            var correlationScope = CorrelationAccessor.Push(new CorrelationContext(correlationId, "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantAccessor.Push(new TenantContext(tenantId, "test-user", new HashSet<string>([role], StringComparer.Ordinal), correlationId, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(correlationScope, tenantScope);
        }
    }

    private sealed class CompositeScope : IDisposable
    {
        private readonly IDisposable _first;
        private readonly IDisposable _second;
        private bool _disposed;

        public CompositeScope(IDisposable first, IDisposable second)
        {
            _first = first;
            _second = second;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _second.Dispose();
            _first.Dispose();
            _disposed = true;
        }
    }
}
