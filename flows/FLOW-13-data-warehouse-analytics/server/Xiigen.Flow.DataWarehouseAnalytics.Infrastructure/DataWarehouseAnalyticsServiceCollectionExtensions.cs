using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DataWarehouseAnalytics.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.DataWarehouseAnalytics.Infrastructure;

public static class DataWarehouseAnalyticsServiceCollectionExtensions
{
    public static IServiceCollection AddDataWarehouseAnalyticsFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<DataWarehouseAnalyticsService>();
        return services;
    }
}
