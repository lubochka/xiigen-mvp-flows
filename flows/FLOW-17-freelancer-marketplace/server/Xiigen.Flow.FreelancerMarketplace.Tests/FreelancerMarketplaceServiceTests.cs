using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FreelancerMarketplace.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.FreelancerMarketplace.Tests;

public sealed class FreelancerMarketplaceServiceTests
{
    [Fact]
    public void P11_Rules_AllFreelancerMarketplaceRules_AreRegistered()
    {
        var ids = FreelancerMarketplaceBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(FreelancerMarketplaceConstants.RuleIds));
        Assert.Equal(14, FreelancerMarketplaceBfaRuleRegistry.Rules.Count);
        Assert.All(FreelancerMarketplaceBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(FreelancerMarketplaceConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public async Task CF17_1_AcceptGigValidatesBolaLocksChecksBidAuditsThenEmits()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.AcceptGigAsync(AcceptGig("gig-a", "bid-a"), null, "accept-a");
        var locks = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigAcceptLocks, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigAudits, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("ACCEPTED", result.Data?.Body["status"]);
        Assert.Single(locks);
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "GIG_ACCEPTED");
        Assert.Equal(FreelancerMarketplaceConstants.GigAcceptedEvent, outbox.Last().Event.Type);
    }

    [Fact]
    public async Task CF17_1_BolaMismatchRejectsBeforeLockAndBidWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Documents.UpsertAsync(FreelancerMarketplaceConstants.GigPostings, "gig-cross", Tenant(new Dictionary<string, object?>
        {
            ["gig_id"] = "gig-cross",
            ["client_tenant_id"] = "northwind",
            ["status"] = "OPEN"
        }, "northwind"), null);

