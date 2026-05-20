using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.ReviewsReputation.Application;
using Xiigen.Freedom;

namespace Xiigen.Flow.ReviewsReputation.Infrastructure;

public static class ReviewsReputationServiceCollectionExtensions
{
    public static IServiceCollection AddReviewsReputationFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<ReviewsReputationService>();
        return services;
    }
}
