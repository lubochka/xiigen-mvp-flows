# xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.AddRequiredString

## Meaning
xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.AddRequiredString implements local project behavior with return type void. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:400::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ValidatePlanRequest, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:462::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ValidateEventIngestionRequest, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:400::xiigen-flows-dot-net-angular::EtlDataIntegrationService.ValidatePlanRequest, xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:462::xiigen-flows-dot-net-angular::EtlDataIntegrationService.ValidateEventIngestionRequest, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:520::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ValidateProvisionRequest.

## Location
flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:517

## Parameters
- List<Dictionary<string
- object?>> errors
- IReadOnlyDictionary<string
- object?> request
- string field
- string? key

## Return
void returned by xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.AddRequiredString

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:400::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ValidatePlanRequest
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:462::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.ValidateEventIngestionRequest
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:400::xiigen-flows-dot-net-angular::EtlDataIntegrationService.ValidatePlanRequest
- xiigen-flows-dot-net-angular::flows\FLOW-14-etl-data-integration\server\Xiigen.Flow.EtlDataIntegration.Application\EtlDataIntegrationService.cs:462::xiigen-flows-dot-net-angular::EtlDataIntegrationService.ValidateEventIngestionRequest
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:520::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ValidateProvisionRequest

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:517::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.AddRequiredString