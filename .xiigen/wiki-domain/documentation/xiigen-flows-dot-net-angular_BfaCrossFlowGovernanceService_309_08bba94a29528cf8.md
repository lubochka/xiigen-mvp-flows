# xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts

## Meaning
xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts implements local project behavior with return type object?>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:119::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:119::xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync.

## Location
flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
object?>> returned by xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:119::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:119::xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts