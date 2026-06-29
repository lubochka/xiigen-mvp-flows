# xiigen-flows-dot-net-angular::in.GetMembershipStatusAsync

## Meaning
xiigen-flows-dot-net-angular::in.GetMembershipStatusAsync implements local project behavior with return type Task<DataProcessResult<UserGroupsCommunitiesMembershipStatus>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:647::xiigen-flows-dot-net-angular::in.CountPendingApprovalsAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:202

## Parameters
- string groupId
- string userId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<UserGroupsCommunitiesMembershipStatus>> returned by xiigen-flows-dot-net-angular::in.GetMembershipStatusAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Tests\UserGroupsCommunitiesServiceTests.cs:88::xiigen-flows-dot-net-angular::UserGroupsCommunitiesServiceTests.CF0604_MembershipApprovalFeedTierAndAccessLogs_AreTenantScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:647::xiigen-flows-dot-net-angular::in.CountPendingApprovalsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:656::xiigen-flows-dot-net-angular::in.FindMembershipAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:202::xiigen-flows-dot-net-angular::in.GetMembershipStatusAsync