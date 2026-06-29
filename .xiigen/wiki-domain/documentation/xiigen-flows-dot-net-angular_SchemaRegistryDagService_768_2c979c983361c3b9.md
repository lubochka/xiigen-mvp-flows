# xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest implements local project behavior with return type SchemaRegistryDagOutcome?. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:42::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RegisterSchemaAsync.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:768

## Parameters
- IReadOnlyDictionary<string
- object?> request

## Return
SchemaRegistryDagOutcome? returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:42::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RegisterSchemaAsync

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:843::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ParseVersion
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:768::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ValidateRegistrationRequest