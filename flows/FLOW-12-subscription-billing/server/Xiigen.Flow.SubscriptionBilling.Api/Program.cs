using System.Text.Json;
using Xiigen.Flow.SubscriptionBilling.Application;
using Xiigen.Flow.SubscriptionBilling.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSubscriptionBillingFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = SubscriptionBillingConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = SubscriptionBillingConstants.FlowId }));

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
        role == "anonymous" || role == "public_marketplace_visitor" ? null : "local-user",
        new HashSet<string> { role },
        correlation,
        "http-header",
        DateTimeOffset.UtcNow));

    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapGet($"{SubscriptionBillingConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = SubscriptionBillingConstants.FlowId,
        summary = "Publish subscription plans, activate vault-backed subscriptions, charge recurring invoices, and report revenue.",
        states = new[] { "plan", "subscription", "billing-cycle", "analytics" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(SubscriptionBillingConstants.DynamicEndpoint, async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.BillingOperatorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.PublishPlanAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SubscriptionBillingConstants.DynamicEndpoint}/subscribe", async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.SubscribeAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SubscriptionBillingConstants.DynamicEndpoint}/billing-cycle", async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.BillingOperatorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.ProcessBillingCycleAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SubscriptionBillingConstants.DynamicEndpoint}/cancel", async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.CancelSubscriptionAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{SubscriptionBillingConstants.DynamicEndpoint}/mrr/recalculate", async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.BillingOperatorRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.RecalculateMrrAsync(cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{SubscriptionBillingConstants.DynamicEndpoint}/dashboard", async (
    HttpContext context,
    SubscriptionBillingService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, SubscriptionBillingConstants.ProtectedRoles);
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

static IResult ToOutcomeHttpResult(DataProcessResult<SubscriptionBillingOutcome> result)
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

    var body = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
        request.Body,
        cancellationToken: cancellationToken).ConfigureAwait(false);

    return body?.ToDictionary(pair => pair.Key, pair => ConvertJson(pair.Value)) ?? new Dictionary<string, object?>();
}

static object? ConvertJson(JsonElement element) =>
    element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt32(out var integer) => integer,
        JsonValueKind.Number when element.TryGetInt64(out var number) => number,
        JsonValueKind.Number when element.TryGetDouble(out var floating) => floating,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJson).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ConvertJson(prop.Value)),
        _ => null
    };

static bool TryReadTenantFromLocalTestToken(string token, out string tenantId)
{
    const string prefix = "tenant:";
    tenantId = string.Empty;
    if (!token.StartsWith(prefix, StringComparison.Ordinal))
    {
        return false;
    }

    var remainder = token[prefix.Length..];
    var separator = remainder.IndexOf(':', StringComparison.Ordinal);
    tenantId = separator >= 0 ? remainder[..separator] : remainder;
    return tenantId.Length > 0;
}

public partial class Program;
