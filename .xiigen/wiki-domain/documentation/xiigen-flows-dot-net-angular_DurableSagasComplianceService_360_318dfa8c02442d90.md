# xiigen-flows-dot-net-angular::DurableSagasComplianceService.EnforceRetentionAsync

## Meaning
xiigen-flows-dot-net-angular::DurableSagasComplianceService.EnforceRetentionAsync implements local project behavior with return type Task<DataProcessResult<DurableSagasComplianceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Tests\DurableSagasComplianceServiceTests.cs:186::xiigen-flows-dot-net-angular::DurableSagasComplianceServiceTests.CF19_4_RetentionSkipsLegalHoldAndArchivesBeforePurge, xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Tests\DurableSagasComplianceServiceTests.cs:246::xiigen-flows-dot-net-angular::DurableSagasComplianceServiceTests.TenantScopePayloadFieldsAreRejectedAcrossMutations.

## Location
flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:360

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DurableSagasComplianceOutcome>> returned by xiigen-flows-dot-net-angular::DurableSagasComplianceService.EnforceRetentionAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Tests\DurableSagasComplianceServiceTests.cs:186::xiigen-flows-dot-net-angular::DurableSagasComplianceServiceTests.CF19_4_RetentionSkipsLegalHoldAndArchivesBeforePurge
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Tests\DurableSagasComplianceServiceTests.cs:246::xiigen-flows-dot-net-angular::DurableSagasComplianceServiceTests.TenantScopePayloadFieldsAreRejectedAcrossMutations

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-19-durable-sagas-compliance\server\Xiigen.Flow.DurableSagasCompliance.Application\DurableSagasComplianceService.cs:360::xiigen-flows-dot-net-angular::DurableSagasComplianceService.EnforceRetentionAsync