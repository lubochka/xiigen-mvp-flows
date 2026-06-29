# xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync

## Meaning
xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync implements local project behavior with return type Task<DataProcessResult<ClientPushOutcome>>. It delegates to xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString, xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject, xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired, xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant, xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync, xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:53::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:100::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.HasCorrelationOutsideCurrentTenant. It is called by xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:101::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventStoresOutboxBeforeTenantScopedPoolPush, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:121::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventDiscardsMissingConnectionWithoutRetryAndBlocksUserFacingEmitDomains, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:141::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventBlocksCrossTenantCorrelationDelivery.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117

## Parameters
- IReadOnlyDictionary<string
- object?> request
- string? idempotencyKey
- string? expectedVersion
- CancellationToken cancellationToken

## Return
Task<DataProcessResult<ClientPushOutcome>> returned by xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:101::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventStoresOutboxBeforeTenantScopedPoolPush
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:121::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventDiscardsMissingConnectionWithoutRetryAndBlocksUserFacingEmitDomains
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:141::xiigen-flows-dot-net-angular::ClientPushServiceTests.BridgeEventBlocksCrossTenantCorrelationDelivery

## Calls
- xiigen-flows-dot-net-angular::flows\FLOW-05-completion-gamification\server\Xiigen.Flow.CompletionGamification.Application\CompletionGamificationService.cs:732::xiigen-flows-dot-net-angular::CompletionGamificationService.ReadString
- xiigen-flows-dot-net-angular::flows\FLOW-11-schema-registry-dag\server\Xiigen.Flow.SchemaRegistryDag.Application\SchemaRegistryDagService.cs:981::xiigen-flows-dot-net-angular::SchemaRegistryDagService.ReadObject
- xiigen-flows-dot-net-angular::flows\FLOW-12-subscription-billing\server\Xiigen.Flow.SubscriptionBilling.Application\SubscriptionBillingService.cs:664::xiigen-flows-dot-net-angular::StatusCodes.ReadBool
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Application\MarketplacePaymentsService.cs:630::xiigen-flows-dot-net-angular::was.ValidateRequired
- xiigen-flows-dot-net-angular::flows\FLOW-16-marketplace-payments\server\Xiigen.Flow.MarketplacePayments.Tests\MarketplacePaymentsServiceTests.cs:294::xiigen-flows-dot-net-angular::MarketplacePaymentsServiceTests.Tenant
- xiigen-flows-dot-net-angular::flows\FLOW-24-ai-safety-moderation\server\Xiigen.Flow.AiSafetyModeration.Application\AiSafetyModerationService.cs:313::xiigen-flows-dot-net-angular::AiSafetyModerationService.ReserveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-32-sharable-flows-marketplace\server\Xiigen.Flow.SharableFlowsMarketplace.Application\SharableFlowsMarketplaceService.cs:251::xiigen-flows-dot-net-angular::SharableFlowsMarketplaceService.TimeoutAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:53::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.PushEvent
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:100::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.HasCorrelationOutsideCurrentTenant

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync