using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MarketplacePluginAdapter.Application;

namespace Xiigen.Flow.MarketplacePluginAdapter.Infrastructure;

public static class MarketplacePluginAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplacePluginAdapterFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<MarketplacePluginAdapterService>();
        return services;
    }
}




