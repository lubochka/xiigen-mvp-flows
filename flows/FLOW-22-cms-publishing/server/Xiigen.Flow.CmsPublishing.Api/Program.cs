using System.Text.Json;
using Xiigen.Flow.CmsPublishing.Application;
using Xiigen.Flow.CmsPublishing.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCmsPublishingFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = CmsPublishingConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = CmsPublishingConstants.FlowId }));

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

app.MapGet($"{CmsPublishingConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = CmsPublishingConstants.FlowId,
        summary = "Preview CMS publishing, approval stages, scheduled release, and aggregate engagement without exposing tenant-private drafts.",
        tabs = new[] { "drafts", "approvals", "schedule", "analytics" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(CmsPublishingConstants.DynamicEndpoint, async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.AuthorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.PublishContentAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{CmsPublishingConstants.DynamicEndpoint}/approvals", async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.ApprovalRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RecordApprovalAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{CmsPublishingConstants.DynamicEndpoint}/schedule", async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.PublisherRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.SchedulePublishAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{CmsPublishingConstants.DynamicEndpoint}/analytics", async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.AnalyticsRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RecordAnalyticsAsync(
        request,
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{CmsPublishingConstants.DynamicEndpoint}/plan/validate", async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.PublisherRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = service.ValidatePlanCoverage(request);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{CmsPublishingConstants.DynamicEndpoint}/dashboard", async (
    HttpContext context,
    CmsPublishingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, CmsPublishingConstants.ReaderRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK) : Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
});

app.Run();

static IResult? RequireRole(HttpContext context, IReadOnlyCollection<string> allowedRoles)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authorization) || string.IsNullOrWhiteSpace(authorization.ToString()))
    {
        return Results.Json(new { code = "MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = authorization.ToString();
    if (!token.StartsWith("Bearer local-", StringComparison.Ordinal))
    {
        return Results.Json(new { code = "INVALID_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var tokenPayload = token["Bearer local-".Length..];
    var tokenTenant = tokenPayload.EndsWith("-token", StringComparison.Ordinal)
        ? tokenPayload[..^"-token".Length]
        : tokenPayload;
    var requestTenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) ? tenantHeader.ToString() : "acme-corp";
    if (!string.Equals(tokenTenant, requestTenant, StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) ? roleHeader.ToString() : string.Empty;
    if (!allowedRoles.Contains(role, StringComparer.Ordinal))
    {
        return Results.Json(new { code = "WRONG_ROLE", allowedRoles }, statusCode: StatusCodes.Status403Forbidden);
    }

    return null;
}

static async Task<IReadOnlyDictionary<string, object?>> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (request.ContentLength is null or 0)
    {
        return new Dictionary<string, object?>();
    }

    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
    return body ?? new Dictionary<string, object?>();
}

static IResult ToOutcomeHttpResult(DataProcessResult<CmsPublishingOutcome> result)
{
    if (result.IsFailure)
    {
        return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }

    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}