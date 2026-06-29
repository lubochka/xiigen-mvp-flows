# xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync

## Meaning
xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync implements local project behavior with return type Task<DataProcessResult<IReadOnlyList<DocumentRecord>>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:261::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:291::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:261::xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:291::xiigen-flows-dot-net-angular::EtlDataIntegrationService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:362::xiigen-flows-dot-net-angular::SaasMultiTenancyService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:431::xiigen-flows-dot-net-angular::was.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:514::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:539::xiigen-flows-dot-net-angular::VisualFlowEngineService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:476::xiigen-flows-dot-net-angular::was.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:424::xiigen-flows-dot-net-angular::AdsPlatformService.GetDashboardAsync, xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:437::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.GetDashboardAsync.

## Location
flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326

## Parameters
- string collection
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<IReadOnlyList<DocumentRecord>>> returned by xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:261::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RunQualityCheckAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:291::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:261::xiigen-flows-dot-net-angular::EtlDataIntegrationService.RunQualityCheckAsync
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:291::xiigen-flows-dot-net-angular::EtlDataIntegrationService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:362::xiigen-flows-dot-net-angular::SaasMultiTenancyService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:431::xiigen-flows-dot-net-angular::was.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-17-freelancer-marketplace\server\Xiigen.Flow.FreelancerMarketplace.Application\FreelancerMarketplaceService.cs:514::xiigen-flows-dot-net-angular::FreelancerMarketplaceService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:539::xiigen-flows-dot-net-angular::VisualFlowEngineService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:476::xiigen-flows-dot-net-angular::was.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-20-ads-platform\server\Xiigen.Flow.AdsPlatform.Application\AdsPlatformService.cs:424::xiigen-flows-dot-net-angular::AdsPlatformService.GetDashboardAsync
- xiigen-flows-dot-net-angular::flows\FLOW-21-dynamic-forms-workflows\server\Xiigen.Flow.DynamicFormsWorkflows.Application\DynamicFormsWorkflowsService.cs:437::xiigen-flows-dot-net-angular::DynamicFormsWorkflowsService.GetDashboardAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:326::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.SearchTenantAsync