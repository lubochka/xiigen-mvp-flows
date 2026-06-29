# xiigen-flows-dot-net-angular::SaasMultiTenancyService.ChangeLifecycleAsync

## Meaning
xiigen-flows-dot-net-angular::SaasMultiTenancyService.ChangeLifecycleAsync implements local project behavior with return type Task<DataProcessResult<SaasMultiTenancyOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:501::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ValidateTransition, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:175::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF153_SuspensionUpdatesStatusAndCascadesWithoutDeletingTenant, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:198::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.TerminationEmitsTenantTerminatedAndDataPurgeRequested, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:217::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.PausedSubscriptionStateIsRejectedAsTenantLifecycleState.

## Location
flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:296

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SaasMultiTenancyOutcome>> returned by xiigen-flows-dot-net-angular::SaasMultiTenancyService.ChangeLifecycleAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:175::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF153_SuspensionUpdatesStatusAndCascadesWithoutDeletingTenant
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:198::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.TerminationEmitsTenantTerminatedAndDataPurgeRequested
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:217::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.PausedSubscriptionStateIsRejectedAsTenantLifecycleState

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:501::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ValidateTransition
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:296::xiigen-flows-dot-net-angular::SaasMultiTenancyService.ChangeLifecycleAsync