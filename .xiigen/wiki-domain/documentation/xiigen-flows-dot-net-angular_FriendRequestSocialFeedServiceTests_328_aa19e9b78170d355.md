# xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.AcceptPairAsync

## Meaning
xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.AcceptPairAsync implements local project behavior with return type Task<string>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:204::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.RespondToFriendRequestAsync, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:347::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.FriendRequest, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:376::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.ResponseRequest. It is called by xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:277::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF893_FeedWindow_UsesConnectionStrengthBuckets, xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:292::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF894_Analytics_AggregatesTenantMetricsWithoutUserIdentifiers.

## Location
flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:328

## Parameters
- FlowFixture fixture
- string prefix
- double baseScore
- double group
- double events
- double purchase
- double questionnaire

## Return
Task<string> returned by xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.AcceptPairAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:277::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF893_FeedWindow_UsesConnectionStrengthBuckets
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:292::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.CF894_Analytics_AggregatesTenantMetricsWithoutUserIdentifiers

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:40::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.SubmitFriendRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Application\FriendRequestSocialFeedService.cs:204::xiigen-flows-dot-net-angular::FriendRequestSocialFeedService.RespondToFriendRequestAsync
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:347::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.FriendRequest
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:376::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.ResponseRequest

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-07-friend-request-social-feed\server\Xiigen.Flow.FriendRequestSocialFeed.Tests\FriendRequestSocialFeedServiceTests.cs:328::xiigen-flows-dot-net-angular::FriendRequestSocialFeedServiceTests.AcceptPairAsync