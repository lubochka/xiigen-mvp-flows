# xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync

## Meaning
xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync implements local project behavior with return type Task<DataProcessResult<EventManagementReadModel>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:432::xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:400

## Parameters
- string eventId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventManagementReadModel>> returned by xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:432::xiigen-flows-dot-net-angular::EventManagementService.ListEventsAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:400::xiigen-flows-dot-net-angular::EventManagementService.GetEventAsync