# xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync

## Meaning
xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync implements local project behavior with return type Task<DataProcessResult<MetaFlowEngineOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:33::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ValidateRulePack, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts. No internal caller was detected.

## Location
flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:119

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MetaFlowEngineOutcome>> returned by xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:33::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ValidateRulePack
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:119::xiigen-flows-dot-net-angular::MetaFlowEngineService.ScanConflictsAsync