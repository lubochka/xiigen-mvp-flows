# xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync

## Meaning
xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync implements local project behavior with return type Task<DataProcessResult<VisualFlowEngineOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:687::xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:69::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_1_PublishedCanvasCannotBeModified, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:145::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationRejectsCycleBeforeTypeCheck, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:161::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationRejectsTypeMismatchAfterAcyclicCheck, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:176::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationUsesOptimisticConcurrencyAndEmitsOnSuccess, xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:311::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.TenantScopePayloadFieldsAreRejectedAcrossAllMutations.

## Location
flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:176

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<VisualFlowEngineOutcome>> returned by xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:69::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_1_PublishedCanvasCannotBeModified
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:145::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationRejectsCycleBeforeTypeCheck
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:161::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationRejectsTypeMismatchAfterAcyclicCheck
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:176::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.CF18_2_PublicationUsesOptimisticConcurrencyAndEmitsOnSuccess
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Tests\VisualFlowEngineServiceTests.cs:311::xiigen-flows-dot-net-angular::VisualFlowEngineServiceTests.TenantScopePayloadFieldsAreRejectedAcrossAllMutations

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:496::xiigen-flows-dot-net-angular::SubscriptionBillingService.StoreAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:391::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.WithTenant
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:547::xiigen-flows-dot-net-angular::was.ReplayIfDuplicateAsync
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:661::xiigen-flows-dot-net-angular::was.ErrorBody
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:687::xiigen-flows-dot-net-angular::VisualFlowEngineService.DetectCycle
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-18-visual-flow-engine\server\Xiigen.Flow.VisualFlowEngine.Application\VisualFlowEngineService.cs:176::xiigen-flows-dot-net-angular::VisualFlowEngineService.PublishFlowAsync