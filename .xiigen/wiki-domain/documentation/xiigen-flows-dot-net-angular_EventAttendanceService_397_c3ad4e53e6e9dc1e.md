# xiigen-flows-dot-net-angular::EventAttendanceService.SubmitFeedbackAsync

## Meaning
xiigen-flows-dot-net-angular::EventAttendanceService.SubmitFeedbackAsync implements local project behavior with return type Task<DataProcessResult<EventAttendanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:602::xiigen-flows-dot-net-angular::is.FindFeedbackWindowAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded, xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:220::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.FeedbackRequiresExistingTraceAndIsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:251::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:397

## Parameters
- string eventId
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EventAttendanceOutcome>> returned by xiigen-flows-dot-net-angular::EventAttendanceService.SubmitFeedbackAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Tests\EventAttendanceServiceTests.cs:293::xiigen-flows-dot-net-angular::EventAttendanceServiceTests.ProductSpec_FeedbackWindowAndFeedbackSubmissionWorkAfterEventEnded
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:220::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.FeedbackRequiresExistingTraceAndIsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:251::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:602::xiigen-flows-dot-net-angular::is.FindFeedbackWindowAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-04-event-attendance\server\Xiigen.Flow.EventAttendance.Application\EventAttendanceService.cs:397::xiigen-flows-dot-net-angular::EventAttendanceService.SubmitFeedbackAsync