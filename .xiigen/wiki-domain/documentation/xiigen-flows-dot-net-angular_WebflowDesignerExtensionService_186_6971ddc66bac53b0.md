# xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync

## Meaning
xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync implements local project behavior with return type Task<DataProcessResult<WebflowDesignerExtensionOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest, xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:172::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements. No internal caller was detected.

## Location
flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:186

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<WebflowDesignerExtensionOutcome>> returned by xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-41-canva-text-adapter\server\Xiigen.Flow.CanvaTextAdapter.Application\CanvaTextAdapterService.cs:191::xiigen-flows-dot-net-angular::CanvaTextAdapterService.ValidateRunRequest
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:172::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.ReadDesignerElements

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:186::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RunAdapterAsync