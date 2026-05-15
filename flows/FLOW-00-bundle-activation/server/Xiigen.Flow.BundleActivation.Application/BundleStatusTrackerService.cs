using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Application;

public sealed class BundleStatusTrackerService : MicroserviceBase<BundleStatusTrackerService>
{
    private readonly IDocumentStore _documents;
    private readonly IOutboxStore _outbox;

    public BundleStatusTrackerService(
        IDocumentStore documents,
        IOutboxStore outbox,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<BundleStatusTrackerService> logger)
        : base(
            new ServiceDescriptor("T576", nameof(BundleStatusTrackerService), BundleActivationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _outbox = outbox;
    }

    public async Task<DataProcessResult<int>> OnFlowRegeneratedAsync(
        string flowId,
        string newVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flowId))
        {
            return Failure<int>("MISSING_FLOW_ID", "flowId is required");
        }

        if (string.IsNullOrWhiteSpace(newVersion))
        {
            return Failure<int>("MISSING_VERSION", "newVersion is required");
        }

        var bundles = await _documents.SearchAsync(
            BundleActivationConstants.SolutionBundles,
            new Dictionary<string, object?> { ["requiredFlows"] = flowId },
            cancellationToken).ConfigureAwait(false);

        if (bundles.IsFailure)
        {
            return Failure<int>(bundles.Error?.Code ?? "BUNDLES_QUERY_FAILED", bundles.Error?.Message ?? "Bundle query failed");
        }

        var changed = 0;
        foreach (var bundle in bundles.Data ?? Array.Empty<DocumentRecord>())
        {
            var minVersions = ReadMap(bundle.Body, "minFlowVersions");
            if (!minVersions.TryGetValue(flowId, out var minimumVersion))
            {
                continue;
            }

            var status = IsVersionBelow(newVersion, Convert.ToString(minimumVersion) ?? string.Empty)
                ? "DEGRADED"
                : "ACTIVE";

            var updated = bundle.Body.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            updated["status"] = status;
            updated["updatedAt"] = DateTimeOffset.UtcNow.ToString("O");

            var stored = await _documents.UpsertAsync(
                BundleActivationConstants.SolutionBundles,
                bundle.Id,
                updated,
                bundle.Version,
                cancellationToken).ConfigureAwait(false);

            if (stored.IsFailure)
            {
                return Failure<int>(stored.Error?.Code ?? "STORE_FAILED", stored.Error?.Message ?? "Status store failed");
            }

            var eventType = status == "DEGRADED" ? "bundle.degraded" : "bundle.restored";
            var outbox = await _outbox.AppendAsync(CreateEvent(eventType, bundle.Id, new Dictionary<string, object?>
            {
                ["bundleId"] = bundle.Id,
                ["flowId"] = flowId,
                ["currentVersion"] = newVersion,
                ["minimumVersion"] = minimumVersion,
                ["status"] = status
            }), cancellationToken).ConfigureAwait(false);

            if (outbox.IsFailure)
            {
                return Failure<int>(outbox.Error?.Code ?? "OUTBOX_FAILED", outbox.Error?.Message ?? "Outbox append failed");
            }

            changed++;
        }

        return Success(changed);
    }

    public static bool IsVersionBelow(string current, string minimum)
    {
        var currentNumber = ParseVersion(current);
        var minimumNumber = ParseVersion(minimum);
        return currentNumber < minimumNumber;
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

    private static int ParseVersion(string version) =>
        int.TryParse(version.TrimStart('v', 'V').Split('.')[0], out var number) ? number : 0;

    private static IReadOnlyDictionary<string, object?> ReadMap(IReadOnlyDictionary<string, object?> source, string key)
    {
        if (!source.TryGetValue(key, out var value) || value is null)
        {
            return new Dictionary<string, object?>();
        }

        return value as IReadOnlyDictionary<string, object?> ?? new Dictionary<string, object?>();
    }
}
