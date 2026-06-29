# xiigen-flows-dot-net-angular::AiSafetyModerationService.GradeQuizAsync

## Meaning
xiigen-flows-dot-net-angular::AiSafetyModerationService.GradeQuizAsync implements local project behavior with return type Task<DataProcessResult<AiSafetyModerationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:110::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_3_ClientScoreIsRejectedBeforeQuizWrite, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:125::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF463_QuizGradingIsServerSideAndGamificationAsyncAppendOnly.

## Location
flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:121

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<AiSafetyModerationOutcome>> returned by xiigen-flows-dot-net-angular::AiSafetyModerationService.GradeQuizAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:110::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_3_ClientScoreIsRejectedBeforeQuizWrite
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:125::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF463_QuizGradingIsServerSideAndGamificationAsyncAppendOnly

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:121::xiigen-flows-dot-net-angular::AiSafetyModerationService.GradeQuizAsync