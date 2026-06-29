# xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync implements local project behavior with return type Task<DataProcessResult<TopologySnapshot>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:919::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderJsonLike. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:337::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:375::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:563::xiigen-flows-dot-net-angular::SchemaRegistryDagService.PublishSchemaBodyAsync.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594

## Parameters
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<TopologySnapshot>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:337::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderDagAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:375::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:563::xiigen-flows-dot-net-angular::SchemaRegistryDagService.PublishSchemaBodyAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:919::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RenderJsonLike

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594::xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync