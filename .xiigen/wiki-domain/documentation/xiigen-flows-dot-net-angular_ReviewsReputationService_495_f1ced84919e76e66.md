# xiigen-flows-dot-net-angular::ReviewsReputationService.GetReputationAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.GetReputationAsync implements local project behavior with return type Task<DataProcessResult<ReputationSummary>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:495

## Parameters
- string targetEntityId
- string targetEntityType
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ReputationSummary>> returned by xiigen-flows-dot-net-angular::ReviewsReputationService.GetReputationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:495::xiigen-flows-dot-net-angular::ReviewsReputationService.GetReputationAsync