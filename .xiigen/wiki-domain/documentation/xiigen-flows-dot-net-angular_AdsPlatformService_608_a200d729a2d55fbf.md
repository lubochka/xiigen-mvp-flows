# xiigen-flows-dot-net-angular::AdsPlatformService.StoreAuditAsync

## Meaning
xiigen-flows-dot-net-angular::AdsPlatformService.StoreAuditAsync implements local project behavior with return type Task. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. No internal caller was detected.

## Location
flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:608

## Parameters
- string collection
- string id
- string auditType
- IReadOnlyDictionary<string
- object?> body
- CancellationToken cancellationToken

## Return
Task returned by xiigen-flows-dot-net-angular::AdsPlatformService.StoreAuditAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:608::xiigen-flows-dot-net-angular::AdsPlatformService.StoreAuditAsync