using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.EventManagement.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.EventManagement.Infrastructure;

public static class EventManagementServiceCollectionExtensions
{
    public static IServiceCollection AddEventManagementFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<EventManagementService>();
        return services;
    }
}

