using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.Marketplace.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.Marketplace.Tests;

public sealed class MarketplaceServiceTests
{
    [Fact]
    public void P11_Rules_ExistingAndNewFunctionalCoverageRules_AreRegistered()
    {
        var ids = MarketplaceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(MarketplaceConstants.RuleIds));
        Assert.Equal(12, MarketplaceBfaRuleRegistry.Rules.Count);
        Assert.All(MarketplaceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(MarketplaceConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF0801_AuditIsWrittenBeforeModerationPriceListingAndEvents()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishListingAsync(Listing("listing-audit"), null, "audit-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        var audits = await fixture.Documents.SearchAsync(MarketplaceConstants.PublicationAudits, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        var listings = await fixture.Documents.SearchAsync(MarketplaceConstants.Listings, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(audits.Data ?? []);
        Assert.Single(listings.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Equal(MarketplaceConstants.SaveRequestedEvent, outbox.Data?.First().Event.Type);
    }

    [Fact]
    public async Task CF0802_IdempotencyReplay_ReturnsCachedResponseWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.PublishListingAsync(Listing("listing-replay"), null, "same-key");
        var second = await fixture.Service.PublishListingAsync(Listing("listing-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        var audits = await fixture.Documents.SearchAsync(MarketplaceConstants.PublicationAudits, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        var listings = await fixture.Documents.SearchAsync(MarketplaceConstants.Listings, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(audits.Data ?? []);
        Assert.Single(listings.Data ?? []);
    }

    [Fact]
    public async Task CF0803_FeedCounterUsesDocumentFabric_NotProcessMemory()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.Service.PublishListingAsync(Listing("listing-feed"), null, "feed-key");

        var feed = await fixture.Documents.SearchAsync(MarketplaceConstants.FeedBatches, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(feed.Data ?? []);
        Assert.Equal(1, Convert.ToInt32(feed.Data![0].Body["listing_count"]));
    }

    [Fact]
    public async Task CF0804_CrossTenantCatalogRead_DoesNotLeakListings()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.PublishListingAsync(Listing("listing-tenant"), null, "tenant-key");
        }

        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            var catalog = await fixture.Service.GetCatalogAsync();
            Assert.True(catalog.IsSuccess, catalog.Error?.Message);
            Assert.Empty(catalog.Data?.Listings ?? []);
        }
    }

    [Fact]
    public async Task CF895_OptimisticConcurrency_AllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = fixture.Service.PublishListingAsync(Listing("listing-race", simulateSlowWrite: true), "v1", "race-a");
        var second = fixture.Service.PublishListingAsync(Listing("listing-race", simulateSlowWrite: true), "v1", "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF896_ThreeRetriesWithSameIdempotencyKey_ReturnCachedPublication()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = await fixture.Service.PublishListingAsync(Listing("listing-three-retries"), null, "three-retry-key");
        var second = await fixture.Service.PublishListingAsync(Listing("listing-three-retries"), null, "three-retry-key");
        var third = await fixture.Service.PublishListingAsync(Listing("listing-three-retries"), null, "three-retry-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(200, third.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["version"], second.Data?.Body["version"]);
        Assert.Equal(second.Data?.Body["version"], third.Data?.Body["version"]);
    }

    [Fact]
    public async Task CF897_BoundaryValidation_BlankTitleDoesNotWriteListingButKeepsAudit()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Listing("listing-blank-title");
        request["title"] = "";

        var result = await fixture.Service.PublishListingAsync(request, null, "blank-title-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        var audits = await fixture.Documents.SearchAsync(MarketplaceConstants.PublicationAudits, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        var listings = await fixture.Documents.SearchAsync(MarketplaceConstants.Listings, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(audits.Data ?? []);
        Assert.Empty(listings.Data ?? []);
    }

    [Fact]
    public async Task CF898_FabricTimeout_Returns504AndRetainsOutboxRecovery()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishListingAsync(Listing("listing-timeout", simulateFabricTimeout: true), null, "timeout-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == MarketplaceConstants.TimeoutRetainedEvent);
    }

    [Fact]
    public async Task CF899_ConcurrencyDuplicateCoverage_UsesTenantScopedListingKey()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("northwind", "tenant_admin");

        var first = fixture.Service.PublishListingAsync(Listing("listing-race-2", simulateSlowWrite: true), "v1", "race-2-a");
        var second = fixture.Service.PublishListingAsync(Listing("listing-race-2", simulateSlowWrite: true), "v1", "race-2-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF900_IdempotencyDuplicateCoverage_StoresOneCatalogRow()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.Service.PublishListingAsync(Listing("listing-one-catalog-row"), null, "one-catalog-key");
        await fixture.Service.PublishListingAsync(Listing("listing-one-catalog-row"), null, "one-catalog-key");
        var catalog = await fixture.Service.GetCatalogAsync();

        Assert.True(catalog.IsSuccess, catalog.Error?.Message);
        Assert.Equal(1, catalog.Data?.Count);
    }

    [Fact]
    public async Task CF901_ZeroPriceIsAcceptedAsFreeButNegativePriceFails()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var free = Listing("listing-free");
        free["price"] = 0;
        var freeResult = await fixture.Service.PublishListingAsync(free, null, "free-key");
        var negative = Listing("listing-negative");
        negative["price"] = -1;
        var negativeResult = await fixture.Service.PublishListingAsync(negative, null, "negative-key");

        Assert.Equal(201, freeResult.Data?.HttpStatus);
        Assert.Equal(400, negativeResult.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", negativeResult.Data?.Body["code"]);
    }

    [Fact]
    public async Task CF902_TimeoutDuplicateCoverage_DoesNotStoreListing()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        await fixture.Service.PublishListingAsync(Listing("listing-timeout-no-state", simulateFabricTimeout: true), null, "timeout-no-state-key");
        var listings = await fixture.Documents.SearchAsync(MarketplaceConstants.Listings, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });

        Assert.Empty(listings.Data ?? []);
    }

