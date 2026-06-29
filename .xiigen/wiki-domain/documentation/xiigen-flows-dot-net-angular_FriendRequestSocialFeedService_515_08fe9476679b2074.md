# xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync implements local project behavior with return type Task<DataProcessResult<bool>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:515

## Parameters
- string targetUserId
- string requesterUserId
- string action
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<bool>> returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:515::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync