using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.BfaCrossFlowGovernance.Application;

namespace Xiigen.Flow.BfaCrossFlowGovernance.Infrastructure;

public static class BfaCrossFlowGovernanceServiceCollectionExtensions
{
    public static IServiceCollection AddBfaCrossFlowGovernanceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<BfaCrossFlowGovernanceService>();
        return services;
    }
}


