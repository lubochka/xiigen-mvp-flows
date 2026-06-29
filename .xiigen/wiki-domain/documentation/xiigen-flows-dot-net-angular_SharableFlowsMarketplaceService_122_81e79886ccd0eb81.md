# xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishRagBlueprintAsync

## Meaning
xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishRagBlueprintAsync implements local project behavior with return type Task<DataProcessResult<SharableFlowsMarketplaceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:124::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.InstallRagSettlementAndSandboxAreTenantScopedAndIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:160::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.RagBlueprintRejectsDatasetsAndSettlementUsesIntegerCents, xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:178::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.DashboardSummarizesMarketplaceLifecycle.

## Location
flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:122

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SharableFlowsMarketplaceOutcome>> returned by xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishRagBlueprintAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:124::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.InstallRagSettlementAndSandboxAreTenantScopedAndIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:160::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.RagBlueprintRejectsDatasetsAndSettlementUsesIntegerCents
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Tests\SharableFlowsMarketplaceServiceTests.cs:178::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceServiceTests.DashboardSummarizesMarketplaceLifecycle

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:122::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.PublishRagBlueprintAsync