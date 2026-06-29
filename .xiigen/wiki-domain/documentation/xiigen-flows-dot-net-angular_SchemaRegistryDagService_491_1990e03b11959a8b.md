# xiigen-flows-dot-net-angular::SchemaRegistryDagService.RunMigrationAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.RunMigrationAsync implements local project behavior with return type Task<DataProcessResult<SchemaRegistryDagOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:231::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF926_MigrationTimeoutRecordsRunBeforeRetryEvent.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:491

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SchemaRegistryDagOutcome>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.RunMigrationAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:231::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF926_MigrationTimeoutRecordsRunBeforeRetryEvent

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:491::xiigen-flows-dot-net-angular::SchemaRegistryDagService.RunMigrationAsync