using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SystemInitiationBootstrap.Application;

namespace Xiigen.Flow.SystemInitiationBootstrap.Infrastructure;

public static class SystemInitiationBootstrapServiceCollectionExtensions
{
    public static IServiceCollection AddSystemInitiationBootstrapFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<SystemInitiationBootstrapService>();
        return services;
    }
}




