# xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync implements local project behavior with return type Task<DataProcessResult<SchemaRegistryDagOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray, xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:211::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF925_ValidationReturnsAllErrorsAndNoSchemaSideEffects.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:439

## Parameters
- IReadOnlyDictionary<string
- object?> request
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SchemaRegistryDagOutcome>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:211::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF925_ValidationReturnsAllErrorsAndNoSchemaSideEffects

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-02-profile-enrichment\server\Xiigen.Flow.ProfileEnrichment.Application\ProfileEnrichmentService.cs:548::xiigen-flows-dot-net-angular::ProfileEnrichmentService.ReadStringArray
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:439::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateDocumentAsync