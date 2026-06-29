# xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RecordPublicationAsync

## Meaning
xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RecordPublicationAsync implements local project behavior with return type Task<DataProcessResult<WebflowDesignerExtensionOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. No internal caller was detected.

## Location
flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:212

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<WebflowDesignerExtensionOutcome>> returned by xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RecordPublicationAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-43-webflow-designer-extension\server\Xiigen.Flow.WebflowDesignerExtension.Application\WebflowDesignerExtensionService.cs:212::xiigen-flows-dot-net-angular::WebflowDesignerExtensionService.RecordPublicationAsync