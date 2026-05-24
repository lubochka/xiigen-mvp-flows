using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.TenantLifecycleManager.Application;

namespace Xiigen.Flow.TenantLifecycleManager.Infrastructure;

public static class TenantLifecycleManagerServiceCollectionExtensions
{
    public static IServiceCollection AddTenantLifecycleManagerFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<TenantLifecycleManagerService>();
        return services;
    }
}




