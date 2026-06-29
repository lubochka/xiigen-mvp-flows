# xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.SubmitChangeAsync

## Meaning
xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.SubmitChangeAsync implements local project behavior with return type Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:78::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.SubmitChangeRequiresContentAddressedDiffAndWritesBeforeEvent, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:196::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:80::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.SubmitChangeRequiresContentAddressedDiffAndWritesBeforeEvent, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:198::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope.

## Location
flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:71

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>> returned by xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.SubmitChangeAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:78::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.SubmitChangeRequiresContentAddressedDiffAndWritesBeforeEvent
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:196::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:80::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.SubmitChangeRequiresContentAddressedDiffAndWritesBeforeEvent
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:198::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.TenantScopePayloadIsRejectedBecauseTenantContextOwnsScope

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:230::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.AuditAsync
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:71::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.SubmitChangeAsync