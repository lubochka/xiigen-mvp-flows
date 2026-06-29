# xiigen-flows-dot-net-angular::is.FindRsvpAsync

## Meaning
xiigen-flows-dot-net-angular::is.FindRsvpAsync implements local project behavior with return type Task<DocumentRecord?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:38::xiigen-flows-dot-net-angular::EventAttendanceService.SubmitRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189::xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:264::xiigen-flows-dot-net-angular::EventAttendanceService.CheckInAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499::xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584

## Parameters
- string eventId
- string attendeeId
- CancellationToken cancellationToken

## Return
Task<DocumentRecord?> returned by xiigen-flows-dot-net-angular::is.FindRsvpAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:38::xiigen-flows-dot-net-angular::EventAttendanceService.SubmitRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189::xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:264::xiigen-flows-dot-net-angular::EventAttendanceService.CheckInAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499::xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584::xiigen-flows-dot-net-angular::is.FindRsvpAsync