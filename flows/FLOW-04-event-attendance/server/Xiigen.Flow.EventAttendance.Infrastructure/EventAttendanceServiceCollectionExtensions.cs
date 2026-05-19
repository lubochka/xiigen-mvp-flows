using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.EventAttendance.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.EventAttendance.Infrastructure;

public static class EventAttendanceServiceCollectionExtensions
{
    public static IServiceCollection AddEventAttendanceFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<EventAttendanceService>();
        return services;
    }
}

