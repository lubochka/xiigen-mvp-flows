using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MiroShapeAdapter.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.MiroShapeAdapter.Infrastructure;

public static class MiroShapeAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddMiroShapeAdapterFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<InMemoryMiroWriter>();
        services.AddSingleton<IMiroWriter>(sp => sp.GetRequiredService<InMemoryMiroWriter>());
        services.AddSingleton<MiroShapeAdapterService>();
        return services;
    }
}

public sealed class InMemoryMiroWriter : IMiroWriter
{
    private readonly List<MiroWritePayload> _payloads = [];
    private readonly object _gate = new();

    public bool FailNext { get; set; }

    public Task<DataProcessResult<bool>> WriteAsync(MiroWritePayload payload, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        lock (_gate)
        {
            if (FailNext)
            {
                FailNext = false;
                return Task.FromResult(DataProcessResult<bool>.Failure("MIRO_WRITE_FAILED", "Simulated Miro writer failure."));
            }

            _payloads.Add(payload);
            return Task.FromResult(DataProcessResult<bool>.Success(true));
        }
    }

    public IReadOnlyList<MiroWritePayload> Payloads
    {
        get
        {
            lock (_gate)
            {
                return _payloads.ToArray();
            }
        }
    }
}
