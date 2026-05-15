using Microsoft.Extensions.Logging;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Application;

public sealed class BundleActivationService : MicroserviceBase<BundleActivationService>
{
    private readonly IDocumentStore _documents;
    private readonly IIdempotencyStore _idempotency;
    private readonly IOutboxStore _outbox;
    private readonly IFlowBootstrapper _flowBootstrapper;
    private readonly BundleValidatorService _validator;

    public BundleActivationService(
        IDocumentStore documents,
        IIdempotencyStore idempotency,
        IOutboxStore outbox,
        IFlowBootstrapper flowBootstrapper,
        BundleValidatorService validator,
        ITenantContextAccessor tenantContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<BundleActivationService> logger)
        : base(
            new ServiceDescriptor("T575", nameof(BundleActivationService), BundleActivationConstants.FlowId),
            tenantContextAccessor,
            correlationContextAccessor,
            logger)
    {
        _documents = documents;
        _idempotency = idempotency;
        _outbox = outbox;
        _flowBootstrapper = flowBootstrapper;
        _validator = validator;
    }

    public async Task<DataProcessResult<BundleActivationOutcome>> ActivateAsync(
        IReadOnlyDictionary<string, object?> request,
        string? ifMatch,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Failure<BundleActivationOutcome>(
                "BAD_REQUEST",
                "X-Idempotency-Key header is required",
                new Dictionary<string, object?> { ["field"] = "X-Idempotency-Key" });
        }

