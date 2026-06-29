# xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync implements local project behavior with return type Task<DataProcessResult<SchemaRegistryDagOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594::xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:787::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ResolveIntAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:250::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF927_DeprecationConcurrency_AllowsOneWinnerAndOneLoser.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:375

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? ifMatch
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SchemaRegistryDagOutcome>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:250::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF927_DeprecationConcurrency_AllowsOneWinnerAndOneLoser

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:594::xiigen-flows-dot-net-angular::SchemaRegistryDagService.BuildTopologyAsync
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:787::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ResolveIntAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:375::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DeprecateSchemaAsync