using System.Text.Json;
using Xiigen.Flow.BundleActivation.Application;
using Xiigen.Flow.BundleActivation.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBundleActivationFlow();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = "FLOW-00" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = "FLOW-00" }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
        ? tenantHeader.ToString()
        : "acme-corp";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader)
        ? correlationHeader.ToString()
        : Guid.NewGuid().ToString("n");

    using var correlationScope = correlationAccessor.Push(new CorrelationContext(correlation, "http", DateTimeOffset.UtcNow));
    using var tenantScope = tenantAccessor.Push(new TenantContext(
        tenant,
        context.User.Identity?.Name,
        new HashSet<string> { context.Request.Headers.TryGetValue("X-Role", out var roleHeader) ? roleHeader.ToString() : "anonymous" },
        correlation,
        "http-header",
        DateTimeOffset.UtcNow));

    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapPost(BundleActivationConstants.DynamicEndpoint, async (
    HttpContext context,
    BundleActivationService service,
    IDocumentStore documents,
    CancellationToken cancellationToken) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var auth) ||
        !auth.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_MISSING_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = auth.ToString()["Bearer ".Length..];
    if (TryReadTenantFromLocalTestToken(token, out var tokenTenant) &&
        context.Request.Headers.TryGetValue("X-Tenant-Id", out var requestTenant) &&
        !string.Equals(tokenTenant, requestTenant.ToString(), StringComparison.Ordinal))
    {
        return Results.Json(new { code = "AUTH_CROSS_TENANT_TOKEN" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader)
        ? roleHeader.ToString()
        : string.Empty;
    if (role is not ("tenant_admin" or "platform_admin"))
    {
        return Results.Json(new { code = "AUTH_WRONG_ROLE" }, statusCode: StatusCodes.Status403Forbidden);
    }

    await EnsureSeedDataAsync(documents, cancellationToken).ConfigureAwait(false);

    var request = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
    var result = await service.ActivateAsync(
        request,
        context.Request.Headers.IfMatch.ToString(),
        context.Request.Headers["X-Idempotency-Key"].ToString(),
        cancellationToken).ConfigureAwait(false);

    if (result.IsSuccess && result.Data is not null)
    {
        return Results.Json(result.Data.Body, statusCode: result.Data.HttpStatus);
    }

    var error = result.Error ?? new DataProcessError("UNKNOWN", "Unknown failure");
    var status = error.Code switch
    {
        "CONCURRENT_UPDATE" => StatusCodes.Status409Conflict,
        "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
        "BAD_REQUEST" => StatusCodes.Status400BadRequest,
        "MISSING_BUNDLE_ID" => StatusCodes.Status400BadRequest,
        "TIMEOUT" => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError
    };
    return Results.Json(new { code = error.Code, message = error.Message, details = error.Details }, statusCode: status);
});

app.Run();

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
        JsonValueKind.Number when element.TryGetInt64(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJson).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(prop => prop.Name, prop => ConvertJson(prop.Value)),
        _ => null
    };

static async Task EnsureSeedDataAsync(IDocumentStore documents, CancellationToken cancellationToken)
{
    var existing = await documents.GetAsync(BundleActivationConstants.SolutionBundles, "BDL-core-platform-v2", cancellationToken).ConfigureAwait(false);
    if (existing.Data is not null)
    {
        return;
    }

    await documents.UpsertAsync(
        BundleActivationConstants.SolutionBundles,
        "BDL-core-platform-v2",
        new Dictionary<string, object?>
        {
            ["bundleId"] = "BDL-core-platform-v2",
            ["requiredFlows"] = new[] { "FLOW-01", "FLOW-02" },
            ["minFlowVersions"] = new Dictionary<string, object?> { ["FLOW-01"] = "v1", ["FLOW-02"] = "v1" },
            ["defaultFreedomConfig"] = new Dictionary<string, object?> { ["bundle00_activation_timeout_ms"] = 30000 },
            ["status"] = "PENDING"
        },
        cancellationToken: cancellationToken).ConfigureAwait(false);

    await documents.UpsertAsync(
        BundleActivationConstants.FlowLifecycle,
        "FLOW-01",
        new Dictionary<string, object?> { ["flowId"] = "FLOW-01", ["version"] = "v1", ["status"] = "READY" },
        cancellationToken: cancellationToken).ConfigureAwait(false);

    await documents.UpsertAsync(
        BundleActivationConstants.FlowLifecycle,
        "FLOW-02",
        new Dictionary<string, object?> { ["flowId"] = "FLOW-02", ["version"] = "v1", ["status"] = "READY" },
        cancellationToken: cancellationToken).ConfigureAwait(false);
}

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
