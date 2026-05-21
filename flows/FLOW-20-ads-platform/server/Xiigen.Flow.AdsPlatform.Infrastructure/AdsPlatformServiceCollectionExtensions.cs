using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AdsPlatform.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.AdsPlatform.Infrastructure;

public static class AdsPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddAdsPlatformFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<AdsPlatformService>();
        return services;
    }
}
