# xiigen-flows-dot-net-angular::EventAttendanceServiceTests.RsvpRequest

## Meaning
xiigen-flows-dot-net-angular::EventAttendanceServiceTests.RsvpRequest implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:38::xiigen-flows-dot-net-angular::EventAttendanceService.SubmitRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:29::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0401_RsvpCreation_StoresSingleRsvpBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:105::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0404_AttendanceData_IsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:137::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF863_ConcurrentSameAttendeeRsvp_AllowsOneWinnerAndOneConflict, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:158::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF864_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:200::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF866_TimeoutReturnsRetryHintAndRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:221::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CapacityZeroRoutesWaitlistAsSuccessAndNullIsUnlimited, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:236::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_PaidRsvpRequiresPaymentConfirmed, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:257::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CheckInRequiresConfirmedRsvpAndIsIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:311

## Parameters
- string eventId
- string attendeeId
- int? capacity
- bool simulateSlowWrite
- bool simulateFabricTimeout
- bool isPaid
- bool paymentConfirmed

## Return
object?> returned by xiigen-flows-dot-net-angular::EventAttendanceServiceTests.RsvpRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:38::xiigen-flows-dot-net-angular::EventAttendanceService.SubmitRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:29::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0401_RsvpCreation_StoresSingleRsvpBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:105::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0404_AttendanceData_IsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:137::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF863_ConcurrentSameAttendeeRsvp_AllowsOneWinnerAndOneConflict
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:158::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF864_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:200::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF866_TimeoutReturnsRetryHintAndRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:221::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CapacityZeroRoutesWaitlistAsSuccessAndNullIsUnlimited
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:236::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_PaidRsvpRequiresPaymentConfirmed
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:257::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CheckInRequiresConfirmedRsvpAndIsIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:311::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.RsvpRequest