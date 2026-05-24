using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.CmsPublishing.Application;

namespace Xiigen.Flow.CmsPublishing.Infrastructure;

public static class CmsPublishingServiceCollectionExtensions
{
    public static IServiceCollection AddCmsPublishingFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<CmsPublishingService>();
        return services;
    }
}