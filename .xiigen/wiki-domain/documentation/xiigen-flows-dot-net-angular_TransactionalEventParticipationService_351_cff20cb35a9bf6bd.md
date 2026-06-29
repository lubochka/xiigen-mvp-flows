# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ValidateParticipation

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ValidateParticipation implements local project behavior with return type TransactionalEventParticipationOutcome?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:351

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
TransactionalEventParticipationOutcome? returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ValidateParticipation

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:351::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ValidateParticipation