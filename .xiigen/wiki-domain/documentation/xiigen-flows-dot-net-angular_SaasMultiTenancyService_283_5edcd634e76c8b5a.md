# xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync

## Meaning
xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync implements local project behavior with return type Task<DataProcessResult<SaasMultiTenancyOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:159::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF153_QuotaMaterializationUsesOneMultiExecBatchFromTierDefinitions.

## Location
flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:283

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SaasMultiTenancyOutcome>> returned by xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:159::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF153_QuotaMaterializationUsesOneMultiExecBatchFromTierDefinitions

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:392::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasCoreAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:283::xiigen-flows-dot-net-angular::SaasMultiTenancyService.MaterializeQuotasAsync