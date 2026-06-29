# xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync

## Meaning
xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync implements local project behavior with return type Task<DataProcessResult<EtlDataIntegrationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. No internal caller was detected.

## Location
flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:261

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<EtlDataIntegrationOutcome>> returned by xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:261::xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync