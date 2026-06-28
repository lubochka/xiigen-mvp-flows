using System.Text.Json;
using DesignConversionDomain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xiigen.Flow.DesignConversionIntake.Application;
using Xiigen.Flow.DesignConversionIntake.Infrastructure;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignConversionIntake.Tests;

public sealed class DesignConversionIntakeServiceTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AuthPolicyRequiresBearerTokenForPlatformAdminSubmit()
    {
        var missing = DesignConversionIntakeAuthPolicy.Evaluate("platform_admin", null, "master", DesignConversionIntakeConstants.SubmitRoles);
        var wrongRole = DesignConversionIntakeAuthPolicy.Evaluate("tenant_admin", "Bearer local-master-token", "master", DesignConversionIntakeConstants.SubmitRoles);
        var crossTenant = DesignConversionIntakeAuthPolicy.Evaluate("platform_admin", "Bearer local-other-token", "master", DesignConversionIntakeConstants.SubmitRoles);
        var accepted = DesignConversionIntakeAuthPolicy.Evaluate("platform_admin", "Bearer local-master-token", "master", DesignConversionIntakeConstants.SubmitRoles);

        Assert.False(missing.IsAllowed);
        Assert.Equal(401, missing.HttpStatus);
        Assert.Equal("MISSING_TOKEN", missing.Code);
        Assert.Equal("WRONG_ROLE", wrongRole.Code);
        Assert.Equal("AUTH_CROSS_TENANT_TOKEN", crossTenant.Code);
        Assert.True(accepted.IsAllowed);
    }

    [Fact]
    public void SubmitAuthPolicyKeepsPlatformSupportOutOfPlatformAdminSubmit()
    {
        var wrongRole = DesignConversionIntakeAuthPolicy.Evaluate(
            "platform_support",
            "Bearer local-tenant-a-token",
            "tenant-a",
            DesignConversionIntakeConstants.SubmitRoles);

        Assert.False(wrongRole.IsAllowed);
        Assert.Equal(403, wrongRole.HttpStatus);
        Assert.Equal("WRONG_ROLE", wrongRole.Code);
        Assert.Equal("platform_admin", Assert.Single(wrongRole.AllowedRoles));
    }

    [Fact]
    public async Task PlatformAdminRoleAndTenantTokenContextAllowSubmitPastWrongRole()
    {
        var auth = DesignConversionIntakeAuthPolicy.Evaluate(
            "platform_admin",
            "Bearer local-tenant-a-token",
            "tenant-a",
            DesignConversionIntakeConstants.SubmitRoles);
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("tenant-a", "platform_admin");

        var result = await fixture.Service.SubmitJobAsync(ValidRequest() with { TenantId = "tenant-a" });

        Assert.True(auth.IsAllowed);
        Assert.Null(auth.Code);
        Assert.Equal(202, result.Data?.HttpStatus);
        Assert.True(result.Data?.StateWritten);
        Assert.True(result.Data?.CoreDispatchAttempted);
        var body = Assert.IsType<DesignConversionJobStatusResponse>(result.Data?.Body);
        Assert.Equal("tenant-a", body.TenantId);
        Assert.Equal("queued", body.Status);
    }

    [Fact]
    public void EndpointConstantsMatchPublishedContractRegistry()
    {
        var endpointsPath = Path.Combine(FindFlowRoot(), "contracts", "endpoints", "flow-endpoints.json");
        using var document = JsonDocument.Parse(File.ReadAllText(endpointsPath));
        var endpoints = document.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        var submit = endpoints.Single(endpoint => endpoint.GetProperty("endpointId").GetString() == "flow47-submit-design-conversion-job");
        Assert.Equal("POST", submit.GetProperty("method").GetString());
        Assert.Equal($"{DesignConversionIntakeConstants.DynamicEndpoint}/jobs", submit.GetProperty("path").GetString());
        Assert.Equal("platform_admin", submit.GetProperty("requiredRoles")[0].GetString());
        Assert.Equal("contracts/schemas/design-conversion-submit-job.schema.json", submit.GetProperty("requestSchema").GetString());
        Assert.Equal("contracts/schemas/design-conversion-job-status.schema.json", submit.GetProperty("responseSchema").GetString());

        var status = endpoints.Single(endpoint => endpoint.GetProperty("endpointId").GetString() == "flow47-get-design-conversion-job-status");
        Assert.Equal("GET", status.GetProperty("method").GetString());
        Assert.Equal($"{DesignConversionIntakeConstants.DynamicEndpoint}/jobs/{{jobId}}", status.GetProperty("path").GetString());
        var statusRoles = status.GetProperty("requiredRoles").EnumerateArray().Select(role => role.GetString()).ToArray();
        Assert.Collection(
            statusRoles,
            role => Assert.Equal("platform_admin", role),
            role => Assert.Equal("platform_support", role));
        Assert.Equal("contracts/schemas/design-conversion-job-status.schema.json", status.GetProperty("responseSchema").GetString());
    }

    [Fact]
    public async Task PlatformSubmitJsonDeserializesWithEndpointDefaultsAndSubmitsPastMissingSourceValidation()
    {
        const string platformSubmitJson = """
            {
              "tenantId": "tenant-a",
              "sourceType": "html",
              "sourceUri": "inline://platform/design-conversion/html",
              "outputTargets": ["angular"],
              "idempotencyKey": "flow-contract",
              "options": {
                "designTier": "pro",
                "loopBudget": 3,
                "userId": "user-a",
                "ifMatch": "etag-1",
                "html": "<main>hello</main>",
                "css": "main { color: #123456; }",
                "metadata": {
                  "source": "endpoint-test"
                }
              }
            }
            """;

        var request = JsonSerializer.Deserialize<DesignConversionSubmitJobRequest>(platformSubmitJson);

        Assert.NotNull(request);
        Assert.Equal("tenant-a", request!.TenantId);
        Assert.Equal("html", request.SourceType);
        Assert.Equal("inline://platform/design-conversion/html", request.SourceUri);
        Assert.Equal("angular", Assert.Single(request.OutputTargets ?? []));
        Assert.Equal("flow-contract", request.IdempotencyKey);
        Assert.NotNull(request.Options);
        Assert.Equal("pro", AssertJsonOption(request.Options!, "designTier").GetString());
        Assert.Equal(3, AssertJsonOption(request.Options!, "loopBudget").GetInt32());
        Assert.Equal("<main>hello</main>", AssertJsonOption(request.Options!, "html").GetString());

        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("tenant-a", "platform_admin");
        var result = await fixture.Service.SubmitJobAsync(request);

        Assert.Equal(202, result.Data?.HttpStatus);
        Assert.True(result.Data?.StateWritten);
        Assert.True(result.Data?.CoreDispatchAttempted);
        var body = Assert.IsType<DesignConversionJobStatusResponse>(result.Data?.Body);
        Assert.Equal("html", body.SourceType);
        Assert.Equal("inline://platform/design-conversion/html", body.SourceUri);
    }

    [Fact]
    public void StatusResponseSerializesWithFlowContractPropertyNames()
    {
        var now = DateTimeOffset.UtcNow;
        var response = new DesignConversionJobStatusResponse(
            "dcj-contract",
            "tenant-a",
            "queued",
            "flow-contract",
            "html",
            "inline://platform/design-conversion/html",
            ["angular"],
            "fake-core-dcj-contract",
            null,
            null,
            now,
            now,
            "FLOW-47 status reflects same-process core dispatch; job metadata is stored in the configured FLOW-47 durable job store.");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, WebJsonOptions));
        var body = document.RootElement;

        Assert.Equal("dcj-contract", body.GetProperty("jobId").GetString());
        Assert.Equal("tenant-a", body.GetProperty("tenantId").GetString());
        Assert.Equal("queued", body.GetProperty("status").GetString());
        Assert.Equal("flow-contract", body.GetProperty("idempotencyKey").GetString());
        Assert.Equal("html", body.GetProperty("sourceType").GetString());
        Assert.Equal("inline://platform/design-conversion/html", body.GetProperty("sourceUri").GetString());
        Assert.Equal("angular", body.GetProperty("outputTargets")[0].GetString());
        Assert.Equal("fake-core-dcj-contract", body.GetProperty("coreProviderJobId").GetString());
        Assert.True(body.TryGetProperty("scaffoldNotice", out _));
    }

    [Fact]
    public async Task SubmitRejectsBodyTenantThatDoesNotMatchContext()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.SubmitJobAsync(ValidRequest() with { TenantId = "tenant-a" });

        Assert.Equal(403, result.Data?.HttpStatus);
        Assert.False(result.Data?.StateWritten);
        Assert.False(result.Data?.CoreDispatchAttempted);
    }

    [Fact]
    public async Task SubmitIsIdempotentWithinTenant()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");
        var request = ValidRequest() with { IdempotencyKey = "design-123" };

        var first = await fixture.Service.SubmitJobAsync(request);
        var second = await fixture.Service.SubmitJobAsync(request);

        var firstBody = Assert.IsType<DesignConversionJobStatusResponse>(first.Data?.Body);
        var secondBody = Assert.IsType<DesignConversionJobStatusResponse>(second.Data?.Body);
        Assert.Equal(202, first.Data?.HttpStatus);
        Assert.Equal(200, second.Data?.HttpStatus);
        Assert.Equal(firstBody.JobId, secondBody.JobId);
        Assert.Equal("queued", secondBody.Status);
        Assert.Equal(1, fixture.Core.DispatchCount);
    }

    [Fact]
    public async Task StatusLookupIsTenantScoped()
    {
        var fixture = FlowFixture.Create();
        string jobId;
        using (fixture.PushContext("master", "platform_admin"))
        {
            var submitted = await fixture.Service.SubmitJobAsync(ValidRequest());
            jobId = Assert.IsType<DesignConversionJobStatusResponse>(submitted.Data?.Body).JobId;
        }

        using var _ = fixture.PushContext("tenant-a", "platform_support");
        var hidden = await fixture.Service.GetJobStatusAsync(jobId);

        Assert.Equal(404, hidden.Data?.HttpStatus);
    }

    [Fact]
    public async Task CoreClientFailureMapsToDispatchFailedStatusAndBadGateway()
    {
        var fixture = FlowFixture.Create(FailingCoreClient.Instance);
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.SubmitJobAsync(ValidRequest());

        var body = Assert.IsType<DesignConversionJobStatusResponse>(result.Data?.Body);
        Assert.Equal(502, result.Data?.HttpStatus);
        Assert.True(result.Data?.StateWritten);
        Assert.True(result.Data?.CoreDispatchAttempted);
        Assert.Equal("failed", body.Status);
        Assert.Equal("CORE_CLIENT_UNAVAILABLE", body.Error?.Code);
    }

    [Fact]
    public async Task StatusReturnsQueuedFakeCoreProviderJob()
    {
        var fixture = FlowFixture.Create();
        using var _ = fixture.PushContext("master", "platform_admin");

        var submitted = await fixture.Service.SubmitJobAsync(ValidRequest());
        var jobId = Assert.IsType<DesignConversionJobStatusResponse>(submitted.Data?.Body).JobId;
        var status = await fixture.Service.GetJobStatusAsync(jobId);

        var body = Assert.IsType<DesignConversionJobStatusResponse>(status.Data?.Body);
        Assert.Equal(200, status.Data?.HttpStatus);
        Assert.Equal("queued", body.Status);
        Assert.StartsWith("fake-core-", body.CoreProviderJobId, StringComparison.Ordinal);
        Assert.Contains("same-process core dispatch", body.ScaffoldNotice, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("durable job store", body.ScaffoldNotice, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileBackedJobStorePersistsIdempotencyAcrossNewStoreInstance()
    {
        var directory = CreateTempJobStoreDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var firstStore = CreateJobStore(directory);
            await firstStore.UpsertAsync(new DesignConversionJobRecord(
                "dcj-persisted",
                "tenant-a",
                "queued",
                "same-request",
                "figma-file",
                "figma://file/persisted",
                ["angular"],
                "fake-core-dcj-persisted",
                null,
                null,
                now,
                now));

            var reopenedStore = CreateJobStore(directory);
            var found = await reopenedStore.FindByIdempotencyKeyAsync("tenant-a", "same-request");

            Assert.NotNull(found);
            Assert.Equal("dcj-persisted", found!.JobId);
            Assert.Equal("queued", found.Status);
        }
        finally
        {
            DeleteTempJobStoreDirectory(directory);
        }
    }

    [Fact]
    public async Task FileBackedJobStoreScopesStatusAndIdempotencyByTenant()
    {
        var directory = CreateTempJobStoreDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var store = CreateJobStore(directory);
            await store.UpsertAsync(new DesignConversionJobRecord(
                "dcj-shared",
                "tenant-a",
                "queued",
                "same-request",
                "figma-file",
                "figma://file/a",
                ["angular"],
                "fake-core-dcj-a",
                null,
                null,
                now,
                now));
            await store.UpsertAsync(new DesignConversionJobRecord(
                "dcj-shared",
                "tenant-b",
                "failed",
                "same-request",
                "figma-file",
                "figma://file/b",
                ["react"],
                null,
                null,
                new DesignConversionErrorResponse("CORE_CLIENT_UNAVAILABLE", "Synthetic tenant-b failure."),
                now,
                now));

            var tenantAStatus = await store.GetAsync("tenant-a", "dcj-shared");
            var tenantBStatus = await store.GetAsync("tenant-b", "dcj-shared");
            var tenantAIdempotency = await store.FindByIdempotencyKeyAsync("tenant-a", "same-request");
            var tenantBIdempotency = await store.FindByIdempotencyKeyAsync("tenant-b", "same-request");

            Assert.Equal("queued", tenantAStatus?.Status);
            Assert.Equal("failed", tenantBStatus?.Status);
            Assert.Equal("fake-core-dcj-a", tenantAIdempotency?.CoreProviderJobId);
            Assert.Equal("CORE_CLIENT_UNAVAILABLE", tenantBIdempotency?.Error?.Code);
        }
        finally
        {
            DeleteTempJobStoreDirectory(directory);
        }
    }

    [Fact]
    public async Task DispatchFailurePersistsAcrossNewStoreInstance()
    {
        var directory = CreateTempJobStoreDirectory();
        try
        {
            var fixture = FlowFixture.Create(FailingCoreClient.Instance, directory);
            using var _ = fixture.PushContext("master", "platform_admin");

            var result = await fixture.Service.SubmitJobAsync(ValidRequest() with { IdempotencyKey = "dispatch-failure" });
            var body = Assert.IsType<DesignConversionJobStatusResponse>(result.Data?.Body);
            var reopenedStore = CreateJobStore(directory);
            var persisted = await reopenedStore.GetAsync("master", body.JobId);

            Assert.Equal(502, result.Data?.HttpStatus);
            Assert.NotNull(persisted);
            Assert.Equal("failed", persisted!.Status);
            Assert.Equal("dispatch-failure", persisted.IdempotencyKey);
            Assert.Equal("CORE_CLIENT_UNAVAILABLE", persisted.Error?.Code);
        }
        finally
        {
            DeleteTempJobStoreDirectory(directory);
        }
    }

    [Fact]
    public void RealCoreClientMapsDispatchRequestToInternalEvaluateRequest()
    {
        var request = new DesignConversionCoreDispatchRequest(
            "dcj-map",
            "tenant-map",
            "figma-file",
            "figma://file/demo",
            ["angular", "react"],
            new Dictionary<string, object?>
            {
                ["requestId"] = "request-map",
                ["html"] = "<main>mapped</main>",
                ["css"] = "main { color: #123456; }",
                ["designTreePayload"] = "{\"node\":\"root\"}",
                ["originalTreePayload"] = "{\"node\":\"original\"}",
                ["designTier"] = "pro",
                ["userId"] = "user-map",
                ["maxImprovementCandidates"] = 2,
                ["maxRenderQaRuns"] = 3,
                ["budgetUsd"] = 12.50m,
                ["viewportWidth"] = 1024,
                ["viewportHeight"] = 768,
                ["deviceScaleFactor"] = 2.0,
                ["designRenderUri"] = "artifact://expected/render.png",
                ["designRenderSha256"] = "sha256:expected",
                ["metadata"] = new Dictionary<string, object?> { ["source"] = "unit-test" }
            });

        var mapped = RealDesignConversionCoreClient.MapToInternalRequest(request);

        Assert.Equal("dcj-map", mapped.JobId);
        Assert.Equal("request-map", mapped.RequestId);
        Assert.Equal(DesignConversionSourceKind.Figma, mapped.SourceKind);
        Assert.Equal("<main>mapped</main>", mapped.Html);
        Assert.Equal("main { color: #123456; }", mapped.Css);
        Assert.Equal("{\"node\":\"root\"}", mapped.DesignTreePayload.Content);
        Assert.Equal("{\"node\":\"original\"}", mapped.OriginalTreePayload.Content);
        Assert.Equal("pro", mapped.Budget.Tier);
        Assert.Equal("user-map", mapped.Budget.UserId);
        Assert.Equal(2, mapped.Budget.MaxImprovementCandidates);
        Assert.Equal(3, mapped.Budget.MaxRenderQaRuns);
        Assert.Equal(12.50m, mapped.Budget.BudgetUsd);
        Assert.Equal("tenant-map", mapped.Provenance["tenant_id"]);
        Assert.Equal("unit-test", mapped.Provenance["source"]);
        var resolution = Assert.Single(mapped.RenderResolutions);
        Assert.Equal(1024, resolution.Width);
        Assert.Equal(768, resolution.Height);
        Assert.Equal(2.0, resolution.DeviceScaleFactor);
        var render = Assert.Single(mapped.DesignRenders);
        Assert.Equal("artifact://expected/render.png", render.Uri);
        Assert.Equal("sha256:expected", render.Sha256);
        Assert.Empty(mapped.Screenshots);
    }

    [Fact]
    public void RealCoreClientExtractsHtmlAndCssFromSourceTreeWhenTopLevelFieldsAreAbsent()
    {
        var request = new DesignConversionCoreDispatchRequest(
            "dcj-source-tree",
            "tenant-map",
            "canva-design",
            "canva://design/demo",
            ["angular"],
            new Dictionary<string, object?>
            {
                ["sourceTree"] = new Dictionary<string, object?>
                {
                    ["files"] = new object?[]
                    {
                        new Dictionary<string, object?> { ["path"] = "canva-selection.json", ["content"] = "{\"selection\":\"hero\"}" },
                        new Dictionary<string, object?> { ["path"] = "generated.css", ["content"] = ".hero { color: #336699; }" },
                        new Dictionary<string, object?> { ["path"] = "generated.html", ["content"] = "<main class=\"hero\">from tree</main>" }
                    }
                }
            });

        var mapped = RealDesignConversionCoreClient.MapToInternalRequest(request);

        Assert.Equal(DesignConversionSourceKind.Canva, mapped.SourceKind);
        Assert.Equal("<main class=\"hero\">from tree</main>", mapped.Html);
        Assert.Equal(".hero { color: #336699; }", mapped.Css);
        Assert.Equal("{\"selection\":\"hero\"}", mapped.DesignTreePayload.Content);
    }

    [Fact]
    public void RealCoreClientMapsSubmittedScreenshotsIntoCoreEvidence()
    {
        var request = new DesignConversionCoreDispatchRequest(
            "dcj-screenshot",
            "tenant-map",
            "figma-file",
            "figma://file/demo",
            ["angular"],
            new Dictionary<string, object?>
            {
                ["html"] = "<main>mapped</main>",
                ["screenshots"] = new object?[]
                {
                    new Dictionary<string, object?>
                    {
                        ["artifactId"] = "reference-1",
                        ["uri"] = "artifact://platform/reference.png",
                        ["sha256"] = "sha256:reference",
                        ["width"] = 390,
                        ["height"] = 844
                    }
                },
                ["viewportWidth"] = 390,
                ["viewportHeight"] = 844
            });

        var mapped = RealDesignConversionCoreClient.MapToInternalRequest(request);

        var screenshot = Assert.Single(mapped.Screenshots);
        Assert.Equal("reference-1", screenshot.ArtifactId);
        Assert.Equal("artifact://platform/reference.png", screenshot.Uri);
        Assert.Equal("sha256:reference", screenshot.Sha256);
        Assert.Equal(390, screenshot.Width);
        Assert.Equal(844, screenshot.Height);
        Assert.Empty(mapped.DesignRenders);
    }

    [Fact]
    public async Task RealCoreSubmitPersistsCompletedEvaluationResult()
    {
        var realCore = new RealDesignConversionCoreClient(new DesignConversionInternalService(
            new DeterministicDesignConversionRenderProvider(),
            new DeterministicDesignConversionComparisonProvider(),
            routingFlow: null));
        var fixture = FlowFixture.Create(realCore);
        using var _ = fixture.PushContext("master", "platform_admin");

        var result = await fixture.Service.SubmitJobAsync(ValidRequest() with
        {
            IdempotencyKey = "real-core-completed",
            Options = new Dictionary<string, object?>
            {
                ["requestId"] = "real-core-completed",
                ["sourceTree"] = new Dictionary<string, object?>
                {
                    ["generated.html"] = "<main>real core</main>",
                    ["generated.css"] = "main { color: #123456; }",
                    ["canva-selection.json"] = "{\"selection\":\"root\"}"
                },
                ["screenshots"] = new object?[]
                {
                    new Dictionary<string, object?>
                    {
                        ["uri"] = "artifact://platform/reference.png",
                        ["sha256"] = "sha256:reference"
                    }
                }
            }
        });

        var submitted = Assert.IsType<DesignConversionJobStatusResponse>(result.Data?.Body);
        var status = await fixture.Service.GetJobStatusAsync(submitted.JobId);
        var body = Assert.IsType<DesignConversionJobStatusResponse>(status.Data?.Body);

        Assert.Equal(202, result.Data?.HttpStatus);
        Assert.Equal(200, status.Data?.HttpStatus);
        Assert.Equal("completed", body.Status);
        Assert.StartsWith("core-", body.CoreProviderJobId, StringComparison.Ordinal);
        Assert.NotNull(body.Evaluation);
        Assert.Equal("<main>real core</main>", body.Evaluation!.CurrentHtml);
        Assert.Equal("main { color: #123456; }", body.Evaluation.CurrentCss);
        Assert.NotEmpty(body.Evaluation.ComparisonArtifacts);
        Assert.NotEmpty(body.Evaluation.EvidenceRefs);
    }

    [Fact]
    public async Task RealCoreClientMapsCoreValidationFailureToDispatchFailure()
    {
        var client = new RealDesignConversionCoreClient(new DesignConversionInternalService(
            new DeterministicDesignConversionRenderProvider(),
            new DeterministicDesignConversionComparisonProvider(),
            routingFlow: null));
        var request = new DesignConversionCoreDispatchRequest(
            "dcj-fail",
            "tenant-fail",
            "html",
            "inline://failure",
            ["angular"],
            new Dictionary<string, object?>
            {
                ["maxRenderQaRuns"] = 0
            });

        var result = await client.DispatchAsync(request);

        Assert.False(result.Accepted);
        Assert.Null(result.ProviderJobId);
        Assert.Equal("RENDER_QA_BUDGET_REQUIRED", result.ErrorCode);
        Assert.Equal("DesignConversion render QA budget must be greater than zero.", result.ErrorMessage);
    }

    [Fact]
    public void FlowRegistrationUsesRealCoreClientByDefaultAndKeepsFakeFallbackExplicit()
    {
        var productionServices = new ServiceCollection();
        productionServices.AddDesignConversionIntakeFlow();

        Assert.Contains(
            productionServices,
            service => service.ServiceType == typeof(IDesignConversionCoreClient) &&
                       service.ImplementationType == typeof(RealDesignConversionCoreClient));
        Assert.Contains(
            productionServices,
            service => service.ServiceType == typeof(IDesignConversionJobStore) &&
                       service.ImplementationType == typeof(FileBackedDesignConversionJobStore));

        var fallbackServices = new ServiceCollection();
        fallbackServices.AddScaffoldFakeDesignConversionCoreClient();

        Assert.Contains(
            fallbackServices,
            service => service.ServiceType == typeof(IDesignConversionCoreClient) &&
                       service.ImplementationType == typeof(ScaffoldFakeDesignConversionCoreClient));
    }

    private static DesignConversionSubmitJobRequest ValidRequest() =>
        new("master", "figma-file", "figma://file/demo", ["angular"], null, new Dictionary<string, object?> { ["quality"] = "scaffold" });

    private static FileBackedDesignConversionJobStore CreateJobStore(string directoryPath) =>
        new(new DesignConversionJobStoreOptions { DirectoryPath = directoryPath });

    private static string CreateTempJobStoreDirectory() =>
        Path.Combine(Path.GetTempPath(), "xiigen-flow-47-tests", Guid.NewGuid().ToString("n"));

    private static void DeleteTempJobStoreDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static JsonElement AssertJsonOption(IReadOnlyDictionary<string, object?> options, string key)
    {
        var option = Assert.IsType<JsonElement>(options[key]);
        return option;
    }

    private static string FindFlowRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "contracts", "endpoints", "flow-endpoints.json");
            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate FLOW-47 root containing contracts/endpoints/flow-endpoints.json.");
    }

    private sealed record FlowFixture(
        DesignConversionIntakeService Service,
        CountingCoreClient Core,
        AsyncLocalTenantContextAccessor TenantAccessor,
        AsyncLocalCorrelationContextAccessor CorrelationAccessor)
    {
        public static FlowFixture Create(IDesignConversionCoreClient? coreClient = null, string? jobStoreDirectory = null)
        {
            var jobs = CreateJobStore(jobStoreDirectory ?? CreateTempJobStoreDirectory());
            var core = coreClient as CountingCoreClient ?? new CountingCoreClient(coreClient);
            var tenant = new AsyncLocalTenantContextAccessor();
            var correlation = new AsyncLocalCorrelationContextAccessor();
            var service = new DesignConversionIntakeService(jobs, core, tenant, correlation, NullLogger<DesignConversionIntakeService>.Instance);
            return new FlowFixture(service, core, tenant, correlation);
        }

        public IDisposable PushContext(string tenantId, string role)
        {
            var correlationId = Guid.NewGuid().ToString("n");
            var correlationScope = CorrelationAccessor.Push(new CorrelationContext(correlationId, "test", DateTimeOffset.UtcNow));
            var tenantScope = TenantAccessor.Push(new TenantContext(tenantId, "test-user", new HashSet<string>([role], StringComparer.Ordinal), correlationId, "test", DateTimeOffset.UtcNow));
            return new CompositeScope(correlationScope, tenantScope);
        }
    }

    private sealed class CountingCoreClient : IDesignConversionCoreClient
    {
        private readonly IDesignConversionCoreClient _inner;
        public int DispatchCount { get; private set; }

        public CountingCoreClient(IDesignConversionCoreClient? inner = null)
        {
            _inner = inner ?? new ScaffoldFakeDesignConversionCoreClient();
        }

        public async Task<DesignConversionCoreDispatchResult> DispatchAsync(DesignConversionCoreDispatchRequest request, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            return await _inner.DispatchAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class FailingCoreClient : IDesignConversionCoreClient
    {
        public static readonly FailingCoreClient Instance = new();

        public Task<DesignConversionCoreDispatchResult> DispatchAsync(DesignConversionCoreDispatchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(DesignConversionCoreDispatchResult.Failed("CORE_CLIENT_UNAVAILABLE", "Synthetic failure for FLOW-47 scaffold test."));
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
