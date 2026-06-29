# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RequestRefundAsync

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RequestRefundAsync implements local project behavior with return type Task<DataProcessResult<TransactionalEventParticipationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:68::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0903_RefundRetryExhaustionRoutesComplianceReview, xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:294::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF912_ThreeRefundRetriesWithSameKey_ReturnCachedEscalation.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:212

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<TransactionalEventParticipationOutcome>> returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RequestRefundAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:68::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0903_RefundRetryExhaustionRoutesComplianceReview
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:294::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF912_ThreeRefundRetriesWithSameKey_ReturnCachedEscalation

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:212::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RequestRefundAsync