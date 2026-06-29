using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.DesignConversionIntake.Application;

public sealed class DesignConversionIntakeService : MicroserviceBase<DesignConversionIntakeService>
{
    private readonly IDesignConversionJobStore _jobs;
    private readonly IDesignConversionCoreClient _coreClient;

    public DesignConversionIntakeService(
        IDesignConversionJobStore jobs,
        IDesignConversionCoreClient coreClient,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<DesignConversionIntakeService> logger)
        : base(new ServiceDescriptor("design-conversion-intake-service", "Design Conversion Intake", DesignConversionIntakeConstants.FlowId), tenantContextAccessor, correlationContextAccessor, logger)
    {
        _jobs = jobs;
        _coreClient = coreClient;
    }

    public async Task<DataProcessResult<DesignConversionIntakeOutcome>> SubmitJobAsync(DesignConversionSubmitJobRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Success(Outcome(400, Error("MISSING_REQUEST", "A design conversion job request is required."), false, false));
        }

        var tenantId = CurrentTenant.TenantId;
        if (!string.IsNullOrWhiteSpace(request.TenantId) && !string.Equals(request.TenantId, tenantId, StringComparison.Ordinal))
        {
            return Success(Outcome(403, Error("TENANT_MISMATCH", "Request tenantId must match the active tenant context."), false, false));
        }

        if (string.IsNullOrWhiteSpace(request.SourceType))
        {
            return Success(Outcome(400, Error("MISSING_SOURCE_TYPE", "sourceType is required."), false, false));
        }

        if (string.IsNullOrWhiteSpace(request.SourceUri))
        {
            return Success(Outcome(400, Error("MISSING_SOURCE_URI", "sourceUri is required."), false, false));
        }

        var idempotencyKey = Normalize(request.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var existing = await _jobs.FindByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return Success(Outcome(200, existing.ToStatusResponse(), true, false));
            }
        }

        var now = DateTimeOffset.UtcNow;
        var jobId = $"dcj-{Guid.NewGuid():N}";
        var targets = request.OutputTargets is { Count: > 0 } ? request.OutputTargets : ["angular"];
        var options = request.Options ?? new Dictionary<string, object?>();

        var dispatch = await _coreClient.DispatchAsync(
            new DesignConversionCoreDispatchRequest(jobId, tenantId, request.SourceType, request.SourceUri, targets, options),
            cancellationToken).ConfigureAwait(false);

        var record = dispatch.Accepted
            ? new DesignConversionJobRecord(
                jobId,
                tenantId,
                dispatch.Evaluation is null ? "queued" : "completed",
                idempotencyKey,
                request.SourceType,
                request.SourceUri,
                targets,
                dispatch.ProviderJobId,
                dispatch.Evaluation,
                null,
                now,
                now)
            : new DesignConversionJobRecord(
                jobId,
                tenantId,
                "failed",
                idempotencyKey,
                request.SourceType,
                request.SourceUri,
                targets,
                null,
                null,
                new DesignConversionErrorResponse(dispatch.ErrorCode ?? "CORE_CLIENT_DISPATCH_FAILED", dispatch.ErrorMessage ?? "Core dispatch failed."),
                now,
                now);

        await _jobs.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        return Success(Outcome(dispatch.Accepted ? 202 : 502, record.ToStatusResponse(), true, true));
    }

    public async Task<DataProcessResult<DesignConversionIntakeOutcome>> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Success(Outcome(400, Error("MISSING_JOB_ID", "jobId is required."), false, false));
        }

        var record = await _jobs.GetAsync(CurrentTenant.TenantId, jobId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return Success(Outcome(404, new { jobId, tenantId = CurrentTenant.TenantId, status = "not_found", code = "JOB_NOT_FOUND" }, false, false));
        }

        return Success(Outcome(200, record.ToStatusResponse(), false, false));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DesignConversionErrorResponse Error(string code, string message) => new(code, message);
    private static DesignConversionIntakeOutcome Outcome(int status, object body, bool stateWritten, bool coreDispatchAttempted) => new(status, body, stateWritten, coreDispatchAttempted);
}
