# xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync

## Meaning
xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync implements local project behavior with return type Task<DataProcessResult<HumanInteractionGateOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:353::xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:407::xiigen-flows-dot-net-angular::HumanInteractionGateService.ReadDictionaryArray. It is called by xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Tests\HumanInteractionGateServiceTests.cs:168::xiigen-flows-dot-net-angular::HumanInteractionGateServiceTests.CompletionGateFailsClosedWhileBlockingTasksOrJudgeGatesRemain.

## Location
flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:228

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<HumanInteractionGateOutcome>> returned by xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Tests\HumanInteractionGateServiceTests.cs:168::xiigen-flows-dot-net-angular::HumanInteractionGateServiceTests.CompletionGateFailsClosedWhileBlockingTasksOrJudgeGatesRemain

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:353::xiigen-flows-dot-net-angular::HumanInteractionGateService.CountOpenRequiredTasksAsync
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:407::xiigen-flows-dot-net-angular::HumanInteractionGateService.ReadDictionaryArray

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:228::xiigen-flows-dot-net-angular::HumanInteractionGateService.EvaluateCompletionAsync