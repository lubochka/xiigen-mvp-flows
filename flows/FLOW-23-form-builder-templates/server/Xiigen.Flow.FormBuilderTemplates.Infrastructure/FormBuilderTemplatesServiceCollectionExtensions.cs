using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.FormBuilderTemplates.Application;

namespace Xiigen.Flow.FormBuilderTemplates.Infrastructure;

public static class FormBuilderTemplatesServiceCollectionExtensions
{
    public static IServiceCollection AddFormBuilderTemplatesFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<FormBuilderTemplatesService>();
        return services;
    }
}
