using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DesignIntelligenceEngine.Application;

namespace Xiigen.Flow.DesignIntelligenceEngine.Infrastructure;

public static class DesignIntelligenceEngineServiceCollectionExtensions
{
    public static IServiceCollection AddDesignIntelligenceEngineFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<DesignIntelligenceEngineService>();
        return services;
    }
}




