using Microsoft.Extensions.DependencyInjection;
using Xiigen.Flow.UserGroupsCommunities.Application;
using Xiigen.Fabrics.InMemory;
using Xiigen.Freedom;

namespace Xiigen.Flow.UserGroupsCommunities.Infrastructure;

public static class UserGroupsCommunitiesServiceCollectionExtensions
{
    public static IServiceCollection AddUserGroupsCommunitiesFlow(this IServiceCollection services)
    {
        services.AddXiigenInMemoryFabrics();
        services.AddXiigenFreedomConfig();
        services.AddSingleton<UserGroupsCommunitiesService>();
        return services;
    }
}


