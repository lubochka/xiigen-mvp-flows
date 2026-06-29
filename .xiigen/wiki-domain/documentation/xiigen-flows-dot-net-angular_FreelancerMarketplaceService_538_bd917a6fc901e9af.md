# xiigen-flows-dot-net-angular::FreelancerMarketplaceService.StartLifoCompensationAsync

## Meaning
xiigen-flows-dot-net-angular::FreelancerMarketplaceService.StartLifoCompensationAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:342::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReleaseMilestoneAsync.

## Location
flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:538

## Parameters
- string milestoneId
- string contractId
- string reason
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::FreelancerMarketplaceService.StartLifoCompensationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:342::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.ReleaseMilestoneAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:538::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.StartLifoCompensationAsync