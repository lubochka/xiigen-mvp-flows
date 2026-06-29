# xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync implements local project behavior with return type Task<DataProcessResult<CompletionGamificationDashboard>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:632::xiigen-flows-dot-net-angular::CompletionGamificationService.GetStreakAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:641::xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync, xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response. No internal caller was detected.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:241

## Parameters
- string learnerId
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CompletionGamificationDashboard>> returned by xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:632::xiigen-flows-dot-net-angular::CompletionGamificationService.GetStreakAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:641::xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-10-reviews-reputation\server\Xiigen.Flow.ReviewsReputation.Tests\ReviewsReputationServiceTests.cs:290::xiigen-flows-dot-net-angular::ReviewsReputationServiceTests.Response

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:241::xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync