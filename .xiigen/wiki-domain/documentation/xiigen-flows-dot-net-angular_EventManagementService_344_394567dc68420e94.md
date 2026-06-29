# xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync

## Meaning
xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync implements local project behavior with return type Task<DataProcessResult<EventManagementOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495::xiigen-flows-dot-net-angular::in.TrackBestEffortAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:603::xiigen-flows-dot-net-angular::in.ResolveFeedPlacements, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:655::xiigen-flows-dot-net-angular::in.GetFreedomStringListAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:84::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0303_ContentSafetyFailureRejectsPromotionWithoutSystemFailure.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:344

## Parameters
- string eventId
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventManagementOutcome>> returned by xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:39::xiigen-flows-dot-net-angular::EventManagementService.CreateEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:84::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0303_ContentSafetyFailureRejectsPromotionWithoutSystemFailure

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:495::xiigen-flows-dot-net-angular::in.TrackBestEffortAsync
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:603::xiigen-flows-dot-net-angular::in.ResolveFeedPlacements
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:655::xiigen-flows-dot-net-angular::in.GetFreedomStringListAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-06-user-groups-communities\server\Xiigen.Flow.UserGroupsCommunities.Application\UserGroupsCommunitiesService.cs:753::xiigen-flows-dot-net-angular::in.ReadDouble
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:37::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.ListAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:344::xiigen-flows-dot-net-angular::EventManagementService.PromoteEventAsync