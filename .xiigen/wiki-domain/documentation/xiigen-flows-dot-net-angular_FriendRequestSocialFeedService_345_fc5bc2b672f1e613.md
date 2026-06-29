# xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetAnalyticsAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetAnalyticsAsync implements local project behavior with return type Task<DataProcessResult<FriendRequestSocialFeedAnalytics>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble. It is called by xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:292::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF894_Analytics_AggregatesTenantMetricsWithoutUserIdentifiers, xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:253::xiigen-flows-dot-net-angular::MarketplaceServiceTests.Analytics_UsesInquiriesOverViewsOrOne_AndNeverStoresViewerIds, xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:311::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.Analytics_AggregatesCountsWithoutParticipantIdentifiers.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:345

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FriendRequestSocialFeedAnalytics>> returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetAnalyticsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:292::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF894_Analytics_AggregatesTenantMetricsWithoutUserIdentifiers
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Tests\MarketplaceServiceTests.cs:253::xiigen-flows-dot-net-angular::MarketplaceServiceTests.Analytics_UsesInquiriesOverViewsOrOne_AndNeverStoresViewerIds
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:311::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.Analytics_AggregatesCountsWithoutParticipantIdentifiers

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:345::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.GetAnalyticsAsync