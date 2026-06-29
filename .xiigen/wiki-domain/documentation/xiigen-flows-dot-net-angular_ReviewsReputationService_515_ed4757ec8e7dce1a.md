# xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync implements local project behavior with return type Task<DataProcessResult<ReviewCollection>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:696::xiigen-flows-dot-net-angular::ReviewsReputationService.WithoutPrivateReviewText. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:515

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ReviewCollection>> returned by xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:696::xiigen-flows-dot-net-angular::ReviewsReputationService.WithoutPrivateReviewText

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:515::xiigen-flows-dot-net-angular::ReviewsReputationService.GetReviewsAsync