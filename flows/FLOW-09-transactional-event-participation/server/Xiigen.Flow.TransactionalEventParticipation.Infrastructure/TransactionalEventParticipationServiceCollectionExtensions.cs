using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.TransactionalEventParticipation.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.TransactionalEventParticipation.Infrastructure;

public static class TransactionalEventParticipationServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionalEventParticipationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<TransactionalEventParticipationService>();
        return services;
    }
}
