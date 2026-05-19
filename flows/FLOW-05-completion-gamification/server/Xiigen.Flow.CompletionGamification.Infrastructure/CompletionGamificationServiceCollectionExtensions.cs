using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.CompletionGamification.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.CompletionGamification.Infrastructure;

public static class CompletionGamificationServiceCollectionExtensions
{
    public static IServiceCollection AddCompletionGamificationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<CompletionGamificationService>();
        return services;
    }
}


