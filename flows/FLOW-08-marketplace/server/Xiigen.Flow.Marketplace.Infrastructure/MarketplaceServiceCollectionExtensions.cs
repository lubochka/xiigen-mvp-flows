using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.Marketplace.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.Marketplace.Infrastructure;

public static class MarketplaceServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplaceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<MarketplaceService>();
        return services;
    }
}



