# xiigen-flows-dot-net-angular::FormBuilderTemplatesService.InstantiateTemplateAsync

## Meaning
xiigen-flows-dot-net-angular::FormBuilderTemplatesService.InstantiateTemplateAsync implements local project behavior with return type Task<DataProcessResult<FormBuilderTemplatesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:145::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_3_InstantiationMergesDefaultsAndBindingsThroughTenantScopedLock.

## Location
flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:143

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FormBuilderTemplatesOutcome>> returned by xiigen-flows-dot-net-angular::FormBuilderTemplatesService.InstantiateTemplateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:145::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_3_InstantiationMergesDefaultsAndBindingsThroughTenantScopedLock

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-00-bundle-activation\server\Xiigen.Flow.BundleActivation.Application\BundleStatusTrackerService.cs:129::xiigen-flows-dot-net-angular::BundleStatusTrackerService.ReadMap
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:143::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.InstantiateTemplateAsync