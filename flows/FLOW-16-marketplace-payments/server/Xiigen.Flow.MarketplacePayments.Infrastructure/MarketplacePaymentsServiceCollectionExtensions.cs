using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.MarketplacePayments.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.MarketplacePayments.Infrastructure;

public static class MarketplacePaymentsServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplacePaymentsFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<MarketplacePaymentsService>();
        return services;
    }
}
