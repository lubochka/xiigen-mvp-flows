namespace Xiigen.Flow.BundleActivation.Application;

public sealed record FlowBootstrapResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null);

public interface IFlowBootstrapper
{
    Task<FlowBootstrapResult> BootstrapAsync(
        string flowId,
        string mode,
        CancellationToken cancellationToken = default);
}

public sealed record BundleActivationOutcome(
    int HttpStatus,
    IReadOnlyDictionary<string, object?> Body,
    bool Stored,
    bool OutboxAppended);

public sealed record BundleValidationReport(
    string BundleId,
    bool Valid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    int EstimatedActivationMs,
    IReadOnlyList<string> RequiredFlows,
    DateTimeOffset CheckedAt);
