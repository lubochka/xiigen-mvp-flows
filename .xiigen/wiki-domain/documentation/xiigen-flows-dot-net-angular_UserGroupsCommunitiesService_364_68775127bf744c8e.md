# xiigen-flows-dot-net-angular::in.PopulateFeedAsync

## Meaning
xiigen-flows-dot-net-angular::in.PopulateFeedAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:334::xiigen-flows-dot-net-angular::in.GetFeedAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:266::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_GroupFeed_SkipsEmptyContentAndClampsRankingScores.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:364

## Parameters
- string groupId
- string userId
- IReadOnlyList<IReadOnlyDictionary<string
- object?>> contentItems
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesOutcome>> returned by xiigen-flows-dot-net-angular::in.PopulateFeedAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:334::xiigen-flows-dot-net-angular::in.GetFeedAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:266::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.ProductSpec_GroupFeed_SkipsEmptyContentAndClampsRankingScores

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:364::xiigen-flows-dot-net-angular::in.PopulateFeedAsync