using Microsoft.Extensions.DependencyInjection;
using Xiigen.Auth;
using Xiigen.Flow.UserRegistration.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Fabrics.Scheduler;
using Xiigen.Freedom;

namespace Xiigen.Flow.UserRegistration.Infrastructure;

public static class UserRegistrationServiceCollectionExtensions
{
    public static IServiceCollection AddUserRegistrationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenAuth();
        services.AddXiigenFreedomConfig();
        services.AddXiigenInMemoryScheduler();
        services.AddSingleton<UserRegistrationService>();
        services.AddSingleton<EmailVerificationService>();
        services.AddSingleton<OnboardingDeliveryService>();
        return services;
    }
}
