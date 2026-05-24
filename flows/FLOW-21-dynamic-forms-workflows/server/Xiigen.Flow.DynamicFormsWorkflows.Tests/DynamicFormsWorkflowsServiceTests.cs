using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DynamicFormsWorkflows.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.DynamicFormsWorkflows.Tests;

public sealed class DynamicFormsWorkflowsServiceTests
{
    [Fact]
    public void P11_Rules_AllDynamicFormsWorkflowsRules_AreRegistered()
    {
        var ids = DynamicFormsWorkflowsBfaRuleRegistry.Rules.Select(rule => rule.RuleId).ToHashSet(StringComparer.Ordinal);

        Assert.True(ids.SetEquals(DynamicFormsWorkflowsConstants.RuleIds));
        Assert.Equal(8, DynamicFormsWorkflowsBfaRuleRegistry.Rules.Count);
        Assert.All(DynamicFormsWorkflowsBfaRuleRegistry.Rules, rule =>
        {
            Assert.Equal(DynamicFormsWorkflowsConstants.FlowId, rule.FlowId);
            Assert.Equal("BUILD_FAILURE", rule.ViolationSeverity);
            Assert.Equal("FLOW_SCOPED", rule.ConnectionType);
            Assert.Equal("GLOBAL", rule.KnowledgeScope);
            Assert.Equal("MASTER_TENANT_ID", rule.TenantId);
        });
    }

    [Fact]
    public void P1_AllLegacyAndServiceTaskTypesHaveSingleResponsibilityPlanSteps()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = fixture.Service.ValidatePlanCoverage(new Dictionary<string, object?> { ["planSteps"] = PlanSteps() });

        Assert.Equal(200, result.Data?.HttpStatus);
        Assert.Equal("PLAN_COVERED", result.Data?.Body["status"]);
        Assert.Equal(38, Convert.ToInt32(result.Data?.Body["stepCount"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CF21_1_FormSchemaPublisherCreatesImmutablePublishedVersion()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");
        var schemas = (await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSchemas, Tenant("acme-corp"))).Data ?? [];
        var versions = (await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.PublishedSchemaVersions, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("PUBLISHED", result.Data?.Body["status"]);
        Assert.Single(schemas, record => Convert.ToString(record.Body["form_id"], CultureInfo.InvariantCulture) == "intake-form");
        Assert.Single(versions, record => Convert.ToString(record.Body["schema_version"], CultureInfo.InvariantCulture) == "1.0.0");
        Assert.Contains(outbox, record => record.Event.Type == DynamicFormsWorkflowsConstants.SchemaPublishedEvent);
    }

    [Fact]
    public async Task CF21_1_IfMatchMismatchRejectsSchemaOverwrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");

        var result = await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.1"), "stale-version", "schema-2");

        Assert.Equal(409, result.Data?.HttpStatus);
        Assert.Equal("CONCURRENT_UPDATE", result.Data?.Body["code"]);
        Assert.Single((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.PublishedSchemaVersions, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF21_2_SubmissionValidatesPublishedSchemaBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");

        var result = await fixture.Service.SubmitFormAsync(FormSubmissionRequest("sub-1", "intake-form"), "sub-1-key");
        var submissions = (await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSubmissions, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("SUBMITTED", result.Data?.Body["status"]);
        Assert.Single(submissions);
        Assert.Contains(outbox, record => record.Event.Type == DynamicFormsWorkflowsConstants.SubmissionAcceptedEvent);
    }

    [Fact]
    public async Task CF21_2_HiddenRoleFieldIsRejectedBeforePartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "freelancer");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");
        var request = FormSubmissionRequest("sub-hidden", "intake-form");
        request["values"] = new Dictionary<string, object?>
        {
            ["displayName"] = "Ada",
            ["priority"] = "standard",
            ["adminNotes"] = "should not be visible"
        };

        var result = await fixture.Service.SubmitFormAsync(request, "sub-hidden-key");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSubmissions, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF21_3_AutomationDispatchAcquiresSetNxEquivalentLockBeforeOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");
        await fixture.Service.SubmitFormAsync(FormSubmissionRequest("sub-1", "intake-form"), "sub-1-key");

        var result = await fixture.Service.DispatchAutomationAsync(new Dictionary<string, object?>
        {
            ["submissionId"] = "sub-1",
            ["ruleId"] = "route-to-review"
        }, "automation-1");
        var locks = (await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.AutomationLocks, Tenant("acme-corp"))).Data ?? [];
        var outbox = (await fixture.Outbox.ListAsync()).Data ?? [];

        Assert.Equal(201, result.Data?.HttpStatus);
        Assert.Equal("AUTOMATION_DISPATCHED", result.Data?.Body["status"]);
        Assert.Single(locks, record => Convert.ToBoolean(record.Body["setnx_equivalent"], CultureInfo.InvariantCulture));
        Assert.Contains(outbox, record => record.Event.Type == DynamicFormsWorkflowsConstants.AutomationDispatchedEvent);
    }

    [Fact]
    public async Task CF21_3_DuplicateAutomationDispatchDoesNotDuplicateOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");
        await fixture.Service.SubmitFormAsync(FormSubmissionRequest("sub-1", "intake-form"), "sub-1-key");

        var first = await fixture.Service.DispatchAutomationAsync(new Dictionary<string, object?> { ["submissionId"] = "sub-1", ["ruleId"] = "route-to-review" }, "automation-1");
        var second = await fixture.Service.DispatchAutomationAsync(new Dictionary<string, object?> { ["submissionId"] = "sub-1", ["ruleId"] = "route-to-review" }, "automation-2");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal("AUTOMATION_ALREADY_DISPATCHED", second.Data?.Body["status"]);
        Assert.Equal(1, ((await fixture.Outbox.ListAsync()).Data ?? []).Count(record => record.Event.Type == DynamicFormsWorkflowsConstants.AutomationDispatchedEvent));
    }

