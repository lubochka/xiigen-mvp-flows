using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.ProfileEnrichment.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.ProfileEnrichment.Infrastructure;

public static class ProfileEnrichmentServiceCollectionExtensions
{
    public static IServiceCollection AddProfileEnrichmentFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<ProfileEnrichmentService>();
        return services;
    }
}
