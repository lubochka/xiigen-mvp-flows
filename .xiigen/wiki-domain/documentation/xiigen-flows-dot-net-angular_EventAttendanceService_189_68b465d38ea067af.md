# xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync

## Meaning
xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync implements local project behavior with return type Task<DataProcessResult<EventAttendanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:518::xiigen-flows-dot-net-angular::EventAttendanceService.PromoteNextWaitlistedAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584::xiigen-flows-dot-net-angular::is.FindRsvpAsync, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747::xiigen-flows-dot-net-angular::is.CanCancel, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189

## Parameters
- string eventId
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAttendanceOutcome>> returned by xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:54::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0402_WaitlistPromotion_UsesFifoJoinTimestamp
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:276::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_CancellationPromotesEarliestWaitlistedAttendee

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:518::xiigen-flows-dot-net-angular::EventAttendanceService.PromoteNextWaitlistedAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:584::xiigen-flows-dot-net-angular::is.FindRsvpAsync
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:747::xiigen-flows-dot-net-angular::is.CanCancel
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:189::xiigen-flows-dot-net-angular::EventAttendanceService.CancelRsvpAsync