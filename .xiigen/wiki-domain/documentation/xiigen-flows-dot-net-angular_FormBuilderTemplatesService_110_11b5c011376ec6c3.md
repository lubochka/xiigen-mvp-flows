# xiigen-flows-dot-net-angular::FormBuilderTemplatesService.PublishTemplateAsync

## Meaning
xiigen-flows-dot-net-angular::FormBuilderTemplatesService.PublishTemplateAsync implements local project behavior with return type Task<DataProcessResult<FormBuilderTemplatesOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:130::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_2_PublishedTemplatesAreImmutable.

## Location
flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:110

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<FormBuilderTemplatesOutcome>> returned by xiigen-flows-dot-net-angular::FormBuilderTemplatesService.PublishTemplateAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Tests\FormBuilderTemplatesServiceTests.cs:130::xiigen-flows-dot-net-angular::FormBuilderTemplatesServiceTests.CF23_2_PublishedTemplatesAreImmutable

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:110::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.PublishTemplateAsync