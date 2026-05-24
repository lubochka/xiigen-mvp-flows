using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MetaArbitrationEngine.Application;

namespace Xiigen.Flow.MetaArbitrationEngine.Infrastructure;

public static class MetaArbitrationEngineServiceCollectionExtensions
{
    public static IServiceCollection AddMetaArbitrationEngineFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<MetaArbitrationEngineService>();
        return services;
    }
}





