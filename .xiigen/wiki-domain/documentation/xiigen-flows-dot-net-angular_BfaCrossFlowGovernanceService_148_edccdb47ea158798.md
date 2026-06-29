# xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.AnalyzeImpactAsync

## Meaning
xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.AnalyzeImpactAsync implements local project behavior with return type Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired. It is called by xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:130::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ImpactAnalysisRejectsUnknownFlowIdsAndAcceptsCycleSafeTraversal, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:132::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ImpactAnalysisRejectsUnknownFlowIdsAndAcceptsCycleSafeTraversal, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved.

## Location
flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:148

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>> returned by xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.AnalyzeImpactAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:130::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ImpactAnalysisRejectsUnknownFlowIdsAndAcceptsCycleSafeTraversal
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:132::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ImpactAnalysisRejectsUnknownFlowIdsAndAcceptsCycleSafeTraversal
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:148::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.AnalyzeImpactAsync