# xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ChargeAsync

## Meaning
xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ChargeAsync implements local project behavior with return type Task<DataProcessResult<PaymentChargeResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:240::xiigen-flows-dot-net-angular::SubscriptionBillingService.ProcessBillingCycleAsync.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Infrastructure\SubscriptionBillingServiceCollectionExtensions.cs:47

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<PaymentChargeResult>> returned by xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ChargeAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:240::xiigen-flows-dot-net-angular::SubscriptionBillingService.ProcessBillingCycleAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Infrastructure\SubscriptionBillingServiceCollectionExtensions.cs:47::xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ChargeAsync