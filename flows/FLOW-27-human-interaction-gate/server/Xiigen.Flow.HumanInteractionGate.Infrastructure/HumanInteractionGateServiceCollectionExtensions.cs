using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.HumanInteractionGate.Application;

namespace Xiigen.Flow.HumanInteractionGate.Infrastructure;

public static class HumanInteractionGateServiceCollectionExtensions
{
    public static IServiceCollection AddHumanInteractionGateFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<HumanInteractionGateService>();
        return services;
    }
}



