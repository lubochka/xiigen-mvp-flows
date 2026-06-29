# xiigen-flows-dot-net-angular::TransactionalEventParticipationService.GetTicketsAsync

## Meaning
xiigen-flows-dot-net-angular::TransactionalEventParticipationService.GetTicketsAsync implements local project behavior with return type Task<DataProcessResult<ParticipationSummary>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:327::xiigen-flows-dot-net-angular::MarketplaceService.CountByStatus. It is called by xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:140::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0907_CrossTenantTicketSearchDoesNotLeak.

## Location
flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:317

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ParticipationSummary>> returned by xiigen-flows-dot-net-angular::TransactionalEventParticipationService.GetTicketsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Tests\TransactionalEventParticipationServiceTests.cs:140::xiigen-flows-dot-net-angular::TransactionalEventParticipationServiceTests.CF0907_CrossTenantTicketSearchDoesNotLeak

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-08-marketplace\server\Xiigen.Flow.Marketplace.Application\MarketplaceService.cs:327::xiigen-flows-dot-net-angular::MarketplaceService.CountByStatus

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-09-transactional-event-participation\server\Xiigen.Flow.TransactionalEventParticipation.Application\TransactionalEventParticipationService.cs:317::xiigen-flows-dot-net-angular::TransactionalEventParticipationService.GetTicketsAsync