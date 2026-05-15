using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.BundleActivation.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Infrastructure;

public static class BundleActivationServiceCollectionExtensions
{
    public static IServiceCollection AddBundleActivationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<BundleValidatorService>();
        services.AddSingleton<BundleActivationService>();
        services.AddSingleton<BundleStatusTrackerService>();
        return services;
    }
}
