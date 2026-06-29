# xiigen-flows-dot-net-angular::was.ResolveIntConfigAsync

## Meaning
xiigen-flows-dot-net-angular::was.ResolveIntConfigAsync implements local project behavior with return type Task<int>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:219::xiigen-flows-dot-net-angular::MarketplacePaymentsService.ReleaseEscrowAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:464::xiigen-flows-dot-net-angular::was.CaptureSplitAndHoldEscrowAsync, xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:342::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReleaseMilestoneAsync.

## Location
flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:565

## Parameters
- string key
- int fallback
- CancellationToken cancellationToken

## Return
Task<int> returned by xiigen-flows-dot-net-angular::was.ResolveIntConfigAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:219::xiigen-flows-dot-net-angular::MarketplacePaymentsService.ReleaseEscrowAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:464::xiigen-flows-dot-net-angular::was.CaptureSplitAndHoldEscrowAsync
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:342::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReleaseMilestoneAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:565::xiigen-flows-dot-net-angular::was.ResolveIntConfigAsync