    [Fact]
    public async Task CF21_4_AnalyticsAreAppendOnlyAndRejectPii()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var accepted = await fixture.Service.CollectAnalyticsAsync(new Dictionary<string, object?>
        {
            ["submissionId"] = "sub-1",
            ["metricName"] = "completion",
            ["metrics"] = new Dictionary<string, object?> { ["durationSeconds"] = 42, ["fieldCount"] = 3 }
        }, "analytics-1");
        var rejected = await fixture.Service.CollectAnalyticsAsync(new Dictionary<string, object?>
        {
            ["submissionId"] = "sub-2",
            ["metricName"] = "completion",
            ["metrics"] = new Dictionary<string, object?> { ["email"] = "person@example.test" }
        }, "analytics-2");

        Assert.Equal(201, accepted.Data?.HttpStatus);
        Assert.False(Convert.ToBoolean(accepted.Data?.Body["piiPersisted"], CultureInfo.InvariantCulture));
        Assert.Equal(400, rejected.Data?.HttpStatus);
        Assert.Single((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.AnalyticsEvents, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task CF969_ConcurrentSchemaPublishesAllowOneWinnerAndOneConflict()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var first = fixture.Service.PublishSchemaAsync(FormSchemaRequest("race-form", "1.0.0", simulateSlowWrite: true), null, "race-a");
        var second = fixture.Service.PublishSchemaAsync(FormSchemaRequest("race-form", "1.0.0", simulateSlowWrite: true), null, "race-b");
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.Data?.HttpStatus == 201);
        Assert.Single(results, result => result.Data?.HttpStatus == 409 && Convert.ToString(result.Data.Body["code"], CultureInfo.InvariantCulture) == "CONCURRENT_UPDATE");
    }

    [Fact]
    public async Task CF970_IdempotencyReplaysSubmissionWithoutDuplicateSideEffects()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_user");
        await fixture.Service.PublishSchemaAsync(FormSchemaRequest("intake-form", "1.0.0"), null, "schema-1");

