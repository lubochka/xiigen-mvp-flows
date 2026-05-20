using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.SchemaRegistryDag.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.SchemaRegistryDag.Infrastructure;

public static class SchemaRegistryDagServiceCollectionExtensions
{
    public static IServiceCollection AddSchemaRegistryDagFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<SchemaRegistryDagService>();
        return services;
    }
}
