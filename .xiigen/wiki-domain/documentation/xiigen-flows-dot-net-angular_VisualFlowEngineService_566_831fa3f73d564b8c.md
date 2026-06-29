# xiigen-flows-dot-net-angular::VisualFlowEngineService.ReplayIfDuplicateAsync

## Meaning
xiigen-flows-dot-net-angular::VisualFlowEngineService.ReplayIfDuplicateAsync implements local project behavior with return type Task<VisualFlowEngineOutcome?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. No internal caller was detected.

## Location
flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:566

## Parameters
- string operationKey
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<VisualFlowEngineOutcome?> returned by xiigen-flows-dot-net-angular::VisualFlowEngineService.ReplayIfDuplicateAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:566::xiigen-flows-dot-net-angular::VisualFlowEngineService.ReplayIfDuplicateAsync