using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AdaptiveRagDeepResearch.Application;

namespace Xiigen.Flow.AdaptiveRagDeepResearch.Infrastructure;

public static class AdaptiveRagDeepResearchServiceCollectionExtensions
{
    public static IServiceCollection AddAdaptiveRagDeepResearchFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<AdaptiveRagDeepResearchService>();
        return services;
    }
}




