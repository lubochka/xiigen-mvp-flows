# xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync

## Meaning
xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync implements local project behavior with return type Task<DataProcessResult<OssCurriculumOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync, xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:63::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.AssignTierEnforcesValidationOrderingTierMapBoundaryAndFreeTenantPayloadRules, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:86::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.AssignTierIsIdempotentVersionAwareAndStoresBeforeQueueBroker, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:166::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.TimeoutRetainsOutboxWithoutPublishingToQueueBroker, xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:185::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.DashboardIsTenantScopedAndInspectorReadable.

## Location
flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- string? expectedVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<OssCurriculumOutcome>> returned by xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:63::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.AssignTierEnforcesValidationOrderingTierMapBoundaryAndFreeTenantPayloadRules
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:86::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.AssignTierIsIdempotentVersionAwareAndStoresBeforeQueueBroker
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:166::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.TimeoutRetainsOutboxWithoutPublishingToQueueBroker
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Tests\OssCurriculumServiceTests.cs:185::xiigen-flows-dot-net-angular::OssCurriculumServiceTests.DashboardIsTenantScopedAndInspectorReadable

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Application\HumanInteractionGateService.cs:332::xiigen-flows-dot-net-angular::HumanInteractionGateService.GetTenantDocumentAsync
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Infrastructure\OssCurriculumServiceCollectionExtensions.cs:25::xiigen-flows-dot-net-angular::InMemoryOssCurriculumQueueBroker.PublishAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-39-oss-curriculum\server\Xiigen.Flow.OssCurriculum.Application\OssCurriculumService.cs:64::xiigen-flows-dot-net-angular::OssCurriculumService.AssignTierAsync