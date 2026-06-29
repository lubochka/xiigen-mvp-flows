# xiigen-flows-dot-net-angular::AiSafetyModerationService.ComposeLessonAsync

## Meaning
xiigen-flows-dot-net-angular::AiSafetyModerationService.ComposeLessonAsync implements local project behavior with return type Task<DataProcessResult<AiSafetyModerationOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:322::xiigen-flows-dot-net-angular::AiSafetyModerationService.HasGrantedConsentAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:329::xiigen-flows-dot-net-angular::AiSafetyModerationService.IssueSafetyToken. It is called by xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:60::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF459_MissingConsentGateFailsClosedBeforeLessonWrite, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:74::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ComposeSafetyPublishOrderIssuesTokenAndOutboxAfterState, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:94::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF470_UnsafeLessonIsRejectedAndNeverPublished.

## Location
flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:71

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<AiSafetyModerationOutcome>> returned by xiigen-flows-dot-net-angular::AiSafetyModerationService.ComposeLessonAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:60::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF459_MissingConsentGateFailsClosedBeforeLessonWrite
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:74::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF24_2_ComposeSafetyPublishOrderIssuesTokenAndOutboxAfterState
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Tests\AiSafetyModerationServiceTests.cs:94::xiigen-flows-dot-net-angular::AiSafetyModerationServiceTests.CF470_UnsafeLessonIsRejectedAndNeverPublished

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:322::xiigen-flows-dot-net-angular::AiSafetyModerationService.HasGrantedConsentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:329::xiigen-flows-dot-net-angular::AiSafetyModerationService.IssueSafetyToken

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:71::xiigen-flows-dot-net-angular::AiSafetyModerationService.ComposeLessonAsync