using Microsoft.Extensions.DependencyInjection;
using Xiigen.Fabrics.InMemory;
using Xiigen.Flow.RagQualityFeedback.Application;

namespace Xiigen.Flow.RagQualityFeedback.Infrastructure;

public static class RagQualityFeedbackServiceCollectionExtensions
{
    public static IServiceCollection AddRagQualityFeedbackFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddSingleton<RagQualityFeedbackService>();
        return services;
    }
}






