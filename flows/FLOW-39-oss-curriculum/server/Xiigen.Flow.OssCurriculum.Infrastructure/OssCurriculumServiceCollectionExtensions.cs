using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.OssCurriculum.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.OssCurriculum.Infrastructure;

public static class OssCurriculumServiceCollectionExtensions
{
    public static IServiceCollection AddOssCurriculumFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<InMemoryOssCurriculumQueueBroker>();
        services.AddSingleton<IOssCurriculumQueueBroker>(sp => sp.GetRequiredService<InMemoryOssCurriculumQueueBroker>());
        services.AddSingleton<OssCurriculumService>();
        return services;
    }
}

public sealed class InMemoryOssCurriculumQueueBroker : IOssCurriculumQueueBroker
{
    private readonly List<QueueDispatchRecord> _records = [];
    private readonly object _gate = new();

    public Task<DataProcessResult<QueueDispatchRecord>> PublishAsync(CloudEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = new QueueDispatchRecord(envelope.Type, envelope, DateTimeOffset.UtcNow);
        lock (_gate)
        {
            _records.Add(record);
        }

        return Task.FromResult(DataProcessResult<QueueDispatchRecord>.Success(record));
    }

    public Task<DataProcessResult<IReadOnlyList<QueueDispatchRecord>>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(DataProcessResult<IReadOnlyList<QueueDispatchRecord>>.Success(_records.ToArray()));
        }
    }
}
