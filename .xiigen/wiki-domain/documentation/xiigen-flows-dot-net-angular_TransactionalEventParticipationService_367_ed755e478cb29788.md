# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.CalculateFeeBreakdownAsync

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.CalculateFeeBreakdownAsync implements local project behavior with return type Task<FeeBreakdown>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:378::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ResolveDecimalAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:367

## Parameters
- decimal amount
- string currency
- CancellationToken cancellationToken

## Return
Task<FeeBreakdown> returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.CalculateFeeBreakdownAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:38::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.StartParticipationAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:378::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.ResolveDecimalAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:367::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.CalculateFeeBreakdownAsync