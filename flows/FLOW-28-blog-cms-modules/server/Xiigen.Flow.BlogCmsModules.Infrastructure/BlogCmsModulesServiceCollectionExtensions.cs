using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.BlogCmsModules.Application;

namespace Xiigen.Flow.BlogCmsModules.Infrastructure;

public static class BlogCmsModulesServiceCollectionExtensions
{
    public static IServiceCollection AddBlogCmsModulesFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<BlogCmsModulesService>();
        return services;
    }
}




