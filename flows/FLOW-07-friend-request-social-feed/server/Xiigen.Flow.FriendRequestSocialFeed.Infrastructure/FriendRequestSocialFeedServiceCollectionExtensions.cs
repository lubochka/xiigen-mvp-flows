using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.FriendRequestSocialFeed.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.FriendRequestSocialFeed.Infrastructure;

public static class FriendRequestSocialFeedServiceCollectionExtensions
{
    public static IServiceCollection AddFriendRequestSocialFeedFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<FriendRequestSocialFeedService>();
        return services;
    }
}


