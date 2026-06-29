# xiigen-flows-dot-net-angular::SaasMultiTenancyService.UpdateConfigurationAsync

## Meaning
xiigen-flows-dot-net-angular::SaasMultiTenancyService.UpdateConfigurationAsync implements local project behavior with return type Task<DataProcessResult<SaasMultiTenancyOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:117::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF152_MachineLockedConfigurationKeysAreRejectedAndAudited, xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:136::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.MutableConfigurationUsesOptimisticConcurrencyAndRejectsStaleWrites.

## Location
flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:214

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SaasMultiTenancyOutcome>> returned by xiigen-flows-dot-net-angular::SaasMultiTenancyService.UpdateConfigurationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:117::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.CF152_MachineLockedConfigurationKeysAreRejectedAndAudited
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Tests\SaasMultiTenancyServiceTests.cs:136::xiigen-flows-dot-net-angular::SaasMultiTenancyServiceTests.MutableConfigurationUsesOptimisticConcurrencyAndRejectsStaleWrites

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-15-saas-multi-tenancy\server\Xiigen.Flow.SaasMultiTenancy.Application\SaasMultiTenancyService.cs:214::xiigen-flows-dot-net-angular::SaasMultiTenancyService.UpdateConfigurationAsync