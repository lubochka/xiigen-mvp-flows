using System.Text.Json;
using Xiigen.Flow.SharableFlowsMarketplace.Application;
using Xiigen.Flow.SharableFlowsMarketplace.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSharableFlowsMarketplaceFlow();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = SharableFlowsMarketplaceConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = SharableFlowsMarketplaceConstants.FlowId }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString()) ? tenantHeader.ToString() : "acme-corp";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) && !string.IsNullOrWhiteSpace(correlationHeader.ToString()) ? correlationHeader.ToString() : Guid.NewGuid().ToString("n");
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader.ToString()) ? roleHeader.ToString() : "anonymous";
    using var correlationScope = correlationAccessor.Push(new CorrelationContext(correlation, "http", DateTimeOffset.UtcNow));
    using var tenantScope = tenantAccessor.Push(new TenantContext(tenant, role == "anonymous" ? null : "local-user", new HashSet<string>([role], StringComparer.Ordinal), correlation, "http-header", DateTimeOffset.UtcNow));
    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapGet($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/public-preview", () => Results.Json(new { flow = SharableFlowsMarketplaceConstants.FlowId, summary = "Browse shareable flow packages, RAG blueprints, install modes, publisher listings, and logic-only marketplace templates without exposing tenant data.", tabs = new[] { "discover", "publish", "install", "settlement", "sandbox" } }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/rules/validate", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.CuratorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateRulePack(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapPost(SharableFlowsMarketplaceConstants.DynamicEndpoint, async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.PublisherRoles);
    if (auth is not null) return auth;
    var result = await service.PublishPackageAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), context.Request.Headers["If-Match"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/install", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.InstallerRoles);
    if (auth is not null) return auth;
    var result = await service.InstallTemplateAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/rag-blueprints/publish", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.PublisherRoles);
    if (auth is not null) return auth;
    var result = await service.PublishRagBlueprintAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/settlements/record", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.CuratorRoles);
    if (auth is not null) return auth;
    var result = await service.RecordUsageAndSettlementAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/sandboxes/provision", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.InstallerRoles);
    if (auth is not null) return auth;
    var result = await service.ProvisionSandboxAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.CuratorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateTaskCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapGet($"{SharableFlowsMarketplaceConstants.DynamicEndpoint}/dashboard", async (HttpContext context, SharableFlowsMarketplaceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SharableFlowsMarketplaceConstants.InspectorRoles);
    if (auth is not null) return auth;
    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK) : Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
});

app.Run();

static IResult? RequireRole(HttpContext context, IReadOnlyCollection<string> allowedRoles)
{
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) ? NormalizeRole(roleHeader.ToString()) : "anonymous";
    if (allowedRoles.Contains("anonymous", StringComparer.Ordinal) && role == "anonymous") return null;
    if (!context.Request.Headers.TryGetValue("Authorization", out var authorization) || string.IsNullOrWhiteSpace(authorization.ToString())) return Results.Json(new { code = "MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    var token = authorization.ToString();
    if (!token.StartsWith("Bearer local-", StringComparison.Ordinal)) return Results.Json(new { code = "INVALID_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    var tokenPayload = token["Bearer local-".Length..];
    var tokenTenant = tokenPayload.EndsWith("-token", StringComparison.Ordinal) ? tokenPayload[..^"-token".Length] : tokenPayload;
    var requestTenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) ? tenantHeader.ToString() : "acme-corp";
    if (!string.Equals(tokenTenant, requestTenant, StringComparison.Ordinal)) return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    return allowedRoles.Contains(role, StringComparer.Ordinal) ? null : Results.Json(new { code = "WRONG_ROLE", allowedRoles }, statusCode: StatusCodes.Status403Forbidden);
}

static async Task<IReadOnlyDictionary<string, object?>> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.ContentLength is null or 0) return new Dictionary<string, object?>();
    return await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new Dictionary<string, object?>();
}

static IResult ToOutcomeHttpResult(DataProcessResult<SharableFlowsMarketplaceOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}

static string NormalizeRole(string value) => value.Replace('-', '_');