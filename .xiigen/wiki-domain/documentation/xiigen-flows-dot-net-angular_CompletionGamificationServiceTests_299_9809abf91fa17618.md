# xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CompletionRequest

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CompletionRequest implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:29::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0501_PointsAreDerivedServerSideAndEarnedPointsInputIsRejected, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:49::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0502_StreakUsesRequiredTimezoneOffsetAndStoresLearnerLocalDate, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:70::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0503_SocialShareRequiresAchievementAndExplicitConsent, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:95::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0504_CompletionData_IsTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:127::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF867_ConcurrentCompletion_AllowsOneWinnerAndOneConflict, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:148::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF868_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:190::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF870_TimeoutReturnsRetryHintAndRetainsOutboxRecord, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:211::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF875_DuplicateCompletionWithDifferentKeyDoesNotDoubleAwardPoints, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:229::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_DashboardSummarizesPointsStreakAchievementsAndCompletions, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:268::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_StatusReportsNotStartedAndCompletedStates, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:283::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_LearningCompletionRunsTerminalOutboxEvent.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:299

## Parameters
- string lessonId
- string learnerId
- int scorePercent
- int timezoneOffset
- bool shareConsent
- bool privateMode
- bool simulateSlowWrite
- bool simulateFabricTimeout

## Return
object?> returned by xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CompletionRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:29::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0501_PointsAreDerivedServerSideAndEarnedPointsInputIsRejected
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:49::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0502_StreakUsesRequiredTimezoneOffsetAndStoresLearnerLocalDate
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:70::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0503_SocialShareRequiresAchievementAndExplicitConsent
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:95::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF0504_CompletionData_IsTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:127::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF867_ConcurrentCompletion_AllowsOneWinnerAndOneConflict
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:148::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF868_IdempotentReplay_ReturnsCachedResponseWithoutDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:190::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF870_TimeoutReturnsRetryHintAndRetainsOutboxRecord
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:211::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CF875_DuplicateCompletionWithDifferentKeyDoesNotDoubleAwardPoints
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:229::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_DashboardSummarizesPointsStreakAchievementsAndCompletions
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:268::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_StatusReportsNotStartedAndCompletedStates
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:283::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.ProductSpec_LearningCompletionRunsTerminalOutboxEvent

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Tests\CompletionGamificationServiceTests.cs:299::xiigen-flows-dot-net-angular::CompletionGamificationServiceTests.CompletionRequest