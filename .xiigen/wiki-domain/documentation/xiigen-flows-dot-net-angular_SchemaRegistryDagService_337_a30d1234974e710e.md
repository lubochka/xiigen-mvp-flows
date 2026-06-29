# xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync implements local project behavior with return type Task<DataProcessResult<DagRenderSnapshot>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594::xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:900::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderMermaid, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:919::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderJsonLike, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:265::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF928_RenderDagUsesCacheForRepeatedRequests.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:337

## Parameters
- string format
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<DagRenderSnapshot>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:265::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF928_RenderDagUsesCacheForRepeatedRequests

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594::xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:900::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderMermaid
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:919::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderJsonLike
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:681::xiigen-flows-dot-net-angular::StatusCodes.ReadInt

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:337::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync