using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.BundleActivation.Application;
using Xiigen.Kernel;

namespace Xiigen.Flow.BundleActivation.Infrastructure;

public static class BundleActivationServiceCollectionExtensions
{
    public static IServiceCollection AddBundleActivationFlow(this IServiceCollection services)
    {
        services.AddSingleton<ITenantContextAccessor, AsyncLocalTenantContextAccessor>();
        services.AddSingleton<ICorrelationContextAccessor, AsyncLocalCorrelationContextAccessor>();
        services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
        services.AddSingleton<IFlowBootstrapper, StubFlowBootstrapper>();
        services.AddSingleton<BundleValidatorService>();
        services.AddSingleton<BundleActivationService>();
        services.AddSingleton<BundleStatusTrackerService>();
        return services;
    }
}
