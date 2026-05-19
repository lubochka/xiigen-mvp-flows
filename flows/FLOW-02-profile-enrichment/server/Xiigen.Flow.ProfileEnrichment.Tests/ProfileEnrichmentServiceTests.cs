using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.ProfileEnrichment.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.ProfileEnrichment.Tests;

public sealed class ProfileEnrichmentServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = ProfileEnrichmentBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ProfileEnrichmentConstants.RuleIds, ids);
        Assert.All(ProfileEnrichmentBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(ProfileEnrichmentConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0201_SubmitQuestionnaire_WritesPrivateProfileAndGlobalMatchingRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-1", "technology"),
            ifMatch: null,
            idempotencyKey: "profile-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);

        var privateProfiles = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["user_id"] = "usr-1" });
        var privateProfile = Assert.Single(privateProfiles.Data ?? []);
        Assert.Equal("PRIVATE", privateProfile.Body["knowledge_scope"]);
        Assert.True(privateProfile.Body.ContainsKey("questionnaire"));

        var globalProfiles = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.MatchingProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["user_id"] = "usr-1" });
        var globalProfile = Assert.Single(globalProfiles.Data ?? []);
        Assert.Equal("GLOBAL", globalProfile.Body["knowledge_scope"]);
        Assert.False(globalProfile.Body.ContainsKey("questionnaire"));
        Assert.Equal("technology", globalProfile.Body["industry_code"]);
        Assert.Equal("startup", globalProfile.Body["business_stage"]);
        Assert.Equal("remote", globalProfile.Body["location_proximity"]);
        Assert.Equal("small", globalProfile.Body["team_size_tier"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ProfileEnrichmentConstants.EnrichmentDataReadyEvent);
    }

    [Fact]
    public async Task CF0202_MatchingTimeout_IsPartialSuccessModeNeverFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-partial", "finance", simulateMatchingTimeout: true),
            ifMatch: null,
            idempotencyKey: "partial-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.True((bool)result.Data!.Body["partialResults"]!);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ProfileEnrichmentConstants.NeedsMoreDataEvent);
    }

    [Fact]
    public async Task CF0203_PersonalizationCompletedLiteral_IsEmittedAndOnboardingCompletedIsNeverEmitted()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-event", "retail"),
            ifMatch: null,
            idempotencyKey: "event-1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent, result.Data?.Body["completionEventName"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ProfileEnrichmentConstants.PersonalizationCompletedMachineEvent);
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type.Contains("OnboardingCompleted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CF0204_RecordsAndReadsAreTenantScoped()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("tenant-a", "tenant_user"))
        {
            var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
                Questionnaire("usr-shared", "technology"),
                ifMatch: null,
                idempotencyKey: "tenant-a");
            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        using (fixture.PushContext("tenant-b", "tenant_user"))
        {
            var read = await fixture.ProfileEnrichment.GetPersonalizationAsync("usr-shared");
            Assert.True(read.IsSuccess, read.Error?.Message);
            Assert.Empty(read.Data!.ContentItems);

            var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
                Questionnaire("usr-shared", "finance"),
                ifMatch: null,
                idempotencyKey: "tenant-b");
            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        var tenantA = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "tenant-a", ["user_id"] = "usr-shared" });
        var tenantB = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "tenant-b", ["user_id"] = "usr-shared" });
        Assert.Single(tenantA.Data ?? []);
        Assert.Single(tenantB.Data ?? []);
    }

    [Fact]
    public async Task CF854_ConcurrentSameResource_AllowsOneWinnerAndRejectsOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-race", "technology", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-a");
        var second = fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-race", "technology", simulateSlowWrite: true),
            ifMatch: null,
            idempotencyKey: "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF855_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-replay", "healthcare"),
            ifMatch: null,
            idempotencyKey: "same-key");
        var second = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-replay", "healthcare"),
            ifMatch: null,
            idempotencyKey: "same-key");

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["profileId"], second.Data?.Body["profileId"]);

        var privateProfiles = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp", ["user_id"] = "usr-replay" });
        Assert.Single(privateProfiles.Data ?? []);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Single(outbox.Data ?? [], record => record.Event.Type == ProfileEnrichmentConstants.EnrichmentDataReadyEvent);
    }

    [Fact]
    public async Task CF856_BoundaryValidationRejectsMissingPrimaryFieldAndLeavesNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            new Dictionary<string, object?> { ["userId"] = "usr-invalid", ["industry"] = "" },
            ifMatch: null,
            idempotencyKey: "invalid-1");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);

        var privateProfiles = await fixture.Documents.SearchAsync(
            ProfileEnrichmentConstants.BusinessProfiles,
            new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Empty(privateProfiles.Data ?? []);
    }

    [Fact]
    public async Task CF857_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.ProfileEnrichment.SubmitQuestionnaireAsync(
            Questionnaire("usr-timeout", "finance", simulateFabricTimeout: true),
            ifMatch: null,
            idempotencyKey: "timeout-1");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        Assert.Equal(5000, result.Error?.Details?["retryAfterMs"]);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == "xiigen.profile-enrichment.timeout-retained.v1");
    }

    private static Dictionary<string, object?> Questionnaire(
        string userId,
        string industry,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false,
        bool simulateMatchingTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["industry"] = industry,
            ["businessStage"] = "startup",
            ["location"] = "remote",
            ["teamSize"] = "small"
        };
        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        if (simulateFabricTimeout)
        {
            request["simulateFabricTimeout"] = true;
        }

        if (simulateMatchingTimeout)
        {
            request["simulateMatchingTimeout"] = true;
        }

        return request;
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            ProfileEnrichmentService profileEnrichment,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            ProfileEnrichment = profileEnrichment;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public ProfileEnrichmentService ProfileEnrichment { get; }
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
            var profileEnrichment = new ProfileEnrichmentService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<ProfileEnrichmentService>.Instance);

            return new FlowFixture(
                documents,
                outbox,
                freedom,
                profileEnrichment,
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
