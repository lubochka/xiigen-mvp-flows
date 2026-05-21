using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.EtlDataIntegration.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.EtlDataIntegration.Infrastructure;

public static class EtlDataIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddEtlDataIntegrationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<EtlDataIntegrationService>();
        return services;
    }
}
