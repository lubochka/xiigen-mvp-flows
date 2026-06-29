# xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.Schema

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.Schema implements local project behavior with return type object?>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:29::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_1_DagCycleDetectorUsesThreeStateDfsAndAllowsDiamond, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:49::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_2_PublishUsesOptimisticVersionPinAndRebuildsTopology, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:68::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_3_BreakingChangeRoutesToApprovalBeforePublication, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:113::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF919_ConcurrentRegistration_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:127::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF920_IdempotencyReplayReturnsCachedSchemaAndNoDuplicateSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:144::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF921_InvalidRegistrationReturnsFieldDetailsAndNoPartialWrite, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:160::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF922_FabricTimeoutReturns504AndRetainsRecoveryOutbox, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:176::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF923_ApprovalConcurrency_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:193::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF924_ApprovalReplayReturnsCachedDecisionWithoutDoublePublish, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:211::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF925_ValidationReturnsAllErrorsAndNoSchemaSideEffects, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:250::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF927_DeprecationConcurrency_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:265::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF928_RenderDagUsesCacheForRepeatedRequests, xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:35::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.SaveTemplateAsync.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:282

## Parameters
- string schemaType
- string version
- string changeType
- string[]? dependencies
- string[]? required
- string[]? removedFields
- bool simulateSlowWrite

## Return
object?> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.Schema

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:29::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_1_DagCycleDetectorUsesThreeStateDfsAndAllowsDiamond
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:49::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_2_PublishUsesOptimisticVersionPinAndRebuildsTopology
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:68::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_3_BreakingChangeRoutesToApprovalBeforePublication
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:113::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF919_ConcurrentRegistration_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:127::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF920_IdempotencyReplayReturnsCachedSchemaAndNoDuplicateSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:144::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF921_InvalidRegistrationReturnsFieldDetailsAndNoPartialWrite
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:160::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF922_FabricTimeoutReturns504AndRetainsRecoveryOutbox
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:176::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF923_ApprovalConcurrency_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:193::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF924_ApprovalReplayReturnsCachedDecisionWithoutDoublePublish
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:211::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF925_ValidationReturnsAllErrorsAndNoSchemaSideEffects
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:250::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF927_DeprecationConcurrency_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:265::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF928_RenderDagUsesCacheForRepeatedRequests
- xiigen-flows-dot-net-angular::flows\FLOW-23-form-builder-templates\server\Xiigen.Flow.FormBuilderTemplates.Application\FormBuilderTemplatesService.cs:35::xiigen-flows-dot-net-angular::FormBuilderTemplatesService.SaveTemplateAsync

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:282::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.Schema