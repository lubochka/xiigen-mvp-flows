# xiigen-flows-dot-net-angular::DesignIntelligenceEngineService.ValidateComplianceAsync

## Meaning
xiigen-flows-dot-net-angular::DesignIntelligenceEngineService.ValidateComplianceAsync implements local project behavior with return type Task<DataProcessResult<DesignIntelligenceEngineOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Tests\DesignIntelligenceEngineServiceTests.cs:121::xiigen-flows-dot-net-angular::DesignIntelligenceEngineServiceTests.TokensComplianceAndGovernanceAreTenantScopedAndIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Tests\DesignIntelligenceEngineServiceTests.cs:147::xiigen-flows-dot-net-angular::DesignIntelligenceEngineServiceTests.DashboardSummarizesDesignIntelligenceLifecycle.

## Location
flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Application\DesignIntelligenceEngineService.cs:125

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DesignIntelligenceEngineOutcome>> returned by xiigen-flows-dot-net-angular::DesignIntelligenceEngineService.ValidateComplianceAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Tests\DesignIntelligenceEngineServiceTests.cs:121::xiigen-flows-dot-net-angular::DesignIntelligenceEngineServiceTests.TokensComplianceAndGovernanceAreTenantScopedAndIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Tests\DesignIntelligenceEngineServiceTests.cs:147::xiigen-flows-dot-net-angular::DesignIntelligenceEngineServiceTests.DashboardSummarizesDesignIntelligenceLifecycle

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-31-design-intelligence-engine\server\Xiigen.Flow.DesignIntelligenceEngine.Application\DesignIntelligenceEngineService.cs:125::xiigen-flows-dot-net-angular::DesignIntelligenceEngineService.ValidateComplianceAsync