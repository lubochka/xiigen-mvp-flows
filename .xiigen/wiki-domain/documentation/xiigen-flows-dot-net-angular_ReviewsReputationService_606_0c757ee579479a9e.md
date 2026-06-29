# xiigen-flows-dot-net-angular::ReviewsReputationService.ValidateSubmission

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.ValidateSubmission implements local project behavior with return type ReviewsReputationOutcome?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:606

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
ReviewsReputationOutcome? returned by xiigen-flows-dot-net-angular::ReviewsReputationService.ValidateSubmission

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:41::xiigen-flows-dot-net-angular::ReviewsReputationService.SubmitReviewAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:606::xiigen-flows-dot-net-angular::ReviewsReputationService.ValidateSubmission