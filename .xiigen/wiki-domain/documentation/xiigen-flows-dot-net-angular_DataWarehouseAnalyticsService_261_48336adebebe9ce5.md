# xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync

## Meaning
xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync implements local project behavior with return type Task<DataProcessResult<DataWarehouseAnalyticsOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Tests\DataWarehouseAnalyticsServiceTests.cs:195::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsServiceTests.DashboardAndQualityCheckDoNotLeakAcrossTenants, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Tests\EtlDataIntegrationServiceTests.cs:195::xiigen-flows-dot-net-angular::EtlDataIntegrationServiceTests.DashboardAndQualityCheckDoNotLeakAcrossTenants.

## Location
flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:261

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> returned by xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Tests\DataWarehouseAnalyticsServiceTests.cs:195::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsServiceTests.DashboardAndQualityCheckDoNotLeakAcrossTenants
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Tests\EtlDataIntegrationServiceTests.cs:195::xiigen-flows-dot-net-angular::EtlDataIntegrationServiceTests.DashboardAndQualityCheckDoNotLeakAcrossTenants

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:261::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync