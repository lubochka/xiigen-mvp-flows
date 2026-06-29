# xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync

## Meaning
xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync implements local project behavior with return type Task<DataProcessResult<EventAnalyticsReadModel>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495::xiigen-flows-dot-net-angular::in.TrackBestEffortAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:315::xiigen-flows-dot-net-angular::EventManagementServiceTests.AnalyticsCounter_IsTtlWindowedAndBestEffort.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:458

## Parameters
- string eventId
- string analyticsType
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAnalyticsReadModel>> returned by xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:315::xiigen-flows-dot-net-angular::EventManagementServiceTests.AnalyticsCounter_IsTtlWindowedAndBestEffort

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495::xiigen-flows-dot-net-angular::in.TrackBestEffortAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:458::xiigen-flows-dot-net-angular::in.TrackAnalyticsAsync