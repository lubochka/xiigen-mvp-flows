using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SaasMultiTenancy.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.SaasMultiTenancy.Infrastructure;

public static class SaasMultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddSaasMultiTenancyFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<SaasMultiTenancyService>();
        return services;
    }
}

