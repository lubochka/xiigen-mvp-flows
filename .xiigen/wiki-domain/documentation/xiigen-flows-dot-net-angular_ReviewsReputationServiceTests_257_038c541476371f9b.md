# xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync implements local project behavior with return type Task<string>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:71::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1002_ModerationHasPassRejectAndUncertainHumanQueuePaths, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:117::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_OwnerResponseRequiresOwnershipAllowsRevisionAfterPolicyRejectAndDoesNotUpdateReputation, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:226::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF917_SecondaryResponseConcurrency_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:241::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF918_ReputationRecomputeReplayReturnsCachedAggregate.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:257

## Parameters
- FlowFixture fixture
- string reviewerId
- string targetEntityId
- int rating

## Return
Task<string> returned by xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:71::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1002_ModerationHasPassRejectAndUncertainHumanQueuePaths
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:117::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_OwnerResponseRequiresOwnershipAllowsRevisionAfterPolicyRejectAndDoesNotUpdateReputation
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:226::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF917_SecondaryResponseConcurrency_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:241::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF918_ReputationRecomputeReplayReturnsCachedAggregate

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:257::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync