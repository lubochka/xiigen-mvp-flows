# xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync implements local project behavior with return type Task<int>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:241::xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:641

## Parameters
- string learnerId
- CancellationToken cancellationToken

## Return
Task<int> returned by xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:241::xiigen-flows-dot-net-angular::CompletionGamificationService.GetDashboardAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:641::xiigen-flows-dot-net-angular::CompletionGamificationService.SumLedgerPointsAsync