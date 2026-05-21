using System.Text.Json;
using Xiigen.Flow.FreelancerMarketplace.Application;
using Xiigen.Flow.FreelancerMarketplace.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFreelancerMarketplaceFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = FreelancerMarketplaceConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = FreelancerMarketplaceConstants.FlowId }));

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
        new HashSet<string> { role },
        correlation,
        "http-header",
        DateTimeOffset.UtcNow));

    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapGet($"{FreelancerMarketplaceConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = FreelancerMarketplaceConstants.FlowId,
        summary = "Preview freelancer gigs, milestone contracts, escrow releases, disputes, and bidirectional reviews.",
        tabs = new[] { "gig posting", "contracts", "escrow", "reviews" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(FreelancerMarketplaceConstants.DynamicEndpoint, async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, FreelancerMarketplaceConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.AcceptGigAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{FreelancerMarketplaceConstants.DynamicEndpoint}/contracts", async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_admin", "business_partner", "event_organiser", "platform_admin", "platform_support");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.UpdateContractAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{FreelancerMarketplaceConstants.DynamicEndpoint}/milestones/release", async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, FreelancerMarketplaceConstants.AdminRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.ReleaseMilestoneAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{FreelancerMarketplaceConstants.DynamicEndpoint}/milestones/dispute", async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, FreelancerMarketplaceConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.DisputeMilestoneAsync(request, cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{FreelancerMarketplaceConstants.DynamicEndpoint}/reviews", async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_user", "freelancer", "business_partner", "tenant_admin", "platform_admin", "platform_support");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.SubmitReviewAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{FreelancerMarketplaceConstants.DynamicEndpoint}/dashboard", async (
    HttpContext context,
    FreelancerMarketplaceService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, FreelancerMarketplaceConstants.ProtectedRoles);
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

static IResult ToOutcomeHttpResult(DataProcessResult<FreelancerMarketplaceOutcome> result)
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
        "RELEASE_FAILED" => StatusCodes.Status500InternalServerError,
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
