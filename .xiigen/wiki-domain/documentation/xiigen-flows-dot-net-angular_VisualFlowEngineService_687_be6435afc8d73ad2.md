# xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle

## Meaning
xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle implements local project behavior with return type string[]?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:700::xiigen-flows-dot-net-angular::VisualFlowEngineService.Dfs, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:176::xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync.

## Location
flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:687

## Parameters
- IReadOnlyList<FlowNode> nodes
- IReadOnlyList<FlowEdge> edges

## Return
string[]? returned by xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:176::xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:700::xiigen-flows-dot-net-angular::VisualFlowEngineService.Dfs
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:687::xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle