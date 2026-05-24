using System.Text.Json;
using Xiigen.Flow.AdaptiveRagDeepResearch.Application;
using Xiigen.Flow.AdaptiveRagDeepResearch.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAdaptiveRagDeepResearchFlow();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = AdaptiveRagDeepResearchConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = AdaptiveRagDeepResearchConstants.FlowId }));

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

app.MapGet($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/public-preview", () => Results.Json(new { flow = AdaptiveRagDeepResearchConstants.FlowId, summary = "Coordinate tenant-scoped adaptive RAG routing, GraphRAG edits, prompt promotion, feedback learning, and stress evidence without cross-tenant leakage.", tabs = new[] { "routing", "graph", "promotion", "feedback" } }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/rules/validate", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.SupportRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateRulePack(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapPost(AdaptiveRagDeepResearchConstants.DynamicEndpoint, async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ResearchRoles);
    if (auth is not null) return auth;
    var result = await service.SubmitResearchAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), context.Request.Headers["If-Match"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/routing-policy", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ControlPlaneRoles);
    if (auth is not null) return auth;
    var result = await service.UpdateRoutingPolicyAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/prompts/promote", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ControlPlaneRoles);
    if (auth is not null) return auth;
    var result = await service.PromotePromptAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/graph/edit", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ControlPlaneRoles);
    if (auth is not null) return auth;
    var result = await service.EditKnowledgeGraphAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/feedback", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ResearchRoles);
    if (auth is not null) return auth;
    var result = await service.SubmitFeedbackAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/stress/run", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.SupportRoles);
    if (auth is not null) return auth;
    var result = await service.RunStressTestsAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.ControlPlaneRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidatePlanCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapGet($"{AdaptiveRagDeepResearchConstants.DynamicEndpoint}/dashboard", async (HttpContext context, AdaptiveRagDeepResearchService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AdaptiveRagDeepResearchConstants.SupportRoles);
    if (auth is not null) return auth;
    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK) : Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
});

app.Run();

static IResult? RequireRole(HttpContext context, IReadOnlyCollection<string> allowedRoles)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authorization) || string.IsNullOrWhiteSpace(authorization.ToString())) return Results.Json(new { code = "MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    var token = authorization.ToString();
    if (!token.StartsWith("Bearer local-", StringComparison.Ordinal)) return Results.Json(new { code = "INVALID_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    var tokenPayload = token["Bearer local-".Length..];
    var tokenTenant = tokenPayload.EndsWith("-token", StringComparison.Ordinal) ? tokenPayload[..^"-token".Length] : tokenPayload;
    var requestTenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) ? tenantHeader.ToString() : "acme-corp";
    if (!string.Equals(tokenTenant, requestTenant, StringComparison.Ordinal)) return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) ? roleHeader.ToString() : string.Empty;
    return allowedRoles.Contains(role, StringComparer.Ordinal) ? null : Results.Json(new { code = "WRONG_ROLE", allowedRoles }, statusCode: StatusCodes.Status403Forbidden);
}

static async Task<IReadOnlyDictionary<string, object?>> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.ContentLength is null or 0) return new Dictionary<string, object?>();
    return await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new Dictionary<string, object?>();
}

static IResult ToOutcomeHttpResult(DataProcessResult<AdaptiveRagDeepResearchOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}
