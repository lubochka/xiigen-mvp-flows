# xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync

## Meaning
xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync implements local project behavior with return type Task<IReadOnlyList<string>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:228::xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync.

## Location
flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:353

## Parameters
- string runId
- CancellationToken cancellationToken

## Return
Task<IReadOnlyList<string>> returned by xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:228::xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:353::xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync