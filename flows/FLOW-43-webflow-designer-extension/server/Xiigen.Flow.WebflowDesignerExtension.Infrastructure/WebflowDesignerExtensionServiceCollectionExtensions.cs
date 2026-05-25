using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.WebflowDesignerExtension.Application;

namespace Xiigen.Flow.WebflowDesignerExtension.Infrastructure;

public static class WebflowDesignerExtensionServiceCollectionExtensions
{
    public static IServiceCollection AddWebflowDesignerExtensionFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<WebflowDesignerExtensionService>();
        return services;
    }
}
