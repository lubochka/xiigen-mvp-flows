# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RecordAnalyticsAsync

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RecordAnalyticsAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:180::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_4_AnalyticsIsAppendOnlyAndRejectsPii.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:386

## Parameters
- string eventId
- string participationId
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RecordAnalyticsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:180::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_4_AnalyticsIsAppendOnlyAndRejectsPii

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:386::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.RecordAnalyticsAsync