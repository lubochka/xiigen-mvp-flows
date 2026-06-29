# xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CreateActiveSubscriptionAsync

## Meaning
xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CreateActiveSubscriptionAsync implements local project behavior with return type Task<string>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:202::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.BillingSuccessStoresInvoiceAndAuditBeforeChargeEvent, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:221::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF933_BillingConcurrencyAllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:237::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF934_BillingIdempotencyReplaysCachedInvoiceWithoutDoubleCharge.

## Location
flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:278

## Parameters
- FlowFixture fixture
- string planId
- string subscriberId
- int priceCents

## Return
Task<string> returned by xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CreateActiveSubscriptionAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:202::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.BillingSuccessStoresInvoiceAndAuditBeforeChargeEvent
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:221::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF933_BillingConcurrencyAllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:237::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CF934_BillingIdempotencyReplaysCachedInvoiceWithoutDoubleCharge

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:44::xiigen-flows-dot-net-angular::SubscriptionBillingService.PublishPlanAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:148::xiigen-flows-dot-net-angular::SubscriptionBillingService.SubscribeAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Tests\SubscriptionBillingServiceTests.cs:278::xiigen-flows-dot-net-angular::SubscriptionBillingServiceTests.CreateActiveSubscriptionAsync