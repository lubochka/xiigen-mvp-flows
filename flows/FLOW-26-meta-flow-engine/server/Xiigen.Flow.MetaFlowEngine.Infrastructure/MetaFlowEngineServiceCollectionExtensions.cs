using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MetaFlowEngine.Application;

namespace Xiigen.Flow.MetaFlowEngine.Infrastructure;

public static class MetaFlowEngineServiceCollectionExtensions
{
    public static IServiceCollection AddMetaFlowEngineFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<MetaFlowEngineService>();
        return services;
    }
}



