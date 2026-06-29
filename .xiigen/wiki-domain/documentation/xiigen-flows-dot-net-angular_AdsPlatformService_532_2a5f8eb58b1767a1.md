# xiigen-flows-dot-net-angular::AdsPlatformService.RestoreBudgetAsync

## Meaning
xiigen-flows-dot-net-angular::AdsPlatformService.RestoreBudgetAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:823::xiigen-flows-dot-net-angular::AdsPlatformService.ReadLong. It is called by xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync.

## Location
flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:532

## Parameters
- string advertiserId
- long bidAmount
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::AdsPlatformService.RestoreBudgetAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:65::xiigen-flows-dot-net-angular::AdsPlatformService.ProcessAdRequestAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:823::xiigen-flows-dot-net-angular::AdsPlatformService.ReadLong

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:532::xiigen-flows-dot-net-angular::AdsPlatformService.RestoreBudgetAsync