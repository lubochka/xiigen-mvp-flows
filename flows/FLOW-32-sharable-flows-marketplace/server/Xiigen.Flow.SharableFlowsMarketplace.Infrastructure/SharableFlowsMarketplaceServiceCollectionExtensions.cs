using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SharableFlowsMarketplace.Application;

namespace Xiigen.Flow.SharableFlowsMarketplace.Infrastructure;

public static class SharableFlowsMarketplaceServiceCollectionExtensions
{
    public static IServiceCollection AddSharableFlowsMarketplaceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<SharableFlowsMarketplaceService>();
        return services;
    }
}




