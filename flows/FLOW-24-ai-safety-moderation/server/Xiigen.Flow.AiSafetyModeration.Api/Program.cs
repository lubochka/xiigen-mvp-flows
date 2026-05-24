using System.Text.Json;
using Xiigen.Flow.AiSafetyModeration.Application;
using Xiigen.Flow.AiSafetyModeration.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAiSafetyModerationFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = AiSafetyModerationConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = AiSafetyModerationConstants.FlowId }));

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

app.MapGet($"{AiSafetyModerationConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = AiSafetyModerationConstants.FlowId,
        summary = "Preview the learning safety gate, consent posture, moderated lesson flow, and aggregate progress without exposing tenant-private records.",
        tabs = new[] { "consent", "lesson", "progress" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/consent", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.AdminRoles);
    if (auth is not null) return auth;
    var result = await service.EvaluateConsentAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/lesson/compose", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.AuthorRoles);
    if (auth is not null) return auth;
    var result = await service.ComposeLessonAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/quiz/grade", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.StudentRoles);
    if (auth is not null) return auth;
    var result = await service.GradeQuizAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/streak/evaluate", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.StudentRoles);
    if (auth is not null) return auth;
    var result = await service.EvaluateStreakAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/lesson/schedule", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.EducatorRoles);
    if (auth is not null) return auth;
    var result = await service.ScheduleLessonAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/domain-pack/deliver", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.EducatorRoles);
    if (auth is not null) return auth;
    var result = await service.DeliverDomainPackAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/calendar/sync", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.EducatorRoles);
    if (auth is not null) return auth;
    var result = await service.SyncCalendarAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/progress/track", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.StudentRoles);
    if (auth is not null) return auth;
    var result = await service.TrackProgressAsync(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false), context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{AiSafetyModerationConstants.DynamicEndpoint}/plan/validate", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.AdminRoles);
    if (auth is not null) return auth;
    var result = service.ValidatePlanCoverage(await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false));
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{AiSafetyModerationConstants.DynamicEndpoint}/dashboard", async (HttpContext context, AiSafetyModerationService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, AiSafetyModerationConstants.ReaderRoles);
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

static IResult ToOutcomeHttpResult(DataProcessResult<AiSafetyModerationOutcome> result)
{
    if (result.IsFailure) return Results.Json(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
}
