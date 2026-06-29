# xiigen-flows-dot-net-angular::SchemaRegistryDagService.SearchSchemasAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.SearchSchemasAsync implements local project behavior with return type Task<DataProcessResult<SchemaCollection>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:304

## Parameters
- IReadOnlyDictionary<string
- object?> query
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SchemaCollection>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.SearchSchemasAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:94::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_4_SearchAndRenderDoNotLeakAcrossTenants

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:304::xiigen-flows-dot-net-angular::SchemaRegistryDagService.SearchSchemasAsync