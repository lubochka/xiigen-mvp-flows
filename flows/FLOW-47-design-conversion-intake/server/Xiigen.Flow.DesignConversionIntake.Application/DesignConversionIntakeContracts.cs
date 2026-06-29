using System.Text.Json.Serialization;

namespace Xiigen.Flow.DesignConversionIntake.Application;

public sealed record DesignConversionSubmitJobRequest(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("sourceType")] string SourceType,
    [property: JsonPropertyName("sourceUri")] string SourceUri,
    [property: JsonPropertyName("outputTargets")] IReadOnlyList<string>? OutputTargets,
    [property: JsonPropertyName("idempotencyKey")] string? IdempotencyKey,
    [property: JsonPropertyName("options")] IReadOnlyDictionary<string, object?>? Options);

public sealed record DesignConversionJobStatusResponse(
    string JobId,
    string TenantId,
    string Status,
    string? IdempotencyKey,
    string SourceType,
    string SourceUri,
    IReadOnlyList<string> OutputTargets,
    string? CoreProviderJobId,
    DesignConversionEvaluationResultResponse? Evaluation,
    DesignConversionErrorResponse? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ScaffoldNotice);

public sealed record DesignConversionErrorResponse(string Code, string Message);

public sealed record DesignConversionEvaluationResultResponse(
    string VerdictId,
    double Score,
    bool Passed,
    IReadOnlyList<DesignConversionDefectResponse> Defects,
    IReadOnlyList<DesignConversionComparisonArtifactResponse> ComparisonArtifacts,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    string CurrentHtml,
    string CurrentCss);

public sealed record DesignConversionDefectResponse(
    string DefectId,
    string Category,
    string Severity,
    string Description,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyList<string> EvidenceImageIds);

public sealed record DesignConversionComparisonArtifactResponse(
    string ArtifactId,
    string ExpectedImageUri,
    string ActualImageUri,
    string DiffImageUri,
    double Score,
    IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyList<string> EvidenceRefs);

public sealed record DesignConversionJobRecord(
    string JobId,
    string TenantId,
    string Status,
    string? IdempotencyKey,
    string SourceType,
    string SourceUri,
    IReadOnlyList<string> OutputTargets,
    string? CoreProviderJobId,
    DesignConversionEvaluationResultResponse? Evaluation,
    DesignConversionErrorResponse? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public DesignConversionJobStatusResponse ToStatusResponse() =>
        new(
            JobId,
            TenantId,
            Status,
            IdempotencyKey,
            SourceType,
            SourceUri,
            OutputTargets,
            CoreProviderJobId,
            Evaluation,
            Error,
            CreatedAt,
            UpdatedAt,
            "FLOW-47 status reflects same-process core dispatch and persisted evaluation evidence; job metadata is stored in the configured FLOW-47 durable job store.");
}

public sealed record DesignConversionCoreDispatchRequest(
    string JobId,
    string TenantId,
    string SourceType,
    string SourceUri,
    IReadOnlyList<string> OutputTargets,
    IReadOnlyDictionary<string, object?> Options);

public sealed record DesignConversionCoreDispatchResult(
    bool Accepted,
    string? ProviderJobId,
    DesignConversionEvaluationResultResponse? Evaluation,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static DesignConversionCoreDispatchResult AcceptedByFakeCore(string providerJobId) => new(true, providerJobId, null, null, null);
    public static DesignConversionCoreDispatchResult Completed(string providerJobId, DesignConversionEvaluationResultResponse evaluation) => new(true, providerJobId, evaluation, null, null);
    public static DesignConversionCoreDispatchResult Failed(string code, string message) => new(false, null, null, code, message);
}

public sealed record DesignConversionIntakeOutcome(int HttpStatus, object Body, bool StateWritten, bool CoreDispatchAttempted);

public sealed record DesignConversionAuthDecision(bool IsAllowed, int HttpStatus, string? Code, IReadOnlyList<string> AllowedRoles);
