# xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync

## Meaning
xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync implements local project behavior with return type Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:33::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ValidateRulePack, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts. It is called by xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:98::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ConflictScanFailsClosedWithoutRulePack, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:111::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ConflictScanDetectsRequiredFieldRemovalAndPersistsBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:210::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.DashboardIsTenantIsolated, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:100::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ConflictScanFailsClosedWithoutRulePack, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:113::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ConflictScanDetectsRequiredFieldRemovalAndPersistsBeforeOutbox, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:212::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.DashboardIsTenantIsolated.

## Location
flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:119

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>> returned by xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:98::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ConflictScanFailsClosedWithoutRulePack
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:111::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ConflictScanDetectsRequiredFieldRemovalAndPersistsBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:210::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.DashboardIsTenantIsolated
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:100::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ConflictScanFailsClosedWithoutRulePack
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:113::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ConflictScanDetectsRequiredFieldRemovalAndPersistsBeforeOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:212::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.DashboardIsTenantIsolated

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:33::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ValidateRulePack
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:309::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.DetectConflicts

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:119::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ScanConflictsAsync