# xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync implements local project behavior with return type Task<DataProcessResult<FriendRequestSocialFeedOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:540::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.StoreNotificationAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:649::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CalculateConnectionStrength, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:673::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.DirectionIndependentConnectionId. It is called by xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:204::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.RespondToFriendRequestAsync.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:388

## Parameters
- DocumentRecord requestRecord
- string actorUserId
- string requestId
- double baseMatchScore
- IReadOnlyDictionary<string
- object?> options
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FriendRequestSocialFeedOutcome>> returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:204::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.RespondToFriendRequestAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:540::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.StoreNotificationAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:649::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CalculateConnectionStrength
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:673::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.DirectionIndependentConnectionId

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:388::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync