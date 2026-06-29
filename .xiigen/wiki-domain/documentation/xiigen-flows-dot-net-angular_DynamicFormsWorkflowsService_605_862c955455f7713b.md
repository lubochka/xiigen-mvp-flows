# xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch

## Meaning
xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch implements local project behavior with return type string. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:785::xiigen-flows-dot-net-angular::VisualFlowEngineService.ReadDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:263::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync.

## Location
flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:605

## Parameters
- IReadOnlyDictionary<string
- object?> request
- IReadOnlyDictionary<string
- object?> submission

## Return
string returned by xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:263::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.DispatchAutomationAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:785::xiigen-flows-dot-net-angular::VisualFlowEngineService.ReadDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:605::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.ResolveAutomationBranch