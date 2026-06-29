# xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.DiscoverGroupsAsync

## Meaning
xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.DiscoverGroupsAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesDiscovery>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:535::xiigen-flows-dot-net-angular::in.EnsureGroupAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:561::xiigen-flows-dot-net-angular::in.CanViewGroupAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:287::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_Discovery_HidesInviteOnlyGroupsFromAnonymousVisitors.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:175

## Parameters
- string? viewerUserId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesDiscovery>> returned by xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.DiscoverGroupsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:287::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_Discovery_HidesInviteOnlyGroupsFromAnonymousVisitors

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:535::xiigen-flows-dot-net-angular::in.EnsureGroupAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:561::xiigen-flows-dot-net-angular::in.CanViewGroupAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:175::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.DiscoverGroupsAsync