# xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync

## Meaning
xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync implements local project behavior with return type Task<DataProcessResult<ReviewsReputationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate.

## Location
flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:302

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ReviewsReputationOutcome>> returned by xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:95::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.CF1003_ReputationUsesPublishedReviewsOnlyAndRetractsFromAggregate

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:539::xiigen-flows-dot-net-angular::ReviewsReputationService.RecalculateReputationInternalAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Application\ReviewsReputationService.cs:302::xiigen-flows-dot-net-angular::ReviewsReputationService.RetractReviewAsync