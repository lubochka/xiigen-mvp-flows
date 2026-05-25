using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.CanvaTextAdapter.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.CanvaTextAdapter.Infrastructure;

public static class CanvaTextAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddCanvaTextAdapterFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<InMemoryCanvaWriter>();
        services.AddSingleton<ICanvaWriter>(sp => sp.GetRequiredService<InMemoryCanvaWriter>());
        services.AddSingleton<CanvaTextAdapterService>();
        return services;
    }
}

public sealed class InMemoryCanvaWriter : ICanvaWriter
{
    private readonly List<CanvaWritePayload> _payloads = [];
    private readonly object _gate = new();

    public bool FailNext { get; set; }

    public Task<DataProcessResult<bool>> WriteAsync(CanvaWritePayload payload, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        lock (_gate)
        {
            if (FailNext)
            {
                FailNext = false;
                return Task.FromResult(DataProcessResult<bool>.Failure("CANVA_WRITE_FAILED", "Simulated Canva writer failure."));
            }

            _payloads.Add(payload);
            return Task.FromResult(DataProcessResult<bool>.Success(true));
        }
    }

    public IReadOnlyList<CanvaWritePayload> Payloads
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
