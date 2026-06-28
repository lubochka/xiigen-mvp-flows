using Microsoft.Extensions.DependencyInjection;
using DesignConversionDomain;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DesignConversionIntake.Application;

namespace Xiigen.Flow.DesignConversionIntake.Infrastructure;

public static class DesignConversionIntakeServiceCollectionExtensions
{
    public static IServiceCollection AddDesignConversionIntakeFlow(this IServiceCollection services, Action<DesignConversionJobStoreOptions>? configureJobStore = null)
    {
        var jobStoreOptions = new DesignConversionJobStoreOptions();
        configureJobStore?.Invoke(jobStoreOptions);

        services.AddXiigenInMemoryFabrics();
        services.AddSingleton(jobStoreOptions);
        services.AddSingleton<IDesignConversionJobStore, FileBackedDesignConversionJobStore>();
        services.AddSingleton<IDesignConversionRenderProvider, DeterministicDesignConversionRenderProvider>();
        services.AddSingleton<IDesignConversionComparisonProvider, DeterministicDesignConversionComparisonProvider>();
        services.AddSingleton<DesignConversionInternalService>();
        services.AddSingleton<IDesignConversionCoreClient, RealDesignConversionCoreClient>();
        services.AddSingleton<DesignConversionIntakeService>();
        return services;
    }

    public static IServiceCollection AddScaffoldFakeDesignConversionCoreClient(this IServiceCollection services)
    {
        services.AddSingleton<IDesignConversionCoreClient, ScaffoldFakeDesignConversionCoreClient>();
        return services;
    }
}