        var first = await fixture.Service.SubmitFormAsync(FormSubmissionRequest("sub-replay", "intake-form"), "same-key");
        var second = await fixture.Service.SubmitFormAsync(FormSubmissionRequest("sub-replay", "intake-form"), "same-key");

        Assert.Equal(201, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(first.Data?.Body["id"], second.Data?.Body["id"]);
        Assert.Single((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSubmissions, Tenant("acme-corp"))).Data ?? []);
        Assert.Equal(1, ((await fixture.Outbox.ListAsync()).Data ?? []).Count(record => record.Event.Type == DynamicFormsWorkflowsConstants.SubmissionAcceptedEvent));
    }

    [Fact]
    public async Task CF971_InvalidBoundaryInputReturnsFieldErrorsAndNoPartialWrite()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");

        var result = await fixture.Service.PublishSchemaAsync(new Dictionary<string, object?> { ["formId"] = "bad-form" }, null, "invalid");

        Assert.Equal(400, result.Data?.HttpStatus);
        Assert.Equal("VALIDATION_FAILED", result.Data?.Body["code"]);
        Assert.Empty((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSchemas, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public async Task CF972_TimeoutReturns504AndRetainsRecoveryOutbox()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "tenant_admin");
        var request = FormSchemaRequest("timeout-form", "1.0.0");
        request["simulateFabricTimeout"] = true;

        var result = await fixture.Service.PublishSchemaAsync(request, null, "timeout");

        Assert.Equal(504, result.Data?.HttpStatus);
        Assert.True(result.Data?.OutboxRetained);
        Assert.Contains((await fixture.Outbox.ListAsync()).Data ?? [], record => record.Event.Type == DynamicFormsWorkflowsConstants.TimeoutRetainedEvent);
        Assert.Empty((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSchemas, Tenant("acme-corp"))).Data ?? []);
    }

    [Fact]
    public async Task DashboardReadsCurrentTenantOnly()
    {
        var fixture = FlowFixture.Create();
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            await fixture.Service.PublishSchemaAsync(FormSchemaRequest("acme-form", "1.0.0"), null, "acme-schema");
            await fixture.Service.SubmitFormAsync(FormSubmissionRequest("acme-sub", "acme-form"), "acme-sub");
        }
        using (fixture.PushContext("northwind", "tenant_admin"))
        {
            await fixture.Service.PublishSchemaAsync(FormSchemaRequest("north-form", "1.0.0"), null, "north-schema");
            await fixture.Service.SubmitFormAsync(FormSubmissionRequest("north-sub", "north-form"), "north-sub");
        }
        using (fixture.PushContext("acme-corp", "tenant_admin"))
        {
            var dashboard = await fixture.Service.GetDashboardAsync();
            Assert.Single(dashboard.Data?.Schemas ?? []);
            Assert.Single(dashboard.Data?.Submissions ?? []);
            Assert.Equal(1, ToInt(dashboard.Data!.Kpis["publishedSchemas"]));
        }
    }

    [Fact]
    public async Task TenantScopePayloadFieldsAreRejectedAcrossMutations()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("acme-corp", "platform_admin");

        var publish = await fixture.Service.PublishSchemaAsync(new Dictionary<string, object?>(FormSchemaRequest("forbidden", "1.0.0")) { ["tenantId"] = "northwind" }, null, "forbidden-publish");
        var submit = await fixture.Service.SubmitFormAsync(new Dictionary<string, object?>(FormSubmissionRequest("forbidden-sub", "forbidden")) { ["formTenantId"] = "northwind" }, "forbidden-submit");
        var analytics = await fixture.Service.CollectAnalyticsAsync(new Dictionary<string, object?> { ["submissionId"] = "sub", ["metricName"] = "m", ["tenant_id"] = "northwind" }, "forbidden-analytics");

        foreach (var result in new[] { publish, submit, analytics })
        {
            Assert.Equal(400, result.Data?.HttpStatus);
            Assert.Equal("TENANT_SCOPE_FORBIDDEN", result.Data?.Body["code"]);
        }
        Assert.Empty((await fixture.Documents.SearchAsync(DynamicFormsWorkflowsConstants.FormSchemas, Tenant("acme-corp"))).Data ?? []);
        Assert.Empty((await fixture.Outbox.ListAsync()).Data ?? []);
    }

    [Fact]
    public void ServiceDoesNotImportProviderSdkOrExposeRawTenantArguments()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Xiigen.Flow.DynamicFormsWorkflows.Application", "DynamicFormsWorkflowsService.cs"));

        Assert.DoesNotContain("Elastic.Clients", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NEST", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ.Client", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string tenantId", source, StringComparison.Ordinal);
    }

    private static Dictionary<string, object?> FormSchemaRequest(string formId, string schemaVersion, bool simulateSlowWrite = false) =>
        new()
        {
            ["formId"] = formId,
            ["schemaVersion"] = schemaVersion,
            ["fields"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["key"] = "displayName",
                    ["type"] = "string",
                    ["required"] = true,
                    ["visibleToRoles"] = DynamicFormsWorkflowsConstants.ProtectedRoles
                },
                new Dictionary<string, object?>
                {
                    ["key"] = "priority",
                    ["type"] = "string",
                    ["required"] = true,
                    ["visibleToRoles"] = DynamicFormsWorkflowsConstants.ProtectedRoles
                },
                new Dictionary<string, object?>
                {
                    ["key"] = "adminNotes",
                    ["type"] = "string",
                    ["required"] = false,
                    ["visibleToRoles"] = DynamicFormsWorkflowsConstants.OperationsRoles
                }
            },
            ["workflowRules"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["ruleId"] = "route-to-review",
                    ["trigger"] = "submit",
                    ["targetState"] = "review",
                    ["condition"] = new Dictionary<string, object?> { ["field"] = "priority", ["equals"] = "urgent" }
                }
            },
            ["planSteps"] = PlanSteps(),
            ["simulateSlowWrite"] = simulateSlowWrite
        };

    private static Dictionary<string, object?> FormSubmissionRequest(string submissionId, string formId) =>
        new()
        {
            ["submissionId"] = submissionId,
            ["formId"] = formId,
            ["values"] = new Dictionary<string, object?>
            {
                ["displayName"] = "Ada",
                ["priority"] = "urgent"
            }
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> PlanSteps()
    {
        var taskIds = DynamicFormsWorkflowsConstants.RequiredPlanTaskTypes.Concat(DynamicFormsWorkflowsConstants.EnforcementTaskTypes).ToArray();
        return taskIds.Select((taskId, index) => new Dictionary<string, object?>
        {
            ["id"] = $"step-{index + 1}",
            ["taskTypeId"] = taskId,
            ["name"] = $"Form workflow responsibility {index + 1}",
            ["responsibility"] = $"single form workflow responsibility {index + 1}",
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
            DynamicFormsWorkflowsService service,
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
        public DynamicFormsWorkflowsService Service { get; }
        private ITenantContextAccessor TenantContextAccessor { get; }
        private ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public static FlowFixture Create()
        {
            var documents = new InMemoryDocumentStore();
            var idempotency = new InMemoryIdempotencyStore();
            var outbox = new InMemoryOutboxStore();
            var tenantAccessor = new AsyncLocalTenantContextAccessor();
            var correlationAccessor = new AsyncLocalCorrelationContextAccessor();
            var service = new DynamicFormsWorkflowsService(
                documents,
                idempotency,
                outbox,
                tenantAccessor,
                correlationAccessor,
                NullLogger<DynamicFormsWorkflowsService>.Instance);

            return new FlowFixture(documents, outbox, service, tenantAccessor, correlationAccessor);
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
