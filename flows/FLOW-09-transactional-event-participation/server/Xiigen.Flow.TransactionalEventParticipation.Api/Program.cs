using System.Text.Json;
using Xiigen.Flow.TransactionalEventParticipation.Application;
using Xiigen.Flow.TransactionalEventParticipation.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransactionalEventParticipationFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = TransactionalEventParticipationConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = TransactionalEventParticipationConstants.FlowId }));

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

app.MapGet($"{TransactionalEventParticipationConstants.DynamicEndpoint}/public-preview", () =>
    Results.Json(new
    {
        flow = TransactionalEventParticipationConstants.FlowId,
        summary = "Ticket purchase, seat reservation, payment clearing, QR token issue, refund, waitlist, and attendance analytics",
        states = new[] { "purchase", "ticket", "refund" }
    }, statusCode: StatusCodes.Status200OK));

app.MapPost(TransactionalEventParticipationConstants.DynamicEndpoint, async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, TransactionalEventParticipationConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.StartParticipationAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);

    return ToOutcomeHttpResult(result);
});

app.MapGet($"{TransactionalEventParticipationConstants.DynamicEndpoint}/tickets", async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, TransactionalEventParticipationConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetTicketsAsync(cancellationToken).ConfigureAwait(false);
    return result.IsSuccess && result.Data is not null
        ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK)
        : ErrorResult(result.Error);
});

app.MapGet($"{TransactionalEventParticipationConstants.DynamicEndpoint}/ticket/{{ticketId}}", async (
    string ticketId,
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, TransactionalEventParticipationConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetTicketAsync(ticketId, cancellationToken).ConfigureAwait(false);
    return result.IsSuccess && result.Data is not null
        ? Results.Json(result.Data, statusCode: StatusCodes.Status200OK)
        : ErrorResult(result.Error);
});

app.MapPost($"{TransactionalEventParticipationConstants.DynamicEndpoint}/refund", async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, TransactionalEventParticipationConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RequestRefundAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{TransactionalEventParticipationConstants.DynamicEndpoint}/waitlist", async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, TransactionalEventParticipationConstants.ProtectedRoles);
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.JoinWaitlistAsync(request, context.Request.Headers["X-Idempotency-Key"].ToString(), cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapPost($"{TransactionalEventParticipationConstants.DynamicEndpoint}/redeem-token", async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "event_organiser", "tenant_admin", "platform_admin", "platform_support");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RedeemTokenAsync(request, cancellationToken).ConfigureAwait(false);
    return ToOutcomeHttpResult(result);
});

app.MapGet($"{TransactionalEventParticipationConstants.DynamicEndpoint}/analytics", async (
    HttpContext context,
    TransactionalEventParticipationService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_admin", "event_organiser", "platform_admin", "platform_support");
    if (auth is not null)
    {
        return auth;
    }

    var result = await service.GetAnalyticsAsync(cancellationToken).ConfigureAwait(false);
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

static IResult ToOutcomeHttpResult(DataProcessResult<TransactionalEventParticipationOutcome> result)
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
        "INVALID_TOKEN" => StatusCodes.Status400BadRequest,
        "TIMEOUT" => StatusCodes.Status504GatewayTimeout,
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "GROUP_BOOKING_ROLLED_BACK" => StatusCodes.Status409Conflict,
        "SOLD_OUT" => StatusCodes.Status409Conflict,
        "PAYMENT_DECLINED" => StatusCodes.Status402PaymentRequired,
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
