using System.Text.Json;
using Xiigen.Flow.BfaCrossFlowGovernance.Application;
using Xiigen.Flow.BfaCrossFlowGovernance.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBfaCrossFlowGovernanceFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = BfaCrossFlowGovernanceConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = BfaCrossFlowGovernanceConstants.FlowId }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString()) ? tenantHeader.ToString() : "acme-corp";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) && !string.IsNullOrWhiteSpace(correlationHeader.ToString()) ? correlationHeader.ToString() : Guid.NewGuid().ToString("n");
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader.ToString()) ? roleHeader.ToString() : "anonymous";

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

app.MapGet($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = BfaCrossFlowGovernanceConstants.FlowId,
        summary = "Preview the governance arbiter, rule-pack coverage, impact analysis, stress-test posture, and release gate without exposing tenant-private change data.",
        tabs = new[] { "rules", "impact", "release" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/rules/validate", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.OperatorRoles);
    if (auth is not null) return auth;
    var result = service.ValidateRulePack(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false));
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/changes/submit", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.OperatorRoles);
    if (auth is not null) return auth;
    var result = await service.SubmitChangeAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/conflicts/scan", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.ReviewerRoles);
    if (auth is not null) return auth;
    var result = await service.ScanConflictsAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/impact/analyze", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.ArchitectRoles);
    if (auth is not null) return auth;
    var result = await service.AnalyzeImpactAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/stress/run", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.ReviewerRoles);
    if (auth is not null) return auth;
    var result = await service.RunStressTestsAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/release/approve", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.ReleaseRoles);
    if (auth is not null) return auth;
    var result = await service.ApproveReleaseAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.AdminRoles);
    if (auth is not null) return auth;
    var result = service.ValidatePlanCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false));
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{BfaCrossFlowGovernanceConstants.DynamicEndpoint}/dashboard", async (HttpContext context, BfaCrossFlowGovernanceService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, BfaCrossFlowGovernanceConstants.ReaderRoles);
    if (auth is not null) return auth;
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
    var tokenTenant = tokenPayload.EndsWith("-token", StringComparison.Ordinal) ? tokenPayload[..^"-token".Length] : tokenPayload;
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
    if (request.ContentLength is null or 0) return new Dictionary<string, object?>();
    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
    return body ?? new Dictionary<string, object?>();
}

static IResult ToOutcomeHttpResult(DataProcessResult<BfaCrossFlowGovernanceOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}
