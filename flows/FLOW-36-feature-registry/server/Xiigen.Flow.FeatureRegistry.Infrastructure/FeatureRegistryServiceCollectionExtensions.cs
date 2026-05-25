using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FeatureRegistry.Application;

namespace Xiigen.Flow.FeatureRegistry.Infrastructure;

public static class FeatureRegistryServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureRegistryFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<FeatureRegistryService>();
        return services;
    }
}





