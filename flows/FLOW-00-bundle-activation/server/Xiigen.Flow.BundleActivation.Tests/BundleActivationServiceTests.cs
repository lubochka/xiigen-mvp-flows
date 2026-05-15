using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.BundleActivation.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Tests;

public sealed class BundleActivationServiceTests
{
    [Fact]
    public void P11_Rules_CF842ThroughCF849_AreRegistered()
    {
        var ids = BundleBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToArray();

        Assert.Equal(BundleActivationConstants.RuleIds.Order(), ids.Order());
        Assert.All(BundleBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal("FLOW-00", rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
        });
    }

    [Fact]
    public async Task ActivateAsync_StoresBeforeOutboxAndRunsDryRunBeforeFull()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();

        var result = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: "1",
            idempotencyKey: Guid.NewGuid().ToString("n"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.True(result.Data?.Stored);
        Assert.True(result.Data?.OutboxAppended);
        Assert.Equal(
            new[] { ("FLOW-01", "DRY_RUN"), ("FLOW-01", "FULL"), ("FLOW-02", "DRY_RUN"), ("FLOW-02", "FULL") },
            fixture.FlowBootstrapper.Calls);

        var activation = await fixture.Documents.GetAsync(BundleActivationConstants.BundleActivations, "BDL-core-platform-v2");
        Assert.NotNull(activation.Data);

        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? Array.Empty<OutboxRecord>(), record => record.Event.Type == "bundle.activated");
    }

    [Fact]
    public async Task ActivateAsync_TreatsEmptyIfMatchAsCurrentVersion()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();

        var result = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: string.Empty,
            idempotencyKey: Guid.NewGuid().ToString("n"));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(201, result.Data?.HttpStatus);
    }

    [Fact]
    public async Task ActivateAsync_ReplaysCachedResponseForSameIdempotencyKey()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();
        var key = Guid.NewGuid().ToString("n");

        var first = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: "1",
            idempotencyKey: key);
        var second = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: "1",
            idempotencyKey: key);

        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
    }

    [Fact]
    public async Task ActivateAsync_ReturnsConcurrentUpdateForStaleVersion()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();
        var winner = fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: "1",
            idempotencyKey: "winner");
        var loser = fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2" },
            ifMatch: "1",
            idempotencyKey: "loser");

        var results = await Task.WhenAll(winner, loser);

        Assert.Contains(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Contains(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task ActivateAsync_RejectsBoundaryValidationWithoutPartialWrite()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();

        var result = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = string.Empty },
            ifMatch: "1",
            idempotencyKey: "bad-input");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
        var activation = await fixture.Documents.GetAsync(BundleActivationConstants.BundleActivations, "BDL-core-platform-v2");
        Assert.Null(activation.Data);
    }

    [Fact]
    public async Task ActivateAsync_TimeoutRetainsOutboxRecord()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();

        var result = await fixture.Service.ActivateAsync(
            new Dictionary<string, object?> { ["bundleId"] = "BDL-core-platform-v2", ["simulateFabricTimeout"] = true },
            ifMatch: "1",
            idempotencyKey: "timeout");

        Assert.True(result.IsFailure);
        Assert.Equal("TIMEOUT", result.Error?.Code);
        var outbox = await fixture.Outbox.ListAsync();
        Assert.Contains(outbox.Data ?? Array.Empty<OutboxRecord>(), record => record.Event.Type == "bundle.activation.timeout");
    }

    [Fact]
    public async Task StatusTracker_UpdatesAllBundlesContainingRegeneratedFlow()
    {
        var fixture = await FlowFixture.CreateAsync();
        using var _ = fixture.PushContext();
        await fixture.Documents.UpsertAsync(
            BundleActivationConstants.SolutionBundles,
            "BDL-secondary",
            new Dictionary<string, object?>
            {
                ["bundleId"] = "BDL-secondary",
                ["requiredFlows"] = new[] { "FLOW-02" },
                ["minFlowVersions"] = new Dictionary<string, object?> { ["FLOW-02"] = "v2" },
                ["status"] = "ACTIVE"
            });

        var result = await fixture.StatusTracker.OnFlowRegeneratedAsync("FLOW-02", "v1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Data);
        var primary = await fixture.Documents.GetAsync(BundleActivationConstants.SolutionBundles, "BDL-core-platform-v2");
        var secondary = await fixture.Documents.GetAsync(BundleActivationConstants.SolutionBundles, "BDL-secondary");
        Assert.Equal("DEGRADED", primary.Data?.Body["status"]);
        Assert.Equal("DEGRADED", secondary.Data?.Body["status"]);
    }

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFlowBootstrapper flowBootstrapper,
            BundleActivationService service,
            BundleStatusTrackerService statusTracker,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            FlowBootstrapper = flowBootstrapper;
            Service = service;
            StatusTracker = statusTracker;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFlowBootstrapper FlowBootstrapper { get; }
        public BundleActivationService Service { get; }
        public BundleStatusTrackerService StatusTracker { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public IDisposable PushContext()
        {
            var tenant = TenantContextAccessor.Push(new TenantContext("acme-corp", "user-1", new HashSet<string> { "tenant_admin" }, "corr-test", "test", DateTimeOffset.UtcNow));
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext("corr-test", "test", DateTimeOffset.UtcNow));
            return new CompositeScope(tenant, correlation);
        }

        public static async Task<FlowFixture> CreateAsync()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var bootstrapper = new InMemoryFlowBootstrapper();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var validator = new BundleValidatorService(documents, outbox, tenantAccessor, correlationAccessor, NullLogger<BundleValidatorService>.Instance);
            var service = new BundleActivationService(documents, idempotency, outbox, bootstrapper, validator, tenantAccessor, correlationAccessor, NullLogger<BundleActivationService>.Instance);
            var statusTracker = new BundleStatusTrackerService(documents, outbox, tenantAccessor, correlationAccessor, NullLogger<BundleStatusTrackerService>.Instance);
            var fixture = new FlowFixture(documents, outbox, bootstrapper, service, statusTracker, tenantAccessor, correlationAccessor);
            await fixture.SeedAsync();
            return fixture;
        }

        private async Task SeedAsync()
        {
            await Documents.UpsertAsync(
                BundleActivationConstants.SolutionBundles,
                "BDL-core-platform-v2",
                new Dictionary<string, object?>
                {
                    ["bundleId"] = "BDL-core-platform-v2",
                    ["requiredFlows"] = new[] { "FLOW-01", "FLOW-02" },
                    ["minFlowVersions"] = new Dictionary<string, object?> { ["FLOW-01"] = "v2", ["FLOW-02"] = "v2" },
                    ["defaultFreedomConfig"] = new Dictionary<string, object?> { ["bundle00_activation_timeout_ms"] = 30000 },
                    ["status"] = "PENDING"
                });
            await Documents.UpsertAsync(BundleActivationConstants.FlowLifecycle, "FLOW-01", new Dictionary<string, object?> { ["flowId"] = "FLOW-01", ["version"] = "v1", ["status"] = "READY" });
            await Documents.UpsertAsync(BundleActivationConstants.FlowLifecycle, "FLOW-02", new Dictionary<string, object?> { ["flowId"] = "FLOW-02", ["version"] = "v1", ["status"] = "READY" });
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
