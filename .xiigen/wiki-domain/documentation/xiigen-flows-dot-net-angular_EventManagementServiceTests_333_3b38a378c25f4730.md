# xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest

## Meaning
xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:28::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0301_CreateEvent_StoresEventBeforeEventCreatedOutbox, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:53::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0302_CapacityNullMeansUnlimitedAndZeroMeansWaitlistOnly, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:84::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0303_ContentSafetyFailureRejectsPromotionWithoutSystemFailure, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:114::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0304_EventsAndReadsAreTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:150::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF858_ConcurrentCreateSameResource_AllowsOneWinnerAndRejectsOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:171::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF859_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:221::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF861_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:240::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:267::xiigen-flows-dot-net-angular::EventManagementServiceTests.ProductSpec_MatchingWeightsFeedPlacementAndNotificationChannels_AreApplied, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:297::xiigen-flows-dot-net-angular::EventManagementServiceTests.ProductSpec_ZeroAudienceFallsBackToRegionAndIndustry, xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:315::xiigen-flows-dot-net-angular::EventManagementServiceTests.AnalyticsCounter_IsTtlWindowedAndBestEffort.

## Location
flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:333

## Parameters
- string title
- int? capacity
- bool promotionEnabled
- bool simulateSlowWrite
- bool simulateFabricTimeout
- IReadOnlyDictionary<string
- object?>? matchingCriteria
- bool pushEnabled
- bool urgent

## Return
object?> returned by xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:28::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0301_CreateEvent_StoresEventBeforeEventCreatedOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:53::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0302_CapacityNullMeansUnlimitedAndZeroMeansWaitlistOnly
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:84::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0303_ContentSafetyFailureRejectsPromotionWithoutSystemFailure
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:114::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF0304_EventsAndReadsAreTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:150::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF858_ConcurrentCreateSameResource_AllowsOneWinnerAndRejectsOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:171::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF859_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:221::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF861_DownstreamTimeoutReturnsRetryHintAndRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:240::xiigen-flows-dot-net-angular::EventManagementServiceTests.CF862_ConcurrentRegistrationCapacity_AllowsOneWinnerAndRejectsOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:267::xiigen-flows-dot-net-angular::EventManagementServiceTests.ProductSpec_MatchingWeightsFeedPlacementAndNotificationChannels_AreApplied
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:297::xiigen-flows-dot-net-angular::EventManagementServiceTests.ProductSpec_ZeroAudienceFallsBackToRegionAndIndustry
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:315::xiigen-flows-dot-net-angular::EventManagementServiceTests.AnalyticsCounter_IsTtlWindowedAndBestEffort

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Tests\EventManagementServiceTests.cs:333::xiigen-flows-dot-net-angular::EventManagementServiceTests.EventRequest