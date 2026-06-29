# xiigen-flows-dot-net-angular::in.LeaveMembershipAsync

## Meaning
xiigen-flows-dot-net-angular::in.LeaveMembershipAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485::xiigen-flows-dot-net-angular::in.StoreMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:465

## Parameters
- string groupId
- string userId
- DocumentRecord? existing
- string operationKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesOutcome>> returned by xiigen-flows-dot-net-angular::in.LeaveMembershipAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485::xiigen-flows-dot-net-angular::in.StoreMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:465::xiigen-flows-dot-net-angular::in.LeaveMembershipAsync