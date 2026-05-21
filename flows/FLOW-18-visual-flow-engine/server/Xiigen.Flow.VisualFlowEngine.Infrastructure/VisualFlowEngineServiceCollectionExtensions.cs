using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.VisualFlowEngine.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.VisualFlowEngine.Infrastructure;

public static class VisualFlowEngineServiceCollectionExtensions
{
    public static IServiceCollection AddVisualFlowEngineFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<VisualFlowEngineService>();
        return services;
    }
}
