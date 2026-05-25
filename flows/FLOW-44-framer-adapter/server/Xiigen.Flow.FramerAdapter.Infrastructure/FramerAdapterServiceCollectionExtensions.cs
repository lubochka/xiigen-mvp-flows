using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FramerAdapter.Application;

namespace Xiigen.Flow.FramerAdapter.Infrastructure;

public static class FramerAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddFramerAdapterFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<FramerAdapterService>();
        return services;
    }
}
