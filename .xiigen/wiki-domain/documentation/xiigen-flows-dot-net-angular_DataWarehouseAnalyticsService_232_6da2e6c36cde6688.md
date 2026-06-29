# xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ExecuteQueryAsync

## Meaning
xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ExecuteQueryAsync implements local project behavior with return type Task<DataProcessResult<DataWarehouseAnalyticsOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:331::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ResolveResultLimitAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Tests\DataWarehouseAnalyticsServiceTests.cs:177::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsServiceTests.ExecuteQueryUsesFreedomLimitAndReportsMetricTotal, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Tests\EtlDataIntegrationServiceTests.cs:177::xiigen-flows-dot-net-angular::EtlDataIntegrationServiceTests.ExecuteQueryUsesFreedomLimitAndReportsMetricTotal.

## Location
flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:232

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DataWarehouseAnalyticsOutcome>> returned by xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ExecuteQueryAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Tests\DataWarehouseAnalyticsServiceTests.cs:177::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsServiceTests.ExecuteQueryUsesFreedomLimitAndReportsMetricTotal
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Tests\EtlDataIntegrationServiceTests.cs:177::xiigen-flows-dot-net-angular::EtlDataIntegrationServiceTests.ExecuteQueryUsesFreedomLimitAndReportsMetricTotal

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:331::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ResolveResultLimitAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:533::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ReadDecimal
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:232::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ExecuteQueryAsync