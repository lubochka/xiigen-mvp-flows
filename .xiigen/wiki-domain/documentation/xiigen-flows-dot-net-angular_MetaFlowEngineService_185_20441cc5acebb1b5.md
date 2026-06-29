# xiigen-flows-dot-net-angular::MetaFlowEngineService.RunStressTestsAsync

## Meaning
xiigen-flows-dot-net-angular::MetaFlowEngineService.RunStressTestsAsync implements local project behavior with return type Task<DataProcessResult<MetaFlowEngineOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:377::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ReadStressResults. No internal caller was detected.

## Location
flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:185

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<MetaFlowEngineOutcome>> returned by xiigen-flows-dot-net-angular::MetaFlowEngineService.RunStressTestsAsync

## Called By
- none recorded

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-25-bfa-cross-flow-governance\server\Xiigen.Flow.BfaCrossFlowGovernance.Application\BfaCrossFlowGovernanceService.cs:377::xiigen-flows-dot-net-angular::BfaCrossFlowGovernanceService.ReadStressResults

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-26-meta-flow-engine\server\Xiigen.Flow.MetaFlowEngine.Application\MetaFlowEngineService.cs:185::xiigen-flows-dot-net-angular::MetaFlowEngineService.RunStressTestsAsync