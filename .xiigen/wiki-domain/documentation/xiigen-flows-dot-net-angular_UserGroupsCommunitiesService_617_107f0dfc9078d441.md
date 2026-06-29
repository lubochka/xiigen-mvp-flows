# xiigen-flows-dot-net-angular::in.ResolveMembershipTierAsync

## Meaning
xiigen-flows-dot-net-angular::in.ResolveMembershipTierAsync implements local project behavior with return type Task<string>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:714::xiigen-flows-dot-net-angular::in.NormalizeTier. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:617

## Parameters
- string userId
- string groupId
- CancellationToken cancellationToken

## Return
Task<string> returned by xiigen-flows-dot-net-angular::in.ResolveMembershipTierAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:714::xiigen-flows-dot-net-angular::in.NormalizeTier

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:617::xiigen-flows-dot-net-angular::in.ResolveMembershipTierAsync