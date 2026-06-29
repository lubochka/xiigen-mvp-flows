# xiigen-flows-dot-net-angular::BlogCmsModulesService.CreateContentAsync

## Meaning
xiigen-flows-dot-net-angular::BlogCmsModulesService.CreateContentAsync implements local project behavior with return type Task<DataProcessResult<BlogCmsModulesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:79::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentRejectsUnsafeHtmlReservedRoutesAndTenantPayload, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:103::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentIsIdempotentAndWritesBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:122::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentOptimisticConcurrencyReturns409, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:153::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.PublishUsesRevisionLockAndSeparateReviewerPools, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:49

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- string? expectedVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BlogCmsModulesOutcome>> returned by xiigen-flows-dot-net-angular::BlogCmsModulesService.CreateContentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:79::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentRejectsUnsafeHtmlReservedRoutesAndTenantPayload
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:103::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentIsIdempotentAndWritesBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:122::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.CreateContentOptimisticConcurrencyReturns409
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:153::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.PublishUsesRevisionLockAndSeparateReviewerPools
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:214::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Application\BlogCmsModulesService.cs:49::xiigen-flows-dot-net-angular::BlogCmsModulesService.CreateContentAsync