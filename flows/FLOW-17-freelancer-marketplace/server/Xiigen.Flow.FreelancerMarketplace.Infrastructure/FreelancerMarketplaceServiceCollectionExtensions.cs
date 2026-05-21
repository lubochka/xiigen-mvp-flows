using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FreelancerMarketplace.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.FreelancerMarketplace.Infrastructure;

public static class FreelancerMarketplaceServiceCollectionExtensions
{
    public static IServiceCollection AddFreelancerMarketplaceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<FreelancerMarketplaceService>();
        return services;
    }
}
