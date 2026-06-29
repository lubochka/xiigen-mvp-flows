# xiigen-flows-dot-net-angular::CmsPublishingService.PublishContentAsync

## Meaning
xiigen-flows-dot-net-angular::CmsPublishingService.PublishContentAsync implements local project behavior with return type Task<DataProcessResult<CmsPublishingOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:502::xiigen-flows-dot-net-angular::CmsPublishingService.WriteAuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:43::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_1_ContentPublishCreatesImmutableVersionAndOutboxAfterState, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:61::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF973_IfMatchMismatchReturnsConcurrentUpdateWithoutPartialWrite, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:77::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF974_PublishReplayReturnsCachedResponseAndNoDuplicateOutbox, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:92::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF975_ValidationRejectsEmptyPrimaryFieldBeforeAnyWrite, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:107::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF976_TimeoutKeepsStateAndOutboxForRetry, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:210::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.DashboardIsTenantIsolated, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:225::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:153::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.PublishUsesRevisionLockAndSeparateReviewerPools.

## Location
flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:71

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<CmsPublishingOutcome>> returned by xiigen-flows-dot-net-angular::CmsPublishingService.PublishContentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:43::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF22_1_ContentPublishCreatesImmutableVersionAndOutboxAfterState
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:61::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF973_IfMatchMismatchReturnsConcurrentUpdateWithoutPartialWrite
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:77::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF974_PublishReplayReturnsCachedResponseAndNoDuplicateOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:92::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF975_ValidationRejectsEmptyPrimaryFieldBeforeAnyWrite
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:107::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.CF976_TimeoutKeepsStateAndOutboxForRetry
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:210::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.DashboardIsTenantIsolated
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Tests\CmsPublishingServiceTests.cs:225::xiigen-flows-dot-net-angular::CmsPublishingServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:153::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.PublishUsesRevisionLockAndSeparateReviewerPools

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-13-data-warehouse-analytics\server\Xiigen.Flow.DataWarehouseAnalytics.Application\DataWarehouseAnalyticsService.cs:481::xiigen-flows-dot-net-angular::DataWarehouseAnalyticsService.RejectsTenantScope
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:502::xiigen-flows-dot-net-angular::CmsPublishingService.WriteAuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:71::xiigen-flows-dot-net-angular::CmsPublishingService.PublishContentAsync