using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.HistoryBootstrap.Application;

namespace Xiigen.Flow.HistoryBootstrap.Infrastructure;

public static class HistoryBootstrapServiceCollectionExtensions
{
    public static IServiceCollection AddHistoryBootstrapFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<HistoryBootstrapService>();
        return services;
    }
}
