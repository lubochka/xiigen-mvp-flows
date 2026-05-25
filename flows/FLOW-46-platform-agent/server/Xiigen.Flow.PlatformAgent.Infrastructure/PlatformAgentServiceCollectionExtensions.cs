using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.PlatformAgent.Application;

namespace Xiigen.Flow.PlatformAgent.Infrastructure;

public static class PlatformAgentServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformAgentFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<PlatformAgentService>();
        return services;
    }
}