        var bundleId = ReadString(request, "bundleId");
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            return Failure<BundleActivationOutcome>(
                "VALIDATION_FAILED",
                "bundleId is required",
                new Dictionary<string, object?> { ["field"] = "bundleId", ["reason"] = "required" });
        }

        var operationKey = $"{BundleActivationConstants.FlowSlug}:activate:{idempotencyKey}";
        var reservation = await _idempotency.TryReserveAsync(operationKey, TimeSpan.FromMinutes(30), cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure)
        {
            return Failure<BundleActivationOutcome>(
                reservation.Error?.Code ?? "IDEMPOTENCY_FAILED",
                reservation.Error?.Message ?? "Idempotency reservation failed");
        }

        if (reservation.Data?.Status == "succeeded" && reservation.Data.CachedResponse is not null)
        {
            return Success(new BundleActivationOutcome(200, reservation.Data.CachedResponse, true, true));
        }

        if (ReadBool(request, "simulateFabricTimeout"))
        {
            var timeoutEvent = CreateEvent("bundle.activation.timeout", bundleId, new Dictionary<string, object?>
            {
                ["bundleId"] = bundleId,
                ["retryAfterMs"] = 5000
            });
            var timeoutOutbox = await _outbox.AppendAsync(timeoutEvent, cancellationToken).ConfigureAwait(false);
            await _idempotency.MarkFailedAsync(operationKey, "TIMEOUT", "Downstream fabric timeout", cancellationToken).ConfigureAwait(false);
            if (timeoutOutbox.IsFailure)
            {
                return Failure<BundleActivationOutcome>("UPSTREAM_FAILURE", "Outbox append failed during timeout handling");
            }

            return Failure<BundleActivationOutcome>(
                "TIMEOUT",
                "Downstream fabric timeout",
                new Dictionary<string, object?> { ["retryAfterMs"] = 5000 });
        }

        var validation = await _validator.ValidateBundleAsync(bundleId, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Failure<BundleActivationOutcome>(
                validation.Error?.Code ?? "VALIDATION_FAILED",
                validation.Error?.Message ?? "Validation failed",
                validation.Error?.Details);
        }

        if (validation.Data is null || !validation.Data.Valid)
        {
            return Failure<BundleActivationOutcome>(
                "VALIDATION_FAILED",
                "Bundle validation failed",
                new Dictionary<string, object?> { ["errors"] = validation.Data?.Errors ?? Array.Empty<string>() });
        }

        var bundle = await _documents.GetAsync(BundleActivationConstants.SolutionBundles, bundleId, cancellationToken).ConfigureAwait(false);
        if (bundle.IsFailure || bundle.Data is null)
        {
            return Failure<BundleActivationOutcome>("BUNDLE_NOT_FOUND", $"Bundle {bundleId} not found");
        }

        if (!string.IsNullOrWhiteSpace(ifMatch) && !string.Equals(ifMatch, bundle.Data.Version, StringComparison.Ordinal))
        {
            return Failure<BundleActivationOutcome>(
                "CONCURRENT_UPDATE",
                "Bundle version changed before activation",
                new Dictionary<string, object?> { ["currentVersion"] = bundle.Data.Version, ["reason"] = "concurrent_update" });
        }

        var orderedFlows = OrderByDependency(validation.Data.RequiredFlows);
        var activatedFlows = new List<string>();
        var versions = new Dictionary<string, object?>();

        foreach (var flowId in orderedFlows)
        {
            var dryRun = await _flowBootstrapper.BootstrapAsync(flowId, "DRY_RUN", cancellationToken).ConfigureAwait(false);
            if (!dryRun.IsSuccess)
            {
                return Failure<BundleActivationOutcome>(
                    dryRun.ErrorCode ?? "DRY_RUN_FAILED",
                    dryRun.ErrorMessage ?? $"DRY_RUN failed for {flowId}");
            }

            var full = await _flowBootstrapper.BootstrapAsync(flowId, "FULL", cancellationToken).ConfigureAwait(false);
            if (!full.IsSuccess)
            {
                return Failure<BundleActivationOutcome>(
                    full.ErrorCode ?? "ACTIVATION_FAILED",
                    full.ErrorMessage ?? $"FULL activation failed for {flowId}");
            }

            var flowRecord = await _documents.GetAsync(BundleActivationConstants.FlowLifecycle, flowId, cancellationToken).ConfigureAwait(false);
            versions[flowId] = flowRecord.Data?.Body.TryGetValue("version", out var version) == true ? version : "v1";
            activatedFlows.Add(flowId);
        }

        await PopulateFreedomDefaultsAsync(bundle.Data.Body, cancellationToken).ConfigureAwait(false);

        var response = new Dictionary<string, object?>
        {
            ["id"] = bundleId,
            ["bundleId"] = bundleId,
            ["activatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["requiredFlowsActivated"] = activatedFlows,
            ["flowVersionsAtActivation"] = versions
        };

        var bundleUpdate = bundle.Data.Body.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        bundleUpdate["status"] = "ACTIVE";
        bundleUpdate["lastActivatedAt"] = response["activatedAt"];
        var expectedBundleVersion = string.IsNullOrWhiteSpace(ifMatch) ? bundle.Data.Version : ifMatch;
        var bundleStored = await _documents.UpsertAsync(
            BundleActivationConstants.SolutionBundles,
            bundleId,
            bundleUpdate,
            expectedBundleVersion,
            cancellationToken).ConfigureAwait(false);

        if (bundleStored.IsFailure)
        {
            return Failure<BundleActivationOutcome>(
                bundleStored.Error?.Code ?? "CONCURRENT_UPDATE",
                bundleStored.Error?.Message ?? "Bundle changed before activation was stored",
                bundleStored.Error?.Details);
        }

        var stored = await _documents.UpsertAsync(
            BundleActivationConstants.BundleActivations,
            bundleId,
            response,
            expectedVersion: null,
            cancellationToken).ConfigureAwait(false);

        if (stored.IsFailure)
        {
            return Failure<BundleActivationOutcome>(
                stored.Error?.Code ?? "STORE_FAILED",
                stored.Error?.Message ?? "Failed to store activation record");
        }

        var outboxRecord = await _outbox.AppendAsync(CreateEvent("bundle.activated", bundleId, response), cancellationToken).ConfigureAwait(false);
        if (outboxRecord.IsFailure)
        {
            return Failure<BundleActivationOutcome>(
                outboxRecord.Error?.Code ?? "OUTBOX_FAILED",
                outboxRecord.Error?.Message ?? "Failed to append outbox record");
        }

        var outcome = new BundleActivationOutcome(201, response, true, true);
        await _idempotency.MarkSucceededAsync(operationKey, response, cancellationToken).ConfigureAwait(false);
        return Success(outcome);
    }

    public IReadOnlyList<string> OrderByDependency(IReadOnlyList<string> flowIds) =>
        flowIds.OrderBy(flowId => flowId == "FLOW-01" ? -1 : ParseFlowNumber(flowId)).ToArray();

    private async Task PopulateFreedomDefaultsAsync(
        IReadOnlyDictionary<string, object?> bundle,
        CancellationToken cancellationToken)
    {
        if (!bundle.TryGetValue("defaultFreedomConfig", out var configValue) ||
            configValue is not IReadOnlyDictionary<string, object?> defaults)
        {
            return;
        }

        var existing = await _documents.GetAsync(BundleActivationConstants.FreedomConfig, "defaults", cancellationToken).ConfigureAwait(false);
        var merged = existing.Data?.Body.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>();
        foreach (var pair in defaults)
        {
            merged.TryAdd(pair.Key, pair.Value);
        }

        await _documents.UpsertAsync(BundleActivationConstants.FreedomConfig, "defaults", merged, existing.Data?.Version, cancellationToken).ConfigureAwait(false);
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

    private static int ParseFlowNumber(string flowId) =>
        int.TryParse(flowId.Replace("FLOW-", string.Empty, StringComparison.Ordinal), out var value)
            ? value
            : int.MaxValue;

    private static string? ReadString(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) ? Convert.ToString(value) : null;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) && value is bool boolValue && boolValue;
}
