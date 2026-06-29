# xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync

## Meaning
xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync implements local project behavior with return type Task<DataProcessResult<EventAttendanceStatus>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:264::xiigen-flows-dot-net-angular::EventAttendanceService.CheckInAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584::xiigen-flows-dot-net-angular::is.FindRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:593::xiigen-flows-dot-net-angular::is.FindCheckInAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:602::xiigen-flows-dot-net-angular::is.FindFeedbackWindowAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747::xiigen-flows-dot-net-angular::is.CanCancel, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:105::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0404_AttendanceData_IsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499

## Parameters
- string eventId
- string attendeeId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAttendanceStatus>> returned by xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:105::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0404_AttendanceData_IsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:264::xiigen-flows-dot-net-angular::EventAttendanceService.CheckInAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584::xiigen-flows-dot-net-angular::is.FindRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:593::xiigen-flows-dot-net-angular::is.FindCheckInAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:602::xiigen-flows-dot-net-angular::is.FindFeedbackWindowAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747::xiigen-flows-dot-net-angular::is.CanCancel
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499::xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync