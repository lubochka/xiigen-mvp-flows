using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DesignSystemGovernance.Application;

namespace Xiigen.Flow.DesignSystemGovernance.Infrastructure;

public static class DesignSystemGovernanceServiceCollectionExtensions
{
    public static IServiceCollection AddDesignSystemGovernanceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<DesignSystemGovernanceService>();
        return services;
    }
}





