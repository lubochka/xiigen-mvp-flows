# xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections

## Meaning
xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections implements local project behavior with return type DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>>. It has no detected internal callee. It is called by xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:83::xiigen-flows-dot-net-angular::ClientPushServiceTests.RegisterConnectionIsIdempotentVersionAwareAndTenantScoped, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:157::xiigen-flows-dot-net-angular::ClientPushServiceTests.KeepaliveReadsConfigCleansDroppedConnectionsBeforeEmitAndIsIdempotent, xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:178::xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool.

## Location
flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86

## Parameters
- none recorded

## Return
DataProcessResult<IReadOnlyList<ClientPushConnectionInfo>> returned by xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections

## Called By
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:117::xiigen-flows-dot-net-angular::ClientPushService.BridgeEventAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Application\ClientPushService.cs:180::xiigen-flows-dot-net-angular::ClientPushService.RunKeepaliveAsync
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:83::xiigen-flows-dot-net-angular::ClientPushServiceTests.RegisterConnectionIsIdempotentVersionAwareAndTenantScoped
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:157::xiigen-flows-dot-net-angular::ClientPushServiceTests.KeepaliveReadsConfigCleansDroppedConnectionsBeforeEmitAndIsIdempotent
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Tests\ClientPushServiceTests.cs:178::xiigen-flows-dot-net-angular::ClientPushServiceTests.TimeoutRetainsOutboxWithoutPublishingToPool

## Calls
- none recorded

## Stack And Version
Target framework: unknown
C# language version: CSharp5_or_CSharp6_review_required

## Evidence
- xiigen-flows-dot-net-angular::flows\FLOW-40-client-push\server\Xiigen.Flow.ClientPush.Infrastructure\ClientPushServiceCollectionExtensions.cs:86::xiigen-flows-dot-net-angular::InMemoryClientPushConnectionPool.GetActiveConnections