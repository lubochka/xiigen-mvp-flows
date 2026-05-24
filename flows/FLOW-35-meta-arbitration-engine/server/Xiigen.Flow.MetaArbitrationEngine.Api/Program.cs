using System.Text.Json;
using Xiigen.Flow.MetaArbitrationEngine.Application;
using Xiigen.Flow.MetaArbitrationEngine.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMetaArbitrationEngineFlow();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = MetaArbitrationEngineConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = MetaArbitrationEngineConstants.FlowId }));

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

app.MapGet($"{MetaArbitrationEngineConstants.DynamicEndpoint}/public-preview", () => Results.Json(new { flow = MetaArbitrationEngineConstants.FlowId, summary = "Meta-arbitration is a platform governance surface for conflict rounds, verdict comparison, escalation briefings, and append-only decision evidence.", tabs = new[] { "intake", "panel", "verdicts", "briefings", "audit" } }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{MetaArbitrationEngineConstants.DynamicEndpoint}/rules/validate", async (HttpContext context, MetaArbitrationEngineService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, MetaArbitrationEngineConstants.OperatorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateRulePack(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapPost(MetaArbitrationEngineConstants.DynamicEndpoint, async (HttpContext context, MetaArbitrationEngineService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, MetaArbitrationEngineConstants.OperatorRoles);
    if (auth is not null) return auth;
    var result = await service.ResolveConflictAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), context.Request.Headers["If-Match"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{MetaArbitrationEngineConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, MetaArbitrationEngineService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, MetaArbitrationEngineConstants.OperatorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateTaskCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapGet($"{MetaArbitrationEngineConstants.DynamicEndpoint}/dashboard", async (HttpContext context, MetaArbitrationEngineService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, MetaArbitrationEngineConstants.InspectorRoles);
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

static IResult ToOutcomeHttpResult(DataProcessResult<MetaArbitrationEngineOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}

static string NormalizeRole(string value) => value.Replace('-', '_');

