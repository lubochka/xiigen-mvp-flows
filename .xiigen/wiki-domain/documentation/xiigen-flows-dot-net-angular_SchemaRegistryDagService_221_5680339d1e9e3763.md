# xiigen-flows-dot-net-angular::SchemaRegistryDagService.DecideApprovalAsync

## Meaning
xiigen-flows-dot-net-angular::SchemaRegistryDagService.DecideApprovalAsync implements local project behavior with return type Task<DataProcessResult<SchemaRegistryDagOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:563::xiigen-flows-dot-net-angular::SchemaRegistryDagService.PublishSchemaBodyAsync, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync. It is called by xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:68::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_3_BreakingChangeRoutesToApprovalBeforePublication, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:176::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF923_ApprovalConcurrency_AllowsOneWinnerAndOneLoser, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:193::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF924_ApprovalReplayReturnsCachedDecisionWithoutDoublePublish.

## Location
flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:221

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<SchemaRegistryDagOutcome>> returned by xiigen-flows-dot-net-angular::SchemaRegistryDagService.DecideApprovalAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:68::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF11_3_BreakingChangeRoutesToApprovalBeforePublication
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:176::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF923_ApprovalConcurrency_AllowsOneWinnerAndOneLoser
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Tests\SchemaRegistryDagServiceTests.cs:193::xiigen-flows-dot-net-angular::SchemaRegistryDagServiceTests.CF924_ApprovalReplayReturnsCachedDecisionWithoutDoublePublish

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:563::xiigen-flows-dot-net-angular::SchemaRegistryDagService.PublishSchemaBodyAsync
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:221::xiigen-flows-dot-net-angular::SchemaRegistryDagService.DecideApprovalAsync