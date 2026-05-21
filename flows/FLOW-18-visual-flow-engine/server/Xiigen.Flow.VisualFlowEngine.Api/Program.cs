using System.Text.Json;
using Xiigen.Flow.VisualFlowEngine.Application;
using Xiigen.Flow.VisualFlowEngine.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVisualFlowEngineFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = VisualFlowEngineConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = VisualFlowEngineConstants.FlowId }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString())
        ? tenantHeader.ToString()
        : "acme-corp";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) && !string.IsNullOrWhiteSpace(correlationHeader.ToString())
        ? correlationHeader.ToString()
        : Guid.NewGuid().ToString("n");
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader.ToString())
        ? roleHeader.ToString()
        : "anonymous";

    using var correlationScope = correlationAccessor.Push(new CorrelationContext(correlation, "http", DateTimeOffset.UtcNow));
    using var tenantScope = tenantAccessor.Push(new TenantContext(
        tenant,
        role == "anonymous" ? null : "local-user",
        new HashSet<string>([role], StringComparer.Ordinal),
        correlation,
        "http-header",
        DateTimeOffset.UtcNow));

    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapGet($"{VisualFlowEngineConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = VisualFlowEngineConstants.FlowId,
        summary = "Preview a visual flow canvas, node palette, type checks, test traces, and deploy readiness without exposing tenant-private execution data.",
        tabs = new[] { "canvas", "node types", "test run", "publish" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(VisualFlowEngineConstants.DynamicEndpoint, async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.AuthorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.SaveCanvasAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{VisualFlowEngineConstants.DynamicEndpoint}/publish", async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.AuthorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.PublishFlowAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{VisualFlowEngineConstants.DynamicEndpoint}/node-types", async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.PlatformOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RegisterNodeTypeAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{VisualFlowEngineConstants.DynamicEndpoint}/injections", async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.PlatformOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.InjectCodeAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{VisualFlowEngineConstants.DynamicEndpoint}/test-runs", async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.AuthorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RunTestAsync(request, cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{VisualFlowEngineConstants.DynamicEndpoint}/dashboard", async (
    HttpContext context,
    VisualFlowEngineService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, VisualFlowEngineConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess && result.Data is not null
        ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK)
        : ErrorResult(result.Error);
});

app.Run();

static IResult? RequireRole(HttpContext context, params string[] allowedRoles)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var auth) ||
        !auth.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    if (ValidateTokenTenantWhenPresent(context) is { } tokenCheck)
    {
        return tokenCheck;
    }

    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader)
        ? roleHeader.ToString()
        : string.Empty;

    return allowedRoles.Contains(role, StringComparer.Ordinal)
        ? null
        : Results.Json(new { code = "AUTH_WRONG_ROLE" }, statusCode: StatusCodes.Status403Forbidden);
}

static IResult? ValidateTokenTenantWhenPresent(HttpContext context)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var auth) ||
        !auth.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
    {
        return null;
    }

    var token = auth.ToString()["Bearer ".Length..];
    if (TryReadTenantFromLocalTestToken(token, out var tokenTenant) &&
        context.Request.Headers.TryGetValue("X-Tenant-Id", out var requestTenant) &&
        !string.Equals(tokenTenant, requestTenant.ToString(), StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    return null;
}

static IResult ToOutcomeHttpResult(DataProcessResult<VisualFlowEngineOutcome> result)
{
    if (result.IsSuccess && result.Data is not null)
    {
        return Results.Json(result.Data.Body, statusCode: result.Data.HttpStatus);
    }

    return ErrorResult(result.Error);
}

static IResult ErrorResult(DataProcessError? error)
{
    error ??= new DataProcessError("UNKNOWN", "Unknown failure");
    var status = error.Code switch
    {
        "CONCURRENT_UPDATE" => StatusCodes.Status409Conflict,
        "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
        "BAD_REQUEST" => StatusCodes.Status400BadRequest,
        "TIMEOUT" => StatusCodes.Status504GatewayTimeout,
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "FLOW_NOT_FOUND" => StatusCodes.Status404NotFound,
        "NODE_TYPE_WRITE_FAILED" => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    return Results.Json(new { code = error.Code, message = error.Message, details = error.Details }, statusCode: status);
}

static async Task<IReadOnlyDictionary<string, object?>> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.ContentLength is 0 or null)
    {
        return new Dictionary<string, object?>();
    }

    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(
        request.Body,
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        cancellationToken).ConfigureAwait(false);
    return body ?? new Dictionary<string, object?>();
}

static bool TryReadTenantFromLocalTestToken(string token, out string tenant)
{
    tenant = string.Empty;
    const string marker = "tenant:";
    var markerIndex = token.IndexOf(marker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
        return false;
    }

    var start = markerIndex + marker.Length;
    var end = token.IndexOf(';', start);
    tenant = end < 0 ? token[start..] : token[start..end];
    return tenant.Length > 0;
}
