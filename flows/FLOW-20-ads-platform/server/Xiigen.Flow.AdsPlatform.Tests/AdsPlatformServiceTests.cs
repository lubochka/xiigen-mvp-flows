using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AdsPlatform.Application;
using Xiigen.Freedom;
using Xiigen.Kernel;

namespace Xiigen.Flow.AdsPlatform.Tests;

public sealed class AdsPlatformServiceTests
{
    [Fact]
    public void P11_Rules_AllAdsPlatformRules_AreRegistered()
    {
        var ids = AdsPlatformBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(AdsPlatformConstants.RuleIds));
        Assert.Equal(8, AdsPlatformBfaRuleRegistry.Rules.Count);
        Assert.All(AdsPlatformBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(AdsPlatformConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public void P1_AllTaskTypesHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERED", result.Data?.Body["status"]);
        Assert.Equal(24, Convert.ToInt32(result.Data?.Body["stepCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CF20_1_ConsentGateRunsBeforeAuctionOrBudgetState()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = AdRequest("ad-no-consent", adsConsent: false);

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "no-consent");
        var locks = (await fixture.Documents.SearchAsync(AdsPlatformConstants.AuctionBidLocks, Tenant("acme-corp"))).Data ?? [];
        var budgets = (await fixture.Documents.SearchAsync(AdsPlatformConstants.AdvertiserBudgets, Tenant("acme-corp"))).Data ?? [];
        var audits = (await fixture.Documents.SearchAsync(AdsPlatformConstants.AuctionAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.Equal("CONSENT_GATE_FAILED", result.Data?.Body["code"]);
        Assert.Empty(locks);
        Assert.Empty(budgets);
        Assert.Single(audits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "CONSENT_GATE_FAILED");
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == AdsPlatformConstants.ConsentGateFailedEvent);
    }

    [Fact]
    public async Task ConsentDocumentsAuthorizeRequestsWhenRequestDoesNotCarryGateFlag()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.RecordConsentAsync(new Dictionary<string, object?>
        {
            ["userId"] = "user-1",
            ["consentScope"] = "ads-personalization"
        }, "consent-user-1");

        var request = AdRequest("ad-doc-consent");
        request.Remove("adsConsent");
        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "doc-consent");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("DELIVERY_AUTHORIZED", result.Data?.Body["status"]);
    }

    [Fact]
    public async Task CF20_2_AuctionLockAndBudgetDeductBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var result = await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-lock-budget"), null, "lock-budget");
        var locks = (await fixture.Documents.SearchAsync(AdsPlatformConstants.AuctionBidLocks, Tenant("acme-corp"))).Data ?? [];
        var budgets = (await fixture.Documents.SearchAsync(AdsPlatformConstants.AdvertiserBudgets, Tenant("acme-corp"))).Data ?? [];
        var deliveries = (await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Single(locks, record => Convert.ToString(record.Body["status"], CultureInfo.InvariantCulture) == "LOCKED");
        Assert.Single(budgets, record => Convert.ToInt64(record.Body["remaining_budget_cents"], CultureInfo.InvariantCulture) == 8000);
        Assert.Single(deliveries);
        Assert.Contains(outbox, record => record.Event.Type == AdsPlatformConstants.BidAcceptedEvent);
        Assert.Contains(outbox, record => record.Event.Type == AdsPlatformConstants.AdDeliveryAuthorizedEvent);
    }

    [Fact]
    public async Task CF20_2_DuplicateBidLockReplaysWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var first = await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-dup"), null, "dup-a");
        var second = await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-dup"), null, "dup-b");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal("BID_ALREADY_LOCKED", second.Data?.Body["status"]);
        Assert.Single((await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task BudgetInsufficientRejectsAndDoesNotAuthorizeDelivery()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = AdRequest("ad-low-budget");
        request["budgetRemainingCents"] = 100;

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "low-budget");

        Assert.Equal(402, result.Data?.HttpStatus);
        Assert.Equal("BUDGET_INSUFFICIENT", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? []);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == AdsPlatformConstants.BidRejectedEvent);
    }

    [Fact]
    public async Task CF20_3_FraudRunsBeforeBillingAndRestoresBudget()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        await fixture.Freedom.SetAsync("flow20_fraud_score_threshold", 0.7, "number");
        var request = AdRequest("ad-fraud");
        request["fraudScore"] = 0.91;

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "fraud");
        var budget = ((await fixture.Documents.SearchAsync(AdsPlatformConstants.AdvertiserBudgets, Tenant("acme-corp"))).Data ?? []).Single();
        var fraudAudits = (await fixture.Documents.SearchAsync(AdsPlatformConstants.FraudAudits, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("FRAUD_DETECTED", result.Data?.Body["code"]);
        Assert.Equal(10000, Convert.ToInt64(budget.Body["remaining_budget_cents"], CultureInfo.InvariantCulture));
        Assert.Single(fraudAudits, record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "FRAUD_REJECTED_PRE_BILLING");
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == AdsPlatformConstants.FraudDetectedEvent);
    }

