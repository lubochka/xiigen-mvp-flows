# xiigen-flows-dot-net-angular::CompletionGamificationService.ValidateCompletionRequest

## Meaning
xiigen-flows-dot-net-angular::CompletionGamificationService.ValidateCompletionRequest implements local project behavior with return type DataProcessResult<CompletionGamificationOutcome>?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:704::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadScorePercent, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync.

## Location
flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:398

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
DataProcessResult<CompletionGamificationOutcome>? returned by xiigen-flows-dot-net-angular::CompletionGamificationService.ValidateCompletionRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:38::xiigen-flows-dot-net-angular::CompletionGamificationService.SubmitCompletionAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-03-event-management\server\Xiigen.Flow.EventManagement.Application\EventManagementService.cs:714::xiigen-flows-dot-net-angular::in.ReadNullableInt
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:704::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadScorePercent
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:398::xiigen-flows-dot-net-angular::CompletionGamificationService.ValidateCompletionRequest