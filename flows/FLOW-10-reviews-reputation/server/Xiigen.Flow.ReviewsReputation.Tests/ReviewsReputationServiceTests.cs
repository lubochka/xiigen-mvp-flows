using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.ReviewsReputation.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.ReviewsReputation.Tests;

public sealed class ReviewsReputationServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = ReviewsReputationBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(ReviewsReputationConstants.RuleIds));
        Assert.Equal(10, ReviewsReputationBfaRuleRegistry.Rules.Count);
        Assert.All(ReviewsReputationBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(ReviewsReputationConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF1001_EligibilityRunsBeforeValidationIdempotencyAuditAndReviewWrites()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Review("reviewer-a", "profile-a", rating: 9);
        request["eligibilityConfirmed"] = false;
        request["checkInConfirmed"] = false;
        request["purchaseConfirmed"] = false;

        var result = await fixture.Service.SubmitReviewAsync(request, null, "eligibility-first");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("ELIGIBILITY_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(ReviewsReputationConstants.Reviews, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(ReviewsReputationConstants.ReviewAudit, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF1001_ServerDerivesReviewIdAndAuditExcludesReviewText()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Review("reviewer-a", "profile-a", rating: 5);
        request["reviewId"] = "client-supplied-review-id";
        request["reviewText"] = "This detailed private review text must stay out of audit and queue payloads.";

        var result = await fixture.Service.SubmitReviewAsync(request, null, "submit-a");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.NotEqual("client-supplied-review-id", result.Data?.Body["reviewId"]);
        Assert.True((bool)result.Data!.Body["serverDerivedReviewId"]!);
        var audits = await fixture.Documents.SearchAsync(ReviewsReputationConstants.ReviewAudit, Tenant("acme-corp"));
        Assert.Single(audits.Data ?? []);
        Assert.DoesNotContain(audits.Data![0].Body.Keys, key => key.Contains("text", StringComparison.OrdinalIgnoreCase));
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReviewAcceptedEvent);
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReviewSubmissionCompletedEvent);
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Data.Values.Any(value => Convert.ToString(value)?.Contains("private review", StringComparison.OrdinalIgnoreCase) == true));
    }

    [Fact]
    public async Task CF1002_ModerationHasPassRejectAndUncertainHumanQueuePaths()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var passReview = await SubmitAcceptedReviewAsync(fixture, "reviewer-pass", "profile-mod");
        var rejectReview = await SubmitAcceptedReviewAsync(fixture, "reviewer-reject", "profile-mod");
        var pendingReview = await SubmitAcceptedReviewAsync(fixture, "reviewer-pending", "profile-mod");

        var pass = await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = passReview, ["confidence"] = 0.92 }, "moderate-pass");
        var reject = await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = rejectReview, ["confidence"] = 0.10 }, "moderate-reject");
        var pending = await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = pendingReview, ["confidence"] = 0.50 }, "moderate-pending");

        Assert.Equal("PASS", pass.Data?.Body["moderationPath"]);
        Assert.Equal("REJECT", reject.Data?.Body["moderationPath"]);
        Assert.Equal("UNCERTAIN", pending.Data?.Body["moderationPath"]);
        Assert.Equal("PENDING", pending.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(ReviewsReputationConstants.ModerationQueue, Tenant("acme-corp"))).Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReviewFlaggedForHumanEvent);
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReviewRejectedEvent);
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReviewPublishedEvent);
    }

    [Fact]
    public async Task CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var publishedReview = await SubmitAcceptedReviewAsync(fixture, "reviewer-score-a", "profile-score", 5);
        var rejectedReview = await SubmitAcceptedReviewAsync(fixture, "reviewer-score-b", "profile-score", 1);

        await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = publishedReview, ["confidence"] = 0.95 }, "score-pass");
        await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = rejectedReview, ["confidence"] = 0.05 }, "score-reject");
        var score = await fixture.Service.GetReputationAsync("profile-score");

        Assert.Equal(5.0d, score.Data?.Score);
        Assert.Equal(1, score.Data?.PublishedReviewCount);

        await fixture.Service.RetractReviewAsync(new Dictionary<string, object?> { ["reviewId"] = publishedReview }, null, "retract-score");
        var afterRetraction = await fixture.Service.GetReputationAsync("profile-score");

        Assert.Equal(1.0d, afterRetraction.Data?.Score);
        Assert.Equal(0, afterRetraction.Data?.PublishedReviewCount);
    }

    [Fact]
    public async Task CF1004_OwnerResponseRequiresOwnershipAllowsRevisionAfterPolicyRejectAndDoesNotUpdateReputation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var reviewId = await SubmitAcceptedReviewAsync(fixture, "reviewer-response", "profile-response");

        var notOwner = await fixture.Service.SubmitResponseAsync(Response(reviewId, ownerConfirmed: false), "not-owner");
        var rejected = Response(reviewId, ownerConfirmed: true);
        rejected["contentPolicyPassed"] = false;
        var policy = await fixture.Service.SubmitResponseAsync(rejected, "policy-reject");
        var revision = Response(reviewId, ownerConfirmed: true);
        revision["allowRevision"] = true;
        var published = await fixture.Service.SubmitResponseAsync(revision, "response-publish");

        Assert.Equal(403, notOwner.Data?.HttpStatus);
        Assert.False((bool)notOwner.Data!.Body["revisionAllowed"]!);
        Assert.Equal(422, policy.Data?.HttpStatus);
        Assert.True((bool)policy.Data!.Body["revisionAllowed"]!);
        Assert.Equal(201, published.Data?.HttpStatus);
        var audits = await fixture.Documents.SearchAsync(ReviewsReputationConstants.ResponseAudit, Tenant("acme-corp"));
        Assert.Single(audits.Data ?? []);
        Assert.DoesNotContain(audits.Data![0].Body.Keys, key => key.Contains("text", StringComparison.OrdinalIgnoreCase));
        var outbox = await fixture.Outbox.ListAsync();
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Type == ReviewsReputationConstants.ReputationUpdatedEvent && Equals(record.Event.Subject, reviewId));
        Assert.DoesNotContain(outbox.Data ?? [], record => record.Event.Data.Values.Any(value => Convert.ToString(value)?.Contains("Owner private response", StringComparison.OrdinalIgnoreCase) == true));
    }

    [Fact]
    public async Task CF1004_CrossTenantReviewSearchDoesNotLeak()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_user"))
        {
            await fixture.Service.SubmitReviewAsync(Review("reviewer-tenant", "profile-tenant"), null, "tenant-a-review");
        }

        using (fixture.PushContext("northwind", "tenant_user"))
        {
            var reviews = await fixture.Service.GetReviewsAsync();
            Assert.True(reviews.IsSuccess, reviews.Error?.Message);
            Assert.Empty(reviews.Data?.Reviews ?? []);
        }
    }

    [Fact]
    public async Task CF913_ConcurrentSubmission_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var first = fixture.Service.SubmitReviewAsync(Review("reviewer-race", "profile-race", simulateSlowWrite: true), "1", "race-a");
        var second = fixture.Service.SubmitReviewAsync(Review("reviewer-race", "profile-race", simulateSlowWrite: true), "1", "race-b");

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF914_IdempotencyReplayReturnsCachedReviewAndNoDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.SubmitReviewAsync(Review("reviewer-retry", "profile-retry"), null, "same-key");
        var second = await fixture.Service.SubmitReviewAsync(Review("reviewer-retry", "profile-retry"), null, "same-key");
        var third = await fixture.Service.SubmitReviewAsync(Review("reviewer-retry", "profile-retry"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(200, third.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["reviewId"], second.Data?.Body["reviewId"]);
        Assert.Single((await fixture.Documents.SearchAsync(ReviewsReputationConstants.Reviews, Tenant("acme-corp"))).Data ?? []);
        Assert.Equal(2, (await fixture.Outbox.ListAsync()).Data?.Count);
    }

    [Fact]
    public async Task CF915_InvalidRatingReturnsFieldDetailsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Review("reviewer-invalid", "profile-invalid", rating: 6);

        var result = await fixture.Service.SubmitReviewAsync(request, null, "invalid-rating-key");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(ReviewsReputationConstants.Reviews, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(ReviewsReputationConstants.ReviewAudit, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF916_FabricTimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = Review("reviewer-timeout", "profile-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.SubmitReviewAsync(request, null, "timeout-key");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == ReviewsReputationConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(ReviewsReputationConstants.Reviews, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF917_SecondaryResponseConcurrency_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var reviewId = await SubmitAcceptedReviewAsync(fixture, "reviewer-secondary", "profile-secondary");

        var first = fixture.Service.SubmitResponseAsync(Response(reviewId, ownerConfirmed: true, simulateSlowWrite: true), "response-race-a");
        var second = fixture.Service.SubmitResponseAsync(Response(reviewId, ownerConfirmed: true, simulateSlowWrite: true), "response-race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF918_ReputationRecomputeReplayReturnsCachedAggregate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var reviewId = await SubmitAcceptedReviewAsync(fixture, "reviewer-cache", "profile-cache", 4);
        await fixture.Service.ModerateReviewAsync(new Dictionary<string, object?> { ["reviewId"] = reviewId, ["confidence"] = 0.95 }, "moderate-cache");

        var first = await fixture.Service.RecalculateReputationAsync(new Dictionary<string, object?> { ["targetEntityId"] = "profile-cache", ["targetEntityType"] = "profile" }, "score-key");
        var second = await fixture.Service.RecalculateReputationAsync(new Dictionary<string, object?> { ["targetEntityId"] = "profile-cache", ["targetEntityType"] = "profile" }, "score-key");

        Assert.Equal(200, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["score"], second.Data?.Body["score"]);
        Assert.Single((await fixture.Documents.SearchAsync(ReviewsReputationConstants.ReputationScores, Tenant("acme-corp"))).Data ?? []);
    }

    private static async Task<string> SubmitAcceptedReviewAsync(FlowFixture fixture, string reviewerId, string targetEntityId, int rating = 5)
    {
        var result = await fixture.Service.SubmitReviewAsync(Review(reviewerId, targetEntityId, rating), null, $"key-{reviewerId}-{targetEntityId}");
        Assert.Equal(201, result.Data?.HttpStatus);
        return Convert.ToString(result.Data?.Body["reviewId"])!;
    }

    private static Dictionary<string, object?> Review(
        string reviewerId,
        string targetEntityId,
        int rating = 5,
        bool simulateSlowWrite = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["reviewerId"] = reviewerId,
            ["targetEntityId"] = targetEntityId,
            ["targetEntityType"] = "profile",
            ["rating"] = rating,
            ["reviewText"] = $"Helpful review from {reviewerId}",
            ["checkInConfirmed"] = true,
            ["purchaseConfirmed"] = true,
            ["eligibilityConfirmed"] = true
        };

        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        return request;
    }

    private static Dictionary<string, object?> Response(string reviewId, bool ownerConfirmed, bool simulateSlowWrite = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["reviewId"] = reviewId,
            ["ownerId"] = "owner-1",
            ["ownerConfirmed"] = ownerConfirmed,
            ["responseText"] = "Owner private response text that must not leave the private record.",
            ["contentPolicyPassed"] = true
        };

        if (simulateSlowWrite)
        {
            request["simulateSlowWrite"] = true;
        }

        return request;
    }

    private static Dictionary<string, object?> Tenant(string tenantId) => new() { ["tenant_id"] = tenantId };

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            ReviewsReputationService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public ReviewsReputationService Service { get; }
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
            var service = new ReviewsReputationService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<ReviewsReputationService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlation = $"corr-{Guid.NewGuid():N}";
            var tenant = TenantContextAccessor.Push(new TenantContext(
                tenantId,
                role == "anonymous" || role == "public_marketplace_visitor" ? null : "user-1",
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
