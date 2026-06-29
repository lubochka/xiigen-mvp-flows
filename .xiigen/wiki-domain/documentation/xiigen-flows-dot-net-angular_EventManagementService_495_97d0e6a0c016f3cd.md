# xiigen-flows-dot-net-angular::in.TrackBestEffortAsync

## Meaning
xiigen-flows-dot-net-angular::in.TrackBestEffortAsync implements local project behavior with return type Task<DataProcessResult<EventAnalyticsReadModel>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:219::xiigen-flows-dot-net-angular::EventManagementService.RegisterAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:344::xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:458::xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495

## Parameters
- string eventId
- string analyticsType
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAnalyticsReadModel>> returned by xiigen-flows-dot-net-angular::in.TrackBestEffortAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:219::xiigen-flows-dot-net-angular::EventManagementService.RegisterAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:344::xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:458::xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495::xiigen-flows-dot-net-angular::in.TrackBestEffortAsync