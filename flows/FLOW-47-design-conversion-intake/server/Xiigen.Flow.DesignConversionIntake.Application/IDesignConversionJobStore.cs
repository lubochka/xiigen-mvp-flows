namespace Xiigen.Flow.DesignConversionIntake.Application;

public interface IDesignConversionJobStore
{
    Task<DesignConversionJobRecord?> GetAsync(string tenantId, string jobId, CancellationToken cancellationToken = default);
    Task<DesignConversionJobRecord?> FindByIdempotencyKeyAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task UpsertAsync(DesignConversionJobRecord record, CancellationToken cancellationToken = default);
}