        var result = await fixture.Service.AcceptGigAsync(AcceptGig("gig-cross", "bid-cross"), null, "cross");

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("BOLA_VIOLATION", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigAcceptLocks, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == FreelancerMarketplaceConstants.GigAcceptanceFailedEvent);
    }

    [Fact]
    public async Task CF953_ConcurrentGigAcceptAllowsOneWinnerAndOneLoser()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = fixture.Service.AcceptGigAsync(AcceptGig("gig-race", "bid-race-a", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.AcceptGigAsync(AcceptGig("gig-race", "bid-race-b", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF954_IdempotencyReplaysGigAcceptanceWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var first = await fixture.Service.AcceptGigAsync(AcceptGig("gig-replay", "bid-replay"), null, "same-key");
        var second = await fixture.Service.AcceptGigAsync(AcceptGig("gig-replay", "bid-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigAudits, Tenant("acme-corp"))).Data ?? []);
        Assert.Single((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == FreelancerMarketplaceConstants.GigAcceptedEvent);
    }

    [Fact]
    public async Task CF955_InvalidPrimaryFieldsReturnErrorsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");

        var result = await fixture.Service.AcceptGigAsync(new Dictionary<string, object?>(), null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        var details = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(result.Data!.Body["details"]);
        Assert.Contains(details, detail => Convert.ToString(detail["field"], CultureInfo.InvariantCulture) == "gigId");
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigAcceptLocks, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task TenantScopePayloadFieldsAreRejectedAcrossMutations()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var contract = await fixture.Service.UpdateContractAsync(new Dictionary<string, object?>
        {
            ["contractId"] = "contract-forbidden",
            ["clientTenantId"] = "northwind",
            ["contractTotalCents"] = 100,
            ["milestones"] = new[] { new Dictionary<string, object?> { ["milestoneId"] = "m1", ["amountCents"] = 100 } }
        }, null, "forbidden-contract");
        var release = await fixture.Service.ReleaseMilestoneAsync(new Dictionary<string, object?>
        {
            ["milestoneId"] = "m1",
            ["contractId"] = "contract-forbidden",
            ["deliveryId"] = "delivery-forbidden",
            ["tenant_id"] = "northwind"
        }, "forbidden-release");
        var dispute = await fixture.Service.DisputeMilestoneAsync(new Dictionary<string, object?>
        {
            ["milestoneId"] = "m1",
            ["contractId"] = "contract-forbidden",
            ["masterTenantId"] = "master-northwind"
        });
        var review = await fixture.Service.SubmitReviewAsync(new Dictionary<string, object?>
        {
            ["engagementId"] = "contract-forbidden",
            ["direction"] = "CLIENT_TO_FREELANCER",
            ["reviewerId"] = "client-1",
            ["targetId"] = "freelancer-1",
            ["rating"] = 5,
            ["tenantId"] = "northwind"
        }, "forbidden-review");

        foreach (var result in new[] { contract, release, dispute, review })
        {
            Assert.Equal(400, result.Data?.HttpStatus);
            Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
        }
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Contracts, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Milestones, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Reviews, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF956_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        var request = AcceptGig("gig-timeout", "bid-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.AcceptGigAsync(request, null, "timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == FreelancerMarketplaceConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.GigPostings, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF17_2_ContractRejectsImmutableFieldsBeforeStorage()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Contract("contract-immutable");
        request["fieldsToUpdate"] = new[] { "clientId" };

        var result = await fixture.Service.UpdateContractAsync(request, null, "immutable");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("FIELD_IMMUTABLE", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Contracts, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == FreelancerMarketplaceConstants.ContractFieldImmutableEvent);
    }

    [Fact]
    public async Task CF17_2_ContractValidatesMilestoneSumUsesOccAuditsThenEmits()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.UpdateContractAsync(Contract("contract-a"), null, "contract-a");
        var contracts = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Contracts, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.ContractAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Single(contracts);
        Assert.Equal(2, result.Data?.Body["milestoneCount"]);
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "MILESTONES_UPDATED");
        Assert.Equal(FreelancerMarketplaceConstants.FreelancerContractActivatedEvent, (await fixture.Outbox.ListAsync()).Data!.Last().Event.Type);
    }

    [Fact]
    public async Task CF17_2_MilestoneSumMismatchDoesNotWriteContract()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = Contract("contract-bad-sum");
        request["contractTotalCents"] = 12000;

        var result = await fixture.Service.UpdateContractAsync(request, null, "bad-sum");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("MILESTONE_SUM_MISMATCH", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Contracts, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF17_3_ReleaseRequiresSubmittedDeliveryAuditsBeforeEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.ReleaseMilestoneAsync(Release("milestone-a"), "release-a");
        var milestones = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Milestones, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.EscrowAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("RELEASED", result.Data?.Body["status"]);
        Assert.Single(milestones, record => Convert.ToString(record.Body["status"], CultureInfo.InvariantCulture) == "RELEASED");
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "MILESTONE_RELEASED");
        Assert.Equal(FreelancerMarketplaceConstants.MilestoneReleasedEvent, (await fixture.Outbox.ListAsync()).Data!.Last().Event.Type);
    }

    [Fact]
    public async Task CF17_3_DisputeChangesStatusWithoutReleaseEvent()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var result = await fixture.Service.DisputeMilestoneAsync(new Dictionary<string, object?>
        {
            ["milestoneId"] = "milestone-dispute",
            ["contractId"] = "contract-dispute",
            ["reason"] = "delivery-mismatch"
        });
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("DISPUTED", result.Data?.Body["status"]);
        Assert.DoesNotContain(outbox, record => record.Event.Type == FreelancerMarketplaceConstants.MilestoneReleasedEvent);
        Assert.Equal(FreelancerMarketplaceConstants.MilestoneDisputedEvent, outbox.Last().Event.Type);
    }

    [Fact]
    public async Task CF17_3_ReleaseFailureRunsLifoCompensation()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");
        var request = Release("milestone-fail");
        request["simulateReleaseWriteFailure"] = true;

        var result = await fixture.Service.ReleaseMilestoneAsync(request, "release-fail");
        var steps = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.CompensationSteps, Tenant("acme-corp"))).Data ?? [];

        Assert.True(result.IsFailure);
        Assert.Equal("RELEASE_FAILED", result.Error?.Code);
        Assert.Equal(FreelancerMarketplaceConstants.EscrowCompensationStepNames, steps.Select(step => Convert.ToString(step.Body["step"], CultureInfo.InvariantCulture)).ToArray());
    }

    [Fact]
    public async Task CF17_4_ReviewDirectionDuplicateAppendOnlyAndAuditExcludesComment()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Documents.UpsertAsync(FreelancerMarketplaceConstants.Contracts, "engagement-a", Tenant(new Dictionary<string, object?>
        {
            ["contract_id"] = "engagement-a",
            ["client_tenant_id"] = "acme-corp",
            ["freelancer_tenant_id"] = "freelancer-tenant"
        }, "acme-corp"), null);

        var first = await fixture.Service.SubmitReviewAsync(Review("engagement-a"), "review-a");
        var second = await fixture.Service.SubmitReviewAsync(Review("engagement-a"), "review-b");
        var reviews = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.Reviews, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(FreelancerMarketplaceConstants.ReviewAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(409, second.Data?.HttpStatus);
        Assert.Single(reviews);
        Assert.Single(audits);
        Assert.Contains("comment_hash", audits[0].Body.Keys);
        Assert.DoesNotContain("Great delivery", JsonSerializer.Serialize(audits[0].Body), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashboardReadsOnlyCurrentTenantFreelancerRecords()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_user"))
        {
            await fixture.Service.AcceptGigAsync(AcceptGig("gig-acme", "bid-acme"), null, "acme-accept");
        }

        using (fixture.PushContext("northwind", "tenant_user"))
        {
            await fixture.Service.AcceptGigAsync(AcceptGig("gig-northwind", "bid-northwind"), null, "northwind-accept");
            var dashboard = await fixture.Service.GetDashboardAsync();

            Assert.Single(dashboard.Data?.Gigs ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["acceptedGigs"]));
        }
    }

    [Fact]
    public void CF287_CF288_ServiceDoesNotImportWarehouseOrSearchProviderSdk()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.FreelancerMarketplace.Application", "FreelancerMarketplaceService.cs"));

        Assert.DoesNotContain("Elastic.Clients", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NEST", source, StringComparison.Ordinal);
        Assert.DoesNotContain("warehouse", source, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> AcceptGig(string gigId, string bidId, bool simulateSlowWrite = false) =>
        new()
        {
            ["gigId"] = gigId,
            ["bidId"] = bidId,
            ["freelancerId"] = "freelancer-tenant",
            ["budgetCents"] = 10000,
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> Contract(string contractId) =>
        new()
        {
            ["contractId"] = contractId,
            ["clientId"] = "client-1",
            ["gigId"] = $"gig-{contractId}",
            ["freelancerId"] = "freelancer-1",
            ["freelancerTenantId"] = "freelancer-tenant",
            ["contractTotalCents"] = 10000,
            ["fieldsToUpdate"] = Array.Empty<string>(),
            ["milestones"] = new List<IReadOnlyDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["milestoneId"] = "m1", ["amountCents"] = 4000, ["description"] = "Discovery" },
                new Dictionary<string, object?> { ["milestoneId"] = "m2", ["amountCents"] = 6000, ["description"] = "Delivery" }
            }
        };

    private static Dictionary<string, object?> Release(string milestoneId) =>
        new()
        {
            ["milestoneId"] = milestoneId,
            ["contractId"] = $"contract-{milestoneId}",
            ["deliveryId"] = $"delivery-{milestoneId}",
            ["deliveryStatus"] = "SUBMITTED"
        };

    private static Dictionary<string, object?> Review(string engagementId) =>
        new()
        {
            ["engagementId"] = engagementId,
            ["direction"] = "CLIENT_TO_FREELANCER",
            ["rating"] = 5,
            ["comment"] = "Great delivery",
            ["reviewerId"] = "client-user",
            ["targetId"] = "freelancer-user"
        };

    private static Dictionary<string, object?> Tenant(string tenant) => new() { ["tenant_id"] = tenant };

    private static IReadOnlyDictionary<string, object?> Tenant(IReadOnlyDictionary<string, object?> body, string tenant)
    {
        var copy = new Dictionary<string, object?>(body)
        {
            ["tenant_id"] = tenant
        };
        return copy;
    }

    private static int ToInt(object? value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            FreelancerMarketplaceService service,
            ITenantContextAccessor tenantContextAccessor,
            ICorrelationContextAccessor correlationContextAccessor)
        {
            Documents = documents;
            Outbox = outbox;
            Freedom = freedom;
            Service = service;
            TenantContextAccessor = tenantContextAccessor;
            CorrelationContextAccessor = correlationContextAccessor;
        }

        public InMemoryDocumentStore Documents { get; }
        public InMemoryOutboxStore Outbox { get; }
        public InMemoryFreedomConfigService Freedom { get; }
        public FreelancerMarketplaceService Service { get; }
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
            var service = new FreelancerMarketplaceService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<FreelancerMarketplaceService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenant, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenant}", "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantContextAccessor.Push(new TenantContext(
                tenant,
                role == "anonymous" ? null : "test-user",
                new HashSet<string> { role },
                $"corr-{tenant}",
                "test",
                DateTimeOffset.UtcNow));
            return new CompositeDisposable(correlation, tenantScope);
        }
    }

    private sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
