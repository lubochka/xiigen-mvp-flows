# xiigen-flows-dot-net-angular::ReviewsReputationService.IsEligibleForReview

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.IsEligibleForReview implements local project behavior with return type bool. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:622

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
bool returned by xiigen-flows-dot-net-angular::ReviewsReputationService.IsEligibleForReview

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:622::xiigen-flows-dot-net-angular::ReviewsReputationService.IsEligibleForReview