using Xiigen.Flow.DesignConversionIntake.Application;

namespace Xiigen.Flow.DesignConversionIntake.Infrastructure;

public sealed class ScaffoldFakeDesignConversionCoreClient : IDesignConversionCoreClient
{
    public Task<DesignConversionCoreDispatchResult> DispatchAsync(DesignConversionCoreDispatchRequest request, CancellationToken cancellationToken = default)
    {
        // FLOW-47 scaffold only: replace with a real core render/ML client after the core contract is approved.
        if (request.SourceUri.Contains("core-fail", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(DesignConversionCoreDispatchResult.Failed("CORE_CLIENT_UNAVAILABLE", "Scaffold fake core client was asked to simulate a dispatch failure."));
        }

        return Task.FromResult(DesignConversionCoreDispatchResult.AcceptedByFakeCore($"fake-core-{request.JobId}"));
    }
}

