using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.AiSafetyModeration.Application;

namespace Xiigen.Flow.AiSafetyModeration.Infrastructure;

public static class AiSafetyModerationServiceCollectionExtensions
{
    public static IServiceCollection AddAiSafetyModerationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<AiSafetyModerationService>();
        return services;
    }
}

