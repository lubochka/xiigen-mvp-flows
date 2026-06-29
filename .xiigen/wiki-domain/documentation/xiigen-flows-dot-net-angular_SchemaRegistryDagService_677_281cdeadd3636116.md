# xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateCycleFreeAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateCycleFreeAsync implements local project behavior with return type Task<SchemaRegistryDagOutcome?>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:859::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DetectCyclePath, xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:42::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RegisterSchemaAsync.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:677

## Parameters
- string schemaType
- IReadOnlyList<string> dependencies
- CancellationToken cancellationToken

## Return
Task<SchemaRegistryDagOutcome?> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateCycleFreeAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:42::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RegisterSchemaAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:859::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DetectCyclePath
- xiigen-flows-dot-net-angular::flows\FLOW-22-cms-publishing\server\Xiigen.Flow.CmsPublishing.Application\CmsPublishingService.cs:570::xiigen-flows-dot-net-angular::CmsPublishingService.ToDictionary

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:677::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateCycleFreeAsync