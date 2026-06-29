# xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:29::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1001_EligibilityRunsBeforeValidationIdempotencyAuditAndReviewWrites, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:48::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1001_ServerDerivesReviewIdAndAuditExcludesReviewText, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:162::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF913_ConcurrentSubmission_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:176::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF914_IdempotencyReplayReturnsCachedReviewAndNoDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:194::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF915_InvalidRatingReturnsFieldDetailsAndNoPartialWrite, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:210::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF916_FabricTimeoutReturns504AndRetainsRecoveryOutbox, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:257::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync, xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Tests\FreelancerMarketplaceServiceTests.cs:278::xiigen-flows-dot-net-angular::FreelancerMarketplaceServiceTests.CF17_4_ReviewDirectionDuplicateAppendOnlyAndAuditExcludesComment.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264

## Parameters
- string reviewerId
- string targetEntityId
- int rating
- bool simulateSlowWrite

## Return
object?> returned by xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:29::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1001_EligibilityRunsBeforeValidationIdempotencyAuditAndReviewWrites
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:48::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1001_ServerDerivesReviewIdAndAuditExcludesReviewText
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:145::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1004_CrossTenantReviewSearchDoesNotLeak
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:162::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF913_ConcurrentSubmission_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:176::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF914_IdempotencyReplayReturnsCachedReviewAndNoDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:194::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF915_InvalidRatingReturnsFieldDetailsAndNoPartialWrite
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:210::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF916_FabricTimeoutReturns504AndRetainsRecoveryOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:257::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.SubmitAcceptedReviewAsync
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Tests\FreelancerMarketplaceServiceTests.cs:278::xiigen-flows-dot-net-angular::FreelancerMarketplaceServiceTests.CF17_4_ReviewDirectionDuplicateAppendOnlyAndAuditExcludesComment

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:264::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Review