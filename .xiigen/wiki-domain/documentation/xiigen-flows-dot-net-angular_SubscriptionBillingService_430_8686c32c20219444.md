# xiigen-flows-dot-net-angular::SubscriptionBillingService.RecalculateMrrAsync

## Meaning
xiigen-flows-dot-net-angular::SubscriptionBillingService.RecalculateMrrAsync implements local project behavior with return type Task<DataProcessResult<SubscriptionBillingOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:255::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF12_3_MrrNormalizesAnnualPlansAndUsesFreedomDunningSchedule.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:430

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SubscriptionBillingOutcome>> returned by xiigen-flows-dot-net-angular::SubscriptionBillingService.RecalculateMrrAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:255::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF12_3_MrrNormalizesAnnualPlansAndUsesFreedomDunningSchedule

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:430::xiigen-flows-dot-net-angular::SubscriptionBillingService.RecalculateMrrAsync