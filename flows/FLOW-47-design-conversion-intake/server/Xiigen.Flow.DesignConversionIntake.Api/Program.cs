using System.Text.Json;
using Xiigen.Flow.DesignConversionIntake.Application;
using Xiigen.Flow.DesignConversionIntake.Infrastructure;
using Xiigen.Kernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDesignConversionIntakeFlow(options =>
{
    options.DirectoryPath = builder.Configuration[$"{DesignConversionJobStoreOptions.SectionName}:DirectoryPath"];
    options.FileName = builder.Configuration[$"{DesignConversionJobStoreOptions.SectionName}:FileName"];
});
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", flow = DesignConversionIntakeConstants.FlowId }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", flow = DesignConversionIntakeConstants.FlowId }));

app.Use(async (context, next) =>
{
    var tenantAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
    var correlationAccessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString()) ? tenantHeader.ToString() : "master";
    var correlation = context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationHeader) && !string.IsNullOrWhiteSpace(correlationHeader.ToString()) ? correlationHeader.ToString() : Guid.NewGuid().ToString("n");
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) && !string.IsNullOrWhiteSpace(roleHeader.ToString()) ? NormalizeRole(roleHeader.ToString()) : "anonymous";
    using var correlationScope = correlationAccessor.Push(new CorrelationContext(correlation, "http", DateTimeOffset.UtcNow));
    using var tenantScope = tenantAccessor.Push(new TenantContext(tenant, role == "anonymous" ? null : "local-user", new HashSet<string>([role], StringComparer.Ordinal), correlation, "http-header", DateTimeOffset.UtcNow));
    context.Response.Headers["X-Correlation-Id"] = correlation;
    await next().ConfigureAwait(false);
});

app.MapPost($"{DesignConversionIntakeConstants.DynamicEndpoint}/jobs", async (HttpContext context, DesignConversionIntakeService service, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DesignConversionIntakeConstants.SubmitRoles);
    if (auth is not null) return auth;

    var request = await JsonSerializer.DeserializeAsync<DesignConversionSubmitJobRequest>(context.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
    if (request is not null && string.IsNullOrWhiteSpace(request.IdempotencyKey) && context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader))
    {
        request = request with { IdempotencyKey = idempotencyHeader.ToString() };
    }

    var result = await service.SubmitJobAsync(request ?? new DesignConversionSubmitJobRequest(null, string.Empty, string.Empty, null, null, null), cancellationToken).ConfigureAwait(false);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
});

app.MapGet($"{DesignConversionIntakeConstants.DynamicEndpoint}/jobs/{{jobId}}", async (HttpContext context, DesignConversionIntakeService service, string jobId, CancellationToken cancellationToken) =>
{
    var auth = RequireRole(context, DesignConversionIntakeConstants.StatusRoles);
    if (auth is not null) return auth;

    var result = await service.GetJobStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
    return Results.Json(result.Data!.Body, statusCode: result.Data.HttpStatus);
});

app.Run();

static IResult? RequireRole(HttpContext context, IReadOnlyCollection<string> allowedRoles)
{
    var role = context.Request.Headers.TryGetValue("X-Role", out var roleHeader) ? NormalizeRole(roleHeader.ToString()) : "anonymous";
    var authorization = context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader) ? authorizationHeader.ToString() : null;
    var tenant = context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) && !string.IsNullOrWhiteSpace(tenantHeader.ToString()) ? tenantHeader.ToString() : "master";
    var decision = DesignConversionIntakeAuthPolicy.Evaluate(role, authorization, tenant, allowedRoles);
    return decision.IsAllowed ? null : Results.Json(new { code = decision.Code, allowedRoles = decision.AllowedRoles }, statusCode: decision.HttpStatus);
}

static string NormalizeRole(string value) => value.Replace('-', '_');
