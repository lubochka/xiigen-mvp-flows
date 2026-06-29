# xiigen-flows-dot-net-angular::in.StoreMembershipAsync

## Meaning
xiigen-flows-dot-net-angular::in.StoreMembershipAsync implements local project behavior with return type Task<DataProcessResult<DocumentRecord>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:285::xiigen-flows-dot-net-angular::in.UpdateTierAsync, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:465::xiigen-flows-dot-net-angular::in.LeaveMembershipAsync.

## Location
flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485

## Parameters
- string groupId
- string userId
- string tier
- string status
- string? ifMatch
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DocumentRecord>> returned by xiigen-flows-dot-net-angular::in.StoreMembershipAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:38::xiigen-flows-dot-net-angular::UserGroupsCommunitiesService.SubmitMembershipAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:232::xiigen-flows-dot-net-angular::in.ReviewJoinRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:285::xiigen-flows-dot-net-angular::in.UpdateTierAsync
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:465::xiigen-flows-dot-net-angular::in.LeaveMembershipAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:485::xiigen-flows-dot-net-angular::in.StoreMembershipAsync