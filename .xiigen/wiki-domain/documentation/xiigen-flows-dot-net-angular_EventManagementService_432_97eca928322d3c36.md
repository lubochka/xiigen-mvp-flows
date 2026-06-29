# xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync

## Meaning
xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync implements local project behavior with return type Task<DataProcessResult<IReadOnlyList<EventManagementReadModel>>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:400::xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:114::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0304_EventsAndReadsAreTenantScoped.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:432

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<IReadOnlyList<EventManagementReadModel>>> returned by xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:114::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0304_EventsAndReadsAreTenantScoped

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:400::xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:432::xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync