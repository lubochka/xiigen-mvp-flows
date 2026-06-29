# xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync

## Meaning
xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync implements local project behavior with return type Task<DataProcessResult<SaasMultiTenancyOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:433::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ResolveTierDefinitionAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:53::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ProvisionTenantAsync, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:283::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync.

## Location
flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392

## Parameters
- string tenant
- string tier
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SaasMultiTenancyOutcome>> returned by xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:53::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ProvisionTenantAsync
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:283::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:433::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ResolveTierDefinitionAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync