namespace Xiigen.Flow.DesignConversionIntake.Application;

public interface IDesignConversionCoreClient
{
    Task<DesignConversionCoreDispatchResult> DispatchAsync(DesignConversionCoreDispatchRequest request, CancellationToken cancellationToken = default);
}

