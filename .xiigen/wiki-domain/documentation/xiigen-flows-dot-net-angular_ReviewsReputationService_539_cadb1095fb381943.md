# xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync implements local project behavior with return type Task<DataProcessResult<ReputationSummary>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:174::xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:267::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:302::xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539

## Parameters
- string targetEntityId
- string targetEntityType
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ReputationSummary>> returned by xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:174::xiigen-flows-dot-net-angular::ReviewsReputationService.ModerateReviewAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:267::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:302::xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync