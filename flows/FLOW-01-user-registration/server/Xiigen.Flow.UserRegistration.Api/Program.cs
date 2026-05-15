using System.Text.Json;
using Xiigen.Flow.UserRegistration.Application;
using Xiigen.Flow.UserRegistration.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserRegistrationFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = UserRegistrationConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = UserRegistrationConstants.FlowId }));

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

app.MapPost(UserRegistrationConstants.DynamicEndpoint, async (
    HttpContext context,
    UserRegistrationService service,
    CancellationToken cancellationToken) =>
{
    var tokenCheck = ValidateTokenTenantWhenPresent(context);
    if (tokenCheck is not null)
    {
        return tokenCheck;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.RegisterAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);

    return ToRegistrationHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/verify-token", async (
    HttpContext context,
    EmailVerificationService service,
    CancellationToken cancellationToken) =>
{
    var tokenCheck = ValidateTokenTenantWhenPresent(context);
    if (tokenCheck is not null)
    {
        return tokenCheck;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.VerifyAsync(
        ReadString(request, "userId") ?? string.Empty,
        ReadString(request, "token") ?? string.Empty,
        cancellationToken).ConfigureAwait(false);

    return ToEmailHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/resend", async (
    HttpContext context,
    EmailVerificationService service,
    CancellationToken cancellationToken) =>
{
    var tokenCheck = ValidateTokenTenantWhenPresent(context);
    if (tokenCheck is not null)
    {
        return tokenCheck;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.ResendAsync(
        ReadString(request, "userId") ?? string.Empty,
        ReadString(request, "email") ?? string.Empty,
        cancellationToken).ConfigureAwait(false);

    return ToEmailHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/onboarding/workspace", async (
    HttpContext context,
    OnboardingDeliveryService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_user", "tenant_admin", "platform_admin");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.DeliverWorkspaceAsync(ReadString(request, "userId") ?? string.Empty, cancellationToken).ConfigureAwait(false);
    return ToOnboardingHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/onboarding/tutorial", async (
    HttpContext context,
    OnboardingDeliveryService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_user", "tenant_admin", "platform_admin");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.DeliverTutorialAsync(ReadString(request, "userId") ?? string.Empty, cancellationToken).ConfigureAwait(false);
    return ToOnboardingHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/onboarding/invitation", async (
    HttpContext context,
    OnboardingDeliveryService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_user", "tenant_admin", "platform_admin");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.DeliverInvitationAsync(ReadString(request, "userId") ?? string.Empty, cancellationToken).ConfigureAwait(false);
    return ToOnboardingHttpResult(result);
});

app.MapPost($"{UserRegistrationConstants.DynamicEndpoint}/onboarding/complete", async (
    HttpContext context,
    OnboardingDeliveryService service,
    CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, "tenant_user", "tenant_admin", "platform_admin");
    if (auth is not null)
    {
        return auth;
    }

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.CompleteAsync(
        ReadString(request, "userId") ?? string.Empty,
        ReadString(request, "email") ?? string.Empty,
        ReadInt(request, "existingOnboardingMaterialsCount"),
        cancellationToken).ConfigureAwait(false);
    return ToOnboardingHttpResult(result);
});

app.Run();

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

static IResult? RequireRole(HttpContext context, params string[] allowedRoles)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var auth) ||
        !auth.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var tokenCheck = ValidateTokenTenantWhenPresent(context);
    if (tokenCheck is not null)
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

static IResult ToRegistrationHttpResult(DataProcessResult<UserRegistrationOutcome> result)
{
    if (result.IsSuccess && result.Data is not null)
    {
        return Results.Json(result.Data.Body, statusCode: result.Data.HttpStatus);
    }

    return ErrorResult(result.Error);
}

static IResult ToEmailHttpResult(DataProcessResult<EmailVerificationOutcome> result)
{
    if (result.IsSuccess && result.Data is not null)
    {
        return Results.Json(result.Data.Body, statusCode: result.Data.HttpStatus);
    }

    return ErrorResult(result.Error);
}

static IResult ToOnboardingHttpResult(DataProcessResult<OnboardingOutcome> result)
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
        "DUPLICATE_EMAIL" => StatusCodes.Status409Conflict,
        "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
        "BAD_REQUEST" => StatusCodes.Status400BadRequest,
        "INVALID_TOKEN" => StatusCodes.Status400BadRequest,
        "USED_TOKEN" => StatusCodes.Status400BadRequest,
        "EXPIRED_TOKEN" => StatusCodes.Status410Gone,
        "RATE_LIMIT_EXCEEDED" => StatusCodes.Status429TooManyRequests,
        "TIMEOUT" => StatusCodes.Status504GatewayTimeout,
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
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJson).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ConvertJson(prop.Value)),
        _ => null
    };

static string? ReadString(IReadOnlyDictionary<string, object?> source, string key) =>
    source.TryGetValue(key, out var value) ? Convert.ToString(value) : null;

static int ReadInt(IReadOnlyDictionary<string, object?> source, string key) =>
    source.TryGetValue(key, out var value) && int.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0;

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
