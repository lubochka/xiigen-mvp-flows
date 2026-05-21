using System.Text.Json;
using Xiigen.Flow.DurableSagasCompliance.Application;
using Xiigen.Flow.DurableSagasCompliance.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDurableSagasComplianceFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = DurableSagasComplianceConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = DurableSagasComplianceConstants.FlowId }));

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

app.MapGet($"{DurableSagasComplianceConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = DurableSagasComplianceConstants.FlowId,
        summary = "Preview saga progress, compensation visibility, and compliance evidence without exposing tenant-private audit payloads.",
        tabs = new[] { "operations", "sagas", "compliance" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(DurableSagasComplianceConstants.DynamicEndpoint, async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.TenantOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.StartSagaAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{DurableSagasComplianceConstants.DynamicEndpoint}/compensate", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.TenantOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.CompensateSagaAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{DurableSagasComplianceConstants.DynamicEndpoint}/compliance", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.PlatformOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.WriteComplianceRecordAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{DurableSagasComplianceConstants.DynamicEndpoint}/retention/enforce", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.PlatformOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.EnforceRetentionAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{DurableSagasComplianceConstants.DynamicEndpoint}/exports", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.PlatformOpsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.QueueComplianceExportAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{DurableSagasComplianceConstants.DynamicEndpoint}/operations/me", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.ConsumerRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess && result.Data is not null
        ? Results.Json(new { operations = result.Data.SagaRuns, kpis = result.Data.Kpis }, statusCode: StatusCodes.Status200OK)
        : ErrorResult(result.Error);
});

app.MapGet($"{DurableSagasComplianceConstants.DynamicEndpoint}/dashboard", async (
    HttpContext context,
    DurableSagasComplianceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DurableSagasComplianceConstants.ProtectedRoles);
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

static IResult ToOutcomeHttpResult(DataProcessResult<DurableSagasComplianceOutcome> result)
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
        "SAGA_NOT_FOUND" => StatusCodes.Status404NotFound,
        "AUDIT_RECORD_NOT_FOUND" => StatusCodes.Status404NotFound,
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
