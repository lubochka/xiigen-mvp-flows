# xiigen-flows-dot-net-angular::EventAttendanceService.OpenFeedbackWindowAsync

## Meaning
xiigen-flows-dot-net-angular::EventAttendanceService.OpenFeedbackWindowAsync implements local project behavior with return type Task<DataProcessResult<EventAttendanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:82::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0403_FeedbackWindow_OpensOnlyFromEventEnded, xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:350

## Parameters
- string eventId
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAttendanceOutcome>> returned by xiigen-flows-dot-net-angular::EventAttendanceService.OpenFeedbackWindowAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:82::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.CF0403_FeedbackWindow_OpensOnlyFromEventEnded
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:500::xiigen-flows-dot-net-angular::ProfileEnrichmentService.GetFreedomNumberAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:350::xiigen-flows-dot-net-angular::EventAttendanceService.OpenFeedbackWindowAsync