    [Fact]
    public async Task CF20_3_RawPaymentDataIsRejectedBeforePartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = AdRequest("ad-pan");
        request["payment"] = new Dictionary<string, object?> { ["cardNumber"] = "4111111111111111", ["cvv"] = "123" };

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "pan");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.AuctionBidLocks, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF20_4_PoliticalContentUsesMinScoreAndQueuesHumanReview()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "ad_moderator");
        var request = AdRequest("ad-political");
        request["isPoliticalContent"] = true;
        request["politicalModelAScore"] = 0.92;
        request["politicalModelBScore"] = 0.66;

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "political");
        var reviews = (await fixture.Documents.SearchAsync(AdsPlatformConstants.HumanReviewQueue, Tenant("acme-corp"))).Data ?? [];

        Assert.Equal(202, result.Data?.HttpStatus);
        Assert.Equal("PENDING_HUMAN_REVIEW", result.Data?.Body["status"]);
        Assert.Equal(0.66, Convert.ToDouble(result.Data?.Body["minScore"], CultureInfo.InvariantCulture));
        Assert.True(Convert.ToBoolean(result.Data?.Body["usesMathMinConvergence"], CultureInfo.InvariantCulture));
        Assert.True(Convert.ToBoolean(result.Data?.Body["humanReviewMandatory"], CultureInfo.InvariantCulture));
        Assert.False(Convert.ToBoolean(result.Data?.Body["ambiguousAutoBlocked"], CultureInfo.InvariantCulture));
        Assert.Single(reviews);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == AdsPlatformConstants.PoliticalReviewPendingEvent);
    }

    [Fact]
    public async Task PoliticalAmbiguityDoesNotAutoBlockWithoutHumanReviewGate()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = AdRequest("ad-ambiguous");
        request["isPoliticalContent"] = true;
        request["politicalModelAScore"] = 0.41;
        request["politicalModelBScore"] = 0.43;

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "ambiguous");

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("APPROVED", result.Data?.Body["politicalStatus"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.HumanReviewQueue, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task HumanReviewDecisionIsImmutableAuditEvidence()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "ad_moderator");
        var request = AdRequest("ad-decision");
        request["isPoliticalContent"] = true;
        request["politicalModelAScore"] = 0.8;
        request["politicalModelBScore"] = 0.8;
        await fixture.Service.ProcessAdRequestAsync(request, null, "decision-pending");

        var result = await fixture.Service.ReviewPoliticalContentAsync(new Dictionary<string, object?>
        {
            ["requestId"] = "ad-decision",
            ["decision"] = "approve"
        }, "decision-approve");

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("HUMAN_APPROVED", result.Data?.Body["status"]);
        Assert.Contains((await fixture.Documents.SearchAsync(AdsPlatformConstants.PoliticalAudits, Tenant("acme-corp"))).Data ?? [],
            record => Convert.ToString(record.Body["audit_type"], CultureInfo.InvariantCulture) == "HUMAN_DECISION_IMMUTABLE");
    }

    [Fact]
    public async Task CF965_ConcurrentAuctionWritesAllowOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var first = fixture.Service.ProcessAdRequestAsync(AdRequest("ad-race", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.ProcessAdRequestAsync(AdRequest("ad-race", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess && result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.IsFailure && result.Error?.Code == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF966_IdempotencyReplaysWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");

        var first = await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-replay"), null, "same-key");
        var second = await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-replay"), null, "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? []);
        Assert.Equal(1, ((await fixture.Outbox.ListAsync()).Data ?? []).Count(record => record.Event.Type == AdsPlatformConstants.AdDeliveryAuthorizedEvent));
    }

    [Fact]
    public async Task CF967_InvalidPrimaryInputReturnsFieldErrorsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = new Dictionary<string, object?> { ["requestId"] = "bad", ["bidAmountCents"] = 0, ["budgetRemainingCents"] = 0 };

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.AuctionBidLocks, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF968_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "business_partner");
        var request = AdRequest("ad-timeout");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.ProcessAdRequestAsync(request, null, "timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == AdsPlatformConstants.RequestTimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task DashboardReadsCurrentTenantOnly()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "business_partner"))
        {
            await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-acme"), null, "acme");
        }
        using (fixture.PushContext("northwind", "business_partner"))
        {
            await fixture.Service.ProcessAdRequestAsync(AdRequest("ad-northwind"), null, "northwind");
        }
        using (fixture.PushContext("acme-corp", "business_partner"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Single(dashboard.Data?.Deliveries ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["authorizedDeliveries"]));
        }
    }

    [Fact]
    public async Task TenantScopePayloadFieldsAreRejectedAcrossMutations()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var process = await fixture.Service.ProcessAdRequestAsync(new Dictionary<string, object?>(AdRequest("forbidden-ad")) { ["tenantId"] = "northwind" }, null, "forbidden-process");
        var consent = await fixture.Service.RecordConsentAsync(new Dictionary<string, object?> { ["userId"] = "user-2", ["consentScope"] = "ads", ["tenant_id"] = "northwind" }, "forbidden-consent");
        var review = await fixture.Service.ReviewPoliticalContentAsync(new Dictionary<string, object?> { ["requestId"] = "missing", ["decision"] = "approve", ["ownerTenantId"] = "northwind" }, "forbidden-review");

        foreach (var result in new[] { process, consent, review })
        {
            Assert.Equal(400, result.Data?.HttpStatus);
            Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
        }
        Assert.Empty((await fixture.Documents.SearchAsync(AdsPlatformConstants.CampaignDeliveries, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public void ServiceDoesNotImportProviderSdkOrExposeRawTenantArguments()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.AdsPlatform.Application", "AdsPlatformService.cs"));

        Assert.DoesNotContain("Elastic.Clients", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NEST", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ.Client", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> AdRequest(string requestId, bool adsConsent = true, bool simulateSlowWrite = false) =>
        new()
        {
            ["requestId"] = requestId,
            ["userId"] = "user-1",
            ["advertiserId"] = "advertiser-1",
            ["auctionId"] = $"auction-{requestId}",
            ["bidId"] = $"bid-{requestId}",
            ["adId"] = $"creative-{requestId}",
            ["bidAmountCents"] = 2000,
            ["budgetRemainingCents"] = 10000,
            ["paymentMethodToken"] = "pm_tok_123",
            ["adsConsent"] = adsConsent,
            ["fraudScore"] = 0.11,
            ["isPoliticalContent"] = false,
            ["politicalModelAScore"] = 0.05,
            ["politicalModelBScore"] = 0.06,
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps()
    {
        var taskIds = AdsPlatformConstants.RequiredPlanTaskTypes.Concat(AdsPlatformConstants.EnforcementTaskTypes).ToArray();
        return taskIds.Select((taskId, index) => new Dictionary<string, object?>
        {
            ["id"] = $"step-{index + 1}",
            ["taskTypeId"] = taskId,
            ["name"] = $"Ads platform responsibility {index + 1}",
            ["responsibility"] = $"single ads platform responsibility {index + 1}",
            ["config"] = new Dictionary<string, object?> { ["returns"] = "DataProcessResult" }
        }).ToArray();
    }

    private static Dictionary<string, object?> Tenant(string tenant) => new() { ["tenant_id"] = tenant };

    private static int ToInt(object? value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private sealed class FlowFixture
    {
        private FlowFixture(
            InMemoryDocumentStore documents,
            InMemoryOutboxStore outbox,
            InMemoryFreedomConfigService freedom,
            AdsPlatformService service,
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
        public AdsPlatformService Service { get; }
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
            var service = new AdsPlatformService(
                documents,
                idempotency,
                outbox,
                freedom,
                tenantAccessor,
                correlationAccessor,
                NullLogger<AdsPlatformService>.Instance);

            return new FlowFixture(documents, outbox, freedom, service, tenantAccessor, correlationAccessor);
        }

        public IDisposable PushContext(string tenant, string role)
        {
            var correlation = CorrelationContextAccessor.Push(new CorrelationContext($"corr-{tenant}", "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantContextAccessor.Push(new TenantContext(
                tenant,
                role == "anonymous" ? null : "test-user",
                new HashSet<string>([role], StringComparer.Ordinal),
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
