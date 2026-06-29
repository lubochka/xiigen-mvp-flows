# xiigen-flows-dot-net-angular::in.GetFeedAsync

## Meaning
xiigen-flows-dot-net-angular::in.GetFeedAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesFeed>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:364::xiigen-flows-dot-net-angular::in.PopulateFeedAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:581::xiigen-flows-dot-net-angular::in.LogAccessAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:233::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_AdminApproval_ActivatesInviteOnlyMembershipAndThenSeedsFeed, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:95::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF0704_TenantPrivateScope_DoesNotLeakConnectionsOrFeedsAcrossTenants, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:277::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF893_FeedWindow_UsesConnectionStrengthBuckets.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:334

## Parameters
- string groupId
- string userId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesFeed>> returned by xiigen-flows-dot-net-angular::in.GetFeedAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:233::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_AdminApproval_ActivatesInviteOnlyMembershipAndThenSeedsFeed
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:95::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF0704_TenantPrivateScope_DoesNotLeakConnectionsOrFeedsAcrossTenants
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:277::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF893_FeedWindow_UsesConnectionStrengthBuckets

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:364::xiigen-flows-dot-net-angular::in.PopulateFeedAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:581::xiigen-flows-dot-net-angular::in.LogAccessAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:334::xiigen-flows-dot-net-angular::in.GetFeedAsync