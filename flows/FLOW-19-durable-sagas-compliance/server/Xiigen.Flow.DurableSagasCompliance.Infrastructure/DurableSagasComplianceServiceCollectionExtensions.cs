using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DurableSagasCompliance.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.DurableSagasCompliance.Infrastructure;

public static class DurableSagasComplianceServiceCollectionExtensions
{
    public static IServiceCollection AddDurableSagasComplianceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<DurableSagasComplianceService>();
        return services;
    }
}
