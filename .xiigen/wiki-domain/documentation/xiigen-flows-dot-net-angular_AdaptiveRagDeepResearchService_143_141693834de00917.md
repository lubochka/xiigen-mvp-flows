# xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchService.EditKnowledgeGraphAsync

## Meaning
xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchService.EditKnowledgeGraphAsync implements local project behavior with return type Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:197::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.GraphEditRequiresGateAsyncRebuildAndFreedomTraversalConfig, xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:251::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Application\AdaptiveRagDeepResearchService.cs:143

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<AdaptiveRagDeepResearchOutcome>> returned by xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchService.EditKnowledgeGraphAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:197::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.GraphEditRequiresGateAsyncRebuildAndFreedomTraversalConfig
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:251::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Application\AdaptiveRagDeepResearchService.cs:143::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchService.EditKnowledgeGraphAsync