# xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ValidateAsync

## Meaning
xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ValidateAsync implements local project behavior with return type Task<DataProcessResult<PaymentValidationResult>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Infrastructure\SubscriptionBillingServiceCollectionExtensions.cs:23

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<PaymentValidationResult>> returned by xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ValidateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Infrastructure\SubscriptionBillingServiceCollectionExtensions.cs:23::xiigen-flows-dot-net-angular::InMemorySubscriptionPaymentFabric.ValidateAsync