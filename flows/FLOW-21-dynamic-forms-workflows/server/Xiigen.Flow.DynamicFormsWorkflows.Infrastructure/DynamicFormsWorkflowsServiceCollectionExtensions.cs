using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.DynamicFormsWorkflows.Application;

namespace Xiigen.Flow.DynamicFormsWorkflows.Infrastructure;

public static class DynamicFormsWorkflowsServiceCollectionExtensions
{
    public static IServiceCollection AddDynamicFormsWorkflowsFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<DynamicFormsWorkflowsService>();
        return services;
    }
}
