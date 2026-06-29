# xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.RunStressTestsAsync

## Meaning
xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.RunStressTestsAsync implements local project behavior with return type Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:377::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ReadStressResults. It is called by xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:146::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix, xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:148::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix, xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved, xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Tests\HumanInteractionGateServiceTests.cs:200::xiigen-flows-dot-net-angular::HumanInteractionGateServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix, xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:199::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix, xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:236::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix.

## Location
flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:185

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<BfaCrossFlowGovernanceOutcome>> returned by xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.RunStressTestsAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:146::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Tests\BfaCrossFlowGovernanceServiceTests.cs:173::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:148::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Tests\MetaFlowEngineServiceTests.cs:175::xiigen-flows-dot-net-angular::MetaFlowEngineServiceTests.ReleaseGateApprovesOnlyAfterEvidenceAndEmitsConflictResolved
- xiigen-flows-dot-net-angular::flows\FLOW-27-human-interaction-gate\server\Xiigen.Flow.HumanInteractionGate.Tests\HumanInteractionGateServiceTests.cs:200::xiigen-flows-dot-net-angular::HumanInteractionGateServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix
- xiigen-flows-dot-net-angular::flows\FLOW-28-blog-cms-modules\server\Xiigen.Flow.BlogCmsModules.Tests\BlogCmsModulesServiceTests.cs:199::xiigen-flows-dot-net-angular::BlogCmsModulesServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix
- xiigen-flows-dot-net-angular::flows\FLOW-29-adaptive-rag-deep-research\server\Xiigen.Flow.AdaptiveRagDeepResearch.Tests\AdaptiveRagDeepResearchServiceTests.cs:236::xiigen-flows-dot-net-angular::AdaptiveRagDeepResearchServiceTests.StressRunBlocksMissingTestsAndAcceptsFullMatrix

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:377::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ReadStressResults

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:185::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.RunStressTestsAsync