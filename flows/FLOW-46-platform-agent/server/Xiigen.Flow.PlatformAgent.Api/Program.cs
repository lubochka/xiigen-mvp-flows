using System.Text.Json;
using Xiigen.Flow.PlatformAgent.Application;
using Xiigen.Flow.PlatformAgent.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlatformAgentFlow();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = PlatformAgentConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = PlatformAgentConstants.FlowId }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString()) ? tenantHeader.ToString() : "master";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) && !string.IsNullOrWhiteSpace(correlationHeader.ToString()) ? correlationHeader.ToString() : Guid.NewGuid().ToString("n");
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader.ToString()) ? NormalizeRole(roleHeader.ToString()) : "anonymous";
    using var correlationScope = correlationAccessor.Push(new CorrelationContext(correlation, "http", DateTimeOffset.UtcNow));
    using var tenantScope = tenantAccessor.Push(new TenantContext(tenant, role == "anonymous" ? null : "local-user", new HashSet<string>([role], StringComparer.Ordinal), correlation, "http-header", DateTimeOffset.UtcNow));
    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapGet($"{PlatformAgentConstants.DynamicEndpoint}/public-preview", () => Results.Json(new { flow = PlatformAgentConstants.FlowId, packageName = PlatformAgentConstants.PackageName, summary = "Platform Agent orchestrates super-engine assistance with master-scope governance, audited tenant action publishing, super-judge arbitration, and sanitized pattern contribution.", tabs = new[] { "run", "scope", "judge", "actions", "patterns", "evidence" } }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{PlatformAgentConstants.DynamicEndpoint}/rules/validate", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.OperatorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateRulePack(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapPost($"{PlatformAgentConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.OperatorRoles);
    if (auth is not null) return auth;
    return ToOutcomeHttpResult(service.ValidateTaskCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false)));
});

app.MapPost($"{PlatformAgentConstants.DynamicEndpoint}/run", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.OperatorRoles);
    if (auth is not null) return auth;
    var result = await service.RunAgentAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{PlatformAgentConstants.DynamicEndpoint}/actions", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.OperatorRoles);
    if (auth is not null) return auth;
    var body = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.PublishActionAsync(ReadValue(body, "sessionId", "api-action"), body, cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status201Created) : Results.Json(result.Error, statusCode: StatusCodes.Status400BadRequest);
});

app.MapPost($"{PlatformAgentConstants.DynamicEndpoint}/contributions", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.OperatorRoles);
    if (auth is not null) return auth;
    var body = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.ContributePatternAsync(ReadValue(body, "sessionId", "api-contribution"), body, cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status201Created) : Results.Json(result.Error, statusCode: StatusCodes.Status400BadRequest);
});

app.MapGet($"{PlatformAgentConstants.DynamicEndpoint}/dashboard", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.InspectorRoles);
    if (auth is not null) return auth;
    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK) : Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
});

app.MapGet("/api/agent/sessions", async (HttpContext context, PlatformAgentService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, PlatformAgentConstants.InspectorRoles);
    if (auth is not null) return auth;
    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(new { sessions = result.Data!.Sessions }, statusCode: StatusCodes.Status200OK) : Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
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
    var requestTenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) ? tenantHeader.ToString() : "master";
    if (!string.Equals(tokenTenant, requestTenant, StringComparison.Ordinal)) return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    return allowedRoles.Contains(role, StringComparer.Ordinal) ? null : Results.Json(new { code = "WRONG_ROLE", allowedRoles }, statusCode: StatusCodes.Status403Forbidden);
}

static async Task<IReadOnlyDictionary<string, object?>> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.ContentLength is null or 0) return new Dictionary<string, object?>();
    return await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new Dictionary<string, object?>();
}

static IResult ToOutcomeHttpResult(DataProcessResult<PlatformAgentOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}

static string ReadValue(IReadOnlyDictionary<string, object?> body, string key, string fallback) => body.TryGetValue(key, out var value) ? value?.ToString() ?? fallback : fallback;
static string NormalizeRole(string value) => value.Replace('-', '_');