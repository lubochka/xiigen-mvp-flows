# xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetFeedAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetFeedAsync implements local project behavior with return type Task<DataProcessResult<FriendRequestSocialFeedFeed>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble. No internal caller was detected.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:307

## Parameters
- string connectionId
- string viewerUserId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FriendRequestSocialFeedFeed>> returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetFeedAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:307::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetFeedAsync