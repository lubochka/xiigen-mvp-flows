# xiigen-flows-dot-net-angular::is.CanCancel

## Meaning
xiigen-flows-dot-net-angular::is.CanCancel implements local project behavior with return type bool. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189::xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499::xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747

## Parameters
- DocumentRecord rsvp

## Return
bool returned by xiigen-flows-dot-net-angular::is.CanCancel

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189::xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:499::xiigen-flows-dot-net-angular::EventAttendanceService.GetAttendanceStatusAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747::xiigen-flows-dot-net-angular::is.CanCancel