    [Fact]
    public async Task ModerationReject_SavesDraftAsSuccessfulOutcome_NotBusinessFailure()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Listing("listing-rejected");
        request["moderationSignal"] = "REJECT";

        var result = await fixture.Service.PublishListingAsync(request, null, "reject-key");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("DRAFT", result.Data?.Body["status"]);
        var drafts = await fixture.Documents.SearchAsync(MarketplaceConstants.Drafts, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(drafts.Data ?? []);
    }

    [Fact]
    public async Task ModerationUncertain_RoutesHumanReviewAndStillStoresReviewRequiredListing()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Listing("listing-review");
        request["moderationSignal"] = "UNCERTAIN";

        var result = await fixture.Service.PublishListingAsync(request, null, "review-key");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("REVIEW_REQUIRED", result.Data?.Body["status"]);
        var reviews = await fixture.Documents.SearchAsync(MarketplaceConstants.ModerationReviews, new Dictionary<string, object?> { ["tenant_id"] = "acme-corp" });
        Assert.Single(reviews.Data ?? []);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? [], record => record.Event.Type == MarketplaceConstants.HumanReviewRequestedEvent);
    }

    [Fact]
    public async Task Analytics_UsesInquiriesOverViewsOrOne_AndNeverStoresViewerIds()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Listing("listing-analytics");
        request["views"] = 0;
        request["inquiries"] = 4;
        request["viewerIds"] = new[] { "user-a", "user-b" };

        await fixture.Service.PublishListingAsync(request, null, "analytics-key");
        var analytics = await fixture.Service.GetAnalyticsAsync();

        Assert.True(analytics.IsSuccess, analytics.Error?.Message);
        Assert.Equal(4, analytics.Data?.ConversionRate);
        Assert.False(analytics.Data?.ViewerIdsStored);
    }

    [Fact]
    public async Task PriceCap_FromRequestRejectsAboveCapButAllowsWithinCap()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var high = Listing("listing-high-price");
        high["price"] = 7000;
        high["maxPriceCap"] = 5000;
        var within = Listing("listing-within-cap");
        within["price"] = 3000;
        within["maxPriceCap"] = 5000;

        var highResult = await fixture.Service.PublishListingAsync(high, null, "high-price-key");
        var withinResult = await fixture.Service.PublishListingAsync(within, null, "within-price-key");

        Assert.True(highResult.IsFailure || highResult.Data?.HttpStatus == 400);
        Assert.Equal(201, withinResult.Data?.HttpStatus);
    }

    private static Dictionary<string, object?> Listing(
        string listingId,
        bool simulateSlowWrite = false,
        bool simulateFabricTimeout = false)
    {
        var request = new Dictionary<string, object?>
        {
            ["listingId"] = listingId,
            ["sellerId"] = "seller-101",
            ["title"] = $"Curated marketplace offer {listingId}",
            ["category"] = "services",
            ["description"] = "A service-forward listing with buyer-safe copy and cooperator potential.",
            ["price"] = 1200,
            ["currency"] = "USD",
            ["inventory"] = 12,
            ["views"] = 20,
            ["inquiries"] = 5
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
            MarketplaceService service,
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
        public MarketplaceService Service { get; }
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
            var service = new MarketplaceService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<MarketplaceService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
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

