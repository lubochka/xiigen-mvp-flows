# xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:515::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:388::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463

## Parameters
- string connectionId
- string userA
- string userB
- double strength
- IReadOnlyDictionary<string
- object?> options
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:388::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.AcceptCoreAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:515::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.CheckPrivacyInlineAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:463::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.PopulateHistoricalFeedAsync