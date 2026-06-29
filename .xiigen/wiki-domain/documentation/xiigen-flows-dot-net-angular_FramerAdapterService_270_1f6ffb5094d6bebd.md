# xiigen-flows-dot-net-angular::FramerAdapterService.RecordPublicationAsync

## Meaning
xiigen-flows-dot-net-angular::FramerAdapterService.RecordPublicationAsync implements local project behavior with return type Task<DataProcessResult<FramerAdapterOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. No internal caller was detected.

## Location
flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:270

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FramerAdapterOutcome>> returned by xiigen-flows-dot-net-angular::FramerAdapterService.RecordPublicationAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-44-framer-adapter\server\Xiigen.Flow.FramerAdapter.Application\FramerAdapterService.cs:270::xiigen-flows-dot-net-angular::FramerAdapterService.RecordPublicationAsync