using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Application;

public sealed class BundleValidatorService : MicroserviceBase<BundleValidatorService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public BundleValidatorService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<BundleValidatorService> logger)
        : base(
            new ServiceDescriptor("T574", nameof(BundleValidatorService), BundleActivationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<BundleValidationReport>> ValidateBundleAsync(
        string bundleId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            return Failure<BundleValidationReport>("MISSING_BUNDLE_ID", "bundleId is required");
        }

        var bundleResult = await _documents.GetAsync(
            BundleActivationConstants.SolutionBundles,
            bundleId,
            cancellationToken).ConfigureAwait(false);

        if (bundleResult.IsFailure)
        {
            return Failure<BundleValidationReport>(
                bundleResult.Error?.Code ?? "BUNDLES_QUERY_FAILED",
                bundleResult.Error?.Message ?? "Bundle query failed");
        }

        if (bundleResult.Data is null)
        {
            return Failure<BundleValidationReport>("BUNDLE_NOT_FOUND", $"Bundle {bundleId} not found");
        }

        var requiredFlows = ToStringList(bundleResult.Data.Body, "requiredFlows");
        if (requiredFlows.Count == 0)
        {
            return Failure<BundleValidationReport>("EMPTY_REQUIRED_FLOWS", "Bundle must have at least one required flow");
        }

        var missingFlows = new List<string>();
        foreach (var flowId in requiredFlows)
        {
            var flow = await _documents.GetAsync(
                BundleActivationConstants.FlowLifecycle,
                flowId,
                cancellationToken).ConfigureAwait(false);
            if (flow.IsFailure || flow.Data is null)
            {
                missingFlows.Add(flowId);
            }
        }

        await _outbox.AppendAsync(CreateEvent("bundle.validation.completed", bundleId, new Dictionary<string, object?>
        {
            ["bundleId"] = bundleId,
            ["requiredFlows"] = requiredFlows,
            ["missingFlows"] = missingFlows
        }), cancellationToken).ConfigureAwait(false);

        var report = new BundleValidationReport(
            bundleId,
            missingFlows.Count == 0,
            missingFlows.Select(flowId => $"Missing flow lifecycle record: {flowId}").ToArray(),
            Array.Empty<string>(),
            requiredFlows.Count * 5000,
            requiredFlows,
            DateTimeOffset.UtcNow);

        return Success(report);
    }

    private CloudEventEnvelope CreateEvent(string type, string subject, IReadOnlyDictionary<string, object?> data) =>
        new(
            Guid.NewGuid().ToString("n"),
            "xiigen://flow/bundle-activation",
            type,
            "1.0",
            DateTimeOffset.UtcNow,
            subject,
            "application/json",
            data,
            new Dictionary<string, string>
            {
                ["xiigenflowid"] = BundleActivationConstants.FlowId,
                ["xiigentenantid"] = CurrentTenant.TenantId,
                ["correlationid"] = CurrentCorrelationId
            });

    private static IReadOnlyList<string> ToStringList(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        return value switch
        {
            string[] values => values,
            IEnumerable<string> values => values.ToArray(),
            IEnumerable<object> values => values.Select(Convert.ToString).Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToArray(),
            _ => Array.Empty<string>()
        };
    }